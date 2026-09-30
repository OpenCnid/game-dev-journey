using System;
using System.Collections.Generic;
using UnityEngine;

namespace Journey
{
    // Exact identity only: never merge nearby targets or distinct attack deadlines.
    // All current warnings share geometry/material/radius. Extend the key if that changes.
    public sealed class WarningCollector
    {
        readonly HashSet<Key> seen = new HashSet<Key>(CombatSimulation.Capacity);
        public readonly Vector3[] Positions = new Vector3[CombatSimulation.Capacity];
        public int Count { get; private set; }
        public int Candidates { get; private set; }
        public void Begin() { seen.Clear(); Count = Candidates = 0; }
        public void Add(Vector3 target, float remaining, bool deduplicate)
        {
            Candidates++;
            if (!deduplicate || seen.Add(new Key { Target = target, Remaining = remaining })) Positions[Count++] = target;
        }
        struct Key : IEquatable<Key>
        {
            public Vector3 Target;
            public float Remaining;
            public bool Equals(Key other) => Target.Equals(other.Target) && Remaining.Equals(other.Remaining);
            public override bool Equals(object other) => other is Key key && Equals(key);
            public override int GetHashCode() => unchecked(Target.GetHashCode() * 397 ^ Remaining.GetHashCode());
        }
    }
}
