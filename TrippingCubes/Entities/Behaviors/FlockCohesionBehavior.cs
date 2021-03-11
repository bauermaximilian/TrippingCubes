using ShamanTK.Common;
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Physics;

namespace TrippingCubes.Entities.Behaviors
{
    class FlockCohesionBehavior<ParamT> : ArriveBehavior<ParamT>
    {
        public float NeighborhoodRadius { get; set; } = 4;

        public Angle NeighborhoodAngle { get; set; } = Angle.Deg(270);

        public FlockCohesionBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            var neighborhood = new List<IEntity>(GetNeighborhood(
                NeighborhoodRadius, NeighborhoodAngle));

            if (neighborhood.Count == 0) return Vector3.Zero;

            Vector3 neighborhoodCenter = Vector3.Zero;

            foreach (var entity in neighborhood)
                neighborhoodCenter += entity.Body.Position; 

            neighborhoodCenter /= neighborhood.Count;

            TargetPosition = neighborhoodCenter;
            return base.CalculateAccelerationLinear();
        }
    }
}
