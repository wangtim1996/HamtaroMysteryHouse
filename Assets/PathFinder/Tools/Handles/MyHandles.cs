using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace K_PathFinder {
    [System.Serializable]
    public struct AreaPointer {
        [SerializeField] public float startX;
        [SerializeField] public float startZ;
        [SerializeField] public float endX;
        [SerializeField] public float endZ;
        [SerializeField] public float y;

        public AreaPointer(float startX, float startZ, float endX, float endZ, float y) {
            this.startX = startX;
            this.startZ = startZ;
            this.endX = endX;
            this.endZ = endZ;
            this.y = y;
        }

        public float sizeX {
            get { return endX - startX; }
        }
        public float sizeZ {
            get { return endZ - startZ; }
        }
        public int roundStartX {
            get { return Mathf.RoundToInt(startX); }
        }
        public int roundStartZ {
            get { return Mathf.RoundToInt(startZ); }
        }
        public int roundEndX {
            get { return Mathf.RoundToInt(endX); }
        }
        public int roundEndZ {
            get { return Mathf.RoundToInt(endZ); }
        }
        public int roundSizeX {
            get { return roundEndX - roundStartX; }
        }
        public int roundSizeZ {
            get { return roundEndZ - roundStartZ; }
        }

        public AreaPointer roundToInt {
            get { return new AreaPointer(roundStartX, roundStartZ, roundEndX, roundEndZ, y); }
        }

        public static AreaPointer operator *(AreaPointer val, float mul) {
            return new AreaPointer(
                val.startX * mul,
                val.startZ * mul,
                val.endX * mul,
                val.endZ * mul,
                val.y);
        }

        public static AreaPointer operator /(AreaPointer val, float div) {
            return new AreaPointer(
                val.startX / div,
                val.startZ / div,
                val.endX / div,
                val.endZ / div,
                val.y);
        }
    }
}



