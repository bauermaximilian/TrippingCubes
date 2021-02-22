﻿/*
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
using System;
using System.Collections.Generic;

namespace TrippingCubes.World
{
    public interface IChunkManager<VoxelT> where VoxelT : unmanaged
    {
        ISet<Vector3I> CheckoutRegistry();

        void BeginPageData(Vector3I offset, VoxelT[,,] data,
            Action onSuccess, Action<Exception> onFailure);

        void BeginCheckoutData(Vector3I offset, Action<VoxelT[,,]> onSuccess,
            Action<Exception> onFailure);

        IChunkBehaviour<VoxelT> CreateBehaviour(Chunk<VoxelT> chunk);
    }
}