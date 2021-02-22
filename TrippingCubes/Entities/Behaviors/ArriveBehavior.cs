using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class ArriveBehavior<ParamT> : SeekBehavior<ParamT>
    {
        public float DecelerateRadius { get; set; }

        public float ArrivalRadius { get; set; }

        public TimeSpan TimeToTargetSpeed { get; set; } =
            TimeSpan.FromSeconds(0.25);

        public ArriveBehavior(IEntity self) : base(self)
        {
            DecelerateRadius = 3.25f;
            ArrivalRadius = 1.25f;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            Vector3 direction = (TargetPosition - Self.Body.Position);
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