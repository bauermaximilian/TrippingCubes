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