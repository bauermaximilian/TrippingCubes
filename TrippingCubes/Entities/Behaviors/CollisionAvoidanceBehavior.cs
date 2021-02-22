using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities.Behaviors
{
    class CollisionAvoidanceBehavior<ParamT> : Behavior<ParamT>
    {
        public IEnumerable<IEntity> Targets { get; set; }

        public float Radius { get; set; } = 1.5f;

        public CollisionAvoidanceBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (Targets != null)
            {
                Vector3? firstTargetPos = null;
                float firstMinSeparation = 0, firstDistance = 0;
                Vector3 firstRelativePos = Vector3.Zero, 
                    firstRelativeVel = Vector3.Zero;
                float shortestTime = float.PositiveInfinity;

                foreach (var target in Targets)
                {
                    Vector3 relativePos = 
                        target.Body.Position - Self.Body.Position;
                    Vector3 relativeVel = target.Body.Velocity - 
                        Self.Body.Velocity;
                    float relativeSpeed = relativeVel.Length();
                    float timeToCollision =
                        Vector3.Dot(relativePos, relativeVel) /
                        (relativeSpeed * relativeSpeed);

                    float distance = relativePos.Length();
                    float minSeparation = distance - 
                        relativeSpeed * shortestTime;
                    if (minSeparation > 2 * Radius) continue;

                    if (timeToCollision > 0 && timeToCollision < shortestTime)
                    {
                        shortestTime = timeToCollision;
                        firstTargetPos = target.Body.Position;
                        firstMinSeparation = minSeparation;
                        firstDistance = distance;
                        firstRelativePos = relativePos;
                        firstRelativeVel = relativeVel;
                    }
                }

                if (firstTargetPos != null)
                {
                    Vector3 relativePos;
                    if (firstMinSeparation <= 0 || firstDistance < 2 * Radius)
                        relativePos = firstTargetPos.Value - 
                            Self.Body.Position;
                    else relativePos = firstRelativePos + 
                            firstRelativeVel * shortestTime;
                    return Vector3.Normalize(relativePos) * 
                        MaximumAccelerationLinear;
                }
            }
            
            return base.CalculateAccelerationLinear();
        }
    }
}
