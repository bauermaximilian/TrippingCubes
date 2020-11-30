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

using System;
using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.Physics
{
    class PhysicsSystem
    {
        public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

        public bool HasGravity => Gravity.Length() > 0;

        public float MinBounceImpulse { get; set; } = 0.5f;

        public float AirDrag { get; set; } = 0.1f;

        public float FluidDrag { get; set; } = 0.4f;

        public float FluidDensity { get; set; } = 2.0f;

        private readonly List<RigidBody> bodies = new List<RigidBody>();

        internal readonly Func<Vector3, bool> isSolid, isFluid;

        public PhysicsSystem(Func<Vector3, bool> isSolid, Func<Vector3, bool> isFluid)
        {
            this.isSolid = isSolid ??
                throw new ArgumentNullException(nameof(isSolid));
            this.isFluid = isFluid ??
                throw new ArgumentNullException(nameof(isFluid));
        }

        public RigidBody AddNewBody(BoundingBox boundingBox)
        {
            RigidBody body = new RigidBody(this, boundingBox);
            bodies.Add(body);
            return body;
        }

        public bool RemoveBody(RigidBody rigidBody)
        {
            return bodies.Remove(rigidBody);
        }

        public void Update(TimeSpan delta)
        {
            foreach (RigidBody body in bodies) body.Update(delta);
        }        
    }
}
