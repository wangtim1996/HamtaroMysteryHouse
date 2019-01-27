using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace K_PathFinder.Pool {
    public static class GenericPoolArray2D<T> {
        static Dictionary<VectorInt.Vector2Int, Stack<T[][]>> poolDictionary = new Dictionary<VectorInt.Vector2Int, Stack<T[][]>>();
        const int INITIAL_POOL_SIZE = 15;

        public static T[][] Take(int sizeX, int sizeZ) {
            T[][] result = null;
            lock (poolDictionary) {
                Stack<T[][]> stack;
                VectorInt.Vector2Int key = new VectorInt.Vector2Int(sizeX, sizeZ);

                if (poolDictionary.TryGetValue(key, out stack) == false) {
                    stack = new Stack<T[][]>();
                    poolDictionary.Add(key, stack);

                    for (int i = 0; i < INITIAL_POOL_SIZE; i++) {
                        stack.Push(Create(sizeX, sizeZ));
                    }
                }

                result = stack.Count > 0 ? stack.Pop() : Create(sizeX, sizeZ);
            }
            return result;
        }

        public static T[][] Take(VectorInt.Vector2Int size) {
            T[][] result = null;
            lock (poolDictionary) {
                Stack<T[][]> stack;               

                if (poolDictionary.TryGetValue(size, out stack) == false) {
                    stack = new Stack<T[][]>();
                    poolDictionary.Add(size, stack);

                    for (int i = 0; i < INITIAL_POOL_SIZE; i++) {
                        stack.Push(Create(size));
                    }
                }

                result = stack.Count > 0 ? stack.Pop() : Create(size);
            }
            return result;
        }

        public static void ReturnToPool(ref T[][] volume, bool makeDefault = true) {
            if (volume == null)
                return;

            if (makeDefault) {
                for (int x = 0; x < volume.Length; x++) {
                    for (int z = 0; z < volume[x].Length; z++) {
                        volume[x][z] = default(T);
                    }
                }
            }

            lock (poolDictionary) {
                poolDictionary[new VectorInt.Vector2Int(volume.Length, volume[0].Length)].Push(volume);
            }

            volume = null;
        }

        private static T[][] Create(int sizeX, int sizeY) {
            T[][] result = new T[sizeX][];
            for (int x = 0; x < sizeX; x++) {
                result[x] = new T[sizeY];
            }
            return result;
        }

        private static T[][] Create(VectorInt.Vector2Int size) {
            return Create(size.x, size.y);
        }
    }
}