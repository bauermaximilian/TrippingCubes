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

using System;
using System.Numerics;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;

namespace TrippingCubes
{
    public abstract class PlayerControllerBase
    {
        public ControlMapping MoveLeft { get; set; }

        public ControlMapping MoveRight { get; set; }

        public ControlMapping MoveForward { get; set; }

        public ControlMapping MoveBackward { get; set; }

        public ControlMapping LookUp { get; set; }

        public ControlMapping LookDown { get; set; }

        public ControlMapping LookLeft { get; set; }

        public ControlMapping LookRight { get; set; }        

        public Camera Camera { get; }        

        protected virtual Vector2 RotationUserInput => new Vector2(
        (LookDown?.Value ?? 0) - (LookUp?.Value ?? 0),
        (LookRight?.Value ?? 0) - (LookLeft?.Value ?? 0));

        protected virtual Vector3 MovementUserInput => new Vector3(
            (MoveRight?.Value ?? 0) - (MoveLeft?.Value ?? 0), 0,
            (MoveForward?.Value ?? 0) - (MoveBackward?.Value ?? 0));

        public PlayerControllerBase(Camera camera)
        {
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public abstract void Update(TimeSpan delta);
    }
}
