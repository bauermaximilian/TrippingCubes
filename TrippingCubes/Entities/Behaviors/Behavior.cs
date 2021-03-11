using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    abstract class Behavior<ParamT>
    {
        public const float DefaultMaximumAccelerationLinear = 3.75f;

        public const float DefaultMaximumAccelerationAngular = 3.57f;

        protected static Vector3 ClearAxisY { get; } = new Vector3(1, 0, 1);

        public ParamT Parameters { get; set; }

        public IEntity Self { get; }

        public Vector3 AccelerationLinear { get; private set; }

        public float MaximumAccelerationLinear
        {
            get => maximumAccelerationLinear;
            set => maximumAccelerationLinear = Math.Max(0, value);
        }
        private float maximumAccelerationLinear = 
            DefaultMaximumAccelerationLinear;

        public Angle AccelerationAngular { get; private set; }

        public float MaximumAccelerationAngular
        {
            get => maximumAccelerationAngular;
            set => maximumAccelerationAngular = Math.Max(0, value);
        }
        private float maximumAccelerationAngular = 
            DefaultMaximumAccelerationAngular;

        protected Behavior(IEntity self)
        {
            Self = self;
        }

        public virtual void Update()
        {
            AccelerationLinear = CalculateAccelerationLinear();            
            if (AccelerationLinear.Length() > MaximumAccelerationLinear)
                AccelerationLinear = Vector3.Normalize(AccelerationLinear) *
                    MaximumAccelerationLinear;

            AccelerationAngular = CalculateAccelerationAngular();
            Angle accelerationAngularAbsolute = Math.Abs(AccelerationAngular);
            if (AccelerationAngular > MaximumAccelerationAngular)
            {
                AccelerationAngular /= accelerationAngularAbsolute;
                AccelerationAngular *= MaximumAccelerationAngular;
            }
        }

        protected virtual Vector3 CalculateAccelerationLinear()
            => Vector3.Zero;

        protected virtual Angle CalculateAccelerationAngular()
            => Angle.Zero;

        protected IEnumerable<IEntity> GetNeighborhood(float radius, 
            Angle viewAngle)
        {
            foreach (IEntity body in Self.World.Entities ??
                Enumerable.Empty<IEntity>())
            {
                Vector3 relativePosition = body.Body.Position -
                    Self.Body.Position;
                if (body != Self && body is ICharacter &&
                    !(body is IPlayerCharacter) &&
                    relativePosition.Length() <= radius)
                {
                    var orientationOffset =
                        MathHelper.CalculateOrientationDifference(
                            MathHelper.CreateOrientationY(relativePosition),
                            Self.Body.Orientation, true);

                    if (orientationOffset < (viewAngle / 2f))
                    {
                        yield return body;
                    }
                }
            }
        }

        public override string ToString()
        {
            return GetType().Name;
        }
    }
}