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

using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using System;
using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes
{
    class FlyCamController : PlayerControllerBase
    {
        public const float LookAccerlationDeg = 42.0f;
        public const float LookDragDeg = 6.9f;

        public const float MoveSpeed = 0.69f;
        public const float MoveDrag = 4.20f;

        public ControlMapping MoveUp { get; set; }

        public ControlMapping MoveDown { get; set; }        

        public Vector3 velocity;
        private Vector2 rotationDeg;

        protected override Vector3 MovementUserInput => base.MovementUserInput
            + new Vector3(0, (MoveUp?.Value ?? 0) - (MoveDown?.Value ?? 0), 0);

        public FlyCamController(Camera camera) : base(camera)
        {
        }

        public override void Update(TimeSpan delta)
        {
            PhysicsHelper.ApplyAccerlationToVelocity(ref rotationDeg,
                RotationUserInput * LookAccerlationDeg, delta);
            PhysicsHelper.ApplyDragToVelocity(ref rotationDeg,
                LookDragDeg, delta);

            Camera.Rotate(Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y));

            Vector3 userInput = Camera.AlignVector(MovementUserInput,
                true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            PhysicsHelper.ApplyAccerlationToVelocity(ref velocity,
                userInput * MoveSpeed, delta);
            PhysicsHelper.ApplyDragToVelocity(ref velocity, MoveDrag, delta);

            Camera.Move(velocity);
        }
    }
}
