using OpenTK.Graphics.ES20;
using OpenTK.Graphics.OpenGL;
using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameCraft.Physics
{
    class RigidBody
    {
        public float Mass { get; set; } = 1;

        public float Friction { get; set; } = 1;

        public float Restitution { get; set; } = 0;

        public float GravityMultiplier { get; set; } = 1;

        public bool AutoStep { get; set; } = false;

        public float? AirDrag { get; set; } = null;

        public float? FluidDrag { get; set; } = null;

        // event argument is impulse J = m * dv
        public event Action<Vector3> Collide;

        public event EventHandler Step;

        public BoundingBox BoundingBox => boundingBox;

        public bool InFluid { get; private set; } = false;

        public Vector3 Resting => resting;

        public Vector3 Velocity => velocity;

        public bool IsNotAffectedByGravity => !world.HasGravity ||
            GravityMultiplier == 0;

        private readonly Sweep sweep;

        private float ratioInFluid = 0;

        private Vector3 forces = Vector3.Zero;
        private Vector3 impulses = Vector3.Zero;

        private Vector3 velocity = Vector3.Zero;
        private Vector3 resting = Vector3.Zero;
        private BoundingBox boundingBox;

        private int sleepFrameCount;

        private readonly World world;

        private const float PhysicsEpsilon = 0.0001f;

        internal RigidBody(World world, in BoundingBox aabb)
        {
            this.world = world ??
                throw new ArgumentNullException(nameof(world));

            sweep = new Sweep(world.isSolid);

            this.boundingBox = aabb;

            MarkActive();
        }

        public void SetPosition(in Vector3 position)
        {
            SanityCheck(position, nameof(position));

            Vector3 translation = position - boundingBox.Position;
            boundingBox = boundingBox.Translated(translation);
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

        private void MarkActive()
        {
            sleepFrameCount = 10 | 0;
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
            Vector3 a = forces * (1 / Mass);
            a += world.Gravity * GravityMultiplier;

            Vector3 deltaVelocity = impulses * (1 / Mass);
            deltaVelocity += a * deltaSeconds;
            velocity += deltaVelocity;

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
                drag = (FluidDrag ?? world.FluidDrag) *
                    (1 - (float)Math.Pow((1 - ratioInFluid), 2));
            else drag = AirDrag ?? world.AirDrag;

            velocity *= Math.Max(1 - drag * deltaSeconds / Mass, 0);

            // x1-x0 = v1*dt
            Vector3 dx = velocity * deltaSeconds;

            // clear forces and impulses for next timestep
            impulses = forces = Vector3.Zero;

            BoundingBox previousBoundingBox = boundingBox;

            // sweeps aabb along dx and accounts for collisions
            ProcessCollisions(ref boundingBox, ref dx, ref resting);

            // if autostep, and on ground, run collisions again with stepped up aabb
            if (AutoStep)
            {
                TryAutoStepping(previousBoundingBox, dx);
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

            // Old slower logic:
            //for (int i = 0; i < 3; ++i)
            //{
            //    if (GetVectorAxis(resting, i) != 0)
            //    {
            //        // count impact only if wasn't collided last frame
            //        if (GetVectorAxis(previousResting, i) == 0)
            //        {
            //            SetVectorAxis(ref impacts,
            //                -GetVectorAxis(velocity, i), i);
            //        }
            //        SetVectorAxis(ref velocity, 0, i);
            //    }
            //}

            float mag = impacts.Length();
            if (mag > 0.001f) // epsilon
            {
                // send collision event - allows client to optionally change
                // body's restitution depending on what terrain it hit
                // event argument is impulse J = m * dv
                impacts *= Mass;
                Collide?.Invoke(impacts);

                if (Restitution > 0 && mag > world.MinBounceImpulse)
                {
                    impacts *= Restitution;
                    ApplyImpulse(impacts);
                }
            }

            // sleep check
            float vsq = velocity.LengthSquared();
            if (vsq > 0.00001f) MarkActive();
        }

        private void TryAutoStepping(BoundingBox previousBoundingBox, Vector3 dx)
        {
            if (resting.Y >= 0 && InFluid) return;

            // direction movement was blocked before trying a step
            bool xBlocked = resting.X != 0;
            bool zBlocked = resting.Z != 0;
            if (!(xBlocked || zBlocked)) return;

            // continue autostepping only if headed sufficiently into obstruction
            float ratio = Math.Abs(dx.X / dx.Z);
            float cutoff = 4;
            if (!xBlocked && ratio > cutoff) return;
            if (!zBlocked && ratio < 1 / cutoff) return;

            // original target position before being obstructed
            Vector3 targetPos = previousBoundingBox.Position + dx;

            // move towards the target until the first X/Z collision
            sweep.Execute(ref previousBoundingBox, ref dx,
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
                {
                    if (axisIndex == 1)
                    {
                        vec.Y = 0;
                        return false;
                    }
                    else return true;
                }, false);

            float y = boundingBox.Position.Y;
            float ydist = (float)Math.Floor(y + 1.001) - y;
            Vector3 upvec = new Vector3(0, ydist, 0);
            bool collided = false;
            // sweep up, bailing on any obstruction
            sweep.Execute(ref previousBoundingBox, ref upvec,
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
                {
                    collided = true;
                    return true;
                }, false);
            if (collided) return;

            // now move in X/Z however far was left over before hitting the obstruction
            Vector3 leftover = targetPos - previousBoundingBox.Position;
            leftover.Y = 0;
            Vector3 tmpResting = Vector3.Zero;
            ProcessCollisions(ref boundingBox, ref leftover, ref tmpResting);

            // bail if no movement happened in the originally blocked direction
            if (xBlocked && previousBoundingBox.Position.X != targetPos.X) return;
            if (xBlocked && previousBoundingBox.Position.Z != targetPos.Z) return;

            // done - oldBox is now at the target autostepped position
            boundingBox = previousBoundingBox;
            resting.X = tmpResting.X;
            resting.Z = tmpResting.Z;
            Step?.Invoke(this, EventArgs.Empty);
        }

        private float ProcessCollisions(ref BoundingBox boundingBox, 
            ref Vector3 velocity, ref Vector3 resting)
        {
            Vector3 restingValue = Vector3.Zero;

            float result = sweep.Execute(ref boundingBox, ref velocity, 
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

        private static float GetVectorAxis(in Vector3 vector, int axisIndex)
        {
            switch (axisIndex % 3)
            {
                case 0: return vector.X;
                case 1: return vector.Y;
                default: return vector.Z;
            }
        }

        private static void SetVectorAxis(ref Vector3 vector, float axisValue,
            int axisIndex)
        {
            switch (axisIndex % 3)
            {
                case 0: vector.X = axisValue; break;
                case 1: vector.Y = axisValue; break;
                case 2: vector.Z = axisValue; break;
            }
        }

        private void ApplyFrictionByAxis(int axisIndex, in Vector3 deltaVelocity)
        {            
            float restDir = GetVectorAxis(resting, axisIndex);
            float vNormal = GetVectorAxis(deltaVelocity, axisIndex);

            // friction applies only if moving into a touched surface
            if (restDir == 0) return;
            if (restDir * vNormal <= 0) return;

            // current vel lateral to friction axis
            Vector3 lateralVelocity = velocity;
            SetVectorAxis(ref lateralVelocity, 0, axisIndex);
            float vCurr = lateralVelocity.Length();
            if (vCurr <= 0.0001) return;

            // treat current change in velocity as the result of a pseudoforce
            //        Fpseudo = m*dv/dt
            // Base friction force on normal component of the pseudoforce
            //        Ff = u * Fnormal
            //        Ff = u * m * dvnormal / dt
            // change in velocity due to friction force
            //        dvF = dt * Ff / m
            //            = dt * (u * m * dvnormal / dt) / m
            //            = u * dvnormal
            float dvMax = Math.Abs(Friction * vNormal);

            // decrease lateral vel by dvMax (or clamp to zero)
            float scaler = (vCurr > dvMax) ? (vCurr - dvMax) / vCurr : 0;
            if (axisIndex != 0) velocity.X *= scaler;
            if (axisIndex != 1) velocity.Y *= scaler;
            if (axisIndex != 2) velocity.Z *= scaler;
        }

        private void ApplyFrictionByAxis(float resting, float deltaVelocity,
            ref float velocityOtherAxisA, ref float velocityOtherAxisB)
        {
            if (resting != 0 && resting * deltaVelocity > 0)
            {
                float axisVelocity = (float)Math.Sqrt(
                    Math.Pow(velocityOtherAxisA, 2) +
                    Math.Pow(velocityOtherAxisB, 2));

                if (axisVelocity > PhysicsEpsilon)
                {
                    float deltaVelocityMax =
                        Math.Abs(Friction * deltaVelocity);

                    float scaler = (axisVelocity > deltaVelocityMax) ?
                        (axisVelocity - deltaVelocityMax) / axisVelocity : 0;

                    velocityOtherAxisA *= scaler;
                    velocityOtherAxisB *= scaler;
                }
            }
        }

        // check if under water, if so apply buoyancy and drag forces
        private void ApplyFluidForces()
        {
            float cx = (float)Math.Floor(boundingBox.Position.X);
            float cz = (float)Math.Floor(boundingBox.Position.Z);//??? ggf. -1 nötig
            float y0 = (float)Math.Floor(boundingBox.Position.Y);
            float y1 = (float)Math.Floor(boundingBox.Maximum.Y);//?

            if (!world.isFluid(new Vector3(cx, y0, cz)))
            {
                InFluid = false;
                this.ratioInFluid = 0;
                return;
            }

            float submerged = 1;
            float cy = y0 + 1;
            while (cy <= y1 && world.isFluid(new Vector3(cx, cy, cz)))
            {
                submerged++;
                cy++;
            }
            float fluidLevel = y0 + submerged;
            float heightInFluid = fluidLevel - boundingBox.Position.Y;
            float ratioInFluid = heightInFluid / boundingBox.Dimensions.Y;
            if (ratioInFluid > 1) ratioInFluid = 1;
            float displaced = boundingBox.Volume * ratioInFluid;
            Vector3 bouyantForce = -world.Gravity * world.FluidDensity * 
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

            Vector3 sleepVector = world.Gravity * 0.5f * deltaSeconds * 
                deltaSeconds * GravityMultiplier;

            bool isResting = false;

            sweep.Execute(ref boundingBox, ref sleepVector, 
                (float dist, int axisIndex, float dir, ref Vector3 vec) =>
                {
                    isResting = true; return true;
                }, true);

            return isResting;
        }        
    }
}
