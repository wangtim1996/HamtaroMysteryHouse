using K_PathFinder.Collector;
using K_PathFinder.Pool;
using K_PathFinder.Rasterization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.Collector3 {
    public partial class ShapeCollector {
        //*********POOL*********//
        static Dictionary<VectorInt.Vector2Int, Stack<ShapeCollector>> poolDictionary = new Dictionary<VectorInt.Vector2Int, Stack<ShapeCollector>>();
        const int INITIAL_POOL_SIZE = 10;
        const int INITIAL_FREE_INDEX_POOL_SIZE = 100;
        //*********POOL*********//

        private const int ARRAY_DATA_SIZE = 2;


        public readonly VectorInt.Vector2Int size;
        public readonly int sizeX, sizeZ;

        private float voxelDistanceThreshold;

        NavMeshTemplateCreation template;

        //escential
        public Data[] arrayData;
        int[] freeIndexStack;
        int freeIndexStackLength;

        HashSet<int> freeStackHS = new HashSet<int>();

        public int firstLayerLength;
        public int filledIndexes;

        //enum DebugState {
        //    Taken, Returned
        //}
        //int workCount = 0;
        //public int work = 0;

        //DebugState lastState = DebugState.Returned;
        //StringBuilder log = new StringBuilder();

        //Queue<string> debug = new Queue<string>();

        //int lastReturnedIndex = 0;


        public struct Data {
            public byte area;
            public int next; // -2 : here is nothing, -1 : no next node
            public sbyte pass;
            public float min, max;

            public Data(float min, float max, sbyte pass, int next, byte area) {
                this.min = min;
                this.max = max;
                this.pass = pass;
                this.next = next;
                this.area = area;
            }

            public bool Intersect(float minVal, float maxVal) {
                return min <= maxVal & max >= minVal;
            }
        }

        public struct DataCompact {
            public sbyte pass;
            public float min, max;

            public DataCompact(float min, float max, sbyte pass) {
                this.min = min;
                this.max = max;
                this.pass = pass;
            }

            public void Update(float Min, float Max, sbyte Pass) {
                if (pass != -1) {
                    if (min > Min) min = Min;
                    if (max < Max) max = Max;
                    if (pass < Pass) pass = Pass;
                }
                else {
                    pass = Pass;
                    min = Min;
                    max = Max;
                }
            }

            public void Update(float newHeight) {
                if (pass != -1) {
                    if (min > newHeight) min = newHeight;
                    if (max < newHeight) max = newHeight;
                }
                else {
                    pass = 0;
                    min = max = newHeight;
                }

            }
            public void Update(float newHeight, sbyte Pass) {
                if (pass != -1) {
                    if (min > newHeight) min = newHeight;
                    if (max < newHeight) max = newHeight;
                    if (pass < Pass) pass = Pass;
                }
                else {
                    pass = Pass;
                    min = max = newHeight;
                }
            }

            public void UpdatePassive(float Min, float Max, sbyte Pass) {
                if (pass != -1) {
                    if (min > Min) min = Min;
                    if (max < Max) max = Max;
                }
                else {
                    min = Min;
                    max = Max;
                    pass = Pass;
                }
            }
        }

        public static ShapeCollector GetFromPool(int sizeX, int sizeZ, NavMeshTemplateCreation template) {
            return GetFromPool(new VectorInt.Vector2Int(sizeX, sizeZ), template);
        }

        public static ShapeCollector GetFromPool(VectorInt.Vector2Int size, NavMeshTemplateCreation template) {
            ShapeCollector result = null;

            lock (poolDictionary) {
                Stack<ShapeCollector> stack;
                if (poolDictionary.TryGetValue(size, out stack) == false) {
                    stack = new Stack<ShapeCollector>();
                    poolDictionary.Add(size, stack);

                    for (int i = 0; i < INITIAL_POOL_SIZE; i++) {
                        stack.Push(new ShapeCollector(size));
                    }
                }

                result = stack.Count > 0 ? stack.Pop() : new ShapeCollector(size);
            }

            result.Init(template);
            return result;
        }
        
        public static void ReturnToPool(ref ShapeCollector shape) {
            GenericPoolArray<Data>.ReturnToPool(ref shape.arrayData);
            GenericPoolArray<int>.ReturnToPool(ref shape.freeIndexStack);
            shape.template = null;
            lock (poolDictionary) {
                poolDictionary[shape.size].Push(shape);
            }
            shape = null;
        }

        private ShapeCollector(VectorInt.Vector2Int size) {
            this.sizeX = size.x;
            this.sizeZ = size.y;
            this.size = size;
        }

        public void Init(NavMeshTemplateCreation template) {
            this.template = template;

            int length = flattenSize;

            //!!!!!!!!!!!!!!!!//
            freeIndexStack = GenericPoolArray<int>.Take(INITIAL_FREE_INDEX_POOL_SIZE);
            arrayData = GenericPoolArray<Data>.Take(length * ARRAY_DATA_SIZE);

            for (int i = 0; i < length; i++) {
                arrayData[i].next = -2;
            }

            for (int i = length; i < arrayData.Length; i++) {
                arrayData[i].next = -1;
            }

            filledIndexes = length;
            freeIndexStackLength = 0;
            voxelDistanceThreshold = template.voxelSize;


            freeStackHS.Clear();
        }
        public int flattenSize {
            get { return sizeX * sizeZ; }
        }

        //IS EXAMPLE
        //private int GetFreeIndex() {
        //    return stackLength > 0 ? freeIndexStack[--stackLength] : filledIndexes++;
        //}

        private int GetFreeIndex() {
            //int result = 0;
            //if (freeStackHS.Count > 0) {
            //    result = freeStackHS.First();
            //    freeStackHS.Remove(result);
            //}
            //else {
            //    result = filledIndexes++;
            //}
            //debug.Enqueue(string.Format("{2} GetFreeIndex: {0}, Cur stack length {1}", result, freeStackHS.Count, work));

            int result = freeIndexStackLength > 0 ? freeIndexStack[--freeIndexStackLength] : filledIndexes++;
            //lastReturnedIndex = 0;
            //debug.Enqueue(string.Format("{2} GetFreeIndex: {0}, Cur stack length {1}", result, freeIndexStackLength, work));
            //if (debug.Count > 200) debug.Dequeue();
            return result;
        }



        //IS EXAMPLE
        private void ReturnFreeIndex(int index) {
            if (freeIndexStack.Length == freeIndexStackLength) {
                //Debug.LogFormat("stack doubled from {0} to {1}\n", freeIndexStack.Length, freeIndexStack.Length * 2);
                int[] newFreeIndexStack = GenericPoolArray<int>.Take(freeIndexStack.Length * 2);
                Array.Copy(freeIndexStack, newFreeIndexStack, freeIndexStack.Length);
                GenericPoolArray<int>.ReturnToPool(ref freeIndexStack);
                freeIndexStack = newFreeIndexStack;
            }
            freeIndexStack[freeIndexStackLength++] = index;
            //Debug.LogFormat("ReturnFreeIndex: returned {0}, stack length {1}, Cur stack {2}", index, stackLength, DebugCurFreeStack());
        }



        //IS EXAMPLE
        public int GetIndex(int x, int z) {
            return (z * sizeX) + x;
        }


        //const int targetIndex = 9700;
        //int debugAdditions = 0;
        public void SetVoxel(int x, int z, float min, float max, sbyte pass, byte area) {
            //work++;

            int curIndex = (z * sizeX) + x;
            Data curNode = arrayData[curIndex];

            //bool doDebug = curIndex == targetIndex;

            //if (doDebug) {
            //    debugAdditions++;
            //    Debug.LogFormat("{3} Set Voxel {0} min {1} max {2}", curIndex, min, max, debugAdditions - 1);

            //    Vector3 vMin = GetPos(x + debugAdditions - 1, z, min);
            //    Vector3 vMax = GetPos(x + debugAdditions - 1, z, max);

            //    Debuger_K.AddDot(vMin, Color.black, 0.005f);
            //    Debuger_K.AddDot(vMax, Color.white, 0.005f);
            //    Debuger_K.AddLine(vMin, vMax, Color.white);
            //    Debuger_K.AddLabel(SomeMath.MidPoint(vMin, vMax), debugAdditions - 1);

            //}

            if (curNode.next == -2) {
                //if (doDebug) Debug.LogFormat("{0} next == -2", debugAdditions - 1);
                arrayData[curIndex] = new Data(min, max, pass, -1, area);
            }
            else {
                bool isApplyed = false;
                int prevIndex = -1;

                while (true) {
                    curNode = arrayData[curIndex];
                    //if (doDebug) Debug.LogFormat("{0} Iteration", debugAdditions - 1);

                    if (curNode.min > max) {
                        //if (doDebug) Debug.LogFormat("{1} curNode.min > max. isApplyed: {0}", isApplyed, debugAdditions - 1);

                        if (isApplyed)
                            break;

                        //int freeIndex = GetFreeIndex();
                        int freeIndex = freeIndexStackLength > 0 ? freeIndexStack[--freeIndexStackLength] : filledIndexes++;
                        
                        if (freeIndex == arrayData.Length) {
                            Data[] newArrayData = GenericPoolArray<Data>.Take(arrayData.Length * 2);
                            Array.Copy(arrayData, newArrayData, arrayData.Length);
                            GenericPoolArray<Data>.ReturnToPool(ref arrayData);
                            arrayData = newArrayData;
                        }
                        
                        arrayData[freeIndex] = curNode;
                        arrayData[curIndex] = new Data(min, max, pass, freeIndex, area);
                        //if (doDebug) Debug.LogFormat("{1} Setted up at {0}", freeIndex, debugAdditions - 1);
                        break;
                    }

                    if (curNode.max < min) {
                        //if (doDebug) Debug.LogFormat("{0} curNode.max < min", debugAdditions - 1);

                        //current node are below current data
                        if (curNode.next == -1) {//no data next just add node
                                                 //if (doDebug) Debug.LogFormat("{0} no data next just add node", debugAdditions - 1);
                            //int freeIndex = GetFreeIndex();
                            int freeIndex = freeIndexStackLength > 0 ? freeIndexStack[--freeIndexStackLength] : filledIndexes++;

                            if (freeIndex == arrayData.Length) {
                                Data[] newArrayData = GenericPoolArray<Data>.Take(arrayData.Length * 2);
                                Array.Copy(arrayData, newArrayData, arrayData.Length);
                                GenericPoolArray<Data>.ReturnToPool(ref arrayData);
                                arrayData = newArrayData;
                            }


                            arrayData[curIndex].next = freeIndex;
                            arrayData[freeIndex] = new Data(min, max, pass, -1, area);
                            break;
                        }
                        else {//there is some data next check it
                            //if (doDebug) Debug.LogFormat("{0} there is some data next check it", debugAdditions - 1);
                            curIndex = curNode.next;
                            continue;
                        }
                    }



                    if (curNode.min < min) min = curNode.min;
                    if (curNode.max > max) max = curNode.max;

                    //if (doDebug) Debug.LogFormat("{0} Overlaping. new min {1} max {2}", debugAdditions - 1, min, max);

                    if (Math.Abs(max - curNode.max) <= voxelDistanceThreshold) {
                        pass = Math.Max(pass, curNode.pass);
                        area = curNode.area; //here actualy should be priority
                    }


                    if (prevIndex != -1 && arrayData[prevIndex].Intersect(min, max)) {
                        //set data to previous index
                        //if (doDebug) Debug.LogFormat("{0} set data to PREVIOUS ({1}) index", debugAdditions - 1, prevIndex);
                        //ReturnFreeIndex(curIndex);

                        if (freeIndexStack.Length == freeIndexStackLength) {
                            int[] newFreeIndexStack = GenericPoolArray<int>.Take(freeIndexStack.Length * 2);
                            Array.Copy(freeIndexStack, newFreeIndexStack, freeIndexStack.Length);
                            GenericPoolArray<int>.ReturnToPool(ref freeIndexStack);
                            freeIndexStack = newFreeIndexStack;
                        }
                        freeIndexStack[freeIndexStackLength++] = curIndex;


                        arrayData[prevIndex] = new Data(min, max, pass, curNode.next, area);

                    }
                    else {
                        //if (doDebug) Debug.LogFormat("{0} set data to CURRENT ({1}) index", debugAdditions - 1, curIndex);
                        arrayData[curIndex] = new Data(min, max, pass, curNode.next, area);
                        prevIndex = curIndex;
                    }
                    isApplyed = true;
                    curIndex = curNode.next;
                    if (curIndex == -1)
                        break;
                }
            }
        }

        public Vector3 GetPos(int x, int z, float y) {
            return 
                template.realOffsetedPosition + 
                template.halfVoxelOffset + 
                new Vector3((x * template.voxelSize), y, (z * template.voxelSize));
        }

        public void ChangeArea(int x, int z, Area area) {
            int index = GetIndex(x, z);

            if (arrayData[index].next == -2)
                return;

            for (; index != -1; index = arrayData[index].next) {
                arrayData[index].area = GetAreaValue(area);
            }
        }

        public void ChangePassability(int x, int z, sbyte pass) {
            int index = GetIndex(x, z);

            if (arrayData[index].next == -2)
                return;

            for (; index != -1; index = arrayData[index].next) {
                arrayData[index].pass = pass;
            }
        }


        public DataCompact[] TakeCompactData() {
            int size = flattenSize;
            DataCompact[] result = GenericPoolArray<DataCompact>.Take(size);
            sbyte initialPass = -1;
            for (int i = 0; i < size; i++) {
                result[i].pass = initialPass;
            }

            return result;
        }


        public void AppendCompactData(DataCompact[] value, byte compactDataArea) {
            AppendCompactData(value, compactDataArea, 0, sizeX, 0, sizeZ);
        }

        public void AppendCompactData(DataCompact[] value, byte compactDataArea, int startX, int endX, int startZ, int endZ) {
            for (int z = startZ; z < endZ; z++) {
                for (int x = startX; x < endX; x++) {
                    int curIndex = (z * sizeX) + x;
                    var val = value[curIndex];

                    if (val.pass != -1) {
                        //SetVoxel(x, z, val.min, val.max, val.pass, compactDataArea);
                        //copy pasted code from set voxel to reduce overhead
                        
                        Data curNode = arrayData[curIndex];

                        byte area = compactDataArea;

                        if (curNode.next == -2) {
                            arrayData[curIndex] = new Data(val.min, val.max, val.pass, -1, area);
                        }
                        else {
                            bool isApplyed = false;
                            int prevIndex = -1;

                            while (true) {
                                curNode = arrayData[curIndex];

                                if (curNode.min > val.max) {
                                    if (isApplyed) break;

                                    int freeIndex = freeIndexStackLength > 0 ? freeIndexStack[--freeIndexStackLength] : filledIndexes++;

                                    if (freeIndex == arrayData.Length) {
                                        Data[] newArrayData = GenericPoolArray<Data>.Take(arrayData.Length * 2);
                                        Array.Copy(arrayData, newArrayData, arrayData.Length);
                                        GenericPoolArray<Data>.ReturnToPool(ref arrayData);
                                        arrayData = newArrayData;
                                    }

                                    arrayData[freeIndex] = curNode;
                                    arrayData[curIndex] = new Data(val.min, val.max, val.pass, freeIndex, area);
                                    break;
                                }

                                if (curNode.max < val.min) {
                                    if (curNode.next == -1) {
                                        int freeIndex = freeIndexStackLength > 0 ? freeIndexStack[--freeIndexStackLength] : filledIndexes++;

                                        if (freeIndex == arrayData.Length) {
                                            Data[] newArrayData = GenericPoolArray<Data>.Take(arrayData.Length * 2);
                                            Array.Copy(arrayData, newArrayData, arrayData.Length);
                                            GenericPoolArray<Data>.ReturnToPool(ref arrayData);
                                            arrayData = newArrayData;
                                        }
                                        
                                        arrayData[curIndex].next = freeIndex;
                                        arrayData[freeIndex] = new Data(val.min, val.max, val.pass, -1, area);
                                        break;
                                    }
                                    else {
                                        curIndex = curNode.next;
                                        continue;
                                    }
                                }

                                if (curNode.min < val.min) val.min = curNode.min;
                                if (curNode.max > val.max) val.max = curNode.max;

                                if (Math.Abs(val.max - curNode.max) <= voxelDistanceThreshold) {
                                    val.pass = Math.Max(val.pass, curNode.pass);
                                    area = curNode.area;
                                }

                                if (prevIndex != -1 && arrayData[prevIndex].Intersect(val.min, val.max)) {
                                    if (freeIndexStack.Length == freeIndexStackLength) {
                                        int[] newFreeIndexStack = GenericPoolArray<int>.Take(freeIndexStack.Length * 2);
                                        Array.Copy(freeIndexStack, newFreeIndexStack, freeIndexStack.Length);
                                        GenericPoolArray<int>.ReturnToPool(ref freeIndexStack);
                                        freeIndexStack = newFreeIndexStack;
                                    }
                                    freeIndexStack[freeIndexStackLength++] = curIndex;
                                    arrayData[prevIndex] = new Data(val.min, val.max, val.pass, curNode.next, area);

                                }
                                else {
                                    arrayData[curIndex] = new Data(val.min, val.max, val.pass, curNode.next, area);
                                    prevIndex = curIndex;
                                }
                                isApplyed = true;
                                curIndex = curNode.next;
                                if (curIndex == -1) break;
                            }
                        }
                    }                   
                }
            }     
        }

        //for terrain
        public void AppendCompactData(DataCompact[] value, byte[] area) {
            sbyte unwalkable = (sbyte)Passability.Unwalkable;
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    int index = GetIndex(x, z);
                    var val = value[index];
                    if (val.pass != -1) {
                        byte targetArea = area[index];
                        if(targetArea == 1)
                            SetVoxel(x, z, val.min, val.max, unwalkable, targetArea);
                        else
                            SetVoxel(x, z, val.min, val.max, val.pass, targetArea);
                    }
                }
            }

            Pool.GenericPoolArray<DataCompact>.ReturnToPool(ref value);
        }

        //for compute shaders
        public void AppendComputeShaderResult(CSRasterization3DResult data, Area area) {
            Voxel3D[] voxels = data.voxels;
            int voxelsX = data.voxelsX;
            //int voxelsY = data.voxelsY;
            int volumeStartX = data.volumeStartX;
            int volumeStartZ = data.volumeStartZ;
            int volumeSizeX = data.volumeSizeX;
            int volumeSizeZ = data.volumeSizeZ;
            byte areaValue = GetAreaValue(area);

            for (int x = 0; x < volumeSizeX; x++) {
                for (int z = 0; z < volumeSizeZ; z++) {
                    var curVoxel = voxels[(z * voxelsX) + x];

                    if (curVoxel.passability != -1) {
                        SetVoxel(volumeStartX + x, volumeStartZ + z, curVoxel.min, curVoxel.max,(sbyte)curVoxel.passability, areaValue);
                    }
                }
            }
        }

        //for terrain with area mask
        public void AppendComputeShaderResult(CSRasterization2DResult data, byte[] area) {
            var voxels = data.voxels;    

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    var curVoxel = voxels[x + (z * sizeX)];
                    if (curVoxel.passability != -1)
                        SetVoxel(x, z, curVoxel.height - 20f, curVoxel.height, (sbyte)curVoxel.passability, area[GetIndex(x, z)]);
                }
            }
        }
        //for terrain with single area
        public void AppendComputeShaderResult(CSRasterization2DResult data, byte area) {
            var voxels = data.voxels;

            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    var curVoxel = voxels[x + (z * sizeX)];
                    if (curVoxel.passability != -1)
                        SetVoxel(x, z, curVoxel.height - 20f, curVoxel.height, (sbyte)curVoxel.passability, area);
                }
            }
        }
        
        private byte GetAreaValue(Area area) {
            return template.hashData.areaToIndex[area];
        }

        public void Append(ShapeDataAbstract shape) {
            if (shape is ShapeDataSphere) {
                AppendSphere(shape as ShapeDataSphere);
            }
            else
            if (shape is ShapeDataCapsule) {
                AppendCapsule(shape as ShapeDataCapsule);
            }
            else
            if (shape is ShapeDataMesh) {
                ShapeDataMesh mesh = shape as ShapeDataMesh;
                if (mesh.convex)
                    AppendMeshConvex(mesh);
                else
                    AppendMeshNonConvex(mesh);
            }
            else
            if (shape is ShapeDataBox) {
                AppendBox(shape as ShapeDataBox);
            }
            else
            if (shape is ShapeDataCharacterControler) {
                AppendCharacterControler(shape as ShapeDataCharacterControler);
            }
            else {
                Debug.LogWarningFormat("Current type are not implemented: {0}. Tell developer to fix that");
            }
        }
        
        public void ChangeArea(List<ShapeDataAbstract> shapes, Area area) {
            byte areaValue = GetAreaValue(area);   

            for (int i = 0; i < shapes.Count; i++) {
                DataCompact[] compactData = TakeCompactData();
                var shape = shapes[i];

                if(shape is ShapeDataSphere) {
                    ShapeDataSphere castedShape = shape as ShapeDataSphere;
                    //not walkable cause in this case we only intrested in min and max and this method ignores it
                    AppendSpherePrivate(compactData, castedShape.bounds.center, castedShape.bounds.extents.x, 0f, false, false);
                }
                else if (shape is ShapeDataCapsule) {
                    ShapeDataCapsule castedShape = shape as ShapeDataCapsule;
                    AppendCapsulePrivate(compactData, castedShape.sphereA, castedShape.sphereB, castedShape.capsileRadius, false, areaValue);
                }
                else if (shape is ShapeDataBox) {
                    ShapeDataBox castedShape = shape as ShapeDataBox;
                    AppendMeshConvexPrivate(compactData, ColliderCollector.cubeVerts, ColliderCollector.cubeTris, castedShape.boxMatrix, areaValue, false);
                }
                else {
                    Debug.LogError("no support for current area modifyer in shape collector");
                }

                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        int index = GetIndex(x, z);
                        DataCompact data = compactData[index];
                        if (data.pass == -1 || arrayData[index].next == -2)
                            continue;

                        for (; index != -1; index = arrayData[index].next) {
                            var arrData = arrayData[index];
                            if (arrData.max >= data.min & arrData.max <= data.max)
                                arrayData[index].area = areaValue;
                        }
                    }
                }
                GenericPoolArray<DataCompact>.ReturnToPool(ref compactData);
            }    
        }


        /// <summary>
        /// if area is null then area is retained
        /// </summary>
        public void MakeHole(List<ShapeDataAbstract> shapes, Area area) {
            bool appyArea = area != null;

            byte areaValue = 0;
            if(appyArea)
                areaValue = GetAreaValue(area);

            for (int i = 0; i < shapes.Count; i++) {
                DataCompact[] compactData = TakeCompactData();
                var shape = shapes[i];

                if (shape is ShapeDataSphere) {
                    ShapeDataSphere castedShape = shape as ShapeDataSphere;
                    AppendSpherePrivate(compactData, castedShape.bounds.center, castedShape.bounds.extents.x, 0f, false, true);
                }
                else if (shape is ShapeDataCapsule) {
                    ShapeDataCapsule castedShape = shape as ShapeDataCapsule;
                    AppendCapsulePrivate(compactData, castedShape.sphereA, castedShape.sphereB, castedShape.capsileRadius, true, areaValue);
                }
                else if (shape is ShapeDataBox) {
                    ShapeDataBox castedShape = shape as ShapeDataBox;
                    AppendMeshConvexPrivate(compactData, ColliderCollector.cubeVerts, ColliderCollector.cubeTris, castedShape.boxMatrix, areaValue, true);
                    //Debug.LogWarning("dont forget to flip Y in ShapeDataBox");
                }
                else {
                    Debug.LogError("no support for current area modifyer in shape collector");
                }

                Vector3 realChunkPos = template.realOffsetedPosition;
                Vector3 offset = template.halfVoxelOffset;

                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        int index = GetIndex(x, z);
                        DataCompact mask = compactData[index];            

                        if (mask.pass != -1 && arrayData[index].next != -2) {
                            int prevIndex = -1;
                            int curIndex = index;
                            
                            while (true) {
                                if (curIndex < 0)
                                    break;

                                Data curNode = arrayData[curIndex];

                                if ((mask.min > curNode.max | mask.max < curNode.min) == false) { //if current mask in not higher or lower than current node               
                                    if (SomeMath.InRangeExclusive(mask.min, curNode.min, curNode.max)) {
                                        arrayData[curIndex].max = mask.min;
                                        arrayData[curIndex].pass = mask.pass;
                                        if (appyArea)
                                            arrayData[curIndex].area = areaValue;

                                        if (SomeMath.InRangeExclusive(mask.max, curNode.min, curNode.max)) {
                                            int freeIndex = GetFreeIndex();
                                            arrayData[freeIndex] = new Data(mask.max, curNode.max, curNode.pass, curNode.next, curNode.area);
                                            arrayData[curIndex].next = freeIndex;
                                            break;
                                        }
                                        prevIndex = curIndex;
                                        curIndex = arrayData[curIndex].next;
                                    }
                                    else if (SomeMath.InRangeExclusive(mask.max, curNode.min, curNode.max)) {
                                        //top of mask inside current shape                                    
                                        arrayData[curIndex].min = mask.max;
                                        break;//this nothing can be intersected after that
                                    }
                                    else {
                                        if (curNode.next == -1) {//if there no nodes after that
                                            if (prevIndex == -1)//if it first node
                                                arrayData[curIndex].next = -2;//make it invalid
                                            else
                                                arrayData[prevIndex].next = -1;
                                            break;
                                        }
                                        //if some nodes after that
                                        else {
                                            //shift next thata to current index
                                            //NOTE: curIndex is not changed to check new data at this index                                       
                                            ReturnFreeIndex(curNode.next);
                                            arrayData[curIndex] = arrayData[curNode.next];
                                         
                                            continue;
                                        }
                                    }
                                }

                                prevIndex = curIndex;
                                curIndex = curNode.next;
                            }
                        }
                    }
                }
                GenericPoolArray<DataCompact>.ReturnToPool(ref compactData);
            }
        }

#if UNITY_EDITOR
        private string DebugCurFreeStack() {
            string result = "";
            for (int i = 0; i < freeIndexStackLength; i++) {
                result += " " + freeIndexStack[i];
            }
            return result;
        }

        public void DebugMe(bool drawMax = true, bool drawMin = true, bool drawMaxMinSize = true) {
            Vector3 realChunkPos = template.realOffsetedPosition;
            Vector3 offset = template.halfVoxelOffset;
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    int gen = 0;
                    int index = (z * sizeX) + x;

                    var current = arrayData[index];

                    if (current.next == -2)
                        continue;

                    while (true) {
                        Vector3 p1 = realChunkPos + offset + new Vector3((x * template.voxelSize), current.max, (z * template.voxelSize));
                        Vector3 p2 = realChunkPos + offset + new Vector3((x * template.voxelSize), current.min, (z * template.voxelSize));

                        Color dColor;
                        switch (current.pass) {
                            case -1:
                                dColor = Color.black;
                                break;
                            case 0:
                                dColor = Color.red;
                                break;
                            case 1:
                                dColor = Color.magenta;
                                break;
                            case 2:
                                dColor = new Color(0f, 0.5f, 0f, 1f);
                                break;
                            case 3:
                                dColor = Color.green;
                                break;
                            default:
                                dColor = Color.white;
                                break;
                        }
                        //Debuger_K.AddLabelFormat(p2 + (new Vector3(0.005f, 0.005f, 0.005f) * gen), "{0}", gen);
                        //Debuger_K.AddLabelFormat(p2 + (new Vector3(0.005f, 0.005f, 0.005f) * gen), "{0}\n{1}\n{2}", gen, curVal.max, curVal.min);

                        if (drawMax) {
                            Debuger_K.AddDot(p1, dColor, 0.02f);
                        }

                        if (drawMin) {
                            Debuger_K.AddDot(p2, Color.gray, 0.02f);
                        }

                        if (drawMaxMinSize) {
                            Debuger_K.AddLine(p1, p2);
                        }

                        gen++;
                        if (current.next == -1)
                            break;
                        else
                            current = arrayData[current.next];
                    }
                }
            }
        }
#endif

    }
}