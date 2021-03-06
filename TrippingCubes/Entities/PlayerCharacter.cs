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

using TrippingCubes.Physics;
using ShamanTK.Common;
using ShamanTK.Controls;
using ShamanTK.Graphics;
using System;
using System.Numerics;
using TrippingCubes.Common;
using System.Collections.Generic;
using ShamanTK.IO;
using ShamanTK;

namespace TrippingCubes.Entities
{
    class PlayerCharacter : IPlayerCharacter
    {
        public const float LookAccerlationDeg = 42.0f;
        public const float LookDragDeg = 6.9f;

        public const float AccerlationFloor = 5f;
        public const float AccerlationAir = 4;
        public const float AccerlationFly = 24;
        public const float JumpVelocity = 7;

        private Vector2 rotationDeg;
        private Vector3 headShift = Vector3.Zero;
        private DateTime lastAttack = DateTime.MinValue;
        private readonly TimeSpan attackFrequency = TimeSpan.FromSeconds(0.8);

        private readonly ControlMapping moveLeft;
        private readonly ControlMapping moveRight;
        private readonly ControlMapping moveForward;
        private readonly ControlMapping moveBackward;
        private readonly ControlMapping lookUp;
        private readonly ControlMapping lookDown;
        private readonly ControlMapping lookLeft;
        private readonly ControlMapping lookRight;
        private readonly ControlMapping attack;
        private readonly ControlMapping jump;
        private readonly ControlMapping moveUp;
        private readonly ControlMapping moveDown;
        private readonly ControlMapping switchInputScheme;
        private readonly ControlMapping debugTrigger1;

        protected virtual Vector2 RotationUserInput => new Vector2(
            (lookDown?.Value ?? 0) - (lookUp?.Value ?? 0),
            (lookRight?.Value ?? 0) - (lookLeft?.Value ?? 0));

        protected virtual Vector3 MovementUserInput => new Vector3(
            (moveRight?.Value ?? 0) - (moveLeft?.Value ?? 0), 0,
            (moveForward?.Value ?? 0) - (moveBackward?.Value ?? 0));

        public GameWorld World { get; }

        public Camera Camera { get; }

        public float FallDeathYTreshold { get; set; } = -20;

        public ResourcePath WeaponModelPath
        {
            get => weaponPath;
            set
            {
                if (value != weaponPath)
                {
                    World.Game.Models.LoadModel(value, (success, model, exc) =>
                    {
                        if (success) WeaponModel = model;
                        else Log.Warning("A weapon model with the path " +
                                $"'{value}' couldn't be loaded.", exc);
                    });
                    weaponPath = value;
                }
            }
        }
        private ResourcePath weaponPath = ResourcePath.Empty;

        public Model WeaponModel { get; private set; }        

        public RigidBody Body { get; }

        public bool FlyModeEnabled
        {
            get => Body.GravityMultiplier == 0;
            set
            {
                if (value) Body.GravityMultiplier = 0;
                else Body.GravityMultiplier = 1;
            }
        }

        public int HealthPoints
        {
            get => healthPoints;
            set
            {
                if (value != healthPoints)
                {
                    int previousValue = healthPoints;
                    healthPoints = Math.Min(value, MaximumHealth);
                    HealthPointsChanged?.Invoke(previousValue, healthPoints);
                }
            }
        }
        private int healthPoints = 100;

        public int MaximumHealth { get; set; } = 150;

        public int DamagePoints { get; set; } = 25;

        public string Name { get; set; } = $"Unnamed{nameof(PlayerCharacter)}";

        public string CurrentState
        {
            get => currentState;
            protected set
            {
                if (currentState != value)
                {
                    string previousState = value;
                    currentState = value;
                    StateChanged?.Invoke(previousState, currentState);
                }
            }
        }
        private string currentState = "";

        public bool IsInvisible => FlyModeEnabled;

        public Vector3 Position
        {
            get => Body.Position;
            set
            {
                Body.MoveTo(value);
                spawnPoint = value;
            }
        }

        public event ValueChangedEventHandler<int> HealthPointsChanged;

        public event ValueChangedEventHandler<string> StateChanged;

        private Vector3 spawnPoint = Vector3.Zero;
        private int initialHealthPoints = 200;

        public PlayerCharacter(GameWorld gameWorld)
        {
            World = gameWorld;
            Camera = gameWorld.Game.Camera;

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 0.6f, 1.6f, 0.6f));
            Body.GravityMultiplier = 0;
            Body.EnableAutoJump = false;
            Body.DoesNotCollideWithOtherObjects = true;

            Body.AutoJump += (s,e)=> 
                Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));

            moveLeft = gameWorld.Game.InputScheme.MoveLeft;
            moveRight = gameWorld.Game.InputScheme.MoveRight;
            moveForward = gameWorld.Game.InputScheme.MoveForward;
            moveBackward = gameWorld.Game.InputScheme.MoveBackward;
            lookUp = gameWorld.Game.InputScheme.LookUp;
            lookDown = gameWorld.Game.InputScheme.LookDown;
            lookLeft = gameWorld.Game.InputScheme.LookLeft;
            lookRight = gameWorld.Game.InputScheme.LookRight;
            attack = gameWorld.Game.InputScheme.Attack;
            jump = gameWorld.Game.InputScheme.Jump;
            moveUp = gameWorld.Game.InputScheme.MoveUp;
            moveDown = gameWorld.Game.InputScheme.MoveDown;
            switchInputScheme = gameWorld.Game.InputScheme.SwitchInputScheme;
            debugTrigger1 = gameWorld.Game.InputScheme.DebugTrigger1;

            HealthPointsChanged += (old, current) =>
            {
                if (old < current) 
                    World.Game.OverlayColor.Clear().Flash(
                        Color.Green.WithAlpha(0.5f));
                if (old > current)
                    World.Game.OverlayColor.Clear().Flash(
                        Color.Red.WithAlpha(0.5f));
                if (current == 0)
                {
                    World.Game.OverlayColor.Clear().Flash(Color.Black, 1);
                    HealthPoints = initialHealthPoints;
                    Position = spawnPoint;
                }
            };

            FlyModeEnabled = World.Game.CreatorMode;
        }

        public static ICharacter Create(GameWorld gameWorld)
        {
            return new PlayerCharacter(gameWorld);
        }

        public void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            PrimitiveTypeParser.TryAssign(parameters, this, true);
        }

        private bool loadedOnce = false;

        public void Update(TimeSpan delta)
        {
            if (!loadedOnce && World.Game.Resources.LoadingTasksPending == 0)
            {
                loadedOnce = true;
                WeaponModel.Animation.Animation.Play();
            }

            if (!loadedOnce) return;

            PhysicsHelper.ApplyAccerlationToVelocity(ref rotationDeg,
                RotationUserInput * LookAccerlationDeg, delta);
            PhysicsHelper.ApplyDragToVelocity(ref rotationDeg,
                LookDragDeg, delta);

            Camera.Rotate(Angle.Deg(rotationDeg.X), Angle.Deg(rotationDeg.Y));

            float cameraX = Angle.Rad(Camera.Orientation.X + Angle.Pi, true);
            cameraX = Math.Max(cameraX, Angle.Deg(100));
            cameraX = Math.Min(cameraX, Angle.Deg(260));
            cameraX = Angle.Rad(cameraX - Angle.Pi, true);
            Camera.RotateTo(cameraX, Camera.Orientation.Y, 0);

            if (Position.Y < FallDeathYTreshold && HealthPoints > 0 && 
                !FlyModeEnabled)
            {
                HealthPoints = 0;
            }

            if (World.Game.CreatorMode && debugTrigger1.IsActivated) 
                Log.Trace($"<Position>{Body.Position.X:F1}," +
                    $"{(Body.Position.Y + 1):F1},{Body.Position.Z:F1}" +
                    $"</Position>");

            if (World.Game.CreatorMode && switchInputScheme.IsActivated) 
                FlyModeEnabled = !FlyModeEnabled;

            if (FlyModeEnabled)
            {
                headShift = Vector3.Zero;
                UpdateWithoutGravity();
            }
            else
            {
                headShift = Math.Min(1, 
                    Body.Velocity.Length() / 3) * new Vector3(0,
                        MathHelper.CalculateTimeSine(1.85, 0.02), 0);
                UpdateWithGravity();
            }

            Camera.MoveTo(Body.BoundingBox.PivotBottom() +
                new Vector3(0, 1.45f, 0) + headShift);

            if (FlyModeEnabled)
            {
                TrippingCubesGame.DebugUpdateText +=
                    $"Player: {Body.Position:F2} - {HealthPoints}HP\n";
            } else
            {
                TrippingCubesGame.DebugUpdateText += $"{HealthPoints}HP\n";
            }

            if (WeaponModel != null && !FlyModeEnabled)
            {
                WeaponModel.Transformation =
                    MathHelper.CreateTransformation(
                        Body.BoundingBox.PivotBottom(), new Vector3(-1,1,1), 
                        Quaternion.CreateFromYawPitchRoll(
                            Angle.Rad(Camera.Orientation.Y), 0, 0));
                WeaponModel.Update(delta);
                WeaponModel.Animation.Animation.SetPlaybackRange(
                    "attack", true);

                bool performAttack = false;

                if ((attack?.IsActivated ?? false) && 
                    (DateTime.Now - lastAttack) > attackFrequency)
                {
                    lastAttack = DateTime.Now;

                    WeaponModel.Animation.Animation.Stop();
                    WeaponModel.Animation.Animation.Play();

                    performAttack = true;
                }

                foreach (var entity in World.Entities)
                {
                    if (entity != this)
                    {
                        if (performAttack && entity is ICharacter character)
                        {
                            Vector3 direction = Body.Position -
                                character.Body.Position;
                            Angle directionAngle =
                                MathHelper.CreateOrientationY(direction);
                            Angle offset =
                                MathHelper.CalculateOrientationDifference(
                                    directionAngle,
                                    Angle.Rad(Camera.Orientation.Y - Angle.Pi,
                                    true), true);

                            if (direction.Length() < 2 && offset.Degrees < 15)
                            {
                                character.HealthPoints -= DamagePoints;
                            }
                        }

                        if (entity is Entity typedEntity)
                        {
                            if (typedEntity.Body.BoundingBox.IntersectsWith(
                                Body.BoundingBox))
                            {
                                typedEntity.Touched(this);
                            }
                        }
                    }
                }
            }
        }

        public void Redraw(IRenderContext context)
        {
            if (!FlyModeEnabled) WeaponModel?.Draw(context);
        }

        private void UpdateWithGravity()
        {
            Vector3 userInput = Camera.AlignVector(MovementUserInput,
                true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            if (Body.Resting.Y == -1)
            {
                if (jump.IsActivated)
                    Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));
                else
                {
                    Body.ApplyAcceleration(userInput * AccerlationFloor);
                    Body.ApplyVelocityChange(userInput * 0.25f *
                        Math.Max(0, (1.75f - (Body.Velocity).Length())));
                }
            }
            else Body.ApplyForce(userInput * AccerlationAir);            
        }

        private void UpdateWithoutGravity()
        {
            Vector3 userInput = Camera.AlignVector(MovementUserInput +
                new Vector3(0, (moveUp?.Value ?? 0) -
                (moveDown?.Value ?? 0), 0), true, false);

            if (userInput.Length() > 1)
                userInput = Vector3.Normalize(userInput);

            Body.ApplyAcceleration(userInput * AccerlationFly);
        }
    }
}
