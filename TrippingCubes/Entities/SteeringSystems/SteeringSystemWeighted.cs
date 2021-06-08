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
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Entities.Behaviors;

namespace TrippingCubes.Entities.SteeringSystems
{
    public struct WeightParameter
    {
        public float Weight { get; }

        public WeightParameter(float weight) => Weight = weight;
    }

    class SteeringSystemWeighted : SteeringSystem<WeightParameter>
    {
        protected override (Vector3 linear, Angle angular) GetAccelerations(
            IEnumerable<Behavior<WeightParameter>> behaviors)
        {
            Vector3 newAccelerationLinear = Vector3.Zero;
            Angle newAccelerationAngular = 0;

            foreach (var behavior in behaviors)
            {
                behavior.Update();

                newAccelerationLinear +=
                    behavior.AccelerationLinear * behavior.Parameters.Weight;
                newAccelerationAngular +=
                    behavior.AccelerationAngular * behavior.Parameters.Weight;
            }

            if (newAccelerationLinear.Length() > MaximumAccelerationLinear)
                newAccelerationLinear =
                    Vector3.Normalize(newAccelerationLinear) *
                    MaximumAccelerationLinear;

            Angle accelerationAngularAbsolute =
                Math.Abs(newAccelerationAngular);
            if (newAccelerationAngular > MaximumAccelerationAngular)
            {
                newAccelerationAngular /= accelerationAngularAbsolute;
                newAccelerationAngular *= MaximumAccelerationAngular;
            }

            return (newAccelerationLinear, newAccelerationAngular);
        }
    }
}
