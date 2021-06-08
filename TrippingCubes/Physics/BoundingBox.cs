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

using System.Numerics;

namespace TrippingCubes.Physics
{
    public readonly struct BoundingBox
    {
        public static BoundingBox Empty { get; } =
            new BoundingBox(0, 0, 0, 0, 0, 0);

        public static BoundingBox Unit { get; } =
            new BoundingBox(0, 0, 0, 1, 1, 1);

        public Vector3 Position { get; }

        public Vector3 Dimensions { get; }

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

        public Vector3 PivotBottom()
        {
            return new Vector3(Dimensions.X / 2, 0, Dimensions.Z / 2) +
                Position;
        }

        public float Volume()
        {
            return Dimensions.X * Dimensions.Y * Dimensions.Z;
        }

        public Vector3 Maximum()
        {
            return Position + Dimensions;
        }

        public bool IntersectsWith(in BoundingBox other)
        {
            Vector3 pos = Position;
            Vector3 max = Maximum();
            Vector3 otherPos = other.Position;
            Vector3 otherMax = other.Maximum();

            return (pos.X <= otherMax.X && max.X >= otherPos.X) &&
                (pos.Y <= otherMax.Y && max.Y >= otherPos.Y) &&
                (pos.Z <= otherMax.Z && max.Z >= otherPos.Z);
        }

        public override string ToString()
        {
            return $"P={Position}, D={Dimensions}";
        }
    }
}
