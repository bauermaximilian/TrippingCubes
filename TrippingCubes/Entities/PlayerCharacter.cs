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

using TrippingCubes.Physics;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using System;
using System.Numerics;
using TrippingCubes.Common;
using System.Collections.Generic;
using ShamanTK.IO;
using ShamanTK;

namespace TrippingCubes.Entities
{
    class PlayerCharacter : ICharacter
    {
        public const float LookAccerlationDeg = 42.0f;
        public const float LookDragDeg = 6.9f;

        public const float AccerlationFloor = 5f;
        public const float AccerlationAir = 4;
        public const float AccerlationFly = 24;
        public const float JumpVelocity = 7;

        private Vector2 rotationDeg;
        private Vector3 headShift = Vector3.Zero;
        private DateTime lastAttack = DateTime.MinValue;
        private readonly TimeSpan attackFrequency = TimeSpan.FromSeconds(0.8);

        private readonly ControlMapping moveLeft;
        private readonly ControlMapping moveRight;
        private readonly ControlMapping moveForward;
        private readonly ControlMapping moveBackward;
        private readonly ControlMapping lookUp;
        private readonly ControlMapping lookDown;
        private readonly ControlMapping lookLeft;
        private readonly ControlMapping lookRight;
        private readonly ControlMapping attack;
        private readonly ControlMapping jump;
        private readonly ControlMapping moveUp;
        private readonly ControlMapping moveDown;
        private readonly ControlMapping switchInputScheme;

        protected virtual Vector2 RotationUserInput => new Vector2(
            (lookDown?.Value ?? 0) - (lookUp?.Value ?? 0),
            (lookRight?.Value ?? 0) - (lookLeft?.Value ?? 0));

        protected virtual Vector3 MovementUserInput => new Vector3(
            (moveRight?.Value ?? 0) - (moveLeft?.Value ?? 0), 0,
            (moveForward?.Value ?? 0) - (moveBackward?.Value ?? 0));

        public GameWorld World { get; }

        public Camera Camera { get; }

        public ResourcePath WeaponModelPath
        {
            get => weaponPath;
            set
            {
                if (value != weaponPath)
                {
                    World.ModelCache.LoadModel(value, (success, model, exc) =>
                    {
                        if (success) WeaponModel = model;
                        else Log.Warning("A weapon model with the path " +
                                $"'{value}' couldn't be loaded.", exc);
                    });
                    weaponPath = value;
                }
            }
        }
        private ResourcePath weaponPath = ResourcePath.Empty;

        public Model WeaponModel { get; private set; }        

        public RigidBody Body { get; }

        public bool FlyModeEnabled
        {
            get => Body.GravityMultiplier == 0;
            set
            {
                if (value) Body.GravityMultiplier = 0;
                else Body.GravityMultiplier = 1;
            }
        }

        public int HealthPoints { get; set; } = 100;

        public bool IsInvisible => FlyModeEnabled;

        public Vector3 Position
        {
            get => Body.Position;
            set => Body.MoveTo(value);
        }

        public PlayerCharacter(GameWorld gameWorld)
        {
            World = gameWorld;
            Camera = gameWorld.Camera;

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 0.6f, 1.6f, 0.6f));
            Body.GravityMultiplier = 0;
            Body.EnableAutoJump = false;

            Body.AutoJump += (s,e)=> 
                Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));

            moveLeft = gameWorld.Game.InputScheme.MoveLeft;
            moveRight = gameWorld.Game.InputScheme.MoveRight;
            moveForward = gameWorld.Game.InputScheme.MoveForward;
            moveBackward = gameWorld.Game.InputScheme.MoveBackward;
            lookUp = gameWorld.Game.InputScheme.LookUp;
            lookDown = gameWorld.Game.InputScheme.LookDown;
            lookLeft = gameWorld.Game.InputScheme.LookLeft;
            lookRight = gameWorld.Game.InputScheme.LookRight;
            attack = gameWorld.Game.InputScheme.Attack;
            jump = gameWorld.Game.InputScheme.Jump;
            moveUp = gameWorld.Game.InputScheme.MoveUp;
            moveDown = gameWorld.Game.InputScheme.MoveDown;
            switchInputScheme = gameWorld.Game.InputScheme.SwitchInputScheme;
        }

        public static ICharacter Create(GameWorld gameWorld)
        {
            return new PlayerCharacter(gameWorld);
        }

        public void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            PrimitiveTypeParser.TryAssign(parameters, this);
        }

        public void Update(TimeSpan delta)
        {
            PhysicsHelper.ApplyAccerlationToVelocity(ref rotationDeg,
                RotationUserInput * LookAccerlationDeg, delta);
            PhysicsHelper.ApplyDragToVelocity(ref rotationDeg,
                LookDragDeg, delta);
            Camera.Rotate(Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y));

            if (switchInputScheme.IsActivated) 
                FlyModeEnabled = !FlyModeEnabled;

            if (FlyModeEnabled)
            {
                headShift = Vector3.Zero;
                UpdateWithoutGravity();
            }
            else
            {
                headShift = Math.Min(1, 
                    Body.Velocity.Length() / 3) * new Vector3(0,
                        MathHelper.CalculateTimeSine(1.85, 0.02), 0);
                UpdateWithGravity();
            }

            Camera.MoveTo(Body.BoundingBox.PivotBottom() +
                new Vector3(0, 1.45f, 0) + headShift);

            if (WeaponModel != null && !FlyModeEnabled)
            {
                WeaponModel.Transformation =
                    MathHelper.CreateTransformation(
                        Body.BoundingBox.PivotBottom(), Vector3.One, 
                        Quaternion.CreateFromYawPitchRoll(
                            Angle.Rad(Camera.Orientation.Y), 0, 0));
                WeaponModel.Update(delta);
                WeaponModel.Animation.Animation.SetPlaybackRange(
                    "attack", true);

                if ((attack?.IsActivated ?? false) && 
                    (DateTime.Now - lastAttack) > attackFrequency)
                {
                    lastAttack = DateTime.Now;

                    WeaponModel.Animation.Animation.Stop();
                    WeaponModel.Animation.Animation.Play();

                    foreach (var entity in World.Entities)
                    {
                        if (entity is ICharacter character)
                        {
                            Vector3 direction = Body.Position -
                                character.Body.Position;
                            Angle directionAngle =
                                MathHelper.CreateOrientationY(direction);
                            Angle offset =
                                MathHelper.CalculateOrientationDifference(
                                    directionAngle,
                                    Angle.Rad(Camera.Orientation.Y - Angle.Pi,
                                    true), true);

                            if (direction.Length() < 2 && offset.Degrees < 15)
                            {
                                character.HealthPoints -= 20;
                            }
                        }
                    }
                }
            }
        }

        public void Redraw(IRenderContext context)
        {
            if (!FlyModeEnabled) WeaponModel?.Draw(context);
        }

        private void UpdateWithGravity()
        {
            Vector3 userInput = Camera.AlignVector(MovementUserInput,
                true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            if (Body.Resting.Y == -1)
            {
                if (jump.IsActivated)
                    Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));
                else
                {
                    Body.ApplyAcceleration(userInput * AccerlationFloor);
                    Body.ApplyVelocityChange(userInput * 0.25f *
                        Math.Max(0, (1.75f - (Body.Velocity).Length())));
                }
            }
            else Body.ApplyForce(userInput * AccerlationAir);            
        }

        private void UpdateWithoutGravity()
        {
            Vector3 userInput = Camera.AlignVector(MovementUserInput +
                new Vector3(0, (moveUp?.Value ?? 0) -
                (moveDown?.Value ?? 0), 0), true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            Body.ApplyAcceleration(userInput * AccerlationFly);
        }
    }
}
