using UnityEngine;
using System.Collections;
using System;

namespace K_PathFinder.VectorInt  {
    public abstract class VectorInt : IEquatable<VectorInt> {
        protected int[] _value;

        protected VectorInt(int vectorSize) {
            _value = new int[vectorSize];
        }



        public static bool operator ==(VectorInt a, VectorInt b) {
            if (ReferenceEquals(a, b))
                return true;

            if (((object)a == null) || ((object)b == null) || a._value.Length != b._value.Length)
                return false;

            for (int i = 0; i < a._value.Length; i++) 
                if (a._value[i] != b._value[i])
                    return false;

            return true;
        }

        public static bool operator !=(VectorInt a, VectorInt b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            int result = 0;

            foreach (var d in _value) {
                result = result ^ d;
            }
           
            return result ^ _value.Length;
        }

        public override bool Equals(object obj) {
            if (obj == null || !(obj is VectorInt))
                return false;

            return Equals((VectorInt)obj);
        }

        public bool Equals(VectorInt other) {
            return this == other;
        }
    }
}
