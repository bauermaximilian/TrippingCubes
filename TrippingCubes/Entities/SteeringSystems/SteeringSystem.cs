using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Entities.Behaviors;
using System.Linq;

namespace TrippingCubes.Entities.SteeringSystems
{
    abstract class SteeringSystem<ParamT>
    {
		private Behavior<ParamT>[] behaviors;

		public bool IsEnabled { get; set; } = true;

		public Vector3 AccelerationLinear { get; private set; }

        public Angle AccelerationAngular { get; private set; }

		public float MaximumAccelerationLinear
		{
			get => maximumAccelerationLinear;
			set => maximumAccelerationLinear = Math.Max(0, value);
		}
		private float maximumAccelerationLinear = 3.75f;

		public float MaximumAccelerationAngular
		{
			get => maximumAccelerationAngular;
			set => maximumAccelerationAngular = Math.Max(0, value);
		}
		private float maximumAccelerationAngular = Angle.Deg(205);		

		private void ValidateBehaviorCache()
        {
			if (behaviors == null)
			{
				behaviors = GetBehaviors()?.ToArray()
					?? new Behavior<ParamT>[0];
			}
		}

		protected void InvalidateBehaviorCache()
        {
			behaviors = null;
		}

		protected virtual IEnumerable<Behavior<ParamT>> GetBehaviors()
        {
			var behaviors = new List<Behavior<ParamT>>();
			foreach (var property in GetType().GetProperties())
			{
				Type requiredType = typeof(Behavior<ParamT>);
				Type propertyType = property.PropertyType;

				if (requiredType.IsAssignableFrom(propertyType))
				{
					object value = property.GetValue(this);
					if (value is Behavior<ParamT> behavior)
						behaviors.Add(behavior);
				}
			}
			return behaviors;
		}

        public void Update()
        {
			ValidateBehaviorCache();

			if (IsEnabled)
			{
				(Vector3 linear, Angle angular) = GetAccelerations(behaviors);

				AccelerationLinear = linear;
				AccelerationAngular = angular;
			}
			else
			{
				AccelerationLinear = Vector3.Zero;
				AccelerationAngular = 0;
			}
        }

		protected abstract (Vector3 linear, Angle angular) GetAccelerations(
			IEnumerable<Behavior<ParamT>> behaviors);
	}
}
