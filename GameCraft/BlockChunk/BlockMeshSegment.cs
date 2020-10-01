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

using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameCraft.BlockChunk
{
    class BlockMeshSegment
    {
        private readonly Vertex[] vertices;

        private readonly Face[] faces;

        public BlockMeshSegment(Vertex[] vertices, Face[] faces)
        {
            this.vertices = vertices ??
                throw new ArgumentNullException(nameof(vertices));
            this.faces = faces ??
                throw new ArgumentNullException(nameof(faces));
        }

        public void AppendVerticesTo(List<Vertex> vertices,
            Vector3 vertexPositionOffset, Rectangle textureClipping)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            foreach (Vertex vertex in this.vertices)
            {
                Vector2 textureCoordinate =
                    vertex.TextureCoordinate * textureClipping.Size
                    + textureClipping.BottomLeft;

                vertices.Add(new Vertex(
                    vertex.Position + vertexPositionOffset,
                    vertex.Normal, textureCoordinate,
                    vertex.Properties));
            }
        }

        public void AppendFacesTo(List<Face> faces,
            uint vertexIndexOffset)
        {
            if (faces == null)
                throw new ArgumentNullException(nameof(faces));

            foreach (Face face in this.faces)
            {
                faces.Add(
                    new Face(face.FirstVertexIndex + vertexIndexOffset,
                    face.SecondVertexIndex + vertexIndexOffset,
                    face.ThirdVertexIndex + vertexIndexOffset));
            }
        }

        public void AppendTo(List<Vertex> vertices,
            List<Face> faces, Vector3 vertexPositionOffset,
            Rectangle textureClipping)
        {
            uint vertexIndexOffset = (uint)vertices.Count;
            AppendVerticesTo(vertices, vertexPositionOffset,
                textureClipping);
            AppendFacesTo(faces, vertexIndexOffset);
        }
    }
}
