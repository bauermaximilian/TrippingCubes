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
using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class FlockSeparationBehavior<ParamT> : Behavior<ParamT>
    {
        public float NeighborhoodRadius { get; set; } = 2;

        public Angle NeighborhoodAngle { get; set; } = Angle.Deg(270);

        public bool UseBalancedSeparationInfluences { get; set; } = false;

        public FlockSeparationBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            var neighborhood = new List<IEntity>(GetNeighborhood(
                NeighborhoodRadius, NeighborhoodAngle));

            Vector3 separationAcceleration = Vector3.Zero;

            foreach (var entity in neighborhood)
            {
                Vector3 relativePosition = Self.Body.Position - 
                    entity.Body.Position;

                float scalingFactor;
                if (UseBalancedSeparationInfluences)
                    scalingFactor = 1f / neighborhood.Count;
                else scalingFactor = 1 / relativePosition.Length();

                Vector3 individualSeparationAcceleration =
                    Vector3.Normalize(relativePosition) * scalingFactor;

                separationAcceleration += individualSeparationAcceleration;
            }

            if (separationAcceleration.Length() > 0)
                separationAcceleration = 
                    Vector3.Normalize(separationAcceleration)
                    * MaximumAccelerationLinear;

            return separationAcceleration;
        }
    }
}
