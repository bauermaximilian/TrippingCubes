using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace GameCraft.Physics
{
    // Source: https://github.com/chrisdickinson/aabb-3d/blob/master/index.js
    public readonly struct BoundingBox
    {
        public Vector3 Position { get; }

        public Vector3 Dimensions { get; }

        public Vector3 Maximum => Position + Dimensions;

        public float Volume => Dimensions.X * Dimensions.Y * Dimensions.Z;

        public BoundingBox(Vector3 position, Vector3 dimensions)
        {
            Position = Vector3.Min(position, position + dimensions);
            Dimensions = dimensions;
        }

        public BoundingBox(float x, float y, float z,
            float w, float h, float d) : this(new Vector3(x, y, z),
                new Vector3(w, h, d))
        { }

        public BoundingBox Translated(Vector3 translation)
        {
            return new BoundingBox(Position + translation, Dimensions);
        }

        public override string ToString()
        {
            return $"P={Position}, D={Dimensions}";
        }
    }
}
