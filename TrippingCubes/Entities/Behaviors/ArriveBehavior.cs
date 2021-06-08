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

using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class ArriveBehavior<ParamT> : SeekBehavior<ParamT>
    {
        public float DecelerateRadius { get; set; } = 3.25f;

        public float ArrivalRadius { get; set; } = 1.25f;

        public TimeSpan TimeToTargetSpeed { get; set; } =
            TimeSpan.FromSeconds(0.25);

        public ArriveBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (!TargetPosition.HasValue) return Vector3.Zero;

            Vector3 direction = (TargetPosition.Value - Self.Body.Position);
            float distance = direction.Length();

            float targetSpeed = 0;

            if (distance > ArrivalRadius)
            {
                if (distance > DecelerateRadius)
                    targetSpeed = MaximumAccelerationLinear;
                else
                    targetSpeed = MaximumAccelerationLinear * 
                        (distance / DecelerateRadius);
            }

            Vector3 targetVelocity = Vector3.Normalize(direction) * 
                targetSpeed;

            return (targetVelocity - Self.Body.Velocity * ClearAxisY) / 
                (float)TimeToTargetSpeed.TotalSeconds;
        }
    }
}