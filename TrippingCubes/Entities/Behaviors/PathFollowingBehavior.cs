using ShamanTK.Common;
using System;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class PathFollowingBehavior<ParamT> : Behavior<ParamT>
    {
        public TimeSpan PredictTime { get; set; } = TimeSpan.Zero;

        public PathLinear Path { get; set; }

        public float PathMovingOffset { get; set; } = 0.420f;

        private float pathOffset = 0;

        public PathFollowingBehavior(IEntity self) : base(self)
        {
            MaximumAccelerationLinear *= 0.69f;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (Path != null)
            {
                Vector3 predictedPosition = Self.Body.Position +
                        Self.Body.Velocity * (float)PredictTime.TotalSeconds;
                pathOffset = Path.GetOffset(predictedPosition, pathOffset) 
                    + PathMovingOffset;
                Vector3 target = Path.GetPosition(pathOffset);

                return Vector3.Normalize((target - Self.Body.Position)
                    * ClearAxisY) * MaximumAccelerationLinear;
            }
            else return base.CalculateAccelerationLinear();
        }
    }
}
