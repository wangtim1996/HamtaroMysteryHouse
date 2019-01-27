using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Pool {
    public static class GenericPool<T> where T : class, new() {
        public const int INITIAL_SIZE = 32;

        static Stack<T> pool = new Stack<T>();

        static GenericPool(){
            for (int i = 0; i < INITIAL_SIZE; i++) {
                pool.Push(new T());
            }
        }

        public static T Take() {
            lock (pool) {
                if (pool.Count > 0)
                    return pool.Pop();
                else return new T();
            }
        }

        public static void ReturnToPool(ref T obj) {
            ReturnToPool(obj);
            obj = null;
        }

        private static void ReturnToPool(T list) {
            lock (pool) {
                pool.Push(list);
            }
        }
    }
}