using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class SeekBehavior<ParamT> : Behavior<ParamT>
    {
        public Vector3 TargetPosition { get; set; }

        public SeekBehavior(IEntity self) : base(self)
        {
            MaximumAccelerationLinear = 3.75f;
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            Vector3 direction = (TargetPosition - Self.Body.Position) 
                * ClearAxisY;
            return Vector3.Normalize(direction) * MaximumAccelerationLinear;
        }
    }
}