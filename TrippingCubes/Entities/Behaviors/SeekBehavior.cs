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

using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class SeekBehavior<ParamT> : Behavior<ParamT>
    {
        public Vector3? TargetPosition { get; set; }

        public SeekBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (TargetPosition.HasValue)
            {
                Vector3 direction = (TargetPosition.Value - Self.Body.Position)
                    * ClearAxisY;
                return Vector3.Normalize(direction) *
                    MaximumAccelerationLinear;
            }
            else return Vector3.Zero;
        }
    }
}