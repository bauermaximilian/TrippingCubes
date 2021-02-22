using ShamanTK.Common;
using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class FlockSeparationBehavior<ParamT> : Behavior<ParamT>
    {
        public float NeighborhoodRadius { get; set; } = 4;

        public Angle NeighborhoodAngle { get; set; } = Angle.Deg(270);

        public bool UseBalancedSeparationInfluences { get; set; } = false;

        public FlockSeparationBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            var neighborhood = new List<IEntity>(GetNeighborhood(
                NeighborhoodRadius, NeighborhoodAngle));

            Vector3 separationAcceleration = Vector3.Zero;

            foreach (var entity in neighborhood)
            {
                Vector3 relativePosition = Self.Body.Position - 
                    entity.Body.Position;

                float scalingFactor;
                if (UseBalancedSeparationInfluences)
                    scalingFactor = 1f / neighborhood.Count;
                else scalingFactor = 1 / relativePosition.Length();

                Vector3 individualSeparationAcceleration =
                    Vector3.Normalize(relativePosition) * scalingFactor;

                separationAcceleration += individualSeparationAcceleration;
            }

            return separationAcceleration;
        }
    }
}
