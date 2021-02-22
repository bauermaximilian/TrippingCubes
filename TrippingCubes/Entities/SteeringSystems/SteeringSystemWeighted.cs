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
