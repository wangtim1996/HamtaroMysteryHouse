using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using K_PathFinder.Pool;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.Collector {
    public class ShapeDataBox : ShapeDataAbstract, IShapeDataClonable {
        public Matrix4x4 boxMatrix;

        public ShapeDataBox(BoxCollider collider, Area area) : base(collider, area) {
            Quaternion rotation = collider.transform.rotation;
            Vector3 scale = Vector3.Scale(collider.transform.lossyScale, (collider as BoxCollider).size);
            boxMatrix = Matrix4x4.TRS(bounds.center, rotation, scale);
        }

        public ShapeDataBox(AreaWorldModMagicValue value, Area area, Quaternion modRotation, ColliderInfoMode infoMode) : base(value, area, infoMode) {
            Quaternion rotation = modRotation * value.rotation;
            Vector3 scale = value.cubeSize * 2;
            boxMatrix = Matrix4x4.TRS(bounds.center, rotation, scale);
        }

        //for cloning
        private ShapeDataBox(ShapeDataBox shapeData) : base(shapeData) {
            boxMatrix = shapeData.boxMatrix;
        }

        //for creating from ShapeDataTreeBox
        public ShapeDataBox(Matrix4x4 boxMatrix, Bounds boxBounds, Area area) :  base (string.Empty, area, boxBounds, ColliderInfoMode.Solid) { 
            this.boxMatrix = boxMatrix;
        }

        public ShapeDataAbstract Clone() {
            return new ShapeDataBox(this);
        }
    }
}