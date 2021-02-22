using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities.Behaviors
{
    class ObstacleAvoidanceBehavior<ParamT> : Behavior<ParamT>
    {
        public float LookAheadDistance { get; set; } = 3;

        public float AvoidDistance { get; set; } = 1;

        public Vector3 RaycastSize { get; set; } = new Vector3(0.4f);

        public ObstacleAvoidanceBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            Vector3 rayVector = Vector3.Normalize(Self.Body.Velocity) *
                    LookAheadDistance;
            BoundingBox raycastBoundingBox = new BoundingBox(
                Self.Body.Position, RaycastSize);

            if (Self.World.Physics.RaycastVolumetric(raycastBoundingBox,
                rayVector, out float collisionDistance,
                out Vector3 collisionNormal))
            {
                Vector3 collisionPosition =
                    (Vector3.Normalize(Self.Body.Velocity)
                    * collisionDistance) + Self.Body.Position;
                Vector3 target = collisionPosition + collisionNormal *
                    AvoidDistance;
                Vector3 relativePos = target - Self.Body.Position;
                return Vector3.Normalize(relativePos) *
                    MaximumAccelerationLinear;
            }
            else return base.CalculateAccelerationLinear();
        }
    }
}
