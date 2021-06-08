/*
 * TrippingCubes
 * A toolkit for creating games in a voxel-based environment.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

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
