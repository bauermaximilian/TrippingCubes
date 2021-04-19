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

using ShamanTK;
using ShamanTK.Common;
using ShamanTK.Graphics;
using ShamanTK.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
using System.Threading;
using TrippingCubes.Common;
using TrippingCubes.Entities;
using TrippingCubes.Entities.Analytics;
using TrippingCubes.Physics;
using TrippingCubes.World;

namespace TrippingCubes
{
    class GameWorld : IDisposable
    {
        private const float AvailabilityDistanceChangeTreshold = 16;
        private const float DistanceForAvailabilityDataOnly = 48;
        private const float DistanceForAvailabilityNone = 64;

        public ResourceManager Resources { get; }

        public PhysicsSystem Physics { get; }

        public BlockRegistry Blocks { get; }

        public Chunk<BlockVoxel> RootChunk { get; }

        public IReadOnlyCollection<IEntity> Entities { get; }

        public TrippingCubesGame Game { get; }

        public IReadOnlyDictionary<string, PathLinear> Paths { get; }

        private IFileSystem FileSystem => Game.Resources.FileSystem;

        private MeshBuffer skyboxMesh;
        private TextureBuffer skyboxTexture;
        private bool isDisposed;

        private readonly List<IEntity> entities = new List<IEntity>();
        private readonly ZipFileSystem zipFileSystem;

        private readonly List<CharacterProtocol> characterProtocols =
            new List<CharacterProtocol>();

        public GameWorld(GameWorldConfiguration configuration,
            FileSystemPath worldFilePath, TrippingCubesGame game)
        {
            DateTime startTime = DateTime.Now;

            Game = game;

            Entities = new ReadOnlyCollection<IEntity>(entities);

            Physics = new PhysicsSystem(IsBlockSolid, IsBlockLiquid);

            Paths = new ReadOnlyDictionary<string, PathLinear>(
                configuration.Paths ?? new Dictionary<string, PathLinear>());

            Log.Trace("Initializing world style...");
            try
            {
                Game.Resources.LoadMesh(MeshData.Skybox).Subscribe(
                    r => skyboxMesh = r);
                Game.Resources.LoadTexture(configuration.SkyboxPath,
                    TextureFilter.Linear).Subscribe(
                    r => skyboxTexture = r);
            }
            catch (Exception exc)
            {
                Log.Warning("The skybox couldn't be loaded and will not " +
                    "be available.", exc);
            }
            try
            {
                DateTime localStartTime = DateTime.Now;
                BlockRegistryBuilder blockRegistryBuilder =
                    BlockRegistryBuilder.FromXml(
                        configuration.BlockRegistryPath,
                        FileSystem, false);
                Blocks = blockRegistryBuilder.GenerateRegistry(
                    FileSystem);
                Log.Trace("Block registry with " + Blocks.Count +
                    " block definitions initialized in " +
                    (DateTime.Now - localStartTime).TotalMilliseconds + "ms.");
            }
            catch (Exception exc)
            {
                throw new Exception("The block registry couldn't be " +
                    "initialized.", exc);
            }


            Log.Trace("Initializing world chunk manager...");
            try
            {
                FileSystemPath primaryWorldBackupPath =
                    FileSystemPath.Combine(worldFilePath.GetParentDirectory(),
                    $"{worldFilePath.GetFileName(false)}1");
                FileSystemPath secondaryWorldBackupPath =
                    FileSystemPath.Combine(worldFilePath.GetParentDirectory(),
                    $"{worldFilePath.GetFileName(false)}2");

                if (FileSystem.IsWritable)
                {
                    try
                    {
                        if (FileSystem.ExistsFile(primaryWorldBackupPath))
                        {
                            using Stream secondaryBackupStream =
                                FileSystem.CreateFile(secondaryWorldBackupPath,
                                true);
                            using Stream primaryWorldBackup =
                                FileSystem.OpenFile(primaryWorldBackupPath,
                                false);

                            primaryWorldBackup.CopyTo(secondaryBackupStream);
                        }

                        if (FileSystem.ExistsFile(worldFilePath))
                        {
                            using Stream primaryWorldBackup =
                                FileSystem.CreateFile(primaryWorldBackupPath,
                                true);
                            using Stream worldFile =
                                FileSystem.OpenFile(worldFilePath, false);

                            worldFile.CopyTo(primaryWorldBackup);
                        }
                    }
                    catch (Exception exc)
                    {
                        throw new Exception("The world backups couldn't be " +
                            "updated.", exc);
                    }
                }

                if (FileSystem.ExistsFile(worldFilePath))
                {
                    zipFileSystem = new ZipFileSystem(FileSystem.OpenFile(
                        worldFilePath, FileSystem.IsWritable));
                }
                else if (FileSystem.IsWritable)
                {
                    Log.Information("No world data file was found at the " +
                        "specified path. A new world file is initialized.");
                    zipFileSystem = ZipFileSystem.Initialize(FileSystem, 
                        worldFilePath, false);
                }
                else throw new Exception("The world data file doesn't exist " +
                    $"under the path {worldFilePath} and can't be created, " +
                    "as the file system is read-only.");

                RootChunk = new Chunk<BlockVoxel>(
                    new BlockChunkManager(Blocks, Game.Resources,
                    zipFileSystem, "/"));
            }
            catch (Exception exc)
            {
                throw new Exception("The world chunk manager couldn't be " +
                    "initialized.", exc);
            }


            Log.Trace("Spawning entities...");
            foreach (EntityInstantiation instantiation in
                configuration.Entities)
            {
                if (configuration.EntityConfigurations.TryGetValue(
                    instantiation.ConfigurationIdentifier,
                    out EntityConfiguration entityConfiguration))
                {
                    try
                    {
                        IEntity entity = entityConfiguration.Instantiate(this,
                            instantiation.InstanceParameters);
                        entities.Add(entity);

                        if (entity is ICharacter characterEntity &&
                            !string.IsNullOrWhiteSpace(characterEntity.Name))
                        {
                            characterProtocols.Add(new CharacterProtocol(
                                characterEntity));
                        }
                    }
                    catch (Exception exc)
                    {
                        Log.Warning($"Entity #{entities.Count} couldn't " +
                            "be spawned and will be skipped.", exc);
                    }
                }
            }
            Log.Trace($"Spawned {entities.Count} entities.");

            Log.Trace("Game world initialized in " +
                (DateTime.Now - startTime).TotalMilliseconds + "ms.");
        }

        public void Redraw(IRenderContext context)
        {
            context.Mesh = skyboxMesh;

            /*
            // Former "outer" skybox
            context.Texture = skyboxTextureOuter;
            context.Transformation = MathHelper.CreateTransformation(
                Game.Camera.Position,
                new Vector3(Game.Camera.ClippingRange.Y - 100),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angle.Deg(45)));
            context.Draw();
            */

            context.Texture = skyboxTexture;
            context.Transformation = MathHelper.CreateTransformation(
                Game.Camera.Position, new Vector3((
                Game.Camera.ClippingRange.Y - 100) * 0.65f),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angle.Deg(45)));
            context.Opacity = MathHelper.CalculateTimeSine(0.5, 0.05) + 0.95f;
            context.Draw();

            context.Opacity = 1;

            context.Fog = new Fog(20, 10, Color.TransparentWhite, false);

            foreach (var chunk in RootChunk) chunk.Redraw(context);

            foreach (var entity in entities) entity.Redraw(context);
        }

        public void Update(TimeSpan delta)
        {
            foreach (var chunk in RootChunk)
            {
                Vector3 chunkCenter = new Vector3(chunk.Offset.X +
                   chunk.SideLength / 2.0f, chunk.Offset.Y +
                   chunk.SideLength / 2.0f, chunk.Offset.Z +
                   chunk.SideLength / 2.0f);

                float distanceFromCamera =
                    (Game.Camera.Position - chunkCenter).Length();

                if (distanceFromCamera < DistanceForAvailabilityDataOnly)
                    chunk.ChangeAvailability(ChunkAvailability.Full);
                else if (distanceFromCamera > (DistanceForAvailabilityDataOnly
                    + AvailabilityDistanceChangeTreshold) &&
                    (distanceFromCamera < DistanceForAvailabilityNone))
                    chunk.ChangeAvailability(ChunkAvailability.DataOnly);
                else if (distanceFromCamera > (DistanceForAvailabilityNone +
                    AvailabilityDistanceChangeTreshold))
                    chunk.ChangeAvailability(ChunkAvailability.None);

                chunk.Update(delta);
            }

            foreach (var entity in entities) entity.Update(delta);

            Physics.Update(delta);
        }

        public void Unload()
        {
            if (isDisposed) return;

            if (RootChunk != null)
            {
                Log.Trace("Saving and closing world...");
                foreach (var chunk in RootChunk)
                {
                    chunk.Unlock();
                    chunk.ChangeAvailability(ChunkAvailability.None);
                    chunk.Update(TimeSpan.Zero);
                }

                bool unsavedChunksLeft = false;
                do
                {
                    Thread.Sleep(1000);
                    foreach (var chunk in RootChunk)
                    {
                        chunk.Update(TimeSpan.Zero);
                        unsavedChunksLeft |=
                            chunk.Availability != ChunkAvailability.None;
                    }
                } while (unsavedChunksLeft);

                Log.Trace("All chunks saved.");
            }

            Log.Trace("Saving analytics...");
            try
            {
                foreach (var protocol in characterProtocols)
                    protocol.Save(FileSystem, "/Analytics/");

                Log.Trace("Analytics data saved successfully.");
            }
            catch (Exception exc)
            {
                Log.Error("The analytics data couldn't be saved.", exc);
            }

            skyboxTexture?.Dispose();
            skyboxMesh?.Dispose();
            zipFileSystem?.Dispose();

            isDisposed = true;
        }

        public void SetArea(Vector3I start, Vector3I end, BlockVoxel voxel)
        {
            GetAreaFromPoints(start, end, out Vector3I areaStart,
                out Vector3I areaScale);

            RootChunk.TraverseToChunk(areaStart, true, out var startChunk);
            RootChunk.TraverseToChunk(areaStart + areaScale, true,
                out var endChunk);
            int chunkSideLength = RootChunk.SideLength;
            Vector3I wholeChunkArea = Vector3I.One +
                ((endChunk.Offset - startChunk.Offset) / chunkSideLength);

            Chunk<BlockVoxel>[,,] chunkCache = new Chunk<BlockVoxel>[
                wholeChunkArea.X, wholeChunkArea.Y, wholeChunkArea.Z];
            chunkCache[0, 0, 0] = startChunk;
            chunkCache[chunkCache.GetLength(0) - 1,
                chunkCache.GetLength(1) - 1,
                chunkCache.GetLength(2) - 1] = endChunk;

            Chunk<BlockVoxel> currentChunk;

            for (int x = 0; x < areaScale.X; x++)
            {
                for (int y = 0; y < areaScale.Y; y++)
                {
                    for (int z = 0; z < areaScale.Z; z++)
                    {
                        Vector3I offset = new Vector3I(x, y, z);
                        Vector3I cacheOffset =
                            ((areaStart - startChunk.Offset) + offset) /
                            chunkSideLength;

                        if (chunkCache[cacheOffset.X, cacheOffset.Y,
                            cacheOffset.Z] == null)
                        {
                            RootChunk.TraverseToChunk(areaStart + offset,
                                true, out chunkCache[cacheOffset.X,
                                cacheOffset.Y, cacheOffset.Z]);
                        }

                        currentChunk = chunkCache[cacheOffset.X,
                                cacheOffset.Y, cacheOffset.Z];

                        if (currentChunk.Availability !=
                            ChunkAvailability.None)
                        {
                            currentChunk.Unlock();
                            currentChunk.SetVoxel(areaStart + offset -
                                currentChunk.Offset, voxel);
                        }
                    }
                }
            }
        }

        public static void GetAreaFromPoints(Vector3I a, Vector3I b,
            out Vector3I areaPosition, out Vector3I areaScale)
        {
            areaPosition = new Vector3I(Math.Min(a.X, b.X),
                Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
            areaScale = new Vector3I(Math.Max(a.X, b.X),
                Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z)) + Vector3I.One -
                areaPosition;
        }

        private bool IsBlockSolid(Vector3 position)
        {
            if (RootChunk.TraverseToChunk((Vector3I)position, false,
                    out var chunk))
            {
                if (chunk.TryGetVoxel(position - chunk.Offset,
                    out var voxel, false))
                {
                    var voxelBlock = Blocks.GetBlock(voxel.BlockKey);
                    return voxelBlock.Properties.Type ==
                        BlockColliderType.Solid;
                }
                else return false;
            }
            else return false;
        }

        private bool IsBlockLiquid(Vector3 position)
        {
            return false;
        }

        public void Dispose()
        {
            Unload();
        }
    }
}
