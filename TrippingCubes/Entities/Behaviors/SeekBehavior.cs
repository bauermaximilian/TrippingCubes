using System.Numerics;

namespace TrippingCubes.Entities.Behaviors
{
    class SeekBehavior<ParamT> : Behavior<ParamT>
    {
        public Vector3? TargetPosition { get; set; }

        public SeekBehavior(IEntity self) : base(self)
        {
        }

        protected override Vector3 CalculateAccelerationLinear()
        {
            if (TargetPosition.HasValue)
            {
                Vector3 direction = (TargetPosition.Value - Self.Body.Position)
                    * ClearAxisY;
                return Vector3.Normalize(direction) *
                    MaximumAccelerationLinear;
            }
            else return Vector3.Zero;
        }
    }
}