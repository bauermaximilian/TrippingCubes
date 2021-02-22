using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using TrippingCubes.Entities.Behaviors;

namespace TrippingCubes.Entities.SteeringSystems
{
    public struct WeightedPriorityParameter : 
        IComparable<WeightedPriorityParameter>
    {
        public float Weight { get; }

        public int Priority { get; }

        public WeightedPriorityParameter(int priority, float weight)
        {
            Weight = weight;
            Priority = priority;
        }

        public int CompareTo([AllowNull] WeightedPriorityParameter other)
        {
            return other.Priority.CompareTo(Priority);
        }
    }

    class SteeringSystemWeightedPriorities : 
        SteeringSystem<WeightedPriorityParameter>
    {
        public float AccelerationLinearMinimumLength { get; set; } 
            = 0.01f;

        public Angle AccelerationAngularMinimumAbsolute { get; set; } 
            = Angle.Deg(0.01f);

        protected override
            IEnumerable<Behavior<WeightedPriorityParameter>> GetBehaviors()
        {
            var behaviors = base.GetBehaviors().ToList();
            behaviors.Sort();
            return behaviors;
        }

        protected override (Vector3 linear, Angle angular) GetAccelerations(
            IEnumerable<Behavior<WeightedPriorityParameter>> behaviors)
        {
            int currentPriority = int.MaxValue;
            bool accelerationLinearCalculated = false,
                accelerationAngularCalculated = false;

            Vector3 currentAccelerationLinear = Vector3.Zero;
            Angle currentAccelerationAngular = 0;

            foreach (var behavior in behaviors)
            {
                behavior.Update();

                if (currentPriority == int.MaxValue)
                    currentPriority = behavior.Parameters.Priority;

                if (currentPriority != behavior.Parameters.Priority)
                {
                    if (currentAccelerationLinear.Length() >
                        AccelerationLinearMinimumLength)
                        accelerationLinearCalculated = true;

                    if (Math.Abs(currentAccelerationAngular) >
                        AccelerationAngularMinimumAbsolute)
                        accelerationLinearCalculated = true;

                    if (accelerationLinearCalculated &&
                        accelerationLinearCalculated) break;
                    else currentPriority = behavior.Parameters.Priority;
                }

                if (!accelerationLinearCalculated)
                    currentAccelerationLinear += 
                        behavior.AccelerationLinear *
                        behavior.Parameters.Weight;

                if (!accelerationAngularCalculated)
                    currentAccelerationAngular +=
                        behavior.AccelerationAngular *
                        behavior.Parameters.Weight;
            }

            if (currentAccelerationLinear.Length() > MaximumAccelerationLinear)
                currentAccelerationLinear =
                    Vector3.Normalize(currentAccelerationLinear) *
                    MaximumAccelerationLinear;

            Angle accelerationAngularAbsolute =
                Math.Abs(currentAccelerationAngular);
            if (currentAccelerationAngular > MaximumAccelerationAngular)
            {
                currentAccelerationAngular /= accelerationAngularAbsolute;
                currentAccelerationAngular *= MaximumAccelerationAngular;
            }

            return (currentAccelerationLinear, currentAccelerationAngular);
        }
    }
}
