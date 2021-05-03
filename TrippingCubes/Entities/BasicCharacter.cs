using ShamanTK.Common;
using System;
using System.Numerics;
using TrippingCubes.Entities.Behaviors;
using TrippingCubes.Common;
using TrippingCubes.Physics;
using ShamanTK.IO;
using ShamanTK;
using System.Linq;
using System.Collections.Generic;
using ShamanTK.Graphics;

namespace TrippingCubes.Entities
{
    class BasicCharacter : ICharacter
    {
        #region Constants and internally used class definitions
        public struct WeightParameter
        {
            public float Weight { get; }

            public WeightParameter(float weight) => Weight = weight;
        }

        public const string AnimationNameDeath = "die";
        public const string AnimationNameShoot = "shoot";
        public const string AnimationNameIdle = "idle";
        public const string AnimationNameRunning = "running";
        public const string AnimationNameAttack = "attack";
        public const float JumpVelocity = 7;
        #endregion

        #region Behaviors
        public AlignTargetBehavior<WeightParameter> Align { get; }

        public ArriveBehavior<WeightParameter> Arrive { get; }

        public WanderBehavior<WeightParameter> Wander { get; }

        private readonly Behavior<WeightParameter>[] behaviors;
        #endregion

        #region Knowledge and memory
        public ICharacter Player
        {
            get
            {
                if (player == null)
                {
                    player =
                        World.Entities.FirstOrDefault(e => e is ICharacter)
                        as ICharacter;
                }
                
                return player;
            }
        }
        private ICharacter player;

        public bool PlayerSeeable { get; private set; }

        public bool PlayerInSight { get; private set; }

        public Vector3 LastKnownPlayerPosition { get; private set; }

        public bool LastKnownPlayerPositionArrivedOnce { get; private set; } 
            = true;

        public Angle OrientationOffsetToTarget { get; private set; }
            = Angle.MaximumNormalized;

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

        private DateTime lastKnowledgeUpdate = DateTime.MinValue;
        private DateTime lastDamageInflict = DateTime.MinValue;
        private bool deathPerformed = false;
        #endregion

        #region Configurable behavior parameters
        public float MaximumSpeed { get; set; } = 3.0f;

        public float AttackTypeMeleeArrivalRadius { get; set; } = 3.25f;

        public float AttackTypeRangeArrivalRadius { get; set; } = 9.25f;

        public int MeleeDamage { get; set; } = 5;

        public int RangeDamage { get; set; } = 2;

        public float MeleeAttackDistanceLimit { get; set; } = 2;

        public float SightDistance { get; set; } = 12;

        public Angle FieldOfVision { get; set; } = Angle.Deg(180);

        public TimeSpan KnowledgeUpdateFrequency { get; set; }
            = TimeSpan.FromSeconds(0.1);

        public float DamageInflictIntervalSeconds
        {
            set => DamageInflictInterval = TimeSpan.FromSeconds(value);
        }

        public TimeSpan DamageInflictInterval { get; set; }
            = TimeSpan.FromSeconds(1.5f);

        public float MaximumAccelerationLinear
        {
            get => maximumAccelerationLinear;
            set => maximumAccelerationLinear = Math.Max(0, value);
        }
        private float maximumAccelerationLinear = 3.75f;

        public float MaximumAccelerationAngular
        {
            get => maximumAccelerationAngular;
            set => maximumAccelerationAngular = Math.Max(0, value);
        }
        private float maximumAccelerationAngular = Angle.Deg(205);

        public bool PreferMeleeAttack
        {
            set
            {
                if (value)
                {
                    Arrive.DecelerateRadius = AttackTypeMeleeArrivalRadius;
                }
                else
                {
                    Arrive.DecelerateRadius = AttackTypeRangeArrivalRadius;
                }

                Arrive.ArrivalRadius = Arrive.DecelerateRadius - 2;
            }
        }

        public bool EnableWander { get; set; } = true;

        public string PatrolPathName { get; set; } // Unused

        #endregion

        #region Visual representation parameters
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

        public Vector3 ShootingOriginOffset { get; set; } =
            new Vector3(-0.025f, 0.78f, 0);

        public Vector3 ShootingRayScale { get; set; } =
            new Vector3(0.05f, 0.05f, 9.25f);

        private MeshBuffer shootingRay;

        private DateTime lastDamageReceiveTime;
        private bool lastDamageDealAttemptWasRange = false;

        public Model CharacterModel { get; private set; }

        public Model WeaponModel { get; private set; }

        public Vector3 Scale { get; set; } = new Vector3(0.9f);

        public float OrientationDegrees
        {
            get => Body.Orientation.Degrees;
            set => Body.Orientation = Angle.Deg(value, true);
        }

        public Vector3 Position
        {
            get => Body.Position;
            set => Body.MoveTo(value);
        }
        #endregion

        #region Basic properties
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

        public string Name { get; set; } = $"Unnamed{nameof(BasicCharacter)}";

        public bool IsInvisible { get; private set; } = false;

        public RigidBody Body { get; }

        public GameWorld World { get; }
        #endregion

        public event ValueChangedEventHandler<int> HealthPointsChanged;

        public event ValueChangedEventHandler<string> StateChanged;

        public BasicCharacter(GameWorld gameWorld)
        {
            World = gameWorld;

            Body = gameWorld.Physics.AddNewBody(
                new BoundingBox(0, 0, 0, 0.6f, 1.6f, 0.6f));
            Body.EnableAutoJump = true;
            Body.AutoJump += (s, e) =>
                Body.ApplyVelocityChange(new Vector3(0, JumpVelocity, 0));
            //Body.DoesNotCollideWithOtherObjects = true;

            Align = new AlignTargetBehavior<WeightParameter>(this);
            Arrive = new ArriveBehavior<WeightParameter>(this);
            Wander = new WanderBehavior<WeightParameter>(this);

            behaviors = new Behavior<WeightParameter>[]
            {
                Align, Arrive, Wander
            };

            gameWorld.Game.Resources.LoadMesh(MeshData.Block).Subscribe(
                r => shootingRay = r);

            HealthPointsChanged += OnHealthPointsChanged;
        }

        private void OnHealthPointsChanged(int previousValue, int currentValue)
        {
            lastDamageReceiveTime = DateTime.Now;
        }

        public static ICharacter Create(GameWorld gameWorld)
        {
            return new BasicCharacter(gameWorld);
        }

        public void ApplyParameters(
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            PrimitiveTypeParser.TryAssign(parameters, this, true);
        }

        public void Update(TimeSpan delta)
        {
            if (CharacterModel == null || WeaponModel == null ||
                Player == null) return;

            if ((DateTime.Now - lastKnowledgeUpdate) >
                KnowledgeUpdateFrequency)
            {
                UpdateKnowledge();
                lastKnowledgeUpdate = DateTime.Now;
            }

            FindAndExecuteDecision();

            (Vector3 accelerationLinear, Angle accelerationAngular) = 
                CalculateCombinedAccelerations();

            Body.ApplyAcceleration(accelerationLinear);
            Body.ApplyAngularAcceleration(accelerationAngular);

            CharacterModel.Transformation = WeaponModel.Transformation =
                MathHelper.CreateTransformation(Body.Position, Scale,
                Quaternion.CreateFromYawPitchRoll(Body.Orientation, 0, 0));
            CharacterModel.Update(delta);
            WeaponModel.Update(delta);
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
                (float)(DateTime.Now - lastDamageInflict).TotalSeconds;
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

        protected virtual void FindAndExecuteDecision()
        {
            if (Player.IsInvisible && HealthPoints > 0)
            {
                PerformPatrol();
            }
            else
            {
                if (HealthPoints > 0)
                {
                    if (PlayerInSight)
                    {
                        if (LastKnownPlayerPositionArrivedOnce)
                        {
                            if ((Player.Body.Position - Body.Position).Length()
                                < AttackTypeMeleeArrivalRadius)
                            {
                                PerformAttackMelee();
                            }
                            else
                            {
                                PerformAttackRange();
                            }
                        }
                        else
                        {
                            PerformPursue();
                        }
                    }
                    else
                    {
                        if (LastKnownPlayerPositionArrivedOnce)
                        {
                            PerformPatrol();
                        }
                        else
                        {
                            PerformPursue();
                        }
                    }
                }
                else
                {
                    PerformDeath();
                }
            }
        }

        protected virtual void FindAndExecuteDecisionSimple()
        {
            if (Player?.IsInvisible ?? false)
            {
                PerformPatrol();
            }
            else
            {
                if (HealthPoints > 0)
                {
                    if (PlayerInSight)
                    {
                        if ((Body.Position - Player.Body.Position).Length()
                            <= Arrive.DecelerateRadius)
                        {
                            if ((Player.Body.Position - Body.Position).Length()
                                < MeleeAttackDistanceLimit)
                            {
                                PerformAttackMelee();
                            }
                            else
                            {
                                PerformAttackRange();
                            }
                        }
                        else
                        {
                            PerformPursue();
                        }
                    }
                    else
                    {
                        PerformPatrol();
                    }
                }
                else
                {
                    PerformDeath();
                }
            }
        }

        protected void UpdateKnowledge()
        {
            BoundingBox raycastBox = new BoundingBox(Body.Position +
                new Vector3(0, Body.BoundingBox.Dimensions.Y, 0),
                new Vector3(0.1f));
            Vector3 raycastDirection = Player.Body.Position - Body.Position;

            LastKnownPlayerPositionArrivedOnce |=
                (LastKnownPlayerPosition - Body.Position).Length() <=
                Arrive.ArrivalRadius;

            if (!Player.IsInvisible &&
                raycastDirection.Length() < SightDistance &&
                !World.Physics.RaycastVolumetric(raycastBox, raycastDirection,
                out _, out _))
            {
                PlayerSeeable = true;

                OrientationOffsetToTarget = 
                    MathHelper.CalculateOrientationDifference(
                    MathHelper.CreateOrientationY(raycastDirection),
                    Body.Orientation, true);

                PlayerInSight = OrientationOffsetToTarget < FieldOfVision;

                if (PlayerInSight)
                {
                    LastKnownPlayerPosition = Player.Body.Position;
                    LastKnownPlayerPositionArrivedOnce =
                        (LastKnownPlayerPosition - Body.Position).Length() <=
                        Arrive.ArrivalRadius;
                }
            }
            else
            {
                PlayerSeeable = false;
                PlayerInSight = false;
            }
        }

        protected (Vector3, Angle) CalculateCombinedAccelerations()
        {
            Vector3 newAccelerationLinear = Vector3.Zero;
            Angle newAccelerationAngular = 0;

            foreach (var behavior in behaviors)
            {
                behavior.Update();

                newAccelerationLinear +=
                    behavior.AccelerationLinear * behavior.Parameters.Weight;
                newAccelerationAngular +=
                    behavior.AccelerationAngular * behavior.Parameters.Weight;
            }

            if (newAccelerationLinear.Length() > MaximumAccelerationLinear)
                newAccelerationLinear =
                    Vector3.Normalize(newAccelerationLinear) *
                    MaximumAccelerationLinear;

            Angle accelerationAngularAbsolute =
                Math.Abs(newAccelerationAngular);
            if (newAccelerationAngular > MaximumAccelerationAngular)
            {
                newAccelerationAngular /= accelerationAngularAbsolute;
                newAccelerationAngular *= MaximumAccelerationAngular;
            }

            return (newAccelerationLinear, newAccelerationAngular);
        }

        private void SetCurrentAnimations(string primaryAnimationName,
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

        protected virtual void PerformPatrol()
        {
            CurrentState = nameof(PerformPatrol);

            Align.TargetPosition = Arrive.TargetPosition = 
                Player.Body.Position;

            Align.Parameters = new WeightParameter(0);
            Arrive.Parameters = new WeightParameter(0);
            Wander.Parameters = new WeightParameter(EnableWander ? 1 : 0);

            Wander.MaximumAccelerationLinear = 2.4f;

            SetCurrentAnimations(AnimationNameIdle, AnimationNameRunning, 
                true, Body.Velocity.Length() / MaximumSpeed);
        }        

        protected virtual void PerformDeath()
        {
            CurrentState = nameof(PerformDeath);

            if (!deathPerformed)
            {
                deathPerformed = true;

                Align.Parameters = new WeightParameter(0);
                Arrive.Parameters = new WeightParameter(0);
                Wander.Parameters = new WeightParameter(0);

                SetCurrentAnimations(AnimationNameIdle, AnimationNameDeath,
                    false, 1);

                Body.DoesNotCollideWithOtherObjects = true;
            }
        }

        protected virtual void PerformWanderAlert()
        {
            CurrentState = nameof(PerformWanderAlert);

            Align.TargetPosition = Arrive.TargetPosition =
                Player.Body.Position;

            Align.Parameters = new WeightParameter(0.2f);
            Arrive.Parameters = new WeightParameter(0.1f);
            Wander.Parameters = new WeightParameter(EnableWander ? 1 : 0);

            Wander.MaximumAccelerationLinear = 3.5f;

            SetCurrentAnimations(AnimationNameIdle, AnimationNameRunning,
                true, Body.Velocity.Length() / MaximumSpeed);
        }

        protected virtual void PerformPursue()
        {
            CurrentState = nameof(PerformPursue);

            Align.TargetPosition = Arrive.TargetPosition =
                LastKnownPlayerPosition;

            Align.Parameters = new WeightParameter(1);
            Arrive.Parameters = new WeightParameter(1);
            Wander.Parameters = new WeightParameter(0);

            /*
            if (Body.Resting.X != 0 || Body.Resting.Z != 0) 
                Wander.Parameters = new WeightParameter(2f);
            else Wander.Parameters = new WeightParameter(0.1f);
            */

            Wander.MaximumAccelerationLinear = 3.5f;

            SetCurrentAnimations(AnimationNameIdle, AnimationNameRunning,
                true, Body.Velocity.Length() / MaximumSpeed);
        }

        protected virtual void PerformAttackMelee()
        {
            CurrentState = "PerformAttack";
            //CurrentState = nameof(PerformAttackMelee);

            Align.TargetPosition = Arrive.TargetPosition =
                LastKnownPlayerPosition;

            Align.Parameters = new WeightParameter(1);
            Arrive.Parameters = new WeightParameter(0);
            Wander.Parameters = new WeightParameter(0);

            Wander.MaximumAccelerationLinear = 3.5f;

            SetCurrentAnimations(AnimationNameAttack, AnimationNameRunning,
                true, Body.Velocity.Length() / MaximumSpeed);

            Vector3 directionToTarget = Player.Body.Position - Position;
            float distanceToTarget = directionToTarget.Length();

            if ((DateTime.Now - lastDamageInflict) > DamageInflictInterval)
            {
                if (OrientationOffsetToTarget.Degrees < 25 &&
                    distanceToTarget < MeleeAttackDistanceLimit)
                {
                    Player.HealthPoints -= MeleeDamage;
                }                
                lastDamageInflict = DateTime.Now;
                lastDamageDealAttemptWasRange = false;
            }
        }

        protected virtual void PerformAttackRange()
        {
            CurrentState = "PerformAttack";
            //CurrentState = nameof(PerformAttackRange);

            Align.TargetPosition = Arrive.TargetPosition =
                LastKnownPlayerPosition;

            Align.Parameters = new WeightParameter(1);
            Arrive.Parameters = new WeightParameter(1);
            Wander.Parameters = new WeightParameter(0);

            Wander.MaximumAccelerationLinear = 3.5f;

            SetCurrentAnimations(AnimationNameShoot, AnimationNameRunning,
                false, Body.Velocity.Length() / MaximumSpeed);

            if ((DateTime.Now - lastDamageInflict) > DamageInflictInterval)
            {
                if (OrientationOffsetToTarget.Degrees < 15)
                {
                    Player.HealthPoints = Math.Max(0,
                        Player.HealthPoints - RangeDamage);
                }
                lastDamageInflict = DateTime.Now;
                lastDamageDealAttemptWasRange = true;
            }
        }
    }
}
