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
    enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    class PhysicsSystem
    {
        internal const float Epsilon = 0.0001f;

        public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

        public bool HasGravity => Gravity.Length() > 0;

        public float MinBounceImpulse { get; set; } = 0.5f;

        public float AirDrag { get; set; } = 1.25f;

        public float FluidDrag { get; set; } = 3f;

        public float AngularDragFactor { get; set; } = 2f;

        public float FluidDensity { get; set; } = 2.0f;

        public IEnumerable<RigidBody> Bodies => bodies;

        private readonly List<RigidBody> bodies = new List<RigidBody>();

        internal readonly Func<Vector3, bool> isSolid, isFluid;

        private readonly Sweep sweep;

        public PhysicsSystem(Func<Vector3, bool> isSolid, Func<Vector3, bool> isFluid)
        {
            this.isSolid = isSolid ??
                throw new ArgumentNullException(nameof(isSolid));
            this.isFluid = isFluid ??
                throw new ArgumentNullException(nameof(isFluid));

            sweep = new Sweep(isSolid);
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
        
        public bool RaycastVolumetric(BoundingBox boundingBox,
            Vector3 distance, out float collisionDistance, 
            out Vector3 collisionNormal)
        {
            bool collided = false;
            float collidedDistance = 0;
            float collidedDir = 0;
            int collidedAxisIndex = 0;

            sweep.Execute(ref boundingBox, distance, (float distance, 
                int axisIndex, float dir, ref Vector3 leftToGo) =>
            {
                collidedDistance = distance;
                collidedAxisIndex = axisIndex;
                collidedDir = dir;
                collided = true;
                return true;
            }, true);

            collisionDistance = collidedDistance;

            collisionNormal = collidedAxisIndex switch
            {
                0 => Vector3.UnitX,
                1 => Vector3.UnitY,
                2 => Vector3.UnitZ,
                _ => Vector3.Zero
            } * (collidedDir > 0 ? 1 : -1);

            return collided;
        }
    }
}
