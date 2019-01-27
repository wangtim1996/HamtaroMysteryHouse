using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


namespace K_PathFinder.Pool {
    public static class GenericPoolArray<T>{
        static Dictionary<int, Stack<T[]>> poolDictionary = new Dictionary<int, Stack<T[]>>();
        const int INITIAL_POOL_SIZE = 15;

        public static T[] Take(int size) {
            T[] result = null;
            lock (poolDictionary) {
                Stack<T[]> stack;

                if (poolDictionary.TryGetValue(size, out stack) == false) {
                    stack = new Stack<T[]>();
                    for (int i = 0; i < INITIAL_POOL_SIZE; i++) { stack.Push(new T[size]); }
                    poolDictionary.Add(size, stack);
                }

                result = stack.Count > 0 ? stack.Pop() : new T[size];
            }

            //Debug.LogFormat("taken type {0} size {1}", typeof(T).Name, size);
            return result;
        }

        public static void ReturnToPool(ref T[] value, bool makeDefault = true) {
            if (value == null)
                return;

            if (makeDefault) {
                for (int i = 0; i < value.Length; i++) {
                    value[i] = default(T);
                }
            }

            //Debug.LogFormat("returned type {0} size {1}", typeof(T).Name, value.Length);

            lock (poolDictionary) {
                poolDictionary[value.Length].Push(value);
            }
            value = null;
        }
        
        public static void DebugState() {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("State of {0} pool\n", typeof(T).Name);
            lock (poolDictionary) {
                foreach (var pair in poolDictionary) {
                    sb.AppendFormat("size {0} count {1}\n", pair.Key, pair.Value.Count);
                }
            }
        }

    }
}