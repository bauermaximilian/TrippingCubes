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
using System.Reflection;
using System.Threading;
using System.Xml;
using TrippingCubes.Common;
using TrippingCubes.Entities;
using TrippingCubes.Physics;
using TrippingCubes.World;

namespace TrippingCubes
{
    class GameWorld : IDisposable
    {
        private const float AvailabilityDistanceChangeTreshold = 16;
        private const float DistanceForAvailabilityDataOnly = 48;
        private const float DistanceForAvailabilityNone = 64;

        public ModelCache ModelCache { get; }

        public ResourceManager Resources { get; }

        public PhysicsSystem Physics { get; }

        public BlockRegistry Blocks { get; }

        public Chunk<BlockVoxel> RootChunk { get; }

        public Camera Camera { get; }

        public IReadOnlyCollection<IEntity> Entities { get; }

        public TrippingCubesGame Game { get; }

        private MeshBuffer skyboxMesh;
        private TextureBuffer skyboxTextureInner;
        private TextureBuffer skyboxTextureOuter;
        private bool isDisposed;

        private readonly List<IEntity> entities = new List<IEntity>();

        public GameWorld(TrippingCubesGame game, ResourceManager resources, 
            Camera camera)
        {
            Game = game;

            Resources = resources;

            Entities = new ReadOnlyCollection<IEntity>(entities);

            ModelCache = new ModelCache(resources);

            Physics = new PhysicsSystem(IsBlockSolid, IsBlockLiquid);

            Camera = camera;

            DateTime startTime = DateTime.Now;
            BlockRegistryBuilder blockRegistryBuilder =
                BlockRegistryBuilder.FromXml("/Vaporwave/registry.xml",
                resources.FileSystem, false);
            Blocks = blockRegistryBuilder.GenerateRegistry(
                resources.FileSystem);
            Log.Trace("Block registry with " + Blocks.Count +
                " block definitions loaded in " +
                (DateTime.Now - startTime).TotalMilliseconds + "ms.");

            IFileSystem userDataFileSystem = 
                FileSystem.CreateUserDataFileSystem("TrippingCubes");
            FileSystemPath worldSubPath = "/world/";

            RootChunk = new Chunk<BlockVoxel>(
                new BlockChunkManager(Blocks, resources,
                userDataFileSystem, worldSubPath));

            resources.LoadMesh(MeshData.Skybox).Subscribe(
                r => skyboxMesh = r);
            resources.LoadTexture("/Vaporwave/skybox-inner.png",
                TextureFilter.Linear).Subscribe(
                r => skyboxTextureInner = r);
            resources.LoadTexture("/Vaporwave/skybox-outer.png",
                TextureFilter.Linear).Subscribe(
                r => skyboxTextureOuter = r);

            try
            {
                FileSystemPath entitiesDefinitionsPath =
                    FileSystemPath.Combine(worldSubPath, "entities.xml");
                if (userDataFileSystem.ExistsFile(entitiesDefinitionsPath))
                {
                    int spawnedEntities =
                        SpawnFromFile(userDataFileSystem,
                        entitiesDefinitionsPath);

                    Log.Information($"Spawned {spawnedEntities} entities.");
                }
                else
                {
                    Log.Information("No entity definitions file was found.");
                }
            }
            catch (Exception exc)
            {
                Log.Warning("Failed spawning entities from file.", exc);
            }
        }

        public void Redraw(IRenderContext context)
        {
            context.Mesh = skyboxMesh;

            context.Texture = skyboxTextureOuter;
            context.Transformation = MathHelper.CreateTransformation(
                Camera.Position,
                new Vector3(Camera.ClippingRange.Y - 100),
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Angle.Deg(45)));
            context.Draw();

            context.Texture = skyboxTextureInner;
            context.Transformation = MathHelper.CreateTransformation(
                Camera.Position, new Vector3((
                Camera.ClippingRange.Y - 100) * 0.65f),
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
                    (Camera.Position - chunkCenter).Length();

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

        public int SpawnFromFile(IFileSystem fileSystem,
            FileSystemPath definitionsFilePath)
        {            
            XmlDocument document = new XmlDocument();

            try
            {
                using Stream stream = fileSystem.OpenFile(definitionsFilePath, 
                    false);
                document.Load(stream);

                if (document.DocumentElement.Name != "Entities")
                    throw new Exception("The document element name is " +
                        "invalid!");
            }
            catch (Exception exc)
            {
                throw new Exception("The entity definitions couldn't be " +
                    "loaded.", exc);
            }

            static IEnumerable<KeyValuePair<string,string>> GetNodeEnumerator(
                XmlNode node)
            {
                foreach (XmlNode parameterNode in node.ChildNodes)
                {
                    yield return new KeyValuePair<string, string>(
                        parameterNode.Name, parameterNode.InnerText);
                }
            }

            int entities = 0;
            foreach (XmlNode entityNode in document.DocumentElement.ChildNodes)
            {
                string typeName = entityNode.Name;
                var parameters = GetNodeEnumerator(entityNode);
                if (!TrySpawnEntity(typeName, parameters, out IEntity entity))
                {
                    Log.Warning($"The entity of type {typeName} couldn't be " +
                        $"spawned.");
                }
                else
                {
                    entities++;
                }
            }
            return entities;
        }

        public bool TrySpawnEntity(string typeName,
            IEnumerable<KeyValuePair<string, string>> parameters,
            out IEntity entity)
        {
            Type type = Assembly.GetExecutingAssembly().GetType(
                $"TrippingCubes.Entities.{typeName}");
            if (type != null && typeof(IEntity).IsAssignableFrom(type))
            {
                ConstructorInfo constructor = 
                    type.GetConstructor(new Type[] { typeof(GameWorld) });

                if (constructor != null)
                {
                    entity = (IEntity)constructor.Invoke(
                        new object[] { this });
                    try { entity.ApplyParameters(parameters); }
                    catch (Exception exc)
                    {
                        Log.Warning("Some of the parameters for an entity " +
                            $"of type {typeName} couldn't be assigned." +
                            "The entity was spawned anyways.", exc);
                    }
                    entities.Add(entity);
                    return true;
                }
            }

            entity = null;
            return false;
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
                    bool isSolid = !voxelBlock.Properties.IsTranslucent;
                    return isSolid;
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
