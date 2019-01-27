using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector {
    public class ShapeDataTreeCapsule : ShapeDataTreeAbstract {


        Matrix4x4 local2world;
        //Matrix4x4 capsuleMatrix;

        Vector3 capsuleCenter;
        Quaternion capsuleDirRot;
        float height, radius;


        public ShapeDataTreeCapsule(Matrix4x4 local2world, CapsuleCollider cc) {
            this.local2world = local2world;

            capsuleDirRot = Quaternion.Euler(cc.direction == 2 ? 90f : 0.0f, 0.0f, cc.direction == 0 ? 90f : 0.0f);
            capsuleCenter = cc.center;
            height = cc.height;
            radius = cc.radius;

            //Vector3 lossyScale = Vector3.Scale(local2world.lossyScale, treeWorldScale);

            //float capsuleHeightScale = cc.height * lossyScale.y;
            //float capsuleRadiusScale = cc.radius * SomeMath.Max(lossyScale.x, lossyScale.z);
            //float capsuleSize = Mathf.Max(1f, capsuleHeightScale / (capsuleRadiusScale * 2f)) - 2f;

            //Matrix4x4 generalMatrix = Matrix4x4.TRS(
            //    treeWorldPos,//actual world position
            //    local2world.rotation * //world rotation
            //    Quaternion.Euler(//plus capsule orientation
            //        direction == 2 ? 90f : 0.0f,
            //        0.0f,
            //        direction == 0 ? 90f : 0.0f),
            //    Vector3.one);
        }


        //public void CalculateSpheres(Vector3 capsuleWorldPos, Vector3 capsuleScale, out Vector3 pointA, out Vector3 pointB, out float capsuleRealRadius) {
        //    Vector3 lossyScale = Vector3.Scale(scale, capsuleScale);
        //    float capsuleHeightScale = height * lossyScale.y;
        //    capsuleRealRadius = radius * Mathf.Max(lossyScale.x, lossyScale.z);
        //    float capsuleSize = Mathf.Max(1f, capsuleHeightScale / (capsuleRealRadius * 2f)) - 2f;

        //    Matrix4x4 generalMatrix = Matrix4x4.TRS(
        //        capsuleWorldPos,//actual world position
        //        rotation * //world rotation
        //        Quaternion.Euler(//plus capsule orientation
        //            direction == 2 ? 90f : 0.0f,
        //            0.0f,
        //            direction == 0 ? 90f : 0.0f),
        //        Vector3.one);

        //    float tSize = (0.5f + capsuleSize * 0.5f) * (capsuleRealRadius * 2f);
        //    pointA = generalMatrix.MultiplyPoint3x4(new Vector3(0, tSize, 0));
        //    pointB = generalMatrix.MultiplyPoint3x4(new Vector3(0, -tSize, 0));

        //    Debuger_K.AddLine(capsuleWorldPos, pointA, Color.blue, 0f, 0.003f);
        //    Debuger_K.AddLine(capsuleWorldPos, pointB, Color.blue, 0f, 0.003f);
        //    Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xz, 36, pointA, capsuleRealRadius), Color.blue, true);
        //    Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xz, 36, pointB, capsuleRealRadius), Color.blue, true);
        //}

        public override ShapeDataAbstract ReturnShapeConstructor(Vector3 treeWorldPos, Vector3 treeWorldScale) {
            Matrix4x4 treeMatrix = Matrix4x4.TRS(treeWorldPos, Quaternion.identity, treeWorldScale);
            Matrix4x4 capsuleMatrix = Matrix4x4.TRS(capsuleCenter, capsuleDirRot, Vector3.one);
            Matrix4x4 matrix = treeMatrix * local2world * capsuleMatrix;

            Vector3 capsuleRealPosition = matrix.MultiplyPoint3x4(new Vector3(0, 0, 0));

            Vector3 lossyScale = Vector3.Scale(local2world.lossyScale, treeWorldScale);
            
            float capsuleHeightScale = height * lossyScale.y;
            float capsuleRealRadius = radius * Mathf.Max(lossyScale.x, lossyScale.z);
            float capsuleSize = Mathf.Max(1f, capsuleHeightScale / (capsuleRealRadius * 2f)) - 2f;

            Matrix4x4 generalMatrix = Matrix4x4.TRS(capsuleRealPosition, local2world.rotation * capsuleDirRot, Vector3.one);

            float tSize = (0.5f + capsuleSize * 0.5f) * (capsuleRealRadius * 2f);
            Vector3 pointA = generalMatrix.MultiplyPoint3x4(new Vector3(0, tSize, 0));
            Vector3 pointB = generalMatrix.MultiplyPoint3x4(new Vector3(0, -tSize, 0));

            //Debuger_K.AddDot(capsuleRealPosition, Color.blue, 0.1f);
            //Debuger_K.AddLine(capsuleRealPosition, pointA, Color.blue, 0, 0.003f);
            //Debuger_K.AddLine(capsuleRealPosition, pointB, Color.blue, 0, 0.003f);
            //Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xz, 36, pointA, capsuleRealRadius), Color.blue, true);
            //Debuger_K.AddLine(SomeMath.DrawCircle(SomeMath.Axises.xz, 36, pointB, capsuleRealRadius), Color.blue, true);


            Vector3 boundsSize = new Vector3(capsuleRealRadius * 2f, capsuleRealRadius * 2f, capsuleRealRadius * 2f);
            Bounds bA = new Bounds(pointA, boundsSize);
            Bounds bB = new Bounds(pointB, boundsSize);
            bA.Encapsulate(bB);
            return new ShapeDataCapsule(pointA, pointB, capsuleRealRadius, bA, PathFinder.getUnwalkableArea);
        }
    }
}