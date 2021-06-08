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
using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities.Behaviors
{
    class ObstacleAvoidanceBehavior<ParamT> : AlignBehavior<ParamT>
    {
        public float LookAheadDistance { get; set; } = 1;

        public float AvoidDistance { get; set; } = 1;

        public ObstacleAvoidanceBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            Vector3 lookAtVector;

            if (Self.Body.Velocity.Length() > 0.5)
                lookAtVector = Vector3.Normalize(Self.Body.Velocity);
            else lookAtVector = MathHelper.RotateDirection(Vector3.UnitZ,
                Vector3.UnitY, Self.Body.Orientation);

            Vector3 rayVector = lookAtVector * LookAheadDistance;

            BoundingBox raycastBoundingBox = new BoundingBox(
                Self.Body.BoundingBox.Position + new Vector3(0.0f, 1, 0.0f),
                Self.Body.BoundingBox.Dimensions - new Vector3(0.0f, 1, 0.0f));

            if (Self.World.Physics.RaycastVolumetric(raycastBoundingBox,
                rayVector, out float collisionDistance,
                out Vector3 collisionNormal))
            {
                Vector3 collisionPosition = lookAtVector * collisionDistance + 
                    Self.Body.Position;
                Vector3 target = collisionPosition + collisionNormal *
                    AvoidDistance;
                Vector3 direction = (target - Self.Body.Position) * ClearAxisY;
                return Vector3.Normalize(direction) *
                    MaximumAccelerationLinear;
            }
            else return base.CalculateAccelerationLinear();
        }

        protected override Vector3? CalculateAlignDirection()
        {
            return AccelerationLinear;
        }
    }
}
