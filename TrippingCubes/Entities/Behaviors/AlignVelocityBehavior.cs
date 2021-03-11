using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class AlignVelocityBehavior<ParamT> : AlignBehavior<ParamT>
    {
        public AlignVelocityBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3? CalculateAlignDirection()
        {
            return Self.Body.Velocity;
        }
    }
}