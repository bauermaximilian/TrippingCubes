/*
 * GameCraft
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

#define DISABLE_HARDWARE_VECTOR

using System;
using System.Numerics;
using System.Text;

namespace GameCraft.Common
{
    public enum Direction : byte
    {
        East,
        West,
        Above,
        Below,
        North,
        South
    }

    public readonly struct Vector3I : IEquatable<Vector3I>
    {
        public static Vector3I Zero { get; } = new Vector3I(0, 0, 0);

        public static Vector3I One { get; } = new Vector3I(1, 1, 1);

        public static Vector3I East { get; } = new Vector3I(1, 0, 0);

        public static Vector3I West { get; } = new Vector3I(-1, 0, 0);

        public static Vector3I Above { get; } = new Vector3I(0, 1, 0);

        public static Vector3I Below { get; } = new Vector3I(0, -1, 0);

        public static Vector3I North { get; } = new Vector3I(0, 0, 1);

        public static Vector3I South { get; } = new Vector3I(0, 0, -1);

        public static Vector3I FromDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.East: return East;
                case Direction.West: return West;
                case Direction.Above: return Above;
                case Direction.Below: return Below;
                case Direction.North: return North;
                case Direction.South: return South;
                default: return Zero;
            }
        }

        public bool IsZero => Equals(Zero);

        public bool IsOne => Equals(One);

        public int X { get; }

        public int Y { get; }

        public int Z { get; }

        public Vector3I(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3I ToAbsolute()
        {
            return new Vector3I(Math.Abs(X), Math.Abs(Y), Math.Abs(Z));
        }

        public int DistanceTo(in Vector3I other)
        {
            return (int)ExactDistanceTo(other);
        }

        public float ExactDistanceTo(in Vector3I other)
        {
            return ExactDistanceTo(new Vector3(other.X, other.Y, other.Z));
        }

        public float ExactDistanceTo(in Vector3 other)
        {
            Vector3 distanceVector = other - new Vector3(X, Y, Z);
            return distanceVector.Length();
        }

        public bool IsInsideArea(in Vector3I areaPosition, 
            in Vector3I areaSize)
        {
            return this >= areaPosition && this < areaPosition + areaSize;
        }

        public bool Equals(Vector3I other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector3I i && Equals(i);
        }

        public override int GetHashCode()
        {
            var hashCode = -307843816;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + Z.GetHashCode();
            return hashCode;
        }

        public static Vector3I Min(Vector3I a, Vector3I b)
        {
            return new Vector3I(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y),
                Math.Min(a.Z, b.Z));
        }

        public static Vector3I Max(Vector3I a, Vector3I b)
        {
            return new Vector3I(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y),
                Math.Max(a.Z, b.Z));
        }

        public static Vector3I operator +(in Vector3I left, in Vector3I right)
        {
            return new Vector3I(left.X + right.X, left.Y + right.Y, 
                left.Z + right.Z);
        }

        public static Vector3I operator -(in Vector3I vector)
        {
            return new Vector3I(-vector.X, -vector.Y, -vector.Z);
        }

        public static Vector3I operator -(in Vector3I left, in Vector3I right)
        {
            return new Vector3I(left.X - right.X, left.Y - right.Y,
                left.Z - right.Z);
        }

        public static Vector3I operator *(in Vector3I left, int scalar)
        {
            return new Vector3I(left.X * scalar, left.Y * scalar,
                left.Z * scalar);
        }

        public static Vector3I operator *(in Vector3I left, in Vector3I right)
        {
            return new Vector3I(left.X * right.X, left.Y * right.Y,
                left.Z * right.Z);
        }

        public static Vector3I operator /(in Vector3I left, in Vector3I right)
        {
            return new Vector3I(left.X / right.X, left.Y / right.Y,
                left.Z / right.Z);
        }

        public static Vector3I operator /(in Vector3I left, int divisor)
        {
            return new Vector3I(left.X / divisor, left.Y / divisor,
                left.Z / divisor);
        } 

        public static Vector3I operator %(in Vector3I left, int mod)
        {
            return new Vector3I(left.X % mod, left.Y % mod, left.Z % mod);
        }

        public static bool operator <(in Vector3I left, in Vector3I right)
        {
            return left.X < right.X && left.Y < right.Y && left.Z < right.Z;
        }

        public static bool operator >(in Vector3I left, in Vector3I right)
        {
            return left.X > right.X && left.Y > right.Y && left.Z > right.Z;
        }

        public static bool operator <=(in Vector3I left, in Vector3I right)
        {
            return left.X <= right.X && left.Y <= right.Y && left.Z <= right.Z;
        }

        public static bool operator >=(in Vector3I left, in Vector3I right)
        {
            return left.X >= right.X && left.Y >= right.Y && left.Z >= right.Z;
        }

        public static bool operator ==(in Vector3I left, in Vector3I right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector3I left, in Vector3I right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(X);
            stringBuilder.Append(ComponentSeparator);
            stringBuilder.Append(Y);
            stringBuilder.Append(ComponentSeparator);
            stringBuilder.Append(Z);
            return stringBuilder.ToString();
        }

        public const char ComponentSeparator = ',';

        public static bool TryParse(string s, out Vector3I result)
        {
            if (s != null)
            {
                string[] segments = s.Split(ComponentSeparator);

                if (segments.Length == 3)
                {
                    if (int.TryParse(segments[0].Trim(), out int x) &&
                        int.TryParse(segments[1].Trim(), out int y) &&
                        int.TryParse(segments[2].Trim(), out int z))
                    {
                        result = new Vector3I(x, y, z);
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        public static Vector3I FromVector3(Vector3 vector)
        {
            return new Vector3I((int)Math.Round(vector.X),
                (int)Math.Round(vector.Y), (int)Math.Round(vector.Z));
        }
         
        public static implicit operator Vector3(Vector3I vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static explicit operator Vector3I(Vector3 vector)
        {
            return new Vector3I((int)vector.X, (int)vector.Y, (int)vector.Z);
        }
    }
}
