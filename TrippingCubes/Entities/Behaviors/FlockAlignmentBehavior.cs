using ShamanTK.Common;
using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class FlockAlignmentBehavior<ParamT> : AlignVelocityBehavior<ParamT>
    {
        public float NeighborhoodRadius { get; set; } = 4;

        public Angle NeighborhoodAngle { get; set; } = Angle.Deg(270);

        public FlockAlignmentBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAlignDirection()
        {
            var neighborhood = new List<IEntity>(GetNeighborhood(
                NeighborhoodRadius, NeighborhoodAngle));

            Vector3 neighborhoodVelocities = Vector3.Zero;

            foreach (var entity in neighborhood)
                neighborhoodVelocities += entity.Body.Velocity;

            neighborhoodVelocities /= neighborhood.Count;

            return neighborhoodVelocities;
        }
    }
}
