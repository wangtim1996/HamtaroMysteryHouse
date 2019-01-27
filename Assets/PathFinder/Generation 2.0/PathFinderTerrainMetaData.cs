using K_PathFinder.PFDebuger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace K_PathFinder.Collector {
    //Some metadata to simplifu collection of trees
    [ExecuteInEditMode]
    public class PathFinderTerrainMetaData : MonoBehaviour {
        Terrain terrain;
        List<TreeInstanceData> treeInstances = new List<TreeInstanceData>();
        TreePrototypeData[] treePrototypesData;
        bool needRefresh = true;

        public void OnEnable() {   
            terrain = GetComponent<Terrain>();
            needRefresh = true;
        }


        public void Refresh() {
            //Debug.LogWarning(needRefresh);
            //Debug.LogWarning(treeInstances.Count);

            if (needRefresh == false)
                return;

            needRefresh = false;

            System.Diagnostics.Stopwatch totalTime = new System.Diagnostics.Stopwatch();
            totalTime.Start();

            if (treeInstances.Count > 0) {
                ChunkContentMap.RemoveContent(treeInstances);   
            }           

            treeInstances.Clear();

            terrain = GetComponent<Terrain>();
            TerrainData terrainData = terrain.terrainData;

            TreePrototype[] prototypes = terrainData.treePrototypes;
            TreeInstance[] instances = terrainData.treeInstances;

            bool[] validPrototype = new bool[prototypes.Length];


            treePrototypesData = new TreePrototypeData[prototypes.Length];

            for (int i = 0; i < prototypes.Length; i++) {
                bool isValid = IsValidTreePrototype(prototypes[i]);
                validPrototype[i] = isValid;

                if (isValid == false)
                    continue;

                GameObject treeInstance = Instantiate(prototypes[i].prefab);
                HashSet<Collider> tColliders = new HashSet<Collider>();
                foreach (var item in treeInstance.GetComponents<Collider>()) {tColliders.Add(item); }
                foreach (var item in treeInstance.GetComponentsInChildren<Collider>()) {tColliders.Add(item); }

                if (tColliders.Count == 0)
                    continue;
                
                Vector3 instancePos = treeInstance.transform.position;
                List<ShapeDataTreeAbstract> treePrototypeData = new List<ShapeDataTreeAbstract>();

                foreach (var item in tColliders) {
                    if (IsValidTreePrototypeCollider(item) == false)
                        continue;

                    Transform colliderTransfrom = item.transform;
                    Vector3 transfromLocalPosition = colliderTransfrom.position - instancePos;
                    Quaternion transfromRotation = colliderTransfrom.rotation;
                    Vector3 transfromScale = colliderTransfrom.lossyScale;

                    Matrix4x4 l2wMatrix = Matrix4x4.TRS(transfromLocalPosition, transfromRotation, transfromScale);

                    if (item is BoxCollider) {
                        treePrototypeData.Add(new ShapeDataTreeBox(l2wMatrix, item as BoxCollider));
                    }
                    else if (item is SphereCollider) {        
                        treePrototypeData.Add(new ShapeDataTreeSphere(l2wMatrix, item as SphereCollider));
                    }
                    else if (item is CapsuleCollider) {
                        treePrototypeData.Add(new ShapeDataTreeCapsule(l2wMatrix, item as CapsuleCollider));
                    }
                    else {
                        Debug.LogWarningFormat("{0} right now is not supported on tree prototype {1}. only box, sphere and capsule supported. remind developer to add your thing on forum thread", item.GetType(), prototypes[i].prefab.name);
                    }
                }

                DestroyImmediate(treeInstance);
                treePrototypesData[i] = new TreePrototypeData(treePrototypeData);
            }

            Vector3 terrainPos = transform.position;
            Vector3 terrainSize = terrainData.size;

            string terrainTag = terrain.gameObject.tag; ;
            int terrainLayer = terrain.gameObject.layer;

            foreach (var instance in instances) {
                TreePrototypeData TPD = treePrototypesData[instance.prototypeIndex];

                if (TPD.colliders == null || TPD.colliders.Count == 0)
                    continue;

                Vector3 instanceWorldScale = new Vector3(instance.widthScale, instance.heightScale, instance.widthScale);
                Vector3 instanceWorldPos = Vector3.Scale(instance.position, terrainSize) + terrainPos;
                //Matrix4x4 localToWorldMatrix = Matrix4x4.TRS(instanceWorldPos, Quaternion.identity, instanceWorldScale);
                
                //Debuger_K.AddDot(instanceWorldPos, Color.red, 0.1f);
                //Debuger_K.AddLabelFormat(instanceWorldPos, "Rotation: {0}\nHeight: {1}\nWidth: {2}", instance.rotation * Mathf.Rad2Deg, instance.heightScale, instance.widthScale);
                //Quaternion rotation = Quaternion.Euler(0, instance.rotation * Mathf.Rad2Deg, 0);


              
                List<IShapeDataClonable> treeShapeData = new List<IShapeDataClonable>();
                Bounds bounds = new Bounds(instanceWorldPos, new Vector3());
             
                foreach (var item in TPD.colliders) {
                    ShapeDataAbstract data = item.ReturnShapeConstructor(instanceWorldPos, instanceWorldScale);
                    treeShapeData.Add(data as IShapeDataClonable);
                    bounds.Encapsulate(data.bounds);                    
                }

                treeInstances.Add(new TreeInstanceData(this, treeShapeData, bounds));
            }

      
            int colliders = 0;
            foreach (var item in treeInstances) { colliders += item.shapeData.Count;}
            Debug.LogFormat("Trees {0}, Colliders {1}", treeInstances.Count, colliders);

            if (treeInstances.Count > 0) {
                ChunkContentMap.Process(treeInstances);
            }

            //ChunkContentMap.SceneDebug();

            totalTime.Stop();
            Debug.Log("Terrain process time: " + totalTime.Elapsed);
        }


        void OnTerrainChanged(TerrainChangedFlags flags) {
            //if ((flags & TerrainChangedFlags.Heightmap) != 0) {
            //    Debug.Log("Heightmap changes");
            //}

            //if ((flags & TerrainChangedFlags.DelayedHeightmapUpdate) != 0) {
            //    Debug.Log("Heightmap painting");
            //}

            if ((flags & TerrainChangedFlags.TreeInstances) != 0) {
                needRefresh = true;
                //Debug.Log("Tree changes");
            }
        }

        private bool IsValidTreePrototype(TreePrototype prototype) {
            if (prototype == null || prototype.prefab == null)
                return false;

            return true;
        }

        private bool IsValidTreePrototypeCollider(Collider collider) {
            if (collider == null)
                return false;

            if (collider.enabled == false || collider.gameObject.activeInHierarchy == false)
                return false;

            return true;
        }

        public void GetTreeShapes(NavMeshTemplateCreation template, ref List<ShapeDataAbstract> listToFill) {
            var bounds = template.chunkOffsetedBounds;

            int x = template.chunkData.x;
            int z = template.chunkData.z;
            HashSet<TreeInstanceData> treeInstanceDataHashSet = new HashSet<TreeInstanceData>();
            Predicate<TreeInstanceData> predicate = (TreeInstanceData value) => { return value.metaDataReference == this && value.bounds.Intersects(bounds); };

            ChunkContentMap.GetContent(x - 1, z + 1, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x - 1, z, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x - 1, z + 1, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x, z + 1, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x, z, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x, z + 1, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x + 1, z + 1, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x + 1, z, treeInstanceDataHashSet, predicate);
            ChunkContentMap.GetContent(x + 1, z + 1, treeInstanceDataHashSet, predicate);
            
            foreach (var tree in treeInstanceDataHashSet) {
                foreach (var shape in tree.shapeData) {
                    if((shape as ShapeDataAbstract).bounds.Intersects(bounds))
                        listToFill.Add(shape.Clone());      
                }             
            }

            //Debug.LogWarning(listToFill.Count);
            //foreach (var item in listToFill) {
            //    Debuger_K.AddBounds(item.bounds, Color.cyan);
            //}
        }
        
        struct TreePrototypeData {
            public List<ShapeDataTreeAbstract> colliders;

            public TreePrototypeData(List<ShapeDataTreeAbstract> colliders) {
                this.colliders = colliders;
            }
        }

        class TreeInstanceData : IChunkContent {
            public PathFinderTerrainMetaData metaDataReference;
            public List<IShapeDataClonable> shapeData;
            public Bounds bounds;

            public TreeInstanceData(PathFinderTerrainMetaData metaDataReference, List<IShapeDataClonable> shapeData, Bounds bounds) {
                this.metaDataReference = metaDataReference;
                this.shapeData = shapeData;
                this.bounds = bounds;
            }

            public Bounds chunkContentBounds {
                get { return bounds; }
            }
        }
    }
}
