using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.Collector {
    public class ShapeDataSphere : ShapeDataAbstract, IShapeDataClonable {
        public ShapeDataSphere(SphereCollider collider, Area area) : base(collider, area) { }
        public ShapeDataSphere(AreaWorldModMagicValue value, Area area, ColliderInfoMode infoMode) : base(value, area, infoMode) { }
        public ShapeDataSphere(Bounds bounds, Area area) :  base(string.Empty, area, bounds, ColliderInfoMode.Solid) {}
        public ShapeDataSphere(ShapeDataSphere dataSphere) : base(dataSphere) { }

        public ShapeDataAbstract Clone() {
            return new ShapeDataSphere(this);
        }
    }
}
