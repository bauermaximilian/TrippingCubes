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
    class PathFollowingBehavior<ParamT> : Behavior<ParamT>
    {
        public TimeSpan PredictTime { get; set; } = TimeSpan.Zero;

        public PathLinear Path { get; set; }

        public float PathMovingOffset { get; set; } = 0.420f;

        private float pathOffset = 0;

        public PathFollowingBehavior(IEntity self) : base(self)
        {
            MaximumAccelerationLinear *= 0.69f;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (Path != null)
            {
                Vector3 predictedPosition = Self.Body.Position +
                        Self.Body.Velocity * (float)PredictTime.TotalSeconds;
                pathOffset = Path.GetOffset(predictedPosition, pathOffset) 
                    + PathMovingOffset;
                Vector3 target = Path.GetPosition(pathOffset);

                return Vector3.Normalize((target - Self.Body.Position)
                    * ClearAxisY) * MaximumAccelerationLinear;
            }
            else return base.CalculateAccelerationLinear();
        }
    }
}
