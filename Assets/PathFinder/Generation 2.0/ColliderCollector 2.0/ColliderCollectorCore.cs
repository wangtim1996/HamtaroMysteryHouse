using K_PathFinder.Collector;
using K_PathFinder.Collector3;
using K_PathFinder.PFDebuger;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder {
    public partial class ColliderCollector {
        public NavmeshProfiler profiler = null;
        public NavMeshTemplateCreation template;
        Bounds chunkBounds;

        public Collector3.ShapeCollector shapeCollectorResult;

        public List<KeyValuePair<AreaWorldMod, Collector3.ShapeCollector>> shapeCollectorMods;

        List<AreaWorldModShapeData> modsChangeArea = new List<AreaWorldModShapeData>();
        List<AreaWorldModShapeData> modsMakeHole = new List<AreaWorldModShapeData>();

        struct AreaWorldModShapeData {
            public AreaWorldMod mod;
            public int priority;
            public Area area;
            public List<ShapeDataAbstract> shapes;
            public ColliderInfoMode mode;

            public AreaWorldModShapeData(AreaWorldMod Mod, List<ShapeDataAbstract> Shapes) {
                mod = Mod;
                shapes = Shapes;
                priority = Mod.priority;
                area = Mod.GetArea();
                mode = Mod.mode;
            }
        }    

        struct TerrainShape {
            public Collector3.ShapeCollector terrain;
            public Collector3.ShapeCollector trees;

            public TerrainShape(Collector3.ShapeCollector terrain, Collector3.ShapeCollector trees) {
                this.terrain = terrain;
                this.trees = trees;
            }
        }

        public ColliderCollector(NavMeshTemplateCreation template) {
            this.template = template;
            chunkBounds = template.chunkOffsetedBounds;
        }
        
        public void AddCollider(params Collider[] colliders) {
            for (int i = 0; i < colliders.Length; i++) {
                Collider collider = colliders[i];
                if (collider == null ||
                   collider.enabled == false ||
                   collider.isTrigger ||
                   chunkBounds.Intersects(collider.bounds) == false)
                    continue;

                if (template.checkHierarchyTag) {
                    for (Transform t = collider.transform; t != null; t = t.parent) {
                        if (template.IgnoredTagsContains(t.tag))
                            continue;
                    }
                }
                else if (template.IgnoredTagsContains(collider.tag))
                    continue;

                if(collider is TerrainCollider) {
                    AddColliderTerrain(collider, PathFinder.terrainCollectionType);                 
                }
                else {
                    switch (PathFinder.colliderCollectorType) {
                        case ColliderCollectorType.CPU:
                            AddColliderGenericCPU(collider);
                            break;
                        case ColliderCollectorType.ComputeShader:
                            if(collider is MeshCollider && (collider as MeshCollider).convex == false)
                                AddColliderGenericCPU(collider);
                            else
                                AddColliderGenericGPU(collider);
                            break;
                    }
           
                }
            }
        }

        public void AddModifyers(List<AreaWorldMod> mods) {
            var chunkBounds = template.chunkData.bounds;

            foreach (var mod in mods) {
                //check if it should be added
                //check agent filters
                if (mod == null ||                                                           //if mode is null for some reason then nothing to do
                    mod.enabled == false ||                                                  //if mod disabled then nothing to do
                    chunkBounds.Intersects(mod.bounds) == false ||          //if bounds of this mod outside generated navmesh then nothing to do
                    template.IgnoredTagsContains(mod.tag) ||                                 //if mod in list of ifnored tags then nothing to do
                    (template.includedLayers.value & (1 << mod.gameObject.layer)) == 0)             //if mod not included in layers then nothing to do
                    continue;

                if (mod.useAdvancedArea)
                    template.hashData.AddAreaHash(mod.advancedArea, false);

                Matrix4x4 modMatrix = mod.localToWorldMatrix;
                Area area = mod.GetArea();
                var modInfoMode = mod.mode;

                List<ShapeDataAbstract> list = new List<ShapeDataAbstract>();
                foreach (var value in mod.allMods) {
                    if (chunkBounds.Intersects(value.bounds) == false)
                        continue;

                    switch (value.myType) {
                        case AreaWorldModMagicValueType.Sphere:
                            list.Add(new ShapeDataSphere(value, area, modInfoMode));
                            break;
                        case AreaWorldModMagicValueType.Capsule:
                            list.Add(new ShapeDataCapsule(value, area, modMatrix.rotation, modInfoMode));
                            break;
                        case AreaWorldModMagicValueType.Cuboid:
                            list.Add(new ShapeDataBox(value, area, modMatrix.rotation, modInfoMode));
                            break;
                    }
                }

                switch (modInfoMode) {
                    case ColliderInfoMode.Solid:
                        shapeDataGenericCPU.AddRange(list);
                        break;
                    case ColliderInfoMode.ModifyArea:
                        modsChangeArea.Add(new AreaWorldModShapeData(mod, list));
                        break;
                    case ColliderInfoMode.MakeHoleApplyArea: case ColliderInfoMode.MakeHoleRetainArea:
                        modsMakeHole.Add(new AreaWorldModShapeData(mod, list));
                        break;
                }
            }
        }

        public void Collect() {
            shapeCollectorResult = ShapeCollector.GetFromPool(template.lengthX_extra, template.lengthZ_extra, template);

            if (profiler != null) profiler.AddLog("Start Collecting Terrain");
            CollectTerrainCPU(shapeCollectorResult);
            CollectTerrainGPU(shapeCollectorResult);
            if (profiler != null) profiler.AddLog("End Collecting Terrain");

            if (profiler != null) profiler.AddLog("Start Collecting shapes");
            CollectCollidersCPU(shapeCollectorResult);
            CollectCollidersGPU(shapeCollectorResult);
            if (profiler != null) profiler.AddLog("End Collecting shapes");
            

            if (profiler != null) profiler.AddLog("Start Collecting shapes");
            modsChangeArea.Sort(Comparer);
            modsMakeHole.Sort(Comparer);

            for (int i = 0; i < modsChangeArea.Count; i++) {
                shapeCollectorResult.ChangeArea(modsChangeArea[i].shapes, modsChangeArea[i].area);
            }

            for (int i = 0; i < modsMakeHole.Count; i++) {   
                if(modsMakeHole[i].mode == ColliderInfoMode.MakeHoleApplyArea)
                    shapeCollectorResult.MakeHole(modsMakeHole[i].shapes, modsMakeHole[i].area);
                if (modsMakeHole[i].mode == ColliderInfoMode.MakeHoleRetainArea)
                    shapeCollectorResult.MakeHole(modsMakeHole[i].shapes, null);
            }

            if (profiler != null) profiler.AddLog("End Collecting shapes");
            //shapeCollectorGeneric.DebugMe();
        }

        static int Comparer(AreaWorldModShapeData left, AreaWorldModShapeData right) {
            if (left.priority < right.priority) return -1;
            if (left.priority > right.priority) return 1;

            //if priority equal
            if (left.area.overridePriority < right.area.overridePriority) return -1;
            if (left.area.overridePriority > right.area.overridePriority) return 1;
            return 0;
        }
    }
}