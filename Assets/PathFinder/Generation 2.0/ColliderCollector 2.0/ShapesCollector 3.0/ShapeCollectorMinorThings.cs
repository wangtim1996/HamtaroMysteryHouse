using K_PathFinder.Collector;
using K_PathFinder.Pool;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        public void AppendBox(ShapeDataBox box) {   
            AppendMeshConvex(ColliderCollector.cubeVerts, ColliderCollector.cubeTris, box.boxMatrix, GetAreaValue(box.area), box.infoMode != ColliderInfoMode.Solid, box.bounds);
        }

        public void AppendCharacterControler(ShapeDataCharacterControler character) {
            Bounds bounds = character.bounds;

            DataCompact[] compactData = TakeCompactData();
            AppendSpherePrivate(
                compactData,
                bounds.center,
                bounds.extents.x,
                Mathf.Abs(bounds.extents.y - bounds.extents.x) * 0.5f,
                true,
                character.area.id != 1);

            AppendCompactData(compactData, GetAreaValue(character.area));
            GenericPoolArray<DataCompact>.ReturnToPool(ref compactData);
        }
        
        protected static bool CalculateWalk(Vector3 A, Vector3 B, Vector3 C, float aMaxSlopeCos, bool flipY = false) {
            if (flipY)
                return (Vector3.Cross(B - A, C - A).normalized.y * -1) >= aMaxSlopeCos;
            else
                return Vector3.Cross(B - A, C - A).normalized.y >= aMaxSlopeCos;
        }
    }
}
