﻿/*
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
using ShamanTK.Controls;
using ShamanTK.Graphics;
using System;
using System.Numerics;

namespace GameCraft
{
    class FlyCamController : PlayerControllerBase
    {
        public ControlMapping MoveUp { get; set; }

        public ControlMapping MoveDown { get; set; }        

        private Vector3 positionAccerlation;
        private Vector2 rotationDegrees;

        protected override Vector3 MovementUserInput => base.MovementUserInput
            + new Vector3(0, (MoveUp?.Value ?? 0) - (MoveDown?.Value ?? 0), 0);

        public FlyCamController(Camera camera) : base(camera)
        {
        }

        public override void Update(TimeSpan delta)
        {
            UpdateRotation(delta, ref rotationDegrees);

            Camera.Rotate(Angle.Deg(rotationDegrees.X),
                Angle.Deg(rotationDegrees.Y));

            UpdateMovement(delta, ref positionAccerlation, true);

            Camera.Move(positionAccerlation);
        }
    }
}
