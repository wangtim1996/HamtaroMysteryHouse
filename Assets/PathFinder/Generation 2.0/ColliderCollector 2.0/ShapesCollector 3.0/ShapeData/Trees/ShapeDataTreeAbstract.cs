using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector {
    public abstract class ShapeDataTreeAbstract {
        public abstract ShapeDataAbstract ReturnShapeConstructor(Vector3 worldPos, Vector3 worldScale);
    }
}