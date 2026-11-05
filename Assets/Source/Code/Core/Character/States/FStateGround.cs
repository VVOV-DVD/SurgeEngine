using SurgeEngine.Source.Code.Core.Character.States.BaseStates;
using SurgeEngine.Source.Code.Core.Character.States.Characters.Sonic.SubStates;
using SurgeEngine.Source.Code.Core.Character.System;
using SurgeEngine.Source.Code.Gameplay.CommonObjects;
using SurgeEngine.Source.Code.Infrastructure.Config;
using SurgeEngine.Source.Code.Infrastructure.Custom;
using UnityEngine;

namespace SurgeEngine.Source.Code.Core.Character.States
{
    public sealed class FStateGround : FCharacterState, IDamageableState
    {
        private const float BreakableThreshold = 0.7f; // If Speed Percent is greater than this, character will destroy Breakable objects

        public FStateGround(CharacterBase owner) : base(owner)
        {
            
        }

        public override void OnEnter()
        {
            base.OnEnter();
            
            Kinematics.SetDetachTime(0f);
        }

        public override void OnTick(float dt)
        {
            base.OnTick(dt);
            
            Model.RotateBody(Kinematics.Velocity, Kinematics.Normal, 18);

            if (!Character.Flags.HasFlag(FlagType.OutOfControl))
            {
                if (Input.APressed)
                {
                    Kinematics.SetDetachTime(0.1f);
                    StateMachine.SetState<FStateJump>();
                }
            }
        }

        public override void OnFixedTick(float dt)
        {
            base.OnFixedTick(dt);
            
            if (Kinematics.Speed / Character.Config.topSpeed > BreakableThreshold)
            {
                HurtBox.CreateAttached(Character, Character.transform, new Vector3(0f, 0f, -0.1f), new Vector3(0.5f, 1f, 1.15f), HurtBoxTarget.Breakable);
            }

            PhysicsConfig config = Character.Config;
            float distance = config.EvaluateCastDistance(Kinematics.Speed / config.topSpeed);
            bool ground = Kinematics.CheckForGround(out RaycastHit data, castDistance: distance);
            bool aheadSupport = false;
            if (!ground && Kinematics.Speed > Mathf.Max(config.slopeMinSpeed, 8f))
                ground = Kinematics.CheckForGroundAhead(out data, out aheadSupport, distance);

            bool isWater = false;
            if (ground && data.transform != null)
            {
                isWater = data.transform.IsWater(out var surface);
                if (isWater)
                {
                    surface.Attach(Rigidbody.position, Character);

                    if (Kinematics.HorizontalVelocity.magnitude < surface.MinimumSpeed)
                    {
                        StateMachine.SetState<FStateAir>();
                        return;
                    }
                }
            }
            
            bool predictedGround = aheadSupport || Kinematics.CheckForPredictedGround(dt, distance, 8);
            if (ground)
            {
                Kinematics.Point = data.point;
                float normalDelta = Vector3.Angle(Kinematics.Normal, data.normal);
                bool canFollowSurface = predictedGround
                    || (Kinematics.Speed >= config.slopeMinSpeed && normalDelta <= 120f);

                if (canFollowSurface) Kinematics.RotateSnapNormal(data.normal);
                else
                {
                    Kinematics.Normal = Vector3.up;
                    StateMachine.SetState<FStateSlip>();
                }

                float maxGroundSpeed = config.maxSpeed;
                if (Character.StateMachine.GetState(out FBoost boost) && boost.Active)
                    maxGroundSpeed = Mathf.Max(maxGroundSpeed, boost.MaxBoostSpeed);
                Kinematics.ClampVelocityToMax(maxGroundSpeed);

                Kinematics.BasePhysics(Kinematics.Normal);
                if (!aheadSupport)
                {
                    if (!isWater || predictedGround)
                        Kinematics.Snap(Kinematics.Point, Kinematics.Normal);
                    else
                        Kinematics.SnapOnWater(data.point);
                }

                Kinematics.ProjectOnNormal();
                Kinematics.SlopePhysics();

                Kinematics.GroundTag.Value = data.transform.gameObject.GetGroundTag();
            }
            else
            {
                StateMachine.SetState<FStateAir>();
            }

            if (Kinematics.CheckForGroundWithDirection(out RaycastHit verticalHit, Rigidbody.transform.up) && Kinematics.Angle >= 90)
            {
                Kinematics.Point = verticalHit.point;
                Kinematics.Normal = verticalHit.normal;
            }
        }
    }

    public enum GroundTag
    {
        Grass,
        Concrete,
        Water
    }
}