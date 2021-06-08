/*
 * TrippingCubes
 * A toolkit for creating games in a voxel-based environment.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.IO;
using System;
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Common;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities
{
    class Entity : IEntity
    {
        public RigidBody Body { get; }

        public GameWorld World { get; }

        public ResourcePath ModelPath
        {
            get => modelPath;
            set
            {
                if (value != modelPath)
                {
                    World.Game.Models.LoadModel(value, (success, model, exc) =>
                    {
                        if (success) Model = model;
                        else Log.Warning("An entity model with the path " +
                                $"'{value}' couldn't be loaded.", exc);
                    });
                    modelPath = value;
                }
            }
        }
        private ResourcePath modelPath = ResourcePath.Empty;

        public Model Model { get; private set; }

        public Vector3 ModelSize { get; set; } = new Vector3(1f);

        public Vector3 Position
        {
            get => Body.Position;
            set => Body.MoveTo(value);
        }

        public float OrientationDegrees
        {
            get => Body.Orientation.Degrees;
            set => Body.Orientation = Angle.Deg(value, true);
        }

        public string Animation
        {
            get => animation;
            set
            {
                animation = value;
                animationStarted = false;
            }
        }
        private string animation;
        private bool animationStarted = false;

        public bool IsSolid
        {
            get => !Body.DoesNotCollideWithOtherObjects;
            set => Body.DoesNotCollideWithOtherObjects = !value;
        }

        public string OnTouch { get; set; }

        public bool IsActive { get; set; } = true;

        public Entity(GameWorld gameWorld)
        {
            World = gameWorld;

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 1, 1, 1));//TODO: Implement
        }

        public void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            PrimitiveTypeParser.TryAssign(parameters, this, true);
        }

        public void Redraw(IRenderContext context)
        {
            if (Model == null || !IsActive) return;

            if (!animationStarted)
            {
                Model.SetAnimations(Animation, null, true);
                animationStarted = true;
            }
            context.Color = Color.Transparent;
            context.ColorBlending = BlendingMode.None;
            Model.Draw(context);
        }

        public void Update(TimeSpan delta)
        {
            if (Model == null || !IsActive) return;

            Model.Transformation = MathHelper.CreateTransformation(
                Body.Position, ModelSize);
            Model.Update(delta);
        }

        public void Touched(ICharacter source)
        {
            if (Model == null || !IsActive) return;

            if (OnTouch.Equals("Powerup",
                StringComparison.InvariantCultureIgnoreCase))
            {
                int previousHealthPoints = source.HealthPoints;
                source.HealthPoints += 20;
                IsActive = previousHealthPoints == source.HealthPoints;
            }
            else if (OnTouch.Equals("Win",
                StringComparison.InvariantCultureIgnoreCase))
            {
                World.Game.EndGame(true);
                IsActive = false;
            }
        }
    }
}
