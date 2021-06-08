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

using ShamanTK.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using TrippingCubes.Common;
using TrippingCubes.Entities.SteeringSystems;
using TrippingCubes.Entities.Behaviors;
using TrippingCubes.Physics;
using TrippingCubes.Entities.DecisionMaking;
using WPP = 
    TrippingCubes.Entities.SteeringSystems.WeightedPriorityParameter;
using ShamanTK.Common;
using ShamanTK.IO;
using ShamanTK;

namespace TrippingCubes.Entities
{
    class AdvancedCharacter : StateMachine, ICharacter
    {
        public const string AnimationNameDeath = "die";
        public const string AnimationNameShoot = "shoot";
        public const string AnimationNameIdle = "idle";
        public const string AnimationNameRunning = "running";
        public const string AnimationNameAttack = "attack";
        public const float MovementSpeedFadeTreshold = 3.0f;
        public const float JumpVelocity = 7;

        public class SteeringSystem : SteeringSystemWeightedPriorities
        {
            //TODO: Repair regions in code
            public ErraticOutburstBehavior<WPP> Erratic { get; }

            #region "Collision avoidance" group
            public CollisionAvoidanceBehavior<WPP> AvoidCharacters { get; }
            public ObstacleAvoidanceBehavior<WPP> AvoidWalls { get; }
            #endregion

            #region "Separation" group
            public FlockSeparationBehavior<WPP> FlockSeparate { get; }
            #endregion

            public FlockCohesionBehavior<WPP> FlockCohese { get; }

            #region "Pursuit" group
            public ArriveBehavior<WPP> Arrive { get; }
            public AlignTargetBehavior<WPP> AlignTarget { get; }
            #endregion

            #region "Path following" group
            public PathFollowingBehavior<WPP> PathFollow { get; }
            public AlignVelocityBehavior<WPP> AlignVelocity { get; }
            #endregion

            #region "Wandering" group (fallback)
            public WanderBehavior<WPP> Wander { get; }
            #endregion 

            public SteeringSystem(IEntity self) : base(self)
            {
                Wander = new WanderBehavior<WPP>(self)
                { Parameters = new WPP(1, 0.24f) };              

                PathFollow = new PathFollowingBehavior<WPP>(self)
                { Parameters = new WPP(2, 1) };

                Arrive = new ArriveBehavior<WPP>(self)
                { Parameters = new WPP(3, 1) };
                AlignTarget = new AlignTargetBehavior<WPP>(self)
                { Parameters = new WPP(3, 1f) };

                Erratic = new ErraticOutburstBehavior<WPP>(self) 
                { Parameters = new WPP(4, 1f) };

                FlockSeparate = new FlockSeparationBehavior<WPP>(self)
                { Parameters = new WPP(0.42f) };
                AvoidWalls = new ObstacleAvoidanceBehavior<WPP>(self)
                { Parameters = new WPP(0.84f) };
                AlignVelocity = new AlignVelocityBehavior<WPP>(self)
                { Parameters = new WPP(1) };
            }
        }

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

        public int HealthPoints
        {
            get => healthPoints;
            set
            {
                if (value != healthPoints)
                {
                    int previousValue = healthPoints;
                    healthPoints = value;
                    HealthPointsChanged?.Invoke(previousValue, healthPoints);
                }
            }
        }
        private int healthPoints = 80;

        public bool IsInvisible { get; private set; } = false;

        public RigidBody Body { get; }

        public GameWorld World { get; }

        public SteeringSystem Behaviors { get; }

        public ResourcePath CharacterModelPath
        {
            get => characterModelPath;
            set
            {
                if (value != characterModelPath)
                {
                    World.Game.Models.LoadModel(value, (success, model, exc) =>
                    {
                        if (success) CharacterModel = model;
                        else Log.Warning("A character model with the path " +
                                $"'{value}' couldn't be loaded.", exc);
                    });
                    characterModelPath = value;
                }
            }
        }
        private ResourcePath characterModelPath = ResourcePath.Empty;

        public ResourcePath CharacterWeaponPath
        {
            get => characterWeaponPath;
            set
            {
                if (value != characterWeaponPath)
                {
                    World.Game.Models.LoadModel(value, (success, model, exc) =>
                    {
                        if (success) WeaponModel = model;
                        else Log.Warning("A weapon model with the path " +
                                $"'{value}' couldn't be loaded.", exc);
                    });
                    characterWeaponPath = value;
                }
            }
        }
        private ResourcePath characterWeaponPath = ResourcePath.Empty;

        public string PatrolPathName
        {
            get => patrolPathName;
            set
            {
                if (value != null)
                {
                    if (World.Paths.TryGetValue(value, out PathLinear path))
                        Path = path;
                }
                else Path = null;

                patrolPathName = value;
            }
        }
        private string patrolPathName = null;

        public PathLinear Path
        {
            get => Behaviors.PathFollow.Path;
            set => Behaviors.PathFollow.Path = value;
        }

        public string Name { get; set; } = 
            $"Unnamed{nameof(AdvancedCharacter)}";

        public Model CharacterModel { get; private set; }

        public Model WeaponModel { get; private set; }

        public Vector3 Position
        {
            get => Body.Position;
            set => Body.MoveTo(value);
        }

        public Vector3 Scale { get; set; } = new Vector3(0.9f);

        public Angle FieldOfVision { get; set; } = Angle.Deg(180);

        public float SightDistance { get; set; } = 12;

        protected Vector3? AssumedPlayerPosition { get; set; }

        protected Vector3? PlayerPosition => Player?.Body.Position;

        protected ICharacter Player
        {
            get
            {
                if (player == null)
                {
                    foreach (IEntity entity in World.Entities)
                    {
                        if (entity is IPlayerCharacter playerEntity)
                        {
                            player = playerEntity;
                            break;
                        }
                    }
                }

                return player;
            }
        }
        private ICharacter player;

        public float AttackTypeMeleeArrivalRadius { get; set; } = 3.25f;

        public float AttackTypeRangeArrivalRadius { get; set; } = 9.25f;

        public bool PreferMeleeAttack
        {
            set
            {
                if (value)
                {
                    Behaviors.Arrive.DecelerateRadius = 
                        AttackTypeMeleeArrivalRadius;
                }
                else
                {
                    Behaviors.Arrive.DecelerateRadius = 
                        AttackTypeRangeArrivalRadius;
                }

                Behaviors.Arrive.ArrivalRadius =
                    Behaviors.Arrive.DecelerateRadius - 2;
            }
        }

        public double MeleeDamageFrequencySeconds
        {
            get => meleeDamageFrequency.TotalSeconds;
            set => meleeDamageFrequency = TimeSpan.FromSeconds(value);
        }
        private TimeSpan meleeDamageFrequency = TimeSpan.FromSeconds(1.5f);

        public int MeleeDamage { get; set; } = 5;

        public double RangeDamageFrequencySeconds
        {
            get => rangeDamageFrequency.TotalSeconds;
            set => rangeDamageFrequency = TimeSpan.FromSeconds(value);
        }
        private TimeSpan rangeDamageFrequency = TimeSpan.FromSeconds(1.5f);

        public int RangeDamage { get; set; } = 2;

        public float DamageInflictIntervalSeconds
        {
            set => MeleeDamageFrequencySeconds = 
                RangeDamageFrequencySeconds = value;
        }

        public bool EnableWander
        {
            get => Behaviors.Wander.Parameters.Weight > 0;
            set
            {
                if (value)
                {
                    Behaviors.Wander.Parameters = new WPP(1, 0.24f);
                    Behaviors.Erratic.Parameters = new WPP(4, 1f);
                }
                else
                {
                    Behaviors.Wander.Parameters = new WPP(1, 0);
                    Behaviors.Erratic.Parameters = new WPP(4, 0);
                }
            }
        }

        public float OrientationDegrees
        {
            get => Body.Orientation.Degrees;
            set => Body.Orientation = Angle.Deg(value, true);
        }

        public Vector3 ShootingOriginOffset { get; set; } =
            new Vector3(-0.025f, 0.78f, 0);

        public Vector3 ShootingRayScale { get; set; } =
            new Vector3(0.05f, 0.05f, 9.25f);

        public event ValueChangedEventHandler<int> HealthPointsChanged;

        public event ValueChangedEventHandler<string> StateChanged;

        private DateTime lastDamageDealAttemptTime;
        private DateTime lastDamageDealTime;
        private DateTime lastDamageReceiveTime;
        private bool lastDamageDealAttemptWasRange;

        private MeshBuffer shootingRay;

        public AdvancedCharacter(GameWorld gameWorld)
        {
            World = gameWorld;            

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 0.6f, 1.6f, 0.6f));
            Body.EnableAutoJump = true;
            Body.AutoJump += (s, e) =>
                Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));
            Body.DoesNotCollideWithOtherObjects = true;

            Behaviors = new SteeringSystem(this);

            PreferMeleeAttack = true;

            gameWorld.Game.Resources.LoadMesh(MeshData.Block).Subscribe(
                r => shootingRay = r);

            HealthPointsChanged += OnHealthPointsChanged;
        }

        private void OnHealthPointsChanged(int previousValue, int currentValue)
        {
            lastDamageReceiveTime = DateTime.Now;
        }

        public void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            PrimitiveTypeParser.TryAssign(parameters, this, true);
        }

        public override void Update(TimeSpan delta)
        {
            if (CharacterModel == null || WeaponModel == null ||
                Player == null) return;

            base.Update(delta);

            Behaviors.Update();
            Body.ApplyAcceleration(Behaviors.AccelerationLinear);
            Body.ApplyAngularAcceleration(Behaviors.AccelerationAngular);

            CharacterModel.Transformation = WeaponModel.Transformation =
                MathHelper.CreateTransformation(Body.Position, Scale,
                Quaternion.CreateFromYawPitchRoll(Body.Orientation, 0, 0));
            CharacterModel.Update(delta);
            WeaponModel.Update(delta);

            CurrentState = CurrentStateBehavior.Method.Name;
        }

        public void Redraw(IRenderContext c)
        {
            float damageDelta = 
                (float)(DateTime.Now - lastDamageReceiveTime).TotalSeconds;
            float flickeringVisualIntensity = new Random().Next(
                (int)Math.Min(100, (damageDelta * 150)), 100) / 100f;
            
            c.Opacity = flickeringVisualIntensity;

            CharacterModel?.Draw(c);
            WeaponModel?.Draw(c);

            float shootDelta = 
                (float)(DateTime.Now - lastDamageDealAttemptTime).TotalSeconds;
            float shootVisualIntensity = Math.Max(0, Math.Min(1,
                (float)Math.Log2(shootDelta / 0.6) / (-2) + 0.1f));

            if (lastDamageDealAttemptWasRange && shootVisualIntensity > 0.01f)
            {
                c.Mesh = shootingRay;
                c.Texture = null;
                c.Color = new Color(Color.Red * shootVisualIntensity);
                c.Opacity = shootVisualIntensity;
                c.Transformation = MathHelper.CreateTransformation(
                    Position + ShootingOriginOffset,
                    ShootingRayScale,
                    Quaternion.CreateFromYawPitchRoll(Body.Orientation, 
                    Angle.Deg(-6.9f), 0));
                c.Draw();
            }
        }

        protected override void PerformInitialBehavior()
        {
            TransitionTo(PerformPatrol);
        }

        protected void PerformPatrol()
        {
            SetCurrentAnimations(AnimationNameIdle, AnimationNameRunning,
                true, Body.Velocity.Length() / MovementSpeedFadeTreshold);

            if (CheckPlayerInSight()) TransitionTo(PerformPursue);
            else AssumedPlayerPosition = null;

            if (HealthPoints <= 0) TransitionTo(PerformDeath);
        }

        protected void PerformPursue()
        {
            SetCurrentAnimations(AnimationNameIdle, AnimationNameRunning,
                true, Body.Velocity.Length() / MovementSpeedFadeTreshold);

            bool assumedTargetInRange = AssumedPlayerPosition.HasValue &&
                (Position - AssumedPlayerPosition.Value).Length() <
                Behaviors.Arrive.DecelerateRadius;

            if (CheckPlayerInSight())
            {
                AssumedPlayerPosition = PlayerPosition;
                if (assumedTargetInRange) TransitionTo(PerformAttack);
            }
            else if (assumedTargetInRange) TransitionTo(PerformPatrol);

            Behaviors.AlignTarget.TargetPosition =
                Behaviors.Arrive.TargetPosition = AssumedPlayerPosition;

            if (HealthPoints <= 0) TransitionTo(PerformDeath);
        }

        protected void PerformAttack()
        {
            Vector3 directionToTarget = PlayerPosition.Value - Position;
            float distanceToTarget = directionToTarget.Length();
            Angle orientationOffsetToTarget =
                    MathHelper.CalculateOrientationDifference(
                    MathHelper.CreateOrientationY(directionToTarget),
                    Body.Orientation, true);
            TimeSpan timeSinceLastDamageDealAttempt = 
                DateTime.Now - lastDamageDealAttemptTime;
            float animationFade = 
                Body.Velocity.Length() / MovementSpeedFadeTreshold;

            Behaviors.AlignTarget.TargetPosition =
                Behaviors.Arrive.TargetPosition = AssumedPlayerPosition =
                PlayerPosition;

            if (distanceToTarget < Behaviors.Arrive.ArrivalRadius
                && CheckPlayerInSight())
            {
                if (distanceToTarget < AttackTypeMeleeArrivalRadius)
                {
                    SetCurrentAnimations(AnimationNameAttack, 
                        AnimationNameRunning, true, animationFade);

                    if (timeSinceLastDamageDealAttempt > meleeDamageFrequency)
                    {
                        lastDamageDealAttemptTime = DateTime.Now;
                        lastDamageDealAttemptWasRange = false;

                        if (orientationOffsetToTarget.Degrees < 25)
                        {
                            Player.HealthPoints -= MeleeDamage;
                            lastDamageDealTime = DateTime.Now;
                        }
                    }
                }
                else
                {
                    SetCurrentAnimations(AnimationNameShoot,
                        AnimationNameRunning, false, animationFade);

                    if (timeSinceLastDamageDealAttempt > rangeDamageFrequency)
                    {
                        lastDamageDealAttemptTime = DateTime.Now;
                        lastDamageDealAttemptWasRange = true;

                        if (orientationOffsetToTarget.Degrees < 6)
                        {
                            Player.HealthPoints -= RangeDamage;
                            lastDamageDealTime = DateTime.Now;
                        }
                    }
                }
            } else if (distanceToTarget > Behaviors.Arrive.DecelerateRadius)
            TransitionTo(PerformPursue);

            if (HealthPoints <= 0) TransitionTo(PerformDeath);
        }

        protected void PerformDeath()
        {
            IsInvisible = true;
            SetCurrentAnimations(AnimationNameIdle, AnimationNameDeath,
                false, 1);
            Behaviors.IsEnabled = false;
        }

        protected void SetCurrentAnimations(string primaryAnimationName,
            string secondaryAnimationName, bool loop, float fade)
        {
            CharacterModel.SetAnimations(primaryAnimationName,
                secondaryAnimationName, loop);
            WeaponModel.SetAnimations(primaryAnimationName,
                secondaryAnimationName, loop);

            if (CharacterModel.Animation != null)
            {
                CharacterModel.Animation.OverlayInfluence = fade;
            }

            if (WeaponModel.Animation != null)
            {
                WeaponModel.Animation.OverlayInfluence = fade;
            }
        }

        protected Angle GetRotationToPlayer()
        {
            if (Player != null)
            {
                Vector3 directionToPlayer =
                    Player.Body.Position - Body.Position;
                return MathHelper.CalculateOrientationDifference(
                    MathHelper.CreateOrientationY(directionToPlayer),
                    Body.Orientation, true);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        protected bool CheckPlayerInSight()
        {
            return CheckPlayerInSight(FieldOfVision, SightDistance);
        }

        protected bool CheckPlayerInSight(Angle fieldOfVision,
            float sightDistance)
        {
            if (Player == null) return false;

            BoundingBox raycastBox = new BoundingBox(Body.Position +
                new Vector3(0, Body.BoundingBox.Dimensions.Y, 0),
                new Vector3(0.1f));
            Vector3 raycastDirection = Player.Body.Position - Body.Position;

            if (!Player.IsInvisible &&
                raycastDirection.Length() < sightDistance &&
                !World.Physics.RaycastVolumetric(raycastBox, raycastDirection,
                out _, out _))
            {
                Angle rotationToPlayer =
                    MathHelper.CalculateOrientationDifference(
                    MathHelper.CreateOrientationY(raycastDirection),
                    Body.Orientation, true);

                return rotationToPlayer < fieldOfVision;
            }
            else return false;
        }
    }
}
