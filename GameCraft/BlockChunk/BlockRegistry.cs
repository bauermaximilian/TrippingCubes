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

using ShamanTK.IO;
using System;
using System.Collections.Generic;

namespace GameCraft.BlockChunk
{
    class BlockRegistry
    {
        public TextureData Texture { get; }

        private readonly Block[][] blocks;

        public Block DefaultBlock { get; }

        public bool HasTexture => Texture != null;

        public int Count { get; }

        public ICollection<string> Identifiers => namedBlocks.Keys;

        private readonly Dictionary<string, Block> namedBlocks = 
            new Dictionary<string, Block>();

        public BlockRegistry(Block[][] blocks, BlockKey? customDefaultBlockKey,
            TextureData texture)
        {
            this.blocks = blocks ??
                throw new ArgumentNullException(nameof(blocks));

            Count = 0;
            for (int i = 0; i < blocks.Length; i++)
            {
                Block[] variations = blocks[i];
                if (variations != null)
                {
                    for (int v = 0; v < variations.Length; v++)
                    {
                        if (variations[v] != null)
                        {
                            namedBlocks[variations[v].Identifier] =
                                variations[v];
                            Count++;
                        }
                    }
                }
            }

            if (customDefaultBlockKey.HasValue)
            {
                DefaultBlock = GetBlock(customDefaultBlockKey.Value, false);
                if (DefaultBlock == null)
                    throw new ArgumentException("The specified default " +
                        "block key doesn't refer to an existing block.");
            }
            else
            {
                for (int i = 0; i < blocks.Length && DefaultBlock == null; i++)
                {
                    Block[] variations = blocks[i];
                    if (variations != null)
                    {
                        for (int v = 0; v < variations.Length; v++)
                        {
                            Block defaultBlockCandidate = variations[v];
                            if (defaultBlockCandidate != null)
                            {
                                DefaultBlock = defaultBlockCandidate;
                                break;
                            }
                        }
                    }
                }
            }

            Texture = texture;
        }

        public bool ContainsBlock(BlockKey key)
        {
            return GetBlock(key, false) != null;
        }

        public Block GetBlock(string identifier, 
            bool returnDefaultErrorBlockOnError = true)
        {
            if (namedBlocks.TryGetValue(identifier, out Block block))
                return block;
            else if (returnDefaultErrorBlockOnError) return DefaultBlock;
            else return null;
        }

        public Block GetBlock(BlockKey blockKey, 
            bool returnDefaultBlockOnError = true)
        {
            ushort id = blockKey.Id, variation = blockKey.Variation;

            if (id >= 0 && id < blocks.Length)
            {
                Block[] blockVariations = blocks[id];
                if (blockVariations != null &&
                    variation >= 0 && variation < blockVariations.Length)
                {
                    Block block = blockVariations[variation];
                    if (block != null) return block;
                }
            }

            if (returnDefaultBlockOnError) return DefaultBlock;
            else return null;
        }

        public Block GetBlock(BlockVoxel voxel, 
            bool returnDefaultBlockOnError = true)
        {
            return GetBlock(voxel.BlockKey, returnDefaultBlockOnError);
        }
    }
}
