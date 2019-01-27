using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Rasterization {
    //recive this from shader
    public struct Voxel3D {
        public float min;
        public float max;
        public int passability;

        public const int stride = (sizeof(float) * 2) + sizeof(int);
    }


    //set this to shader as info about triangles or quads
    public struct DataSegment3D {
        public int index;
        public int length;
        public int passability;
        public const int stride = (sizeof(int) * 3);

        public DataSegment3D(int Index, int Length, int Passability) {
            index = Index;
            length = Length;
            passability = Passability;
        }
    }
}
