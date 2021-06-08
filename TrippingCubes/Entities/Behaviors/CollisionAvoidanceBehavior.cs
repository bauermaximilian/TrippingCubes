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

using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class CollisionAvoidanceBehavior<ParamT> : Behavior<ParamT>
    {
        public IEnumerable<IEntity> Targets { get; set; }

        public float Radius { get; set; } = 1.5f;

        public CollisionAvoidanceBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (Targets != null)
            {
                Vector3? firstTargetPos = null;
                float firstMinSeparation = 0, firstDistance = 0;
                Vector3 firstRelativePos = Vector3.Zero, 
                    firstRelativeVel = Vector3.Zero;
                float shortestTime = float.PositiveInfinity;

                foreach (var target in Targets)
                {
                    Vector3 relativePos = 
                        target.Body.Position - Self.Body.Position;
                    Vector3 relativeVel = target.Body.Velocity - 
                        Self.Body.Velocity;
                    float relativeSpeed = relativeVel.Length();
                    float timeToCollision =
                        Vector3.Dot(relativePos, relativeVel) /
                        (relativeSpeed * relativeSpeed);

                    float distance = relativePos.Length();
                    float minSeparation = distance - 
                        relativeSpeed * shortestTime;
                    if (minSeparation > 2 * Radius) continue;

                    if (timeToCollision > 0 && timeToCollision < shortestTime)
                    {
                        shortestTime = timeToCollision;
                        firstTargetPos = target.Body.Position;
                        firstMinSeparation = minSeparation;
                        firstDistance = distance;
                        firstRelativePos = relativePos;
                        firstRelativeVel = relativeVel;
                    }
                }

                if (firstTargetPos != null)
                {
                    Vector3 relativePos;
                    if (firstMinSeparation <= 0 || firstDistance < 2 * Radius)
                        relativePos = firstTargetPos.Value - 
                            Self.Body.Position;
                    else relativePos = firstRelativePos + 
                            firstRelativeVel * shortestTime;
                    return Vector3.Normalize(relativePos) * 
                        MaximumAccelerationLinear;
                }
            }
            
            return base.CalculateAccelerationLinear();
        }
    }
}
