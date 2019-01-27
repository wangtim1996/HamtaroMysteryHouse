using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector {
    public abstract class ShapeDataAbstract {        
        public string name;
        public Area area;
        public Bounds bounds;
        public ColliderInfoMode infoMode;
        
        public ShapeDataAbstract(string name, Area area, Bounds bounds, ColliderInfoMode infoMode) {
            this.name = name;
            this.area = area;
            this.bounds = bounds;
            this.infoMode = infoMode;
        }

        public ShapeDataAbstract(AreaWorldModMagicValue value, Area area, ColliderInfoMode infoMode) {
            //AreaWorldMod container = value.container;
            name = value.name;
            bounds = value.bounds;
            this.area = area;
            this.infoMode = infoMode;
        }
        
        public ShapeDataAbstract(Collider collider, Area area) {
            GameObject go = collider.gameObject;
            name = go.name;
            bounds = collider.bounds;
            this.area = area;
            infoMode = ColliderInfoMode.Solid;
        }


        //for cloning
        public ShapeDataAbstract(ShapeDataAbstract data) {
            name = data.name;
            area = data.area;
            bounds = data.bounds;
            infoMode = data.infoMode;
        }
    }

    public interface IShapeDataClonable {
        ShapeDataAbstract Clone();
    }
}
