using ShamanTK.Common;
using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    abstract class AlignBehavior<ParamT> : Behavior<ParamT>
    {
        public float DecelerateRadius { get; set; } = Angle.Deg(10);

        public float ArrivalRadius { get; set; } = Angle.Deg(3);

        public TimeSpan TimeToTargetSpeed { get; set; } =
            TimeSpan.FromSeconds(0.1);

        public AlignBehavior(IEntity self) : base(self)
        {
            MaximumAccelerationAngular = Angle.Deg(225);
        }

        protected override Angle CalculateAccelerationAngular()
        {
            Vector3? direction = CalculateAlignDirection();
            if (!direction.HasValue) return Angle.Zero;

            Angle targetOrientation = direction.Value.Length() > 0 ?
                (Angle)Math.Atan2(direction.Value.X, direction.Value.Z) : 
                Self.Body.Orientation;

            Angle rotation = (targetOrientation - Self.Body.Orientation +
                Angle.PiRad(1)).ToNormalized() - Angle.PiRad(1);

            Angle distance = Math.Abs(rotation);

            if (distance > ArrivalRadius)
            {
                Angle targetRotation = MaximumAccelerationAngular;
                if (distance < DecelerateRadius)
                    distance = MaximumAccelerationAngular *
                        distance / DecelerateRadius;

                targetRotation *= (float)(rotation / distance);

                return (targetRotation - Self.Body.Rotation) /
                    (float)TimeToTargetSpeed.TotalSeconds;
            }
            else return 0;
        }

        protected abstract Vector3? CalculateAlignDirection();
    }
}