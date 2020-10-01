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

using ShamanTK.Graphics;
using ShamanTK.IO;
using GameCraft.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GameCraft.BlockChunk
{
    class BlockChunkManager : IChunkManager<BlockVoxel>
    {
        public const string ChunkFileExtension = "chunk";

        public BlockRegistry BlockRegistry { get; }

        public ResourceManager Resources { get; }

        public TextureBuffer BlockTexture { get; private set; }

        private readonly FileSystemPath saveRootDirectoryPath;

        public BlockChunkManager(FileSystemPath saveRootDirectoryPath,
            BlockRegistry blockRegistry,
            ResourceManager applicationResourceManager)
        {
            BlockRegistry = blockRegistry ??
                throw new ArgumentNullException(nameof(blockRegistry));
            Resources = applicationResourceManager ??
                throw new ArgumentNullException(
                    nameof(applicationResourceManager));
            try
            {
                if (saveRootDirectoryPath.IsEmpty)
                    throw new Exception("The path is empty.");
                if (!saveRootDirectoryPath.IsAbsolute)
                    throw new Exception("The path is not absolute.");
                if (!saveRootDirectoryPath.IsDirectoryPath)
                    throw new Exception("The path references a file, not a " +
                        "directory.");

                this.saveRootDirectoryPath = saveRootDirectoryPath;

                ProbeSaveDirectory();                
            }
            catch (Exception exc)
            {
                throw new ArgumentException("The specified save root path " +
                    "is invalid.", exc);
            }            

            if (blockRegistry.HasTexture)
                Resources.LoadTexture(blockRegistry.Texture,
                    TextureFilter.Nearest).AddFinalizer(r => BlockTexture = r);
        }

        public IChunkBehaviour<BlockVoxel> CreateBehaviour(
            Chunk<BlockVoxel> chunk)
        {
            if (chunk == null)
                throw new ArgumentNullException(nameof(chunk));
            return new BlockChunkBehaviour(chunk, this);
        }

        public ISet<Vector3I> CheckoutRegistry()
        {
            HashSet<Vector3I> offsets = new HashSet<Vector3I>();

            if (Resources.FileSystem.ExistsDirectory(saveRootDirectoryPath))
            {
                foreach (var element in
                    Resources.FileSystem.Enumerate(saveRootDirectoryPath))
                {
                    if (!element.IsDirectoryPath &&
                        element.GetFileExtension() == ChunkFileExtension)
                    {
                        string fileName = element.GetFileName(true);
                        if (Vector3I.TryParse(fileName, out Vector3I offset))
                            offsets.Add(offset);
                    }
                }
            }

            return offsets;
        }

        public void BeginPageData(Vector3I offset, BlockVoxel[,,] data, 
            Action onSuccess, Action<Exception> onFailure)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null)
                throw new ArgumentNullException(nameof(onFailure));
            if (data.Rank != 3)
                throw new ArgumentException("The rank of the specified data " +
                    "array was invalid.");

            int width = data.GetLength(0);
            int height = data.GetLength(1);
            int depth = data.GetLength(2);
            if (width != height || height != depth)
                throw new ArgumentException("The specified data array is " +
                    "not cubic.");

            Action commitDataAsync = delegate ()
            {
                try
                {
                    ProbeSaveDirectory();
                    FileSystemPath chunkFilePath = GetChunkFilePath(offset);

                    //True if at least one voxel had a non-default block key.
                    bool containedNonDefaultBlocks = false;

                    using (Stream stream = Resources.FileSystem.CreateFile(
                            chunkFilePath, true))
                    {
                        stream.WriteSignedInteger(data.GetLength(0));
                        for (int x = 0; x < width; x++)
                        {
                            for (int y = 0; y < height; y++)
                            {
                                for (int z = 0; z < depth; z++)
                                {
                                    BlockVoxel voxel = data[x, y, z];
                                    stream.Write(voxel);

                                    if (voxel.BlockKey != default)
                                        containedNonDefaultBlocks = true;
                                }
                            }
                        }
                    }

                    //Removes empty chunk files to keep the registry clean.
                    if (!containedNonDefaultBlocks)
                        Resources.FileSystem.Delete(chunkFilePath, false);

                    onSuccess();
                }
                catch (Exception exc) { onFailure(exc); }
            };

            Task.Run(commitDataAsync);
        }

        public void BeginCheckoutData(Vector3I offset, 
            Action<BlockVoxel[,,]> onSuccess, Action<Exception> onFailure)
        {
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onFailure == null)
                throw new ArgumentNullException(nameof(onFailure));

            Action checkoutDataAsync = delegate ()
            {
                try
                {
                    using (Stream stream = Resources.FileSystem.OpenFile(
                        GetChunkFilePath(offset), false))
                    {
                        int sideLength = stream.ReadSignedInteger();
                        if (sideLength <= 0 || sideLength >
                            Chunk<BlockVoxel>.SideLengthMaximum)
                            throw new FormatException("The side length of " +
                                "the chunk in the file is invalid.");

                        BlockVoxel[,,] data = new BlockVoxel[sideLength,
                            sideLength, sideLength];

                        for (int x = 0; x < sideLength; x++)
                            for (int y = 0; y < sideLength; y++)
                                for (int z = 0; z < sideLength; z++)
                                    data[x, y, z] = stream.Read<BlockVoxel>();

                        onSuccess(data);
                    }
                }
                catch (Exception exc) { onFailure(exc); }
            };

            Task.Run(checkoutDataAsync);
        }

        /// <summary>
        /// Ensures that the target <see cref="saveRootDirectoryPath"/> exists.
        /// </summary>
        /// <exception cref="Exception">
        /// Is thrown when checking the directory existance or creating a 
        /// non-existant directory fails.
        /// </exception>
        private void ProbeSaveDirectory()
        {
            try
            {
                if (!Resources.FileSystem.ExistsDirectory(saveRootDirectoryPath))
                    Resources.FileSystem.CreateDirectory(saveRootDirectoryPath);
            }
            catch (Exception exc)
            {
                throw new Exception("The save directory couldn't be accessed.",
                    exc);
            }
        }

        private FileSystemPath GetChunkFilePath(Vector3I chunkOffset)
        {
            return FileSystemPath.Combine(saveRootDirectoryPath,
                FileSystemPath.CombineFileName(chunkOffset.ToString(),
                ChunkFileExtension));
        }
    }
}
