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

using Eterra.Common;
using Eterra.Controls;
using System;
using System.Numerics;

namespace GameCraft
{
    public class FlyCamController
    {
        private const float DegXLimitAbs = 180.0f;

        private const float DegYLimit = 360.0f;

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

        private Vector3 position, positionAccerlation;

        private Vector2 rotationDeg, rotationAccerlationDeg;

        public Vector3 Position => position;

        public Angle RotationX => Angle.Deg(rotationDeg.X);

        public Angle RotationY => Angle.Deg(rotationDeg.Y);

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

        public FlyCamController()
        {
            position = new Vector3(0, 0, -3);
        }

        public void Update(TimeSpan delta)
        {
            Vector2 inputRotationAccerlation = new Vector2(
                (LookDown != null ? LookDown.Value : 0) -
                (LookUp != null ? LookUp.Value : 0),
                (LookRight != null ? LookRight.Value : 0) -
                (LookLeft != null ? LookLeft.Value : 0));
            rotationAccerlationDeg -= rotationAccerlationDeg *
                (float)(LookFriction * delta.TotalSeconds);
            rotationAccerlationDeg += inputRotationAccerlation *
                (float)(LookSpeed * delta.TotalSeconds);            

            rotationDeg = new Vector2(
                (rotationDeg.X + rotationAccerlationDeg.X) % DegYLimit,
                (rotationDeg.Y + rotationAccerlationDeg.Y) % DegYLimit);

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

            Vector3 inputPositionAccerlation = CreateLookAt(
                inputAxisPositionAccerlation, 0, //Angle.Deg(rotationDeg.X), 
                Angle.Deg(rotationDeg.Y));

            positionAccerlation -= positionAccerlation *
                (float)(MoveFriction * delta.TotalSeconds);
            positionAccerlation += inputPositionAccerlation *
                (float)(MoveSpeed * delta.TotalSeconds);
            
            position += positionAccerlation;
        }

        public static Vector3 CreateLookAt(in Vector3 unitAxisVector,
            Angle rotationX, Angle rotationY)
        {
            Vector2 rotationSin = new Vector2((float)Math.Sin(rotationX),
                (float)Math.Sin(rotationY));
            Vector2 rotationCos = new Vector2((float)Math.Cos(rotationX),
                (float)Math.Cos(rotationY));

            return new Vector3(unitAxisVector.X * rotationCos.Y +
                unitAxisVector.Z * rotationSin.Y * rotationCos.X,
                unitAxisVector.Y * (rotationCos.X >= 0 ? 1 : -1) - 
                unitAxisVector.Z * rotationSin.X,
                unitAxisVector.Z * rotationCos.Y * rotationCos.X - 
                unitAxisVector.X * rotationSin.Y);
        }

        public void ApplyTo(Eterra.Graphics.Camera camera)
        {
            camera.RotateTo(
                Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y), 0);
            camera.MoveTo(position);
        }

        public override string ToString()
        {
            return Position + " [Rotation X = " + RotationX.Degrees + "°, " +
                "Rotation Y = " + RotationY.Degrees + "°]";
        }
    }
}
