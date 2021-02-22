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

using TrippingCubes.Common;
using ShamanTK;
using ShamanTK.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TrippingCubes.World
{
    public enum ChunkAvailability
    {
        None,
        DataOnly,
        Full
    }

    public class Chunk<VoxelT> : IEnumerable<Chunk<VoxelT>>, IDisposable
        where VoxelT : unmanaged
    {
        public const int SideLengthDefault = 16;

        public const int SideLengthMaximum = 64;

        public bool HasBehaviour => behaviour != null;

        public ChunkAvailability Availability
        {
            get
            {
                if (data == null) return ChunkAvailability.None;
                else
                {
                    if (behaviour != null && behaviour.ViewAvailable)
                        return ChunkAvailability.Full;
                    else return ChunkAvailability.DataOnly;
                }
            }
        }

        public bool IsAvailabilityChanging => 
            Availability != targetAvailability;

        public bool IsLocked => isLocked;

        private volatile ChunkAvailability targetAvailability = 
            ChunkAvailability.DataOnly;
        private volatile bool isLocked = false;
        private volatile bool dataCommitRunning = false;
        private volatile bool dataClearAfterCommitRequested = false;
        private volatile bool dataCheckoutRunning = false;
        private volatile VoxelT[,,] dataFromCheckout = null;        

        private VoxelT[,,] data;

        private readonly Dictionary<Vector3I, Chunk<VoxelT>> chunkRegistry;
        private readonly AdjacentList<Chunk<VoxelT>>
            adjacentChunks = new AdjacentList<Chunk<VoxelT>>();

        private readonly IChunkBehaviour<VoxelT> behaviour;

        //Only assigned in root chunk.
        private readonly IChunkManager<VoxelT> configuration;

        /// <summary>
        /// Gets the amount of <see cref="VoxelT"/> instances on each axis
        /// of a <see cref="Chunk{VoxelT}"/> instance.
        /// </summary>
        public int SideLength { get; }

        public Vector3I Dimensions { get; }

        /// <summary>
        /// Gets the absolute offset of this chunk to the <see cref="Root"/>
        /// in full voxels. The <see cref="Direction.West"/>, 
        /// <see cref="Direction.Below"/> <see cref="Direction.South"/>
        /// corner of this <see cref="Chunk{VoxelT, PropertyT}"/> instance.
        /// That position (which is also the position of the voxel at position
        /// <see cref="Vector3I"/>).
        /// </summary>
        public Vector3I Offset { get; }

        /// <summary>
        /// Gets the <see cref="Chunk{VoxelT}"/> instance that contains 
        /// this instance as branches.
        /// </summary>
        public Chunk<VoxelT> Root { get; }

        /// <summary>
        /// Gets a value indicating whether the current 
        /// <see cref="Chunk{VoxelT}"/> instance is the root instance
        /// inside this collection of <see cref="Chunk{VoxelT}"/>
        /// instances (<c>true</c>) or not (<c>false</c>).
        /// </summary>
        public bool IsRoot => Root == this;
        
        /// <summary>
        /// 
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        public Chunk(IChunkManager<VoxelT> chunkConfiguration)
            : this(chunkConfiguration, SideLengthDefault) { }

        public Chunk(IChunkManager<VoxelT> chunkConfiguration, 
            int sideLength) : this(sideLength)
        {
            configuration = chunkConfiguration ??
                throw new ArgumentNullException(nameof(chunkConfiguration));
            behaviour = chunkConfiguration.CreateBehaviour(this);

            ISet<Vector3I> existingChunkOffsets = 
                chunkConfiguration.CheckoutRegistry();

            foreach (Vector3I offset in existingChunkOffsets)
            {
                if (offset == Vector3I.Zero) data = null;
                else new Chunk<VoxelT>(this, offset, false);
            }
        }

        public Chunk(int sideLength)
        {
            if (sideLength < 1)
                throw new ApplicationException("The value of the side " +
                    "length constant is invalid.");
            else SideLength = sideLength;

            Dimensions = new Vector3I(SideLength, SideLength,
                SideLength);

            Root = this;

            chunkRegistry = new Dictionary<Vector3I, Chunk<VoxelT>>
            { { Vector3I.Zero, this } };

            data = new VoxelT[SideLength, SideLength, SideLength];
        }

        public Chunk() : this(SideLengthDefault) { }

        private Chunk(Chunk<VoxelT> root, Vector3I offset, bool initializeData)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            Offset = offset;
            SideLength = root.SideLength;

            Dimensions = new Vector3I(SideLength, SideLength,
                SideLength);

            chunkRegistry = root.chunkRegistry;
            
            behaviour = root.configuration.CreateBehaviour(this);

            if (initializeData)
                data = new VoxelT[SideLength, SideLength, SideLength];
            else data = null;

            RegisterChunk();
        }

        private void RegisterChunk()
        {
            if (!chunkRegistry.ContainsKey(Offset))
            {
                //First, the current chunk is added to the registry (in the 
                //root chunk via the copied reference from the constructor) 
                //with the current chunk offset.
                chunkRegistry[Offset] = this;

                //Then, any possible "gaps" between this new chunk and possible
                //adjacent chunks (adjacent in the space of this chunk system)
                //are closed by adding a reference to this chunk to any chunk
                //that is actually located next to this chunk.
                //Other methods for traversing through the chunk system
                //rely on that - the connection is removed when a chunk is
                //unregistered.
                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.East * SideLength, out var chunkEast))
                {
                    adjacentChunks.East = chunkEast;
                    chunkEast.adjacentChunks.West = this;                    
                }
                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.West * SideLength, out var chunkWest))
                {
                    adjacentChunks.West = chunkWest;
                    chunkWest.adjacentChunks.East = this;                    
                }

                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.Above * SideLength, out var chunkAbove))
                {
                    adjacentChunks.Above = chunkAbove;
                    chunkAbove.adjacentChunks.Below = this;
                }
                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.Below * SideLength, out var chunkBelow))
                {
                    adjacentChunks.Below = chunkBelow;
                    chunkBelow.adjacentChunks.Above = this;
                }

                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.North * SideLength, out var chunkNorth))
                {
                    adjacentChunks.North = chunkNorth;
                    chunkNorth.adjacentChunks.South = this;
                }
                if (chunkRegistry.TryGetValue(
                    Offset + Vector3I.South * SideLength, out var chunkSouth))
                {
                    adjacentChunks.South = chunkSouth;
                    chunkSouth.adjacentChunks.North = this;
                }
            }
            else throw new InvalidOperationException("The offset of the " +
                "current chunk is already occupied by another chunk with " +
                "the same root.");
        }

        private void UnregisterChunk()
        {
            if (chunkRegistry.Remove(Offset) && !IsDisposed)
            {
                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.East * SideLength, out var chunkEast))
                    chunkEast.adjacentChunks.West = null;
                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.West * SideLength, out var chunkWest))
                    chunkWest.adjacentChunks.East = null;

                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.Above * SideLength, out var chunkAbove))
                    chunkAbove.adjacentChunks.Below = null;
                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.Below * SideLength, out var chunkBelow))
                    chunkBelow.adjacentChunks.Above = null;

                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.North * SideLength, out var chunkNorth))
                    chunkNorth.adjacentChunks.South = null;
                if (chunkRegistry.TryGetValue(
                    Offset - Vector3I.South * SideLength, out var chunkSouth))
                    chunkSouth.adjacentChunks.North = null;

                adjacentChunks.Clear();
            } else throw new InvalidOperationException("The current chunk " +
                "is not registered at the current offset in the root chunk " +
                "or is disposed already.");
        }

        public static Vector3I GetChunkOffsetFromPosition(in Vector3I position, 
            int chunkSideLength)
        {
            return new Vector3I(
                (int)Math.Ceiling((position.X - chunkSideLength + 1) / 
                    (float)chunkSideLength) * chunkSideLength,
                (int)Math.Ceiling((position.Y - chunkSideLength + 1) /
                    (float)chunkSideLength) * chunkSideLength,
                (int)Math.Ceiling((position.Z - chunkSideLength + 1) /
                    (float)chunkSideLength) * chunkSideLength);
        }

        private Vector3I GetChunkOffsetFromDirection(Direction direction)
        {
            return Vector3I.FromDirection(direction) * SideLength + Offset;
        }

        public bool Encloses(Vector3 position)
        {
            return position >= Offset && position < Offset + Dimensions;
        }

        public bool TraverseToChunk(Direction direction, 
            bool createNonExistantChunk, out Chunk<VoxelT> chunk)
        {
            chunk = adjacentChunks[direction];

            if (chunk != null) return true;
            else if (createNonExistantChunk)
            {
                Vector3I chunkOffset = GetChunkOffsetFromDirection(direction);
                //Chunk is set as adjacent chunk by "RegisterChunk" method
                //which is called in constructor, so it doesn't have to be
                //explicitely assigned here.
                chunk = new Chunk<VoxelT>(Root, chunkOffset, true);
                return true;
            }
            else return false;
        }

        public bool TraverseToChunk(Vector3I position, 
            bool createNonExistantChunk, out Chunk<VoxelT> chunk)
        {
            Vector3I chunkOffset = 
                GetChunkOffsetFromPosition(position, SideLength);

            if (chunkRegistry.TryGetValue(chunkOffset, out chunk))
                return true;
            else if (createNonExistantChunk)
            {
                //Chunk is set as adjacent chunk by "RegisterChunk" method
                //which is called in constructor, so it doesn't have to be
                //explicitely assigned here.
                chunk = new Chunk<VoxelT>(Root, chunkOffset, true);
                return true;
            }
            else return false;
        }

        public bool RemoveChunk(Direction direction)
        {
            var nextChunk = adjacentChunks[direction];
            if (nextChunk != null)
            {
                //Chunk is removed as adjacent chunk by "UnregisterChunk" 
                //method which is called in Dispose method, so it doesn't have
                //to be explicitely unassigned here.
                nextChunk.Dispose();
                return true;
            }
            else return false;
        }

        public bool RemoveChunk(Vector3I position)
        {
            Vector3I chunkOffset =
                GetChunkOffsetFromPosition(position, SideLength);

            if (chunkRegistry.TryGetValue(chunkOffset, out var chunk))
            {
                chunk.Dispose();
                chunkRegistry.Remove(chunkOffset);
                return true;
            }
            else return false;
        }

        public bool TryGetVoxel(in Vector3 position, out VoxelT voxel,
            bool includeAdjacentChunks = false)
        {
            return TryGetVoxel((Vector3I)position, out voxel, 
                includeAdjacentChunks);
        }

        public bool TryGetVoxel(in Vector3I position, out VoxelT voxel, 
            bool includeAdjacentChunks = false)
        {
            bool xInsideChunk = position.X >= 0 && position.X < SideLength;
            bool yInsideChunk = position.Y >= 0 && position.Y < SideLength;
            bool zInsideChunk = position.Z >= 0 && position.Z < SideLength;

            if (xInsideChunk && yInsideChunk && zInsideChunk)
                return TryGetVoxel(position.X, position.Y, position.Z, 
                    out voxel);
            else if (includeAdjacentChunks)
            {
                if (yInsideChunk && zInsideChunk)
                {
                    if (position.X < 0 && position.X > -SideLength &&
                        TraverseToChunk(Direction.West, false, out var west))
                        return west.TryGetVoxel(SideLength + position.X,
                            position.Y, position.Z, out voxel);
                    else if (position.X >= SideLength &&
                        position.X < SideLength * 2 &&
                        TraverseToChunk(Direction.East, false, out var east))
                        return east.TryGetVoxel(position.X - SideLength,
                            position.Y, position.Z, out voxel);
                }
                else if (xInsideChunk && zInsideChunk)
                {
                    if (position.Y < 0 && position.Y > -SideLength &&
                        TraverseToChunk(Direction.Below, false, out var below))
                        return below.TryGetVoxel(position.X,
                            SideLength + position.Y, position.Z, out voxel);
                    else if (position.Y >= SideLength &&
                        position.Y < SideLength * 2 &&
                        TraverseToChunk(Direction.Above, false, out var above))
                        return above.TryGetVoxel(position.X,
                            position.Y - SideLength, position.Z, out voxel);
                }
                else if (xInsideChunk && yInsideChunk)
                {
                    if (position.Z < 0 && position.Z > -SideLength &&
                        TraverseToChunk(Direction.South, false, out var south))
                        return south.TryGetVoxel(position.X, position.Y,
                            SideLength + position.Z, out voxel);
                    else if (position.Z >= SideLength &&
                        position.Z < SideLength * 2 &&
                        TraverseToChunk(Direction.North, false, out var north))
                        return north.TryGetVoxel(position.X, position.Y,
                            position.Z - SideLength, out voxel);
                }
            }

            voxel = default;
            return false;
        }

        public bool TryGetAdjacentVoxels(in Vector3I position,
            bool includeAdjacentChunks,
            ref AdjacentList<VoxelT> adjacentVoxelCollection)
        {
            if (adjacentVoxelCollection == null)
                throw new ArgumentNullException(
                    nameof(adjacentVoxelCollection));

            bool found = true;

            found &= TryGetVoxel(position + Vector3I.West, 
                out VoxelT west, includeAdjacentChunks);
            found &= TryGetVoxel(position + Vector3I.East,
                out VoxelT east, includeAdjacentChunks);
            found &= TryGetVoxel(position + Vector3I.Above,
                out VoxelT above, includeAdjacentChunks);
            found &= TryGetVoxel(position + Vector3I.Below,
                out VoxelT below, includeAdjacentChunks);
            found &= TryGetVoxel(position + Vector3I.North,
                out VoxelT north, includeAdjacentChunks);
            found &= TryGetVoxel(position + Vector3I.South,
                out VoxelT south, includeAdjacentChunks);

            adjacentVoxelCollection.West = west;
            adjacentVoxelCollection.East = east;
            adjacentVoxelCollection.Above = above;
            adjacentVoxelCollection.Below = below;
            adjacentVoxelCollection.North = north;
            adjacentVoxelCollection.South = south;

            return found;
        }

        private bool TryGetVoxel(int positionX, int positionY, int positionZ,
            out VoxelT voxel)
        {
            if (data == null)
            {
                voxel = default;
                return false;
            }
            else
            {
                voxel = data[positionX, positionY, positionZ];
                return true;
            }
        }

        public bool SetVoxel(in Vector3I relativePosition, VoxelT voxel)
        {
            if (data == null || isLocked) return false;

            if (relativePosition >= Vector3I.Zero &&
                relativePosition < Dimensions)
            {
                VoxelT previousVoxel = data[relativePosition.X,
                    relativePosition.Y, relativePosition.Z];

                data[relativePosition.X, relativePosition.Y,
                    relativePosition.Z] = voxel;

                behaviour?.OnVoxelModified(relativePosition,
                    voxel, previousVoxel);

                if (relativePosition.X == (SideLength - 1))
                    adjacentChunks.East?.behaviour?.TriggerRefreshView();
                else if (relativePosition.X == 0)
                    adjacentChunks.West?.behaviour?.TriggerRefreshView();
                else if (relativePosition.Y == (SideLength - 1))
                    adjacentChunks.Above?.behaviour?.TriggerRefreshView();
                else if (relativePosition.Y == 0)
                    adjacentChunks.Below?.behaviour?.TriggerRefreshView();
                if (relativePosition.Z == (SideLength - 1))
                    adjacentChunks.North?.behaviour?.TriggerRefreshView();
                else if (relativePosition.Z == 0)
                    adjacentChunks.South?.behaviour?.TriggerRefreshView();

                return true;
            }
            else return false;
        }

        public bool Clear()
        {
            if (data == null || isLocked) return false;

            for (int x = 0; x < SideLength; x++)
                for (int y = 0; y < SideLength; y++)
                    for (int z = 0; z < SideLength; z++)
                        data[x, y, z] = default;

            behaviour?.OnVoxelsCleared();

            return true;
        }

        /// <summary>
        /// Only draws if <see cref="Availability"/> is
        /// <see cref="ChunkAvailability.Full"/>.
        /// </summary>
        /// <param name="delta"></param>
        public void Redraw<RenderContextT>(RenderContextT context) 
            where RenderContextT : IRenderContext
        {
            if (behaviour != null && behaviour.ViewAvailable)
                behaviour.OnRedraw(context);
        }

        public void Update(TimeSpan delta)
        {
            if (IsAvailabilityChanging && !isLocked)
            {
                if (targetAvailability == ChunkAvailability.Full)
                {
                    if (SetDataAvailability(true))
                        behaviour.ViewAvailable = true;
                }
                else if (targetAvailability == ChunkAvailability.DataOnly)
                {
                    SetDataAvailability(true);
                    behaviour.ViewAvailable = false;
                }
                else
                {
                    SetDataAvailability(false);
                    behaviour.ViewAvailable = false;
                }
            }

            behaviour.OnUpdate(delta);
        }

        public bool ChangeAvailability(ChunkAvailability availability)
        {
            if (!Enum.IsDefined(typeof(ChunkAvailability), availability))
                throw new ArgumentException("The specified availablilty " +
                    "is invalid.");
            if (behaviour == null)
                throw new InvalidOperationException("The availability " +
                    "can only be changed when the chunk has a behaviour.");

            if (!isLocked)
            {
                targetAvailability = availability;
                return true;
            }
            else return false;
        }

        public void Lock(bool abortAvailabilityChange = true)
        {
            if (abortAvailabilityChange) targetAvailability = Availability;
            isLocked = true;
        }

        public void Unlock()
        {
            isLocked = false;
        }

        private bool SetDataAvailability(bool makeDataAvailable)
        {
            if (makeDataAvailable && data == null &&
                !dataCheckoutRunning && !dataCommitRunning)
            {
                if (dataFromCheckout != null)
                {
                    data = dataFromCheckout;
                    dataFromCheckout = null;
                    behaviour?.OnDataCheckout();
                    return true;
                }
                else
                {
                    dataCheckoutRunning = true;
                    Root.configuration.BeginCheckoutData(
                        Offset, OnSuccessfulAsyncDataCheckout,
                        OnFailedAsyncDataCheckout);
                    return false;
                }
            }
            
            if (!makeDataAvailable && data != null && !dataCheckoutRunning
                && !dataCommitRunning)
            {
                if (dataClearAfterCommitRequested)
                {
                    data = null;
                    dataClearAfterCommitRequested = false;
                    behaviour?.OnDataPaged();
                    return true;
                }
                else
                {
                    dataCommitRunning = true;
                    Root.configuration.BeginPageData(Offset, data,
                        OnSuccessfulAsyncDataCommit,
                        OnFailedAsyncDataCommit);
                    return false;
                }
            }

            return true;
        }

        private void OnSuccessfulAsyncDataCheckout(VoxelT[,,] data)
        {
            if (data != null)
            {
                if (data.Rank == 3 && data.GetLength(0) == SideLength &&
                    data.GetLength(1) == SideLength &&
                    data.GetLength(2) == SideLength)
                {
#if VERBOSE_LOGGING
                    Log.Trace("Chunk [" + Offset + "] data checkout " +
                        "completed.");
#endif
                    dataFromCheckout = data;
                    dataCheckoutRunning = false;
                }
                else
                {
                    Log.Error("Chunk [" + Offset + "] data checkout failed: " +
                        "invalid dimensions.");
                    Lock();
                    dataCheckoutRunning = false;
                }
            }
            else
            {
                Log.Error("Chunk [" + Offset + "] data checkout failed: " +
                    "importer returned null.");
                Lock();
                dataCheckoutRunning = false;
            }            
        }

        private void OnFailedAsyncDataCheckout(Exception exc)
        {
            Log.Error("Chunk [" + Offset + "] data checkout failed.", exc);
            Lock();
            dataCheckoutRunning = false;
        }

        private void OnSuccessfulAsyncDataCommit()
        {
#if VERBOSE_LOGGING
            Log.Trace("Chunk [" + Offset + "] data paging completed.");
#endif
            dataClearAfterCommitRequested = true;
            dataCommitRunning = false;
        }

        private void OnFailedAsyncDataCommit(Exception exc)
        {
            Log.Error("Chunk [" + Offset + "] data commit failed.", exc);
            Lock();
            dataCommitRunning = false;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                if (IsRoot)
                {
                    while (chunkRegistry.Count > 0)
                    {
                        var removalCandidate = chunkRegistry.Values.First();
                        removalCandidate.Dispose();
                    }
                }
                else UnregisterChunk();

                behaviour?.Dispose();

                IsDisposed = true;
            }
        }

        public IEnumerator<Chunk<VoxelT>> GetEnumerator()
        {
            return chunkRegistry.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
