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
using TrippingCubes.Physics;
using TrippingCubes.World;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using System;
using System.Numerics;

namespace TrippingCubes
{
    class FirstPersonController : PlayerControllerBase
    {
        public const float LookAccerlationDeg = 42.0f;
        public const float LookDragDeg = 6.9f;

        public ControlMapping Jump { get; set; } 

        private Vector2 rotationDeg;

        private PhysicsSystem world;
        private RigidBody playerBody;

        private Chunk<BlockVoxel> rootChunk;
        private readonly BlockRegistry registry;

        public FirstPersonController(Camera camera, 
            Chunk<BlockVoxel> rootChunk, BlockRegistry registry) : base(camera)
        {
            world = new PhysicsSystem(IsBlockSolid, IsBlockLiquid);
            playerBody = world.AddNewBody(
                new BoundingBox(5, 2, 5, 0.6f, 1.6f, 0.6f));
            playerBody.Restitution = 0.142f;
            playerBody.Friction = 0.8f;

            this.rootChunk = rootChunk;
            this.registry = registry;
        }

        private bool IsBlockSolid(Vector3 position)
        {
            if (rootChunk.TraverseToChunk((Vector3I)position, false,
                    out var chunk))
            {
                if (chunk.TryGetVoxel(position - chunk.Offset,
                    out var voxel, false))
                {
                    var voxelBlock = registry.GetBlock(voxel.BlockKey);
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

        public override void Update(TimeSpan delta)
        {
            PhysicsHelper.ApplyAccerlationToVelocity(ref rotationDeg,
                RotationUserInput * LookAccerlationDeg, delta);
            PhysicsHelper.ApplyDragToVelocity(ref rotationDeg,
                LookDragDeg, delta);
            Camera.Rotate(Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y));

            Vector3 userInput = Camera.AlignVector(MovementUserInput, true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            Vector3 horizontalVelocity = new Vector3(playerBody.Velocity.X,
                0, playerBody.Velocity.Z);

            float currentSpeed = horizontalVelocity.Length();

            if (playerBody.Resting.Y == -1)
            {
                playerBody.ApplyForce(userInput * 7 * (5 - currentSpeed));
                if (Jump.IsActivated) playerBody.ApplyImpulse(new Vector3(0, 6, 0));
            }
            else playerBody.ApplyForce(userInput * 4 * (4 - currentSpeed));

            world.Update(delta);

            Camera.MoveTo(playerBody.BoundingBox.Position +
                new Vector3(0.3f, 1.45f, 0.3f));
        }
    }
}
