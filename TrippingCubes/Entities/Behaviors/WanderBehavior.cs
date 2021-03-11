using ShamanTK.Common;
using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class WanderBehavior<ParamT> : AlignBehavior<ParamT>
    {
        private readonly Random random = new Random();
        private Angle wanderOrientation;

        public float WanderOffset { get; set; }

        public float WanderRadius { get; set; }

        public Angle WanderRate { get; set; }        

        public WanderBehavior(IEntity self) : base(self)
        {
            WanderOffset = 4.2f;
            WanderRadius = 1f;
            WanderRate = Angle.Deg(6);
            MaximumAccelerationLinear *= 0.64f;
        }

        protected override Vector3? CalculateAlignDirection()
        {
            wanderOrientation += WanderRate *
                (float)(random.NextDouble() - random.NextDouble());

            Angle targetOrientation = wanderOrientation + 
                Self.Body.Orientation;

            Vector3 targetPosition = Self.Body.Position +
                WanderOffset * CreateOrientationVector(Self.Body.Orientation) +
                WanderRadius * CreateOrientationVector(targetOrientation);

            return targetPosition - Self.Body.Position;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            return CreateOrientationVector(Self.Body.Orientation) *
                MaximumAccelerationLinear;
        }

        private static Vector3 CreateOrientationVector(Angle orientation)
        {
            return MathHelper.RotateDirection(Vector3.UnitZ,
                Quaternion.CreateFromAxisAngle(Vector3.UnitY,
                orientation));
        }
    }
}