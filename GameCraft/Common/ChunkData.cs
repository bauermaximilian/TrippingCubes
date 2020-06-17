using Eterra.Graphics;
using Eterra.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeinKraft.Voxels
{
    public delegate ChunkData<VoxelT> ChunkDataFactory<VoxelT>(
        Chunk<VoxelT> parentChunk) where VoxelT : unmanaged;

    public abstract class ChunkData<VoxelT> : IDisposable
        where VoxelT : unmanaged
    {
        public int SideLength { get; }

        public Vector3I Dimensions { get; }

        public ChunkData(int sideLength)
        {
            if (sideLength < 1) throw new ArgumentException("The side " +
                "length must not be less than 1.");
            SideLength = sideLength;
            Dimensions = new Vector3I(sideLength, sideLength, sideLength);
        }

        public abstract bool TryGetVoxel(in Vector3I position, 
            out VoxelT voxel);

        public abstract bool TrySetVoxel(in Vector3I position, VoxelT voxel);

        internal protected abstract void Update(TimeSpan delta);

        public abstract void Dispose();
    }

    class ChunkDataNonPageable<VoxelT> : ChunkData<VoxelT>
        where VoxelT : unmanaged
    {
        private readonly VoxelT[,,] data;

        public ChunkDataNonPageable(int sideLength) : base(sideLength)
        {
            data = new VoxelT[SideLength, SideLength, SideLength];
        }

        public override void Dispose() { }

        public override bool TryGetVoxel(in Vector3I position, 
            out VoxelT voxel)
        {
            bool xInsideChunk = position.X >= 0 && position.X < SideLength;
            bool yInsideChunk = position.Y >= 0 && position.Y < SideLength;
            bool zInsideChunk = position.Z >= 0 && position.Z < SideLength;

            if (xInsideChunk && yInsideChunk && zInsideChunk)
            {
                voxel = data[position.X, position.Y, position.Z];
                return true;
            }
            else
            {
                voxel = default;
                return false;
            }
        }

        public override bool TrySetVoxel(in Vector3I position, VoxelT voxel)
        {
            bool xInsideChunk = position.X >= 0 && position.X < SideLength;
            bool yInsideChunk = position.Y >= 0 && position.Y < SideLength;
            bool zInsideChunk = position.Z >= 0 && position.Z < SideLength;

            if (xInsideChunk && yInsideChunk && zInsideChunk)
            {
                data[position.X, position.Y, position.Z] = voxel;
                return true;
            }
            else return false;
        }

        protected internal override void Update(TimeSpan delta) { }
    }

    public enum DataPagingState
    {
        HD,
        RAM,
        RAMandVRAM
    }

    class ChunkDataPageable<VoxelT, BlockIndexT> : ChunkData<VoxelT>
        where VoxelT : unmanaged where BlockIndexT : unmanaged
    {
        private VoxelT[,,] data;

        private readonly ResourceManager resourceManager;
        private readonly IBlockRegistry<BlockIndexT, VoxelT> blockRegistry;
        private readonly Chunk<VoxelT> parentChunk;

        //Changes are applied in the Update method.
        public DataPagingState PagingState { get; private set; }
        private DataPagingState nextPagingState;

        public MeshBuffer Mesh { get; private set; }

        public ChunkDataPageable(Chunk<VoxelT> parentChunk,
            IBlockRegistry<BlockIndexT, VoxelT> blockRegistry,
            ResourceManager resourceManager, ResourcePath targetStorageRoot) 
            : base(parentChunk != null ? parentChunk.SideLength : 1)
        {
            this.parentChunk = parentChunk ??
                throw new ArgumentNullException(nameof(parentChunk));
            this.blockRegistry = blockRegistry ??
                throw new ArgumentNullException(nameof(blockRegistry));
            this.resourceManager = resourceManager ??
                throw new ArgumentNullException(nameof(resourceManager));

            data = new VoxelT[SideLength, SideLength, SideLength];
            nextPagingState = PagingState = DataPagingState.RAM;            
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetVoxel(in Vector3I position, 
            out VoxelT voxel)
        {
            throw new NotImplementedException();
        }

        public override bool TrySetVoxel(in Vector3I position, VoxelT voxel)
        {
            throw new NotImplementedException();
        }

        public void ChangePagingState(DataPagingState newPagingState)
        {
            throw new NotImplementedException();
        }

        protected internal override void Update(TimeSpan delta)
        {
            throw new NotImplementedException();
        }
    }
}
