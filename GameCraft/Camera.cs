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
using Eterra.Graphics;
using System;
using System.Numerics;

namespace GameCraft
{
    public readonly struct Camera
    {
        private static readonly Vector3 defaultPosition = Vector3.UnitZ * -5;

        private static readonly Vector3 defaultRotation = Vector3.Zero;

        private static readonly float rotationLimitX =
            (float)(Math.PI * 0.5);

        private static readonly float rotationLimitY =
            (float)(Math.PI * 2);

        private static readonly float rotationLimitZ = 
            rotationLimitY;

        /// <summary>
        /// Gets a default <see cref="Camera"/> with 
        /// <see cref="ProjectionMode.OrthographicRelative"/> as 
        /// <see cref="ProjectionMode"/>.
        /// </summary>
        public static Camera DefaultOrthographic { get; } =
            new Camera(defaultPosition, defaultRotation,
                ProjectionMode.OrthographicRelative, 0);

        /// <summary>
        /// Gets a default <see cref="Camera"/> with 
        /// <see cref="ProjectionMode.Perspective"/> as 
        /// <see cref="ProjectionMode"/>.
        /// </summary>
        public static Camera DefaultPerspective { get; } = 
            new Camera(defaultPosition, defaultRotation,
                ProjectionMode.Perspective, Angle.Deg(70));

        /// <summary>
        /// Gets the position of the current <see cref="Camera"/>.
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the rotation of current <see cref="Camera"/> as euler 
        /// rotation, where each component of the vector defines the rotation
        /// around the axis with the components name (in radians).
        /// </summary>
        public Vector3 Rotation { get; }

        /// <summary>
        /// Gets the rotation angle of the current <see cref="Camera"/> around 
        /// the X axis.
        /// </summary>
        public Angle RotationX => Rotation.X;

        /// <summary>
        /// Gets the rotation angle of the current <see cref="Camera"/> around 
        /// the Y axis.
        /// </summary>
        public Angle RotationY => Rotation.Y;

        /// <summary>
        /// Gets the rotation angle of the current <see cref="Camera"/> around 
        /// the Z axis.
        /// </summary>
        public Angle RotationZ => Rotation.Z;

        /// <summary>
        /// Gets the current <see cref="Eterra.Graphics.ProjectionMode"/> of
        /// the current <see cref="Camera"/>.
        /// </summary>
        public ProjectionMode ProjectionMode { get; }

        /// <summary>
        /// Gets the field of vision of the current <see cref="Camera"/> if
        /// the current <see cref="ProjectionMode"/> is
        /// <see cref="ProjectionMode.Perspective"/>. 
        /// The value is ignored otherwise.
        /// </summary>
        public Angle PerspectiveFieldOfView { get; }

        public Camera(ProjectionMode projectionMode,
            Angle perspectiveFieldOfView) : this(defaultPosition,
                defaultRotation, projectionMode, perspectiveFieldOfView)
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="position">
        /// The <see cref="Camera"/> position.
        /// </param>
        /// <param name="rotation">
        /// The <see cref="Camera"/> rotation.
        /// Automatically normalized and clamped to avoid gimbal lock.
        /// </param>
        /// <param name="projectionMode">
        /// The <see cref="Eterra.Graphics.ProjectionMode"/> of the 
        /// <see cref="Camera"/>.
        /// </param>
        /// <param name="perspectiveFieldOfView">
        /// The field of view of the <see cref="Camera"/>, if the chosen
        /// <see cref="Eterra.Graphics.ProjectionMode"/> is
        /// <see cref="ProjectionMode.Perspective"/>, ignored otherwise.
        /// </param>
        public Camera(Vector3 position, Vector3 rotation, 
            ProjectionMode projectionMode, Angle perspectiveFieldOfView)
        {
            Position = position;

            float rotationX = Math.Min(Math.Max(
                rotation.X, -rotationLimitX + float.Epsilon),
                rotationLimitX - float.Epsilon);
            float rotationY = rotation.Y % rotationLimitY;
            float rotationZ = rotation.Z % rotationLimitZ;

            Rotation = new Vector3(rotationX, rotationY, rotationZ);

            ProjectionMode = projectionMode;
            PerspectiveFieldOfView = perspectiveFieldOfView;
        }

        public Camera Moved(Vector3 translation)
        {
            return new Camera(Position + translation, Rotation,
                ProjectionMode, PerspectiveFieldOfView);
        }

        public Camera Transformed(Vector3 translation, Vector3 rotation)
        {
            return new Camera(Position + translation,
                Rotation + rotation, ProjectionMode, 
                PerspectiveFieldOfView);
        }

        public Camera Rotated(Vector3 rotation)
        {
            return new Camera(Position, Rotation + rotation,
                ProjectionMode, PerspectiveFieldOfView);
        }

        public Camera MovedTo(Vector3 position)
        {
            return new Camera(position, Rotation,
                ProjectionMode, PerspectiveFieldOfView);
        }

        public Camera TransformedTo(Vector3 position, Vector3 targetRotation)
        {
            return new Camera(position, targetRotation, ProjectionMode,
                PerspectiveFieldOfView);
        }

        public Camera RotatedTo(Vector3 targetRotation)
        {
            return new Camera(Position, targetRotation,
                ProjectionMode, PerspectiveFieldOfView);
        }

        public Vector3 CreateRotatedUnit(in Vector3 unitVector)
        {
            return CreateRotatedUnit(in unitVector, false);
        }

        public Vector3 CreateRotatedUnit(in Vector3 unitVector, bool nullifyX)
        {
            float rotationX = nullifyX ? 0 : Rotation.X;
            float rotationY = Rotation.Y;

            Vector2 rotationSin = new Vector2((float)Math.Sin(rotationX),
                (float)Math.Sin(rotationY));
            Vector2 rotationCos = new Vector2((float)Math.Cos(rotationX),
                (float)Math.Cos(rotationY));

            return new Vector3(unitVector.X * rotationCos.Y +
                unitVector.Z * rotationSin.Y * rotationCos.X,
                unitVector.Y * (rotationCos.X >= 0 ? 1 : -1) -
                unitVector.Z * rotationSin.X,
                unitVector.Z * rotationCos.Y * rotationCos.X -
                unitVector.X * rotationSin.Y);
        }

        public Quaternion GetRotationQuaternion()
        {
            return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, Rotation.Z) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, Rotation.X) *
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, Rotation.Y);
        }

        public override string ToString()
        {
            return Position + " [R.X = " + RotationX.Degrees + "°, " +
                "R.Y = " + RotationY.Degrees + "°, R.Z = " + 
                RotationZ.Degrees + "°]";
        }
    }
}
