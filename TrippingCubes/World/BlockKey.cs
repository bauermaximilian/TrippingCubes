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

namespace TrippingCubes.World
{
    public readonly struct BlockKey : IEquatable<BlockKey>
    {
        public ushort Id { get; }

        public ushort Variation { get; }

        public BlockKey(ushort id, ushort variation)
        {
            Id = id;
            Variation = variation;
        }

        public static bool TryParse(string str,
            out BlockKey blockReference)
        {
            if (str != null)
            {
                string[] parts = str.Split(':');
                if (parts.Length > 0)
                {
                    if (ushort.TryParse(parts[0], out ushort id))
                    {
                        ushort variation = 0;
                        if (parts.Length == 1)
                        {
                            blockReference = new BlockKey(id, variation);
                            return true;
                        }
                        else if (parts.Length == 2)
                        {
                            if (ushort.TryParse(parts[1], out variation))
                            {
                                blockReference =
                                    new BlockKey(id, variation);
                                return true;
                            }
                        }
                    }
                }
            }

            blockReference = default;
            return false;
        }

        public override string ToString()
        {
            if (Variation > 0) return Id + ":" + Variation;
            else return Id.ToString(); ;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockKey reference && Equals(reference);
        }

        public bool Equals(BlockKey other)
        {
            return Id == other.Id &&
                   Variation == other.Variation;
        }

        public override int GetHashCode()
        {
            int hashCode = -578872354;
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + Variation.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BlockKey left,
            BlockKey right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockKey left,
            BlockKey right)
        {
            return !(left == right);
        }
    }
}
