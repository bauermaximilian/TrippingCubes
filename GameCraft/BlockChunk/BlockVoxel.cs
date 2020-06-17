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

using System;

namespace GameCraft.BlockChunk
{
    readonly struct BlockVoxel : IEquatable<BlockVoxel>
    {
        public BlockKey BlockKey { get; }

        public int BlockLight => light & 0x0F;

        public int SkyLight => (light & 0xF0) >> 4;

        private readonly byte light;

        public BlockVoxel(ushort blockKeyId) : this(blockKeyId, 0) { }

        public BlockVoxel(ushort blockKeyId, ushort blockKeyVariation)
            : this(new BlockKey(blockKeyId, blockKeyVariation)) { }

        public BlockVoxel(BlockKey blockKey) : this(blockKey, 0, 0) { }

        public BlockVoxel(BlockKey blockKey, int blockLight, int skyLight)
        {
            BlockKey = blockKey;
            light = (byte)((skyLight << 4) | blockLight);
        }

        public override string ToString()
        {
            return BlockKey.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is BlockVoxel voxel && Equals(voxel);
        }

        public bool Equals(BlockVoxel other)
        {
            return BlockKey.Equals(other.BlockKey) &&
                   light == other.light;
        }

        public override int GetHashCode()
        {
            int hashCode = 1825949216;
            hashCode = hashCode * -1521134295 + BlockKey.GetHashCode();
            hashCode = hashCode * -1521134295 + light.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BlockVoxel left, BlockVoxel right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlockVoxel left, BlockVoxel right)
        {
            return !(left == right);
        }
    }
}
