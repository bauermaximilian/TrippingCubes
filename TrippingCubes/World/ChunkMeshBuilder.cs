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
using ShamanTK.IO;
using TrippingCubes.Common;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace TrippingCubes.World
{
    class ChunkMeshBuilder
    {
        private readonly Chunk<BlockVoxel> chunk;
        private readonly BlockRegistry registry;

        public ChunkMeshBuilder(Chunk<BlockVoxel> blockChunk, 
            BlockRegistry blockRegistry)
        {
            chunk = blockChunk ?? 
                throw new ArgumentNullException(nameof(blockChunk));
            registry = blockRegistry ??
                throw new ArgumentNullException(nameof(blockRegistry));
        }

        private static void AddBlockMesh(Block block, 
            in Vector3I offset, AdjacentList<bool> visibleDirections, 
            List<Vertex> vertices, List<Face> faces)
        {
            if (block.Mesh == null) return;

            Vector3 offsetF = new Vector3(offset.X, offset.Y, offset.Z);

            if (block.Mesh.Base != null)
                block.Mesh.Base.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.Base ?? default);

            if (block.Mesh.East != null && visibleDirections.East)
                block.Mesh.East.AppendTo(vertices, faces, offsetF, 
                    block.TextureClippings?.East ?? default);
            if (block.Mesh.West != null && visibleDirections.West)
                block.Mesh.West.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.West ?? default);
            if (block.Mesh.Above != null && visibleDirections.Above)
                block.Mesh.Above.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.Above ?? default);
            if (block.Mesh.Below != null && visibleDirections.Below)
                block.Mesh.Below.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.Below ?? default);
            if (block.Mesh.North != null && visibleDirections.North)
                block.Mesh.North.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.North ?? default);
            if (block.Mesh.South != null && visibleDirections.South)
                block.Mesh.South.AppendTo(vertices, faces, offsetF,
                    block.TextureClippings?.South ?? default);
        }

        public MeshData GenerateMesh()
        {
            int sideLength = chunk.SideLength;

            var vertices = new List<Vertex>();
            var faces = new List<Face>();

            var adjVoxels = new AdjacentList<BlockVoxel>();
            var visibleDirections = new AdjacentList<bool>();

            for (int x = 0; x < sideLength; x++)
            {
                for (int y = 0; y < sideLength; y++)
                {
                    for (int z = 0; z < sideLength; z++)
                    {
                        Vector3I position = new Vector3I(x, y, z);

                        chunk.TryGetVoxel(position, out BlockVoxel center);
                        Block centerBlock = registry.GetBlock(center.BlockKey);
                        
                        chunk.TryGetAdjacentVoxels(position, true, 
                            ref adjVoxels);
                        adjVoxels.Convert(ref visibleDirections, v =>
                        registry.GetBlock(v).Properties.IsTranslucent);

                        AddBlockMesh(centerBlock, position,
                            visibleDirections, vertices, faces);
                    }
                }
            }

            if (vertices.Count == 0) vertices.Add(new Vertex(0, 0, 0));
            if (faces.Count == 0) faces.Add(new Face((uint)vertices.Count - 1,
                (uint)vertices.Count - 1, (uint)vertices.Count - 1));

            return MeshData.Create(vertices.ToArray(), faces.ToArray());
        }
    }
}
