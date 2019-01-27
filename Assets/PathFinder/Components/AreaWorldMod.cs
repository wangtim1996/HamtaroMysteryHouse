using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using K_PathFinder.Trees;
using K_PathFinder.Settings;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using K_PathFinder.PFDebuger;
using UnityEditor;
#endif

namespace K_PathFinder {
    [ExecuteInEditMode()]
    public class AreaWorldMod : MonoBehaviour, IChunkContent {
#if UNITY_EDITOR
        [SerializeField] public bool drawGizmos = true;
        [SerializeField] [Range(0f, 1f)]public float drawGizmosAlpha = 0.5f;
        [SerializeField] public bool drawBounds = false;
#endif

        //area control
        [SerializeField] public ColliderInfoMode mode = ColliderInfoMode.ModifyArea;
        [SerializeField] private bool _useAdvancedArea = false;
        [SerializeField] public AreaAdvanced advancedArea;      //advanced area case      
        [SerializeField][Area(false)] public int areaInt = 0;  //normal area case

        [SerializeField] public int priority = 0;
        [SerializeField] public Bounds bounds; //bounds of all bounds! that bound enclose all bounds from allMods collection
        [SerializeField] public List<AreaWorldModMagicValue> allMods = new List<AreaWorldModMagicValue>(); //all stored shapes
        [SerializeField] private bool _cellUsabilityState = true;

        //dont add to this directly. use AddCellPathContent and RemoveCellPathContent
        [NonSerialized] public List<CellPathContentAbstract> cellPathContents = new List<CellPathContentAbstract>();
        [NonSerialized] public bool needMapUpdate = true;
        [NonSerialized] public int chunkContentMapID = -1;
        [NonSerialized] public bool _init = false;

        [NonSerialized] public Dictionary<GameObject, Bounds> childsAndBounds = new Dictionary<GameObject, Bounds>();
        static List<GameObject> childsAndBoundsHelper = new List<GameObject>(100);
        static List<Bounds> recalculateBoundsHelper = new List<Bounds>(100);

        #region Shape Value Control
        public void ShapeAdd(AreaWorldModMagicValueType shape) {
            Vector3 pos = UnityEngine.Random.onUnitSphere * 0.2f;
            AreaWorldModMagicValue mod = new AreaWorldModMagicValue();
            mod.position = pos;
            mod.rotation = Quaternion.identity;
            mod.myType = shape;

            switch (shape) {
                case AreaWorldModMagicValueType.Sphere:
                    mod.SetValuesAsSphere(0.5f);
                    break;
                case AreaWorldModMagicValueType.Capsule:
                    mod.SetValuesAsCapsule(0.5f, 2f);
                    break;
                case AreaWorldModMagicValueType.Cuboid:
                    mod.SetValuesAsCuboid(0.5f, 0.5f, 0.5f);
                    break;
            }

            mod.id = allMods.Count;
            mod.container = this;
            mod.SetDirty();
            allMods.Add(mod);
            RecalculateBounds();
        }
        public void ShapeRemoveAt(int index) {
            allMods.RemoveAt(index);
            ReassignIndexes();
            RecalculateBounds();
        }
        public void ShapeCopyAt(int index) {
            if (index < 0 | index > allMods.Count - 1)
                return;

            AreaWorldModMagicValue copy = allMods[index].Copy();
            copy.container = this;
            copy.SetDirty();      
            allMods.Insert(index, copy);
            CheckDirty();
            ReassignIndexes();
        }
        public void ShapeIncreaseValueID(int index) {
            if (index < 0 | index > allMods.Count - 2)
                return;

            AreaWorldModMagicValue a = allMods[index];
            AreaWorldModMagicValue b = allMods[index + 1];

            allMods[index + 1] = a;
            allMods[index] = b;

            allMods[index + 1].id = index + 1;
            allMods[index].id = index;
        }
        public void ShapeDecreaseValueID(int index) {
            if (index < 1 | index > allMods.Count - 1)
                return;

            AreaWorldModMagicValue a = allMods[index];
            AreaWorldModMagicValue b = allMods[index - 1];

            allMods[index - 1] = a;
            allMods[index] = b;

            allMods[index - 1].id = index - 1;
            allMods[index].id = index;
        }
        #endregion
        
        public void Init() {
            if (_init)
                return;

            _init = true;

            if (advancedArea != null)
                advancedArea.container = this;

            foreach (var item in allMods) {
                item.container = this;
            }

            DirtyAll();
            CheckDirty();
        }


        public void OnEnable() {
            Init();
            RebuildChilds(false);
            DirtyAll();
            CheckDirty();
            
            ChunkContentMap.Process(this);         
        }

        public void OnDisable() {
            ChunkContentMap.RemoveContent(this);         
        }

        public void OnDestroy() {
            ChunkContentMap.RemoveContent(this);            
        }

        public void Update() {           
            if(transform.hasChanged) {
                transform.hasChanged = false;
                DirtyAll();
                CheckDirty();
            }

            if(childsAndBounds.Count > 0) {          
                foreach (var key in childsAndBounds.Keys) {              
                    if (key.transform.hasChanged) {
                        key.transform.hasChanged = false;
                        childsAndBoundsHelper.Add(key);             
                    }
                }

                for (int i = 0; i < childsAndBoundsHelper.Count; i++) {
                    childsAndBounds[childsAndBoundsHelper[i]] = GetMeshBounds(childsAndBoundsHelper[i].GetComponent<MeshFilter>());
                }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (drawGizmos) {
                Color gizmosColor = Gizmos.color;
                Gizmos.color = color;

                for (int i = 0; i < allMods.Count; i++) {
                    allMods[i].DrawGizmos(drawGizmosAlpha);
                }

                Gizmos.color = gizmosColor;
            }

            if (drawBounds) {
                Color gizmosColor = Gizmos.color;
                Gizmos.color = color;
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
                
                Gizmos.color = Color.blue;
                foreach (var mod in allMods) {
                    Gizmos.DrawWireCube(mod.bounds.center, mod.bounds.size);
                }
                Gizmos.color = Color.cyan;
                foreach (var item in childsAndBounds) {
                    Gizmos.DrawWireCube(item.Value.center, item.Value.size);
                }

                Gizmos.color = gizmosColor;
            }
        }
#endif
        void OnTransformChildrenChanged() {
            RebuildChilds(true);
            RecalculateBounds();
        }

        private Bounds GetMeshBounds(MeshFilter mf) {            
            Mesh m = mf.sharedMesh;
            Matrix4x4 l2w = mf.transform.localToWorldMatrix;
            Vector3[] verts = m.vertices;

            Vector3 first = l2w.MultiplyPoint3x4(verts[0]);
            float minX, maxX, minY, maxY, minZ, maxZ;
            minX = maxX = first.x;
            minY = maxY = first.y;
            minZ = maxZ = first.z;
            for (int i = 1; i < verts.Length; i++) {
                Vector3 vector = l2w.MultiplyPoint3x4(verts[i]);
                minX = Math.Min(minX, vector.x);
                maxX = Math.Max(maxX, vector.x);
                minY = Math.Min(minY, vector.y);
                maxY = Math.Max(maxY, vector.y);
                minZ = Math.Min(minZ, vector.z);
                maxZ = Math.Max(maxZ, vector.z);
            }

            Vector3 size = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            return new Bounds(new Vector3(minX + (size.x * 0.5f), minY + (size.y * 0.5f), minZ + (size.z * 0.5f)), size);
        } 

        public void RebuildChilds(bool checkContains) {
            if (checkContains) {
                foreach (Transform child in transform) {
                    MeshFilter mf = child.gameObject.GetComponent<MeshFilter>();
                    if (childsAndBounds.ContainsKey(child.gameObject) == false && 
                        mf != null && 
                        child.gameObject.GetComponent<Collider>() == null) {
                        childsAndBounds[child.gameObject] = GetMeshBounds(mf);
                    }
                }
            }
            else {
                childsAndBounds.Clear();
                foreach (Transform child in transform) {
                    MeshFilter mf = child.gameObject.GetComponent<MeshFilter>();
                    if (mf != null && 
                        child.gameObject.GetComponent<Collider>() == null) {
                        childsAndBounds[child.gameObject] = GetMeshBounds(mf);             
                    }
                }
            }
        }

        /// <summary>
        /// Change all related cells state. 
        /// If cells can be used then it can be used by path search.
        /// If not then it will be excluded from path search.
        /// </summary>
        public void SetCellsState(bool canBeUsed) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to change cell state when this AreaWorldMod is not setted up for advanced area. Position {0}", transform.position);
            
            if (_cellUsabilityState == canBeUsed)
                return; //nothing really happened

            if (_useAdvancedArea) {
                lock (advancedArea.cells) {
                    foreach (var cell in advancedArea.cells) {
                        cell.SetCanBeUsed(canBeUsed);
                    }
                }
            }
            _cellUsabilityState = canBeUsed;
        }

        /// <summary>
        /// Change all related cells state. 
        /// If cells can be used then it can be used by path search.
        /// If not then it will be excluded from path search.
        /// Only targeted AgentProperties related cells will be changed
        /// </summary>
        public void SetCellsState(bool canBeUsed, params AgentProperties[] targets) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to change cell state when this AreaWorldMod is not setted up for advanced area. Position {0}", transform.position);

            if (_cellUsabilityState == canBeUsed)
                return; //nothing really happened

            if (_useAdvancedArea) {
                lock (advancedArea.cells) {
                    foreach (var cell in advancedArea.cells) {
                        if(targets.Contains(cell.graph.properties))
                            cell.SetCanBeUsed(canBeUsed);
                    }
                }
            }
            _cellUsabilityState = canBeUsed;
        }

        public bool cellUsabilityState {
            get { return _cellUsabilityState; }
        }

        #region Cell Path Content
        /// <summary>
        /// function to add Cell Path Content
        /// </summary>
        public void AddCellPathContent(CellPathContentAbstract value) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to add CellPathContentAbstract to AreaWorldMod when it's not setted up for advanced area. Position {0}", transform.position);

            //add to existing cells
            lock (cellPathContents) {
                cellPathContents.Add(value);

                if (_useAdvancedArea) {
                    lock (advancedArea.cells) {
                        foreach (var cell in advancedArea.cells) {
                            cell.AddPathContent(value);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// function to add Cell Path Content
        /// </summary>
        public void AddCellPathContent(IEnumerable<CellPathContentAbstract> values) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to add CellPathContentAbstract to AreaWorldMod when it's not setted up for advanced area. Position {0}", transform.position);

            //add to existing cells
            lock (cellPathContents) {
                lock (values) {
                    cellPathContents.AddRange(values);

                    if (_useAdvancedArea) {
                        lock (advancedArea.cells) {
                            foreach (var cell in advancedArea.cells) {
                                cell.AddPathContent(values);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// function to add Cell Path Content
        /// </summary>
        public void AddCellPathContent(params CellPathContentAbstract[] values) {
            AddCellPathContent(values);
        }

        /// <summary>
        /// function to remove Cell Path Content
        /// </summary>
        public bool RemoveCellPathContent(CellPathContentAbstract value) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to remove CellPathContentAbstract from AreaWorldMod when it's not setted up for advanced area. Position {0}", transform.position);

            bool result;
            //remove from existing cells
            lock (cellPathContents) {
                result = cellPathContents.Remove(value);

                if (_useAdvancedArea) {
                    lock (advancedArea.cells) {
                        foreach (var cell in advancedArea.cells) {
                            cell.RemovePathContent(value);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// function to remove Cell Path Content
        /// </summary>
        public void RemoveCellPathContent(IEnumerable<CellPathContentAbstract> values) {
            if (_useAdvancedArea == false)
                Debug.LogWarningFormat("You trying to remove CellPathContentAbstract from AreaWorldMod when it's not setted up for advanced area. Position {0}", transform.position);

            //add to existing cells
            lock (cellPathContents) {
                lock (values) {
                    cellPathContents.AddRange(values);

                    if (_useAdvancedArea) {
                        lock (advancedArea.cells) {
                            foreach (var cell in advancedArea.cells) {
                                cell.RemovePathContent(values);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// function to remove Cell Path Content
        /// </summary>
        public void RemoveCellPathContent(params CellPathContentAbstract[] values) {
            RemoveCellPathContent(values);
        }
        #endregion

        public void SetUseAdvancedArea(bool value) {
            if(_useAdvancedArea == value) return; //nothing will be changed in that case
            _useAdvancedArea = value;
        }
        public bool useAdvancedArea {
            get { return _useAdvancedArea; }
        }
        
        public Vector3 position {
            get { return transform.position; }
        }
        public Quaternion rotation {
            get { return transform.rotation; }
        }
        public Matrix4x4 localToWorldMatrix {
            get { return Matrix4x4.TRS(position, rotation, Vector3.one); }
        }
        public Matrix4x4 worldToLocalMatrix {
            get { return localToWorldMatrix.inverse; }
        }

        public Color color {
            get { return GetArea().color; }
        }

        public Bounds chunkContentBounds {
            get { return bounds; }
        }

        public Area GetArea() {
            if (useAdvancedArea) {
                return advancedArea;
            }
            else {
                return PathFinder.GetArea(areaInt);
            }
        }

        public void DirtyAll() {
            for (int i = 0; i < allMods.Count; i++) {
                allMods[i].SetDirty();
            }
        }

        public bool CheckDirty() {            
            bool any = false;
            for (int i = 0; i < allMods.Count; i++) {
                if (allMods[i].CheckDirty())
                    any = true;
            }

            if (any) {
                RecalculateBounds();
                ChunkContentMap.Process(this);
            }

            return any;
        }

        public void RecalculateBounds() {
            foreach (var mod in allMods) {
                recalculateBoundsHelper.Add(mod.bounds);
            }
            recalculateBoundsHelper.AddRange(childsAndBounds.Values);

            if (recalculateBoundsHelper.Count == 0)
                return;

            if (recalculateBoundsHelper.Count == 1) {
                bounds = recalculateBoundsHelper[0];
                return;
            }
            else { //if count > 1
                float boundsMinX, boundsMinY, boundsMinZ, boundsMaxX, boundsMaxY, boundsMaxZ;
                Bounds modBounds = recalculateBoundsHelper[0];
                Vector3 center = modBounds.center;
                Vector3 extends = modBounds.extents;

                boundsMinX = center.x - extends.x;
                boundsMinY = center.y - extends.y;
                boundsMinZ = center.z - extends.z;
                boundsMaxX = center.x + extends.x;
                boundsMaxY = center.y + extends.y;
                boundsMaxZ = center.z + extends.z;

                for (int i = 1; i < recalculateBoundsHelper.Count; i++) {
                    Bounds bounds = recalculateBoundsHelper[i];
                    Vector3 bCenter = bounds.center;
                    Vector3 bExtents = bounds.extents;
                    boundsMinX = SomeMath.Min(bCenter.x - bExtents.x, boundsMinX);
                    boundsMinY = SomeMath.Min(bCenter.y - bExtents.y, boundsMinY);
                    boundsMinZ = SomeMath.Min(bCenter.z - bExtents.z, boundsMinZ);
                    boundsMaxX = SomeMath.Max(bCenter.x + bExtents.x, boundsMaxX);
                    boundsMaxY = SomeMath.Max(bCenter.y + bExtents.y, boundsMaxY);
                    boundsMaxZ = SomeMath.Max(bCenter.z + bExtents.z, boundsMaxZ);
                }

                bounds = new Bounds(
                    new Vector3((boundsMinX + boundsMaxX) * 0.5f, (boundsMinY + boundsMaxY) * 0.5f, (boundsMinZ + boundsMaxZ) * 0.5f),
                    new Vector3(boundsMaxX - boundsMinX, boundsMaxY - boundsMinY, boundsMaxZ - boundsMinZ));

                recalculateBoundsHelper.Clear();

                //Debuger_K.AddBounds(bounds, Color.red);
            }
        }

        private void ReassignIndexes() {
            for (int i = 0; i < allMods.Count; i++) {
                allMods[i].id = i;
            }
        }

#if UNITY_EDITOR
        [MenuItem(PathFinderSettings.UNITY_TOP_MENU_FOLDER + "/Create Area Modifyer", false, 2)]
        public static void Create() {
            GameObject go = new GameObject("Area Modifyer");
            Undo.RegisterCreatedObjectUndo(go, "Created Area Modifyer");
            AreaWorldMod am = go.AddComponent<AreaWorldMod>();
            am.OnEnable();
        }
#endif
    }
}