using GameCraft.BlockChunk;
using GameCraft.Common;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameCraft.Physics
{
    class World
    {
        public Vector3 Gravity { get; set; } = new Vector3(0, -9.81f, 0);

        public bool HasGravity => Gravity.Length() > 0;

        public float MinBounceImpulse { get; set; } = 0.5f;

        public float AirDrag { get; set; } = 0.1f;

        public float FluidDrag { get; set; } = 0.4f;

        public float FluidDensity { get; set; } = 2.0f;

        private readonly List<RigidBody> bodies = new List<RigidBody>();

        internal readonly Func<Vector3, bool> isSolid, isFluid;

        public World(Func<Vector3, bool> isSolid, Func<Vector3, bool> isFluid)
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
