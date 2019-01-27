using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.PFDebuger {   
    public struct DebugArrayVector3 {
        public const int size = 
            sizeof(int) + 
            sizeof(float) + sizeof(float) + sizeof(float) +
            (sizeof(float) * 3) +
            (sizeof(float) * 3 * 20);

        //cool number
        public float cn1, cn2, cn3;
        public Vector3 pos;
        public int i;
        public Vector3 
            V0, V1, V2, V3, V4, V5, V6, V7, V8, V9, 
            V10, V11, V12, V13, V14, V15, V16, V17, V18, V19;

        public Vector3 this[int index] {
            get {
                switch (index) {
                    case 0:
                        return V0;
                    case 1:
                        return V1;
                    case 2:
                        return V2;
                    case 3:
                        return V3;
                    case 4:
                        return V4;
                    case 5:
                        return V5;
                    case 6:
                        return V6;
                    case 7:
                        return V7;
                    case 8:
                        return V8;
                    case 9:
                        return V9;
                    case 10:
                        return V10;
                    case 11:
                        return V11;
                    case 12:
                        return V12;
                    case 13:
                        return V13;
                    case 14:
                        return V14;
                    case 15:
                        return V15;
                    case 16:
                        return V16;
                    case 17:
                        return V17;
                    case 18:
                        return V18;
                    case 19:
                        return V19;
                    default:
                        return Vector3.zero;             
                }
            }
        }

        public Vector3 center {
            get {
                Vector3 val = Vector3.zero;
                for (int index = 0; index < i; index++) {
                    val += this[index];
                }
                val /= i;
                return val;
            }
        }

        public override string ToString() {
            return string.Format("size: {0}\nn1: {1}\nn2: {2}\nn3: {3}", i, cn1, cn2, cn3);
        }
    }
}