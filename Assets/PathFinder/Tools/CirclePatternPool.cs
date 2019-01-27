using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Pool {
    public static class CirclePatternPool {
        static Dictionary<int, CirclePattern> patterns = new Dictionary<int, CirclePattern>();

        public static CirclePattern GetPattern(int radius) {
            CirclePattern result;
            lock (patterns) {
                if (!patterns.TryGetValue(radius, out result)) {
                    result = new CirclePattern(radius);
                    patterns.Add(radius, result);
                }
            }
            return result;
        }

        public class CirclePattern {
            public int radius;
            public bool[] pattern; // (y * size) + x;
            public int size;

            public CirclePattern(int radius) {
                this.radius = radius;
                size = radius + radius - 1;
                int sqrRadius = (radius - 1) * (radius - 1);
                pattern = new bool[size * size];

                for (int x = 0; x < size; x++) {
                    for (int y = 0; y < size; y++) {
                        pattern[(y * size) + x] = SomeMath.SqrDistance(x, y, radius - 1, radius - 1) <= sqrRadius;
                    }
                }
            }
        }
    }
}