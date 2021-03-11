using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TrippingCubes.Entities.Behaviors;

namespace TrippingCubes.Entities.SteeringSystems
{
    public struct WeightedPriorityParameter
    {
        public float Weight { get; }

        public int Priority { get; }

        public WeightedPriorityParameter(float weight)
        {
            Weight = weight;
            Priority = 0;
        }

        public WeightedPriorityParameter(int priority, float weight)
        {
            Weight = weight;
            Priority = priority;
        }
    }

    internal class WeightedPriorityParameterComparer :
        IComparer<Behavior<WeightedPriorityParameter>>
    {
        public static WeightedPriorityParameterComparer Instance { get; }
            = new WeightedPriorityParameterComparer();

        public int Compare(Behavior<WeightedPriorityParameter> x,
            Behavior<WeightedPriorityParameter> y)
        {
            return y.Parameters.Priority.CompareTo(x.Parameters.Priority);
        }
    }

    class SteeringSystemWeightedPriorities : 
        SteeringSystem<WeightedPriorityParameter>
    {
        public float AccelerationLinearMinimumLength { get; set; } 
            = 0.1f;

        protected IEntity Self { get; }

        protected SteeringSystemWeightedPriorities(IEntity self)
        {
            Self = self;
        }

        protected override
            IEnumerable<Behavior<WeightedPriorityParameter>> GetBehaviors()
        {
            var behaviors = base.GetBehaviors().ToList();
            behaviors.Sort(WeightedPriorityParameterComparer.Instance);
            return behaviors;
        }

        protected override (Vector3 linear, Angle angular) GetAccelerations(
            IEnumerable<Behavior<WeightedPriorityParameter>> behaviors)
        {
            int currentPriority = int.MaxValue;
            float currentPriorityLengthMaximum = 0;
            Vector3 currentAccelerationLinear = Vector3.Zero;
            Angle currentAccelerationAngular = 0;

            foreach (var behavior in behaviors)
            {
                if (currentPriority == int.MaxValue)
                    currentPriority = behavior.Parameters.Priority;

                if (currentPriority != behavior.Parameters.Priority)
                {
                    if (currentAccelerationLinear.Length() >
                        AccelerationLinearMinimumLength &&
                        behavior.Parameters.Priority > 0)
                    {
                        continue;
                    }
                    else if (currentAccelerationLinear.Length() < 
                        AccelerationLinearMinimumLength)
                    {
                        currentPriority = behavior.Parameters.Priority;
                        currentAccelerationLinear = Vector3.Zero;
                        currentPriorityLengthMaximum = 0;
                    }
                }

                behavior.Update();

                currentAccelerationLinear +=
                    behavior.AccelerationLinear *
                    behavior.Parameters.Weight;

                currentPriorityLengthMaximum = Math.Max(
                    currentPriorityLengthMaximum,
                    behavior.AccelerationLinear.Length());

                if (Math.Abs(currentAccelerationAngular) < 0.01f)
                {
                    currentAccelerationAngular +=
                        behavior.AccelerationAngular *
                        behavior.Parameters.Weight;
                }
            }

            if (currentAccelerationLinear.Length() > 0)
            {
                currentAccelerationLinear =
                    Vector3.Normalize(currentAccelerationLinear) *
                    Math.Min(currentPriorityLengthMaximum,
                    MaximumAccelerationLinear);
            }

            Angle accelerationAngularAbsolute =
                Math.Abs(currentAccelerationAngular);
            if (currentAccelerationAngular > MaximumAccelerationAngular)
            {
                currentAccelerationAngular /= accelerationAngularAbsolute;
                currentAccelerationAngular *= MaximumAccelerationAngular;
            }

            if (float.IsNaN(currentAccelerationLinear.X) ||
                float.IsNaN(currentAccelerationLinear.Y) ||
                float.IsNaN(currentAccelerationLinear.Z))
            {
                currentAccelerationLinear = Vector3.Zero;
            }

            return (currentAccelerationLinear, currentAccelerationAngular);
        }
    }
}
