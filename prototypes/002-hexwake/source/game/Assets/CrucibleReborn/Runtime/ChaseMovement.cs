using UnityEngine;

namespace GameDevHarness.CrucibleReborn
{
    public sealed class ChaseMovement
    {
        public Vector3 HorizontalVelocity { get; private set; }
        public float VerticalVelocity { get; private set; }
        public int JumpCount { get; private set; }
        bool launchPending;
        double launchRemaining;
        bool previousHop;
        double jumpBuffer;
        public const float AirWishSpeed = 3.2f;
        public const float JumpBufferSeconds = .1f;

        public void Reset()
        {
            HorizontalVelocity = Vector3.zero; VerticalVelocity = 0; JumpCount = 0;
            launchPending = false; launchRemaining = 0; previousHop = false; jumpBuffer = 0;
        }

        // The caller supplies world-space launch velocity and moves the returned displacement through collision.
        public void Launch(Vector3 horizontal, float vertical)
        {
            horizontal.y = 0;
            HorizontalVelocity = horizontal;
            VerticalVelocity = vertical;
            launchPending = true;
            launchRemaining = .35;
            jumpBuffer = 0;
        }

        public Vector3 Step(Vector3 input, bool grounded, bool hopHeld, float dt,
            float speedMultiplier = 1, float airSteeringMultiplier = 1)
        {
            input.y = 0;
            if (input.sqrMagnitude > 1) input.Normalize();
            // CharacterController.isGrounded still describes the move before a launch.
            if (launchPending) { grounded = false; launchPending = false; }
            bool launching = launchRemaining > 0;
            launchRemaining = System.Math.Max(0, launchRemaining - dt);
            // A short press immediately before contact survives a one-frame grounded delay.
            // Held Space still hops on every landing; a consumed press cannot double-jump.
            if (hopHeld && !previousHop && !launching) jumpBuffer = JumpBufferSeconds;
            previousHop = hopHeld;
            Vector3 before = HorizontalVelocity;
            Vector3 horizontalDisplacement;
            bool takeoff = grounded && (hopHeld || jumpBuffer > 0) && !launching;
            if (grounded) VerticalVelocity = takeoff ? 7.6f : -2;
            if (takeoff)
            {
                JumpCount++;
                jumpBuffer = 0;
                if (input.sqrMagnitude > .0001f)
                {
                    // Held hops from rest still receive the ground's running launch.
                    // Existing faster momentum keeps its direction and accumulated speed.
                    if (HorizontalVelocity.magnitude < 6.8f * speedMultiplier - .001f)
                        HorizontalVelocity = input * (6.8f * speedMultiplier);
                    HorizontalVelocity += HorizontalVelocity.normalized * (.65f * speedMultiplier);
                }
            }
            if (grounded && !takeoff && !launching)
            {
                Vector3 target = input * (6.8f * speedMultiplier);
                float acceleration = (input.sqrMagnitude > .0001f ? 110 : 90) * speedMultiplier;
                float acceleratingTime = Mathf.Min(dt, (target - before).magnitude / acceleration);
                HorizontalVelocity = Vector3.MoveTowards(HorizontalVelocity, input * (6.8f * speedMultiplier),
                    acceleration * dt);
                // Integrate only until reaching the target, then carry it for the remaining frame.
                horizontalDisplacement = (before + HorizontalVelocity) * (.5f * acceleratingTime) +
                    HorizontalVelocity * (dt - acceleratingTime);
            }
            else
            {
                if (input.sqrMagnitude > .0001f)
                {
                    Vector3 direction = input.normalized;
                    // A small directional projection cap allows air strafing without ground-like reversal.
                    float add = Mathf.Clamp(AirWishSpeed * speedMultiplier * input.magnitude -
                        Vector3.Dot(HorizontalVelocity, direction), 0, 9 * speedMultiplier * dt);
                    HorizontalVelocity += direction * add;
                    if (before.sqrMagnitude > .0001f)
                    {
                        // Forward speed must not veto the sideways part of W+A/D
                        // or a small mouse-relative turn. Rotate momentum with
                        // bounded perpendicular acceleration, not a ground reversal.
                        Vector3 lateral = direction - Vector3.Project(direction, before);
                        HorizontalVelocity += lateral * (9 * speedMultiplier *
                            airSteeringMultiplier * dt);
                    }
                }
                else HorizontalVelocity = Vector3.MoveTowards(HorizontalVelocity, Vector3.zero, .4f * dt);
                // Ground contact and the hop cap cannot erase an ability's short impulse window.
                HorizontalVelocity = Vector3.ClampMagnitude(HorizontalVelocity,
                    launching ? Mathf.Max(9 * speedMultiplier, before.magnitude) : 9 * speedMultiplier);
                horizontalDisplacement = (before + HorizontalVelocity) * (.5f * dt);
            }
            float verticalBefore = VerticalVelocity;
            VerticalVelocity -= 20 * dt;
            jumpBuffer = System.Math.Max(0, jumpBuffer - dt);
            return horizontalDisplacement + Vector3.up * ((verticalBefore + VerticalVelocity) * .5f * dt);
        }

        public void ResolveSides(Vector3 actualDisplacement)
        {
            actualDisplacement.y = 0;
            HorizontalVelocity = actualDisplacement.sqrMagnitude < .000001f ? Vector3.zero :
                Vector3.Project(HorizontalVelocity, actualDisplacement.normalized);
        }
    }
}
