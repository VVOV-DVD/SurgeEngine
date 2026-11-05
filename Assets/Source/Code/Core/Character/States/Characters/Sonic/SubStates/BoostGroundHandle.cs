using SurgeEngine.Source.Code.Core.Character.System;
using SurgeEngine.Source.Code.Infrastructure.Config.Sonic;
using UnityEngine;

namespace SurgeEngine.Source.Code.Core.Character.States.Characters.Sonic.SubStates
{
    public class BoostGroundHandle : IBoostHandler
    {
        public void BoostHandle(CharacterBase character, BoostConfig config)
        {
            float dt = Time.deltaTime;
            var kinematics = character.Kinematics;
            var body = kinematics.Rigidbody;
            float speed = kinematics.Speed;

            if (character.StateMachine.GetState(out FBoost boost) && boost.Active)
            {
                Vector3 normal = kinematics.Normal.sqrMagnitude > 0.0001f
                    ? kinematics.Normal.normalized
                    : Vector3.up;
                Vector3 boostDirection = Vector3.ProjectOnPlane(body.transform.forward, normal);
                if (boostDirection.sqrMagnitude < 0.0001f)
                    boostDirection = Vector3.ProjectOnPlane(kinematics.Velocity, normal);
                if (boostDirection.sqrMagnitude < 0.0001f)
                    boostDirection = body.transform.forward;
                boostDirection.Normalize();

                if (character.Input.MoveVector == Vector3.zero)
                    kinematics.SetInputDir(boostDirection);

                float maxSpeed = Mathf.Max(config.MaxBoostSpeed,
                    character.Config.topSpeed * config.TopSpeedMultiplier);
                if (speed < maxSpeed)
                {
                    body.AddForce(boostDirection * (config.Acceleration * dt), ForceMode.Impulse);
                }
                else
                {
                    if (character.Flags.HasFlag(FlagType.Autorun)) return;

                    Vector3 target = body.linearVelocity.normalized * maxSpeed;
                    if (!body.isKinematic)
                        body.linearVelocity = Vector3.MoveTowards(body.linearVelocity, target, 8f * dt);
                }
            }
        }
    }
}