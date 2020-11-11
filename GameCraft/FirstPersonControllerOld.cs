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

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Numerics;
using System.Text;
using GameCraft.BlockChunk;
using GameCraft.Common;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;

namespace GameCraft
{
    class FirstPersonControllerOld : PlayerControllerBase
    {
        public class ChunkCollisionChecker
        {
            private Chunk<BlockVoxel> currentChunk;
            private Vector3I currentChunkOffset;

            private readonly BlockRegistry blockRegistry;

            private Block currentBlock;

            public Vector3 BodyDimensions { get; }
                //= new Vector3(1f, 1.7f, 1f);

            public ChunkCollisionChecker(Chunk<BlockVoxel> currentChunk,
                BlockRegistry blockRegistry, in Vector3 bodyDimensions)
            {
                this.currentChunk = currentChunk ??
                    throw new ArgumentNullException(nameof(currentChunk));
                this.blockRegistry = blockRegistry ??
                    throw new ArgumentNullException(nameof(blockRegistry));
                currentChunkOffset = currentChunk.Offset;
                BodyDimensions = bodyDimensions;
            }

            private void CalculateVoxelCollisionResponse(Vector3 voxelPosition, 
                ref Vector3 position, ref Vector3 velocity)
            {
                Vector3 colliderOffset = new Vector3(
                    BodyDimensions.X / 2, 0, BodyDimensions.Z / 2);
                Vector3 bodyPosition = position - colliderOffset;

                Vector3 colliderPosition = voxelPosition -
                    new Vector3(0f);
                Vector3 colliderDimensions = new Vector3(1.0f);

                if (PhysicsHelper.Collides(bodyPosition,
                    BodyDimensions, velocity, colliderPosition,
                    colliderDimensions, out float time,
                    out Vector3 normal))
                {
                    bodyPosition += velocity * time;
                    float remainingTime = 1 - time;
                    velocity = (velocity - normal *
                        Vector3.Dot(normal, velocity)) * remainingTime;
                }
                else bodyPosition += velocity;

                position = bodyPosition + colliderOffset;
            }

            public Vector3I CalculateNearestTargetVoxelPosition(
                in Vector3 position, in Vector3 velocity)
            {
                Vector3 dimensions = BodyDimensions;

                int targetX, targetY, targetZ;

                if (velocity.X > 0)
                {
                    targetX = (int)Math.Ceiling(position.X + dimensions.X + 
                        double.Epsilon);
                }
                else
                {
                    targetX = (int)Math.Ceiling(position.X + double.Epsilon) 
                        - 1;
                }

                if (velocity.Y > 0)
                {
                    targetY = (int)Math.Ceiling(position.Y + dimensions.Y +
                        double.Epsilon);
                }
                else
                {
                    targetY = (int)Math.Ceiling(position.Y + double.Epsilon) 
                        - 1;
                }

                if (velocity.Z > 0)
                {
                    targetZ = (int)Math.Ceiling(position.Z + dimensions.Z +
                        double.Epsilon);
                }
                else
                {
                    targetZ = (int)Math.Ceiling(position.Z + double.Epsilon)
                        - 1;
                }

                return new Vector3I(targetX, targetY, targetZ);
            }

            //public void CalculateCollisionResponse(ref Vector3 position,
            //    ref Vector3 velocity)
            //{
            //    Vector3I currentVoxelPosition = (Vector3I)(position +
            //        new Vector3(0f, 0f, -1f));
            //    Vector3 targetPosition = position + velocity;
            //    Vector3I targetVoxelPosition = (Vector3I)(targetPosition +
            //        new Vector3(0f, 0f, -1f));
            //    Vector3I targetChunkOffset =
            //        Chunk<BlockVoxel>.GetChunkOffsetFromPosition(
            //            targetVoxelPosition, currentChunk.SideLength);

            //    if (targetChunkOffset != currentChunkOffset)
            //    {
            //        currentChunkOffset = targetChunkOffset;

            //        if (currentChunk.Offset != currentChunkOffset)
            //        {
            //            if (currentChunk.TraverseToChunk(targetVoxelPosition,
            //                false, out Chunk<BlockVoxel> targetChunk))
            //                currentChunk = targetChunk;
            //        }
            //    }

            //    if (currentChunk.Offset == currentChunkOffset)
            //    {
            //        Vector3I relativeTargetPlayerPosition =
            //            targetVoxelPosition - currentChunkOffset;

            //        currentChunk.TryGetVoxel(relativeTargetPlayerPosition,
            //            out BlockVoxel voxel);

            //        Block previousBlock = currentBlock;
            //        currentBlock = blockRegistry.GetBlock(voxel.BlockKey);

            //        static bool isSolid(Block block) =>
            //            !block?.Properties.IsTranslucent ?? false;

            //        if (isSolid(previousBlock))
            //        {
            //            CalculateVoxelCollisionResponse(ref position,
            //                ref velocity, currentVoxelPosition);
            //        }
            //        if (isSolid(currentBlock))
            //        {
            //            CalculateVoxelCollisionResponse(ref position,
            //                ref velocity, targetVoxelPosition);
            //        }
            //        else position += velocity;
            //    }
            //    else position += velocity;
            //}

            private void CalculateVoxelCollisionResponse(ref Vector3 position,
                ref Vector3 velocity, Vector3 targetVoxelPosition)
            {
                Vector3 colliderOffset = new Vector3(
                    BodyDimensions.X / 2, 0, BodyDimensions.Z / 2);
                Vector3 bodyPosition = position - colliderOffset;

                Vector3 colliderPosition = targetVoxelPosition -
                    new Vector3(0f);
                Vector3 colliderDimensions = new Vector3(1.0f);

                if (PhysicsHelper.Collides(bodyPosition,
                    BodyDimensions, velocity, colliderPosition,
                    colliderDimensions, out float time,
                    out Vector3 normal))
                {
                    bodyPosition += velocity * time;
                    float remainingTime = 1 - time;
                    velocity = (velocity - normal *
                        Vector3.Dot(normal, velocity)) * remainingTime;
                }
                bodyPosition += velocity;

                position = bodyPosition + colliderOffset;
            }

            public void CalculateCollisionResponse(ref Vector3 position,
                ref Vector3 velocity)
            {
                Vector3I currentVoxelPosition = (Vector3I)(position +
                    new Vector3(0f, 0f, -1f));
                Vector3 targetPosition = position + velocity;
                Vector3I targetVoxelPosition = (Vector3I)(targetPosition +
                    new Vector3(0f, 0f, -1f));
                Vector3I targetChunkOffset =
                    Chunk<BlockVoxel>.GetChunkOffsetFromPosition(
                        targetVoxelPosition, currentChunk.SideLength);

                if (targetChunkOffset != currentChunkOffset)
                {
                    currentChunkOffset = targetChunkOffset;

                    if (currentChunk.Offset != currentChunkOffset)
                    {
                        if (currentChunk.TraverseToChunk(targetVoxelPosition,
                            false, out Chunk<BlockVoxel> targetChunk))
                            currentChunk = targetChunk;
                    }
                }

                if (currentChunk.Offset == currentChunkOffset)
                {
                    Vector3I relativeTargetPlayerPosition =
                        targetVoxelPosition - currentChunkOffset;

                    currentChunk.TryGetVoxel(relativeTargetPlayerPosition,
                        out BlockVoxel voxel);

                    Block previousBlock = currentBlock;
                    currentBlock = blockRegistry.GetBlock(voxel.BlockKey);

                    static bool isSolid(Block block) =>
                        !block?.Properties.IsTranslucent ?? false;

                    if (isSolid(previousBlock))
                    {
                        CalculateVoxelCollisionResponse(ref position,
                            ref velocity, currentVoxelPosition);
                    }
                    if (isSolid(currentBlock))
                    {
                        CalculateVoxelCollisionResponse(ref position,
                            ref velocity, targetVoxelPosition);
                    }
                    else position += velocity;
                }
                else position += velocity;
            }
        }

        public const float LookAccerlationDeg = 42.0f;
        public const float LookDragDeg = 6.9f;

        public const float MoveSpeed = 0.69f;
        public const float MoveDrag = 4.20f;

        private readonly Vector3 playerDimensions = Vector3.Zero;
            //new Vector3(1f, 1.7f, 1f);
        private readonly float eyeHeightOffset = 1.6f;

        public Vector3 velocity;
        private Vector2 rotationDeg;
        public Vector3 playerPosition = new Vector3(0, 1, 0);

        public readonly ChunkCollisionChecker collisionChecker;

        private readonly BlockRegistry blockRegistry;

        public ControlMapping Jump { get; set; }

        public FirstPersonControllerOld(Camera camera, 
            Chunk<BlockVoxel> rootChunk, BlockRegistry blockRegistry) 
            : base(camera)
        {
            this.blockRegistry = blockRegistry ??
                throw new ArgumentNullException(nameof(blockRegistry));

            collisionChecker =
                new ChunkCollisionChecker(rootChunk, blockRegistry, 
                playerDimensions);
        }

        public override void Update(TimeSpan delta)
        {
            PhysicsHelper.ApplyAccerlationToVelocity(ref rotationDeg,
                RotationUserInput * LookAccerlationDeg, delta);
            PhysicsHelper.ApplyDragToVelocity(ref rotationDeg,
                LookDragDeg, delta);

            Camera.Rotate(Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y));

            Vector3 userInput = Camera.AlignVector(MovementUserInput,
                true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            PhysicsHelper.ApplyAccerlationToVelocity(ref velocity,
                userInput * MoveSpeed, delta);
            PhysicsHelper.ApplyAccerlationToVelocity(ref velocity,
                new Vector3(0, -0.981f, 0), delta);

            collisionChecker.CalculateCollisionResponse(ref playerPosition,
                ref velocity);

            PhysicsHelper.ApplyDragToVelocity(ref velocity, MoveDrag, delta);

            Camera.MoveTo(playerPosition + new Vector3(0, eyeHeightOffset, 0));
        }
    }
}
