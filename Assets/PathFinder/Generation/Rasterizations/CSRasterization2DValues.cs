using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using UnityEngine;

namespace K_PathFinder.Rasterization {
    //recive this from shader
    public struct Voxel2D {
        public float height;
        public int passability;
        //public float x, z;
        //public float 
        //    ax, ay, az, 
        //    bx, by, bz, 
        //    cx, cy, cz;
        //public float f1, f2, f3;
        public const int stride = sizeof(float) + sizeof(int);
    }

    //set this to shader as info about triangles or quads
    public struct DataSegment2D {
        public int index;
        public int minX;
        public int maxX;
        public int minZ;
        public int maxZ;
        public int passability;

        public const int stride = sizeof(int) * 6;

        public DataSegment2D(int Index, int Passability, int MinX, int MaxX, int MinZ, int MaxZ) {
            index = Index;
            passability = Passability;
            minX = MinX;
            maxX = MaxX;
            minZ = MinZ;
            maxZ = MaxZ;
        }
    }

    public static class BlittableHelper {
        public static bool IsBlittable<T>() {
            return IsBlittableCache<T>.Value;
        }

        public static bool IsBlittable(this Type type) {
            if (type.IsArray) {
                var elem = type.GetElementType();
                return elem.IsValueType && IsBlittable(elem);
            }
            try {
                object instance = FormatterServices.GetUninitializedObject(type);
                GCHandle.Alloc(instance, GCHandleType.Pinned).Free();
                return true;
            }
            catch {
                return false;
            }
        }

        private static class IsBlittableCache<T> {
            public static readonly bool Value = IsBlittable(typeof(T));
        }
    }
}