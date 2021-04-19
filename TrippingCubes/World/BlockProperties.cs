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

using OpenTK.Mathematics;
using ShamanTK.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrippingCubes.World
{
    public enum BlockColliderType
    {
        None,
        Solid,
        Liquid
    }

    public class BlockProperties
    {
        public static BlockProperties Air =>
            new BlockProperties(BlockColliderType.None, true, 0);

        public static BlockProperties Solid =>
            new BlockProperties(BlockColliderType.Solid, false, 0);

        public int Luminance
        {
            get => luminance;
            private set
            {
                if (value < 16 && value >= 0) luminance = value;
                else throw new ArgumentOutOfRangeException();
            }
        }
        private int luminance = 0;

        public bool IsTranslucent { get; private set; }

        public BlockColliderType Type { get; private set; }

        public ColliderPrimitive Collider { get; private set; }

        public Vector3 ColliderOffset { get; private set; }

        public BlockProperties(BlockColliderType type, bool isTranslucent, 
            int luminance)
        {
            Type = type;
            IsTranslucent = isTranslucent;
            Luminance = luminance;
        }
    }
}
