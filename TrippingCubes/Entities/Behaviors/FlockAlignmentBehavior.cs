﻿/*
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
    class FlockAlignmentBehavior<ParamT> : AlignVelocityBehavior<ParamT>
    {
        public float NeighborhoodRadius { get; set; } = 4;

        public Angle NeighborhoodAngle { get; set; } = Angle.Deg(270);

        public FlockAlignmentBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3? CalculateAlignDirection()
        {
            var neighborhood = new List<IEntity>(GetNeighborhood(
                NeighborhoodRadius, NeighborhoodAngle));

            if (neighborhood.Count == 0) return null;

            Vector3 neighborhoodVelocities = Vector3.Zero;

            foreach (var entity in neighborhood)
                neighborhoodVelocities += entity.Body.Velocity;

            neighborhoodVelocities /= neighborhood.Count;

            return neighborhoodVelocities;
        }
    }
}
