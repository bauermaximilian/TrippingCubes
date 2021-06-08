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
using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class WanderBehavior<ParamT> : AlignBehavior<ParamT>
    {
        private readonly Random random = new Random();
        private Angle wanderOrientation;

        public float WanderOffset { get; set; }

        public float WanderRadius { get; set; }

        public Angle WanderRate { get; set; }        

        public WanderBehavior(IEntity self) : base(self)
        {
            WanderOffset = 4.2f;
            WanderRadius = 1f;
            WanderRate = Angle.Deg(6);
            MaximumAccelerationLinear *= 0.64f;
        }

        protected override Vector3? CalculateAlignDirection()
        {
            wanderOrientation += WanderRate *
                (float)(random.NextDouble() - random.NextDouble());

            Angle targetOrientation = wanderOrientation + 
                Self.Body.Orientation;

            Vector3 targetPosition = Self.Body.Position +
                WanderOffset * CreateOrientationVector(Self.Body.Orientation) +
                WanderRadius * CreateOrientationVector(targetOrientation);

            return targetPosition - Self.Body.Position;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            return CreateOrientationVector(Self.Body.Orientation) *
                MaximumAccelerationLinear;
        }

        private static Vector3 CreateOrientationVector(Angle orientation)
        {
            return MathHelper.RotateDirection(Vector3.UnitZ,
                Quaternion.CreateFromAxisAngle(Vector3.UnitY,
                orientation));
        }
    }
}