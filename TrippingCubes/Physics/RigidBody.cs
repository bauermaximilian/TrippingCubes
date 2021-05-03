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
using System.Numerics;

namespace TrippingCubes.Physics
{
    class RigidBody
    {
        public float Mass { get; set; } = 1;

        public float Friction { get; set; } = 0.1f;

        public float Restitution { get; set; } = 0;

        public float GravityMultiplier { get; set; } = 1;

        public bool EnableAutoJump { get; set; } = false;

        public float? AirDrag { get; set; } = null;

        public float? FluidDrag { get; set; } = null;

        public Angle Orientation { get; set; } = Angle.Zero;

        public Angle Rotation { get; private set; } = Angle.Zero;

        // event argument is impulse J = m * dv
        public event Action<Vector3> Collide;

        public event EventHandler AutoJump;

        public BoundingBox BoundingBox => boundingBox;

        public Vector3 Position => BoundingBox.PivotBottom();

        public bool InFluid { get; private set; } = false;

        public Vector3 Resting => resting;

        public Vector3 Velocity => velocity;

        public bool IsNotAffectedByGravity => !World.HasGravity ||
            GravityMultiplier == 0;

        public bool DoesNotCollideWithOtherObjects { get; set; } = false;

        public bool IsCharacter { get; set; } = false;

        private readonly Sweep sweep;

        private float ratioInFluid = 0;

        private Vector3 forces = Vector3.Zero;
        private Vector3 impulses = Vector3.Zero;
        private Angle angularForces = Angle.Zero;

        private Vector3 velocity = Vector3.Zero;
        private Vector3 resting = Vector3.Zero;
        private BoundingBox boundingBox;

        private int sleepFrameCount;

        public PhysicsSystem World { get; }

        internal RigidBody(PhysicsSystem world, in BoundingBox boundingBox)
        {
            World = world ??
                throw new ArgumentNullException(nameof(world));

            sweep = new Sweep(world.isSolid);

            this.boundingBox = boundingBox;

            MarkActive();
        }

        public void MoveTo(in Vector3 position)
        {
            SanityCheck(position, nameof(position));

            Vector3 translation = position - boundingBox.Position;
            boundingBox = boundingBox.Translated(translation);
            MarkActive();
        }

        public void Move(in Vector3 translation)
        {
            SanityCheck(translation, nameof(translation));
            boundingBox = boundingBox.Translated(translation);
            MarkActive();
        }

        public void ApplyAcceleration(Vector3 accerlation)
        {
            SanityCheck(accerlation, nameof(accerlation));
            forces += accerlation * Mass;
            MarkActive();
        }

        public void ApplyForce(Vector3 force)
        {
            SanityCheck(force, nameof(force));
            forces += force;
            MarkActive();
        }

        public void ApplyImpulse(Vector3 impulse)
        {
            SanityCheck(impulse, nameof(impulse));
            impulses += impulse;
            MarkActive();
        }

        public void ApplyVelocityChange(Vector3 velocityChange)
        {
            SanityCheck(velocityChange, nameof(velocityChange));
            impulses += velocityChange * Mass;
            MarkActive();
        }

        public void ApplyAngularAcceleration(Angle acceleration)
        {
            angularForces += acceleration * Mass;
        }

        private void MarkActive()
        {
            sleepFrameCount = 10;
        }

        private void SanityCheck(Vector3 vector, string vectorName)
        {
            if (float.IsNaN(vector.Length()))
                throw new InvalidOperationException(
                    $"The specified vector {vectorName} is invalid (NaN).");
        }

        internal void Update(TimeSpan delta)
        {
            float deltaSeconds = (float)delta.TotalSeconds;
            Vector3 previousResting = resting;

            // treat bodies with <= mass as static
            if (Mass <= 0)
            {
                velocity = Vector3.Zero;
                forces = Vector3.Zero;
                impulses = Vector3.Zero;
                return;
            }

            // skip bodies if static or no velocity/forces/impulses
            if (IsAsleep(delta)) return;
            sleepFrameCount--;

            // check if under water, if so apply buoyancy and drag forces
            ApplyFluidForces();

            // debug hooks
            SanityCheck(forces, nameof(forces));
            SanityCheck(forces, nameof(impulses));
            SanityCheck(forces, nameof(velocity));
            SanityCheck(forces, nameof(resting));

            // semi-implicit Euler integration
            Vector3 accerlation = forces / Mass;
            accerlation += World.Gravity * GravityMultiplier;

            Vector3 deltaVelocity = accerlation * deltaSeconds;
            deltaVelocity += impulses / Mass;
            velocity += deltaVelocity;

            Angle deltaRotation = (angularForces / Mass);
            Rotation += deltaRotation * deltaSeconds;

            // apply friction based on change in velocity this frame
            if (Friction > 0)
            {
                ApplyFrictionByAxis(0, deltaVelocity);
                ApplyFrictionByAxis(1, deltaVelocity);
                ApplyFrictionByAxis(2, deltaVelocity);
            }

            // linear air or fluid friction - effectively v *= drag
            // body settings override global settings
            float drag;
            if (InFluid) 
                drag = (FluidDrag ?? World.FluidDrag) *
                    (1 - (float)Math.Pow((1 - ratioInFluid), 2));
            else drag = AirDrag ?? World.AirDrag;

            velocity *= Math.Max(1 - drag * deltaSeconds / Mass, 0);
            Rotation *= Math.Max(1 - (drag * World.AngularDragFactor) * 
                deltaSeconds / Mass, 0);

            Orientation += Rotation * deltaSeconds;

            // "dx" specifies the "instantenous change in position"
            // "dt" the time delta (here "deltaSeconds")
            // velocity = dx / dt
            Vector3 dx = velocity * deltaSeconds;

            // clear forces and impulses for next timestep
            impulses = forces = Vector3.Zero;
            angularForces = Angle.Zero;

            BoundingBox previousBoundingBox = boundingBox;

            // sweeps aabb along dx and accounts for collisions
            ProcessCollisions(ref boundingBox, dx, ref resting);

            if (EnableAutoJump)
            {
                TryAutoJumping(boundingBox, dx);
            }

            if (!DoesNotCollideWithOtherObjects)
            {
                // HACK: If there's a collision with any other entity in the
                // world (checked via bounding box X/Z), reset the bounding box
                // to the previous one. Very dirty and time-consuming. Pfui.
                foreach (RigidBody body in World.Bodies)
                {
                    if (body == this || body.DoesNotCollideWithOtherObjects)
                        continue;

                    if (boundingBox.IntersectsWith(body.boundingBox))
                    {
                        boundingBox = previousBoundingBox;
                        velocity *= 0.5f;
                        break;
                    }
                }
            }

            // Collision impacts. resting shows which axes had collisions:
            Vector3 impacts = Vector3.Zero;

            // count impact only if wasn't collided last frame
            if (resting.X != 0)
            {
                if (previousResting.X == 0) impacts.X = -velocity.X;
                velocity.X = 0;
            }
            if (resting.Y != 0)
            {
                if (previousResting.Y == 0) impacts.Y = -velocity.Y;
                velocity.Y = 0;
            }
            if (resting.Z != 0)
            {
                if (previousResting.Z == 0) impacts.Z = -velocity.Z;
                velocity.Z = 0;
            }

            float impactsLength = impacts.Length();
            if (impactsLength > PhysicsSystem.Epsilon)
            {
                // send collision event - allows client to optionally change
                // body's restitution depending on what terrain it hit
                // event argument is impulse J = m * dv
                impacts *= Mass;
                Collide?.Invoke(impacts);

                if (Restitution > 0 && impactsLength > World.MinBounceImpulse)
                {
                    impacts *= Restitution;
                    ApplyImpulse(impacts);
                }
            }

            // sleep check
            float vsq = velocity.LengthSquared();
            if (vsq > PhysicsSystem.Epsilon) MarkActive();
        }

        private void TryAutoJumping(BoundingBox previousBoundingBox, 
            Vector3 dx)
        {
            if (resting.Y >= 0 || InFluid) return;

            // direction movement was blocked before trying a step
            bool xBlocked = resting.X != 0;
            bool zBlocked = resting.Z != 0;
            if (!(xBlocked || zBlocked)) return;

            previousBoundingBox = previousBoundingBox.Translated(new Vector3(
                0, 1.1f, 0));
            bool collided = false;
            sweep.Execute(ref previousBoundingBox, dx,
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
                {
                    collided = true;
                    return true;
                }, false);
            if (collided) return;

            AutoJump?.Invoke(this, EventArgs.Empty);
        }

        private float ProcessCollisions(ref BoundingBox boundingBox, 
            Vector3 dx, ref Vector3 resting)
        {
            Vector3 restingValue = Vector3.Zero;

            float result = sweep.Execute(ref boundingBox, dx, 
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
            {
                axisIndex %= 3;

                if (axisIndex == 0) restingValue.X = dir;
                else if (axisIndex == 1) restingValue.Y = dir;
                else restingValue.Z = dir;

                if (axisIndex == 0) vec.X = 0;
                else if (axisIndex == 1) vec.Y = 0;
                else vec.Z = 0;
                
                return false;
            }, false);

            resting = restingValue;

            return result;
        }

        private void ApplyFrictionByAxis(int axisIndex, 
            in Vector3 deltaVelocity)
        {            
            float restDir = (axisIndex % 3) switch
            {
                0 => resting.X,
                1 => resting.Y,
                _ => resting.Z,
            };

            float vNormal = (axisIndex % 3) switch
            {
                0 => deltaVelocity.X,
                1 => deltaVelocity.Y,
                _ => deltaVelocity.Z,
            };

            // friction applies only if moving into a touched surface
            if (restDir == 0) return;
            if (restDir * vNormal <= 0) return;

            // current vel lateral to friction axis
            Vector3 lateralVelocity = velocity;
            switch (axisIndex % 3)
            {
                case 0: lateralVelocity.X = 0; break;
                case 1: lateralVelocity.Y = 0; break;
                case 2: lateralVelocity.Z = 0; break;
            }

            float vCurr = lateralVelocity.Length();
            if (vCurr <= PhysicsSystem.Epsilon) return;

            float dvMax = Math.Abs(Friction * vNormal);

            // decrease lateral vel by dvMax (or clamp to zero)
            float scaler = (vCurr > dvMax) ? (vCurr - dvMax) / vCurr : 0;
            if (axisIndex != 0) velocity.X *= scaler;
            if (axisIndex != 1) velocity.Y *= scaler;
            if (axisIndex != 2) velocity.Z *= scaler;
        }

        // check if under water, if so apply buoyancy and drag forces
        private void ApplyFluidForces()
        {
            float cx = (float)Math.Floor(boundingBox.Position.X);
            float cz = (float)Math.Floor(boundingBox.Position.Z);
            float y0 = (float)Math.Floor(boundingBox.Position.Y);
            float y1 = (float)Math.Floor(boundingBox.Maximum().Y);

            if (!World.isFluid(new Vector3(cx, y0, cz)))
            {
                InFluid = false;
                this.ratioInFluid = 0;
                return;
            }

            float submerged = 1;
            float cy = y0 + 1;
            while (cy <= y1 && World.isFluid(new Vector3(cx, cy, cz)))
            {
                submerged++;
                cy++;
            }
            float fluidLevel = y0 + submerged;
            float heightInFluid = fluidLevel - boundingBox.Position.Y;
            float ratioInFluid = heightInFluid / boundingBox.Dimensions.Y;
            if (ratioInFluid > 1) ratioInFluid = 1;
            float displaced = boundingBox.Volume() * ratioInFluid;
            Vector3 bouyantForce = -World.Gravity * World.FluidDensity * 
                displaced;
            ApplyForce(bouyantForce);

            InFluid = true;
            this.ratioInFluid = ratioInFluid;
        }

        private bool IsAsleep(TimeSpan delta)
        {
            if (sleepFrameCount > 0) return false;

            // Without gravity, bodies stay asleep until a force/impulse 
            // wakes them up
            if (IsNotAffectedByGravity) return true;

            // Otherwise check body is resting against something
            // i.e. sweep along by distance d = 1/2 g*t^2
            // and check there's still a collision
            float deltaSeconds = (float)delta.TotalSeconds;

            Vector3 sleepVector = World.Gravity * 0.5f * deltaSeconds * 
                deltaSeconds * GravityMultiplier;

            bool isResting = false;

            sweep.Execute(ref boundingBox, sleepVector, 
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
                {
                    isResting = true; return true;
                }, true);

            return isResting;
        }        
    }
}
