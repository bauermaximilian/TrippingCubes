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

using ShamanTK.Common;
using ShamanTK.Controls;
using System;
using System.Numerics;

namespace GameCraft
{
    public class FlyCamController
    {
        public ControlMapping MoveUp { get; set; }

        public ControlMapping MoveDown { get; set; }

        public ControlMapping MoveLeft { get; set; }

        public ControlMapping MoveRight { get; set; }

        public ControlMapping MoveForward { get; set; }

        public ControlMapping MoveBackward { get; set; }

        public ControlMapping LookUp { get; set; }

        public ControlMapping LookDown { get; set; }

        public ControlMapping LookLeft { get; set; }

        public ControlMapping LookRight { get; set; }

        private Vector3 positionAccerlation;
        private Vector2 rotationAccerlationDeg;

        /// <summary>
        /// Per second.
        /// </summary>
        public float LookSpeed = 70f;

        /// <summary>
        /// Per second.
        /// </summary>
        public float MoveSpeed = 0.4f;

        /// <summary>
        /// Per second.
        /// </summary>
        public float LookFriction = 10.0f;

        /// <summary>
        /// Per second.
        /// </summary>
        public float MoveFriction = 3.0f;

        public ShamanTK.Graphics.Camera Camera { get; }

        public FlyCamController(ShamanTK.Graphics.Camera camera)
        {
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));
        }

        public void Update(TimeSpan delta)
        {
            positionAccerlation -= positionAccerlation *
                (float)(MoveFriction * delta.TotalSeconds);

            Vector2 inputRotationAccerlation = new Vector2(
                (LookDown != null ? LookDown.Value : 0) -
                (LookUp != null ? LookUp.Value : 0),
                (LookRight != null ? LookRight.Value : 0) -
                (LookLeft != null ? LookLeft.Value : 0));
            rotationAccerlationDeg -= rotationAccerlationDeg *
                (float)(LookFriction * delta.TotalSeconds);
            rotationAccerlationDeg += inputRotationAccerlation *
                (float)(LookSpeed * delta.TotalSeconds);

            Vector3 inputAxisPositionAccerlation =
                new Vector3((MoveRight != null ? MoveRight.Value : 0) -
                (MoveLeft != null ? MoveLeft.Value : 0),
                (MoveUp != null ? MoveUp.Value : 0) -
                (MoveDown != null ? MoveDown.Value : 0),
                (MoveForward != null ? MoveForward.Value : 0) -
                (MoveBackward != null ? MoveBackward.Value : 0));
            if (inputAxisPositionAccerlation.Length() > 1)
                inputAxisPositionAccerlation = Vector3.Normalize(
                    inputAxisPositionAccerlation);
            inputAxisPositionAccerlation *=
                (float)(MoveSpeed * delta.TotalSeconds);

            Camera.Rotate(Angle.Deg(rotationAccerlationDeg.X),
                Angle.Deg(rotationAccerlationDeg.Y));

            positionAccerlation += Camera.AlignVector(
                inputAxisPositionAccerlation, true, false);

            Camera.Move(positionAccerlation);
        }
    }
}
