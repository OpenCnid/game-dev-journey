using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GameDevHarness.CrucibleReborn
{
    // D's pre-swept live jumps retained; no teleport-to-player shortcut.
    public sealed class EnemyMotor
    {
        public readonly NavMeshAgent Agent;
        public readonly Transform Root;
        public int JumpsStarted, JumpsCompleted;
        public string LastJumpRejection;
        public bool Jumping { get; private set; }
        public float Speed;
        public int Plans, PlansDeferred, DestinationPlans, DestinationsSkipped, JumpSweeps;
        Vector3 jumpStart, jumpEnd, previousTarget, plannedTarget;
        float jumpAge, jumpDuration;
        double nextPlan, previousTargetTime, destinationTime;
        bool perched, paused, retired, planningStarted, targetObserved, destinationPlanned, planningQueued;
        readonly int id;
        readonly float initialSpeed, bodyHeight, bodyRadius;
        const int Solids = (1 << 8) | (1 << 9);
        // Bound expensive work across the swarm, not just within each motor.
        static int budgetFrame = -1, framePlans, frameJumpSweeps;
        static readonly Queue<EnemyMotor> duePlans = new Queue<EnemyMotor>();
        const int MaxFramePlans = 32, MaxFrameJumpSweeps = 4;

        public EnemyMotor(Transform root, float speed, int identity, float sizeScale)
        {
            Root = root; Speed = speed; initialSpeed = Mathf.Max(speed, 0.01f); id = identity;
            bodyHeight = 2.2f * sizeScale; bodyRadius = .42f * sizeScale;
            Agent = root.GetComponent<NavMeshAgent>() ?? root.gameObject.AddComponent<NavMeshAgent>();
            Agent.agentTypeID = NavMesh.GetSettingsByIndex(0).agentTypeID;
            Agent.height = bodyHeight; Agent.radius = bodyRadius;
            Agent.speed = Mathf.Max(0, speed); Agent.acceleration = 22; Agent.angularSpeed = 360;
            Agent.stoppingDistance = bodyRadius + .35f;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
            Agent.autoRepath = false;
            Agent.enabled = true;
        }

        public void Retire()
        {
            retired = true; paused = true;
            Agent.enabled = false;
        }

        public void SetPaused(bool value)
        {
            paused = value;
            if (Agent.enabled && Agent.isOnNavMesh && Agent.isStopped != (value || Speed <= 0))
                Agent.isStopped = value || Speed <= 0;
        }

        public void AdjustPosition(Vector3 point)
        {
            if (Jumping || perched) return;
            if (Agent.enabled && Agent.isOnNavMesh) Agent.Warp(point);
        }

        public void Tick(float dt, Vector3 playerFeet, double elapsed)
        {
            if (retired) return;
            // Slow/stun changes reach the real agent even between destination plans.
            float effectiveSpeed = Mathf.Max(0, Speed);
            if (Agent.speed != effectiveSpeed) Agent.speed = effectiveSpeed;
            if (Agent.enabled && Agent.isOnNavMesh)
                if (Agent.isStopped != (paused || effectiveSpeed <= 0))
                    Agent.isStopped = paused || effectiveSpeed <= 0;
            if (paused || effectiveSpeed <= 0) return;
            if (Jumping)
            {
                jumpAge += dt * Mathf.Clamp01(effectiveSpeed / initialSpeed);
                float t = Mathf.Clamp01(jumpAge / jumpDuration);
                Root.position = JumpPoint(t);
                Face(playerFeet);
                if (t >= 1)
                {
                    Jumping = false; JumpsCompleted++;
                    if (NavMesh.SamplePosition(Root.position, out var landing, 0.32f, NavMesh.AllAreas)
                        && Mathf.Abs(landing.position.y - Root.position.y) < 0.3f)
                    {
                        Agent.enabled = true; Agent.Warp(landing.position);
                        Agent.isStopped = false; perched = false;
                    }
                    else perched = true;
                }
                return;
            }
            if (!planningStarted)
            {
                planningStarted = true;
                nextPlan = elapsed + (id % 64) * 0.004;
            }
            if (elapsed < nextPlan) return;
            if (budgetFrame != Time.frameCount)
            {
                budgetFrame = Time.frameCount;
                framePlans = frameJumpSweeps = 0;
            }
            // FIFO keeps iteration order from starving later ids at low frame rates.
            // Discard destroyed or immobilized entries without holding the queue up.
            while (duePlans.Count > 0 && (duePlans.Peek().retired || duePlans.Peek().Root == null ||
                duePlans.Peek().paused || duePlans.Peek().Speed <= 0))
                duePlans.Dequeue().planningQueued = false;
            if (!planningQueued) { duePlans.Enqueue(this); planningQueued = true; }
            if (framePlans >= MaxFramePlans || duePlans.Peek() != this)
            { PlansDeferred++; return; }
            duePlans.Dequeue(); planningQueued = false;
            framePlans++; Plans++;
            Vector3 delta = playerFeet - Root.position;
            float horizontal = new Vector2(delta.x, delta.z).magnitude;
            nextPlan = elapsed + (perched || horizontal < 8 ? 0.22 : 0.85) + (id % 32) * 0.006;
            Vector3 approach = new Vector3(-delta.x, 0, -delta.z).normalized;
            Vector3 contactLanding = playerFeet + approach * 0.4f;
            if (horizontal <= 4 && Mathf.Abs(delta.y) > 0.4f && delta.y <= 2.4f && delta.y >= -4 &&
                FindFoot(contactLanding, out var elevated) && TryJump(elevated)) return;
            if (horizontal <= 4 && horizontal > 0.15f && Mathf.Abs(delta.y) <= 0.4f &&
                !Physics.Raycast(Vector3.Lerp(Root.position, playerFeet, 0.5f) + Vector3.up * 0.3f,
                    Vector3.down, 1, Solids, QueryTriggerInteraction.Ignore) &&
                FindFoot(playerFeet - approach * 0.4f, out var acrossGap) && TryJump(acrossGap)) return;
            if (perched)
            {
                if (delta.sqrMagnitude <= 1.2f * 1.2f) { Face(playerFeet); return; }
                if (FindFoot(contactLanding, out var playerLanding) && TryJump(playerLanding)) return;
                if (NavMesh.SamplePosition(Root.position - Vector3.up * 1.5f,
                    out var descent, 3, NavMesh.AllAreas) && TryJump(descent.position)) return;
                return;
            }
            if (!Agent.enabled || !Agent.isOnNavMesh) return;
            Vector3 forward = new Vector3(delta.x, 0, delta.z).normalized;
            if (horizontal > 2 && Physics.Raycast(Root.position + Vector3.up * 0.55f,
                forward, out var obstacle, 2.8f, 1 << 8) &&
                obstacle.collider.bounds.max.y - Root.position.y <= 2.4f)
            {
                Vector3 beyond = obstacle.point + forward * 1.4f;
                beyond.y = obstacle.collider.bounds.max.y + 0.2f;
                if (FindFoot(beyond, out var across) && TryJump(across)) return;
            }
            Vector3 velocity = Vector3.zero;
            if (targetObserved && elapsed > previousTargetTime)
            {
                velocity = (playerFeet - previousTarget) / (float)(elapsed - previousTargetTime);
                velocity.y = 0;
                velocity = Vector3.ClampMagnitude(velocity, 12);
            }
            previousTarget = playerFeet; previousTargetTime = elapsed; targetObserved = true;
            float threshold = horizontal < 8 ? 0.35f : 1.5f;
            Vector3 targetChange = playerFeet - plannedTarget;
            bool unchanged = new Vector2(targetChange.x, targetChange.z).sqrMagnitude < threshold * threshold &&
                Mathf.Abs(targetChange.y) < 0.3f;
            if (destinationPlanned && unchanged && elapsed - destinationTime < 2 &&
                (Agent.pathPending || (Agent.hasPath && !Agent.isPathStale &&
                    Agent.pathStatus == NavMeshPathStatus.PathComplete)))
            { DestinationsSkipped++; return; }
            if (!NavMesh.SamplePosition(playerFeet, out var target, 4, NavMesh.AllAreas)) return;
            Vector3 destination = target.position;
            // Rotate id-dependent roles slowly; converge on real feet for contact.
            // Sample and raycast on the NavMesh so offsets never cut through solids
            // or select a different height tier as a supposed flank.
            if (horizontal > 6 && Mathf.Abs(delta.y) < 0.4f &&
                Mathf.Abs(target.position.y - playerFeet.y) < 0.4f)
            {
                int role = (id + (int)(elapsed / 12)) % 4;
                float blend = Mathf.Clamp01((horizontal - 6) / 6);
                Vector3 offset = role == 1 ? Vector3.ClampMagnitude(velocity * 0.3f, 1.8f) :
                    role == 2 ? Vector3.Cross(Vector3.up, forward) * 1.5f :
                    role == 3 ? Vector3.Cross(forward, Vector3.up) * 1.5f : Vector3.zero;
                if (offset.sqrMagnitude > 0 &&
                    NavMesh.SamplePosition(destination + offset * blend, out var flank, 0.6f, NavMesh.AllAreas) &&
                    Mathf.Abs(flank.position.y - destination.y) < 0.35f &&
                    !NavMesh.Raycast(destination, flank.position, out _, NavMesh.AllAreas))
                    destination = flank.position;
            }
            // A baked radius projects a wall-hugging player inward. Subtract that
            // same-floor projection from the stop radius; contact still belongs
            // to the controller's actual distance/height/ClearPath checks.
            float projection = Mathf.Abs(target.position.y - playerFeet.y) < 0.4f ?
                new Vector2(target.position.x - playerFeet.x, target.position.z - playerFeet.z).magnitude : 0;
            Agent.stoppingDistance = Mathf.Max(0.05f, bodyRadius + .35f - projection);
            if (Agent.SetDestination(destination))
            {
                DestinationPlans++;
                destinationPlanned = true; plannedTarget = playerFeet; destinationTime = elapsed;
            }
        }

        static bool FindFoot(Vector3 desired, out Vector3 foot)
        {
            if (Physics.Raycast(desired + Vector3.up * 0.4f, Vector3.down,
                out var hit, 4.5f, Solids, QueryTriggerInteraction.Ignore))
            { foot = hit.point + Vector3.up * 0.025f; return true; }
            foot = default; return false;
        }

        bool TryJump(Vector3 end)
        {
            Vector3 delta = end - Root.position;
            float span = new Vector2(delta.x, delta.z).magnitude;
            if (span > 4 || span < 0.15f || delta.y > 2.4f || delta.y < -4)
            { LastJumpRejection = $"bounds span={span} rise={delta.y}"; return false; }
            if (frameJumpSweeps >= MaxFrameJumpSweeps) return false;
            frameJumpSweeps++; JumpSweeps++;
            jumpStart = Root.position; jumpEnd = end;
            Vector3 previous = jumpStart;
            for (int i = 1; i <= 16; i++)
            {
                Vector3 next = JumpPoint(i / 16f);
                Vector3 segment = next - previous;
                if (Physics.CapsuleCast(previous + Vector3.up * bodyRadius,
                    previous + Vector3.up * (bodyHeight - bodyRadius), bodyRadius - .03f, segment.normalized,
                    out var obstruction, segment.magnitude, Solids, QueryTriggerInteraction.Ignore))
                {
                    LastJumpRejection = $"solid={obstruction.collider.name} fraction={i / 16f} from={jumpStart} to={jumpEnd}";
                    return false;
                }
                previous = next;
            }
            Agent.enabled = false;
            destinationPlanned = false;
            jumpAge = 0; jumpDuration = Mathf.Max(0.42f, span / 6);
            Jumping = true; perched = false; JumpsStarted++;
            return true;
        }

        Vector3 JumpPoint(float fraction)
        {
            float arc = Mathf.Max(1.8f, Mathf.Max(0, jumpEnd.y - jumpStart.y) * 0.7f);
            return Vector3.Lerp(jumpStart, jumpEnd, fraction) +
                Vector3.up * (Mathf.Sin(fraction * Mathf.PI) * arc);
        }

        void Face(Vector3 point)
        {
            Vector3 forward = point - Root.position; forward.y = 0;
            if (forward.sqrMagnitude > 0.001f) Root.rotation = Quaternion.LookRotation(forward);
        }
    }
}
