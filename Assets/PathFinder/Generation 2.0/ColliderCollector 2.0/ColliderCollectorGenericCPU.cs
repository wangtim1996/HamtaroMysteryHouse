using K_PathFinder.Collector;
using K_PathFinder.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
    public partial class ColliderCollector {
        List<ShapeDataAbstract> shapeDataGenericCPU = new List<ShapeDataAbstract>();

        
        private void AddColliderGenericCPU(Collider collider) {
            var gameObjectArea = collider.transform.GetComponent<AreaGameObject>();
            Area area;
            if (gameObjectArea != null) {
                area = PathFinder.GetArea(gameObjectArea.areaInt);
            }
            else {
                if (PathFinder.settings.checkRootTag) {
                    area = PathFinderSettings.tagAssociations[collider.transform.root.tag];
                }
                else {
                    area = PathFinderSettings.tagAssociations[collider.transform.tag];
                }
            }

            if (collider is BoxCollider) {
                shapeDataGenericCPU.Add(new ShapeDataBox(collider as BoxCollider, area));
            }
            else if (collider is SphereCollider) {
                shapeDataGenericCPU.Add(new ShapeDataSphere(collider as SphereCollider, area));
            }
            else if (collider is CapsuleCollider) {
                shapeDataGenericCPU.Add(new ShapeDataCapsule(collider as CapsuleCollider, area));
            }
            else if (collider is CharacterController) {
                shapeDataGenericCPU.Add(new ShapeDataCharacterControler(collider as CharacterController, area));
            }
            else if (collider is MeshCollider) {
                shapeDataGenericCPU.Add(new ShapeDataMesh(collider as MeshCollider, area));
            }
            else {
                Debug.LogFormat("Collider type on {0} currently not supported. Tell developer what is going on", collider.gameObject.name);
                return;
            }

#if UNITY_EDITOR
            if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                Debuger_K.AddColliderBounds(template.gridPosition.x, template.gridPosition.z, template.properties, collider);
#endif
        }

        private void CollectCollidersCPU(Collector3.ShapeCollector collector) {
            if (profiler != null) profiler.AddLog("Start shape collecting by CPU");
            //Debuger_K.ClearLabels();

            //Debug.Log(shapeDataGenericCPU.Count);
            //int c = 0;
            foreach (var shape in shapeDataGenericCPU) {
                collector.Append(shape);

                //Debuger_K.AddLabel(shape.bounds.center, c++);
                //try {
                //    collector.Append(shape);
                //}
                //catch (System.Exception) {
                //    Debuger_K.AddLine(shape.bounds.center, shape.bounds.center + Vector3.up, Color.red);
                //    Debug.LogError(c + " : " + shape.name);
                //    throw;
                //}
  
                if (profiler != null) profiler.AddLogFormat("Collected {0} of type {1}", shape.name, shape.GetType().Name);
            }

            if (profiler != null) profiler.AddLog("Finish shape collecting by CPU");
        }
    }
}
