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

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.IO;
using GameCraft.Common;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace GameCraft.BlockChunk
{
    class BlockChunkBehaviour : IChunkBehaviour<BlockVoxel>
    {
        public bool ViewAvailable
        {
            get => viewAvailable;
            set => targetViewAvailable = value;
        }
        private bool viewAvailable = false;
        private bool targetViewAvailable = false;

        private readonly BlockChunkManager manager;

        private Dictionary<Vector3I, Block> lightSources =
            new Dictionary<Vector3I, Block>();

        private bool meshRecalculationRequired;
        private bool meshRecalculationInProgress = false;

        private readonly Chunk<BlockVoxel> chunk;
        private readonly BlockRegistry registry;        

        private MeshBuffer mesh;

        public BlockChunkBehaviour(Chunk<BlockVoxel> chunk,
            BlockChunkManager manager)
        {
            this.chunk = chunk ??
                throw new ArgumentNullException(nameof(chunk));
            this.manager = manager ??
                throw new ArgumentNullException(nameof(chunk));
            registry = manager.BlockRegistry;
        }

        private void ClearView(bool resetViewAvailable)
        {
            lightSources.Clear();
            mesh?.Dispose();
            mesh = null;
            meshRecalculationRequired = false;

            if (resetViewAvailable)
            {
                viewAvailable = false;
                targetViewAvailable = false;
            }
        }

        public bool TriggerRefreshView()
        {
            if (ViewAvailable)
            {
                meshRecalculationRequired = true;
                return true;
            }
            else return false;
        }

        public void OnVoxelsCleared()
        {
            ClearView(false);
        }

        public void OnDataPaged()
        {
            ClearView(true);
        }

        public void OnDataCheckout()
        {
            lightSources.Clear();

            for (int x = 0; x < chunk.SideLength; x++)
            {
                for (int y = 0; y < chunk.SideLength; y++)
                {
                    for (int z = 0; z < chunk.SideLength; z++)
                    {
                        Vector3I position = new Vector3I(x, y, z);
                        if (chunk.TryGetVoxel(position, out BlockVoxel voxel))
                        {
                            Block block = registry.GetBlock(voxel.BlockKey);
                            if (block.Properties.Luminance > 0)
                                lightSources.Add(position, block);
                        }
                    }
                }
            }

            meshRecalculationRequired = true;
        }

        public void OnVoxelModified(Vector3I position, 
            BlockVoxel currentValue, BlockVoxel previousValue)
        {
            Block currentBlock = registry.GetBlock(currentValue.BlockKey);
            Block previousBlock = registry.GetBlock(previousValue.BlockKey);

            bool currentValueIsLightSource = 
                currentBlock.Properties.Luminance > 0;
            bool previousValueIsLightSource = 
                previousBlock.Properties.Luminance > 0;

            bool lightSourcesChanged = 
                currentValueIsLightSource != previousValueIsLightSource;

            if (lightSourcesChanged)
            {
                if (currentValueIsLightSource) 
                    lightSources.Add(position, currentBlock);
                else lightSources.Remove(position);
            }

            meshRecalculationRequired = true;
        }

        public void OnRedraw<RenderContextT>(RenderContextT context)
            where RenderContextT : IRenderContext
        {
            if (mesh != null && manager.BlockTexture != null)
            {
                context.Mesh = mesh;
                context.Transformation = Matrix4x4.CreateTranslation(
                    chunk.Offset.X, chunk.Offset.Y, chunk.Offset.Z);
                context.Texture = manager.BlockTexture;
                context.Draw();
            }
        }

        public void OnUpdate(TimeSpan delta)
        {
            if (viewAvailable && !targetViewAvailable)
            {
                ClearView(true);
#if VERBOSE_LOGGING
                Log.Trace("Chunk [" + chunk.Offset + "] mesh discarded.");
#endif
            }
            else if (!meshRecalculationInProgress &&
                chunk.Availability >= ChunkAvailability.DataOnly
                && (!viewAvailable || meshRecalculationRequired)
                && targetViewAvailable)
            {
                meshRecalculationInProgress = true;
                GenerateChunkMeshAsync(chunk).AddFinalizer(
                    OnViewMadeAvailableSuccess, OnViewMadeAvailableFailed);
            }
        }

        private SyncTask<MeshBuffer> GenerateChunkMeshAsync(
            Chunk<BlockVoxel> chunk)
        {
            return manager.Resources.LoadMesh(delegate ()
            {
                DateTime start = DateTime.Now;
                ChunkMeshBuilder chunkMeshBuilder = new ChunkMeshBuilder(chunk,
                    manager.BlockRegistry);
                MeshData chunkMesh = chunkMeshBuilder.GenerateMesh();

#if VERBOSE_LOGGING
                Log.Trace("Chunk [" + chunk.Offset + "] mesh generation " +
                    "finished (" + (DateTime.Now - start).TotalMilliseconds
                    + "ms).");
#endif
                return chunkMesh;
            });
        }

        private void OnViewMadeAvailableSuccess(MeshBuffer mesh)
        {
            this.mesh?.Dispose();
            this.mesh = mesh;
            meshRecalculationInProgress = false;
            meshRecalculationRequired = false;

            targetViewAvailable = viewAvailable = true;
        }

        private void OnViewMadeAvailableFailed(Exception exc)
        {
            meshRecalculationInProgress = false;
            meshRecalculationRequired = false;

            targetViewAvailable = viewAvailable = false;

            chunk.Lock();

            Log.Error("Chunk [" + chunk.Offset + "] mesh generation failed.", 
                exc);
        }

        public void Dispose()
        {
            ClearView(true);
        }
    }
}
