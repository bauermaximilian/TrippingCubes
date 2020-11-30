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
using TrippingCubes.Common;
using System;

namespace TrippingCubes.World
{
    class Block
    {
        public static Block Empty { get; }
            = new Block(new BlockKey(0, 0), "empty", 
                new BlockProperties(true, 0));

        public string Identifier { get; }

        public BlockKey Key { get; }

        public BlockProperties Properties { get; }

        public bool HasTextureClippings => TextureClippings != null;

        public bool HasMesh => Mesh != null;

        public AdjacentList<Rectangle> TextureClippings { get; }

        public AdjacentList<BlockMeshSegment> Mesh { get; }

        public Block(BlockKey key, string identifier, 
            BlockProperties properties)
        {
            Key = key;
            Identifier = identifier ??
                throw new ArgumentNullException(nameof(identifier));
            Properties = properties ??
                throw new ArgumentNullException(nameof(properties));
        }

        public Block(BlockKey key, string identifier, 
            BlockProperties parameters,
            AdjacentList<BlockMeshSegment> mesh, 
            AdjacentList<Rectangle> textureClippings) 
            : this(key, identifier, parameters)
        {
            Mesh = mesh;
            TextureClippings = textureClippings;
        }
    }
}
