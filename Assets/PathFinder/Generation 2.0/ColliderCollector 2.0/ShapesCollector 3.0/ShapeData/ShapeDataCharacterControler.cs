using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif


namespace K_PathFinder.Collector {
    public class ShapeDataCharacterControler : ShapeDataAbstract {
        //private Matrix4x4 generalMatrix, scaleMatrix;
        //private float size;

        public ShapeDataCharacterControler(CharacterController collider, Area area) : base(collider, area) {
            //Transform transfrom = collider.transform;
            //Vector3 lossyScale = transfrom.lossyScale;
            //float height = collider.height * lossyScale.y;
            //float radius = collider.radius * Mathf.Max(lossyScale.x, lossyScale.z);

            //size = Mathf.Max(1f, height / (radius * 2f)) - 2f;
            //scaleMatrix = Matrix4x4.Scale(new Vector3(radius * 2f, radius * 2f, radius * 2f));
            //generalMatrix = Matrix4x4.TRS(bounds.center, transfrom.rotation, Vector3.one);
        }
    }
}