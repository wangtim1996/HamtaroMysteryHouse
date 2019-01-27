using K_PathFinder.CoolTools;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder.Pool;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//**********************************//
//**************CORE****************//
//**********************************//
//*****For general voxels stuff*****//
//**********************************//

namespace K_PathFinder {
    public partial class VolumeContainerNew {
        public struct DataCollum {
            public int index;
            public sbyte count;

            public DataCollum(int Index, sbyte Count) {
                index = Index; count = Count;
            }
        }
        public struct Data {
            public float y;
            public byte pass;
            public byte area;
            public short layer;

            public const sbyte INVALID_CONNECTION = -1;
            public sbyte xPlus, xMinus, zPlus, zMinus;
            public int flags;


            public Data(float Y, byte Pass, byte Area) {
                y = Y; pass = Pass; area = Area;
                xPlus = xMinus = zPlus = zMinus = INVALID_CONNECTION;
                flags = 0;
                layer = -1;
            }

            public sbyte GetConnection(Directions direction) {
                switch (direction) {
                    case Directions.xPlus:
                        return xPlus;
                    case Directions.xMinus:
                        return xMinus;
                    case Directions.zPlus:
                        return zPlus;
                    case Directions.zMinus:
                        return zMinus;
                    default:
                        return -1;
                }
            }

            public void SetState(VoxelState state, bool value) {
                flags = value ? (flags | (int)state) : (flags & ~(int)state);
            }
            public bool GetState(VoxelState state) {
                return (flags & (int)state) != 0;
            }
        }

        //only name is changed but it to deferentiate use cases
        //use case for this - index lead to index in data, not layer
        public struct DataPos_DirectIndex {
            public int x, z, index;

            public DataPos_DirectIndex(int X, int Z, int Index) {
                x = X; z = Z; index = Index;
            }

            public override int GetHashCode() {
                return index;
            }
        }
        public struct DataPos {
            public int x, z, layer;

            public DataPos(int X, int Z, int Index) {
                x = X; z = Z; layer = Index;
            }

            public override int GetHashCode() {
                return layer;
            }

            public override string ToString() {
                return string.Format("x: {0}, z: {1}, layer: {2}", x, z, layer);
            }
        }
        public struct DataCover {
            public sbyte hash;
            public sbyte coverHeight;
        }


        public NavMeshTemplateCreation template;
        public NavmeshProfiler profiler;
        public readonly int sizeX, sizeZ, flattenedSize;


        bool doCrouch, doCover, doHalfCover;
        float halfCover, fullCover;

        public int maxColumCount;
        public short layersCount;

        Collector3.ShapeCollector.Data[] arrayData;

        HashSet<DataPos_DirectIndex> nearObstacleSet, nearCrouchSet;


        public BattleGrid battleGrid;



        public Collector3.ShapeCollector shape;


        public DataCollum[] collums;
        public Data[] data;

        //public DataLayer[] dataLayers;
        Vector3 realChunkPos, offset;

        //Dictionary<DataPos, HashSet<VolumeArea>> volumeArea;
        public List<VolumeArea> volumeAreas = new List<VolumeArea>();

        public StackedList<VolumeArea> areaSet;

        public DataCover[] coverData;

        public VolumeContainerNew(NavMeshTemplateCreation template) {
            this.template = template;
            this.profiler = template.profiler;
     
            doCrouch = template.canCrouch;
            doCover = template.doCover;

            if (doCover) {
                fullCover = template.properties.fullCover;
                doHalfCover = template.doHalfCover;
                if (doHalfCover)
                    halfCover = template.properties.halfCover;
            }

            sizeX = template.lengthX_extra;
            sizeZ = template.lengthZ_extra;
            flattenedSize = sizeX * sizeZ;

            //volumeArea = new Dictionary<DataPos, HashSet<VolumeArea>>();
        }

        public int GetIndex(int x, int z) {
            return (z * sizeX) + x;
        }

        public void AddGenericColliders(Collector3.ShapeCollector shape) {
            arrayData = shape.arrayData;
            this.shape = shape;
        }


        public void DoStuff() {
            realChunkPos = template.realOffsetedPosition;
            offset = template.halfVoxelOffset;

            if (profiler != null) profiler.AddLog("start setting height flags");
            SetHeightFlags();
            if (profiler != null) profiler.AddLog("end setting height flags");

            if (profiler != null) profiler.AddLog("start making compact data");
            MakeCompactData();
            if (profiler != null) profiler.AddLog("end making compact data");

  
            areaSet = new StackedList<VolumeArea>(shape.filledIndexes, flattenedSize);

            if (profiler != null) profiler.AddLog("start generating connections");
            SetConnections();
            if (profiler != null) profiler.AddLog("end generating connections");

            if (profiler != null) profiler.AddLog("start making obstacles set");
            CreateNearObstaclesSet();
            if (profiler != null) profiler.AddLog("end making obstacles set");

            //create jump spots
            if (template.canJump) {
                if (profiler != null) profiler.AddLog("agent can jump. start capturing areas for jump");

                int sqrArea = template.agentRagius * template.agentRagius;
                int doubleAreaSqr = (template.agentRagius * 2) * (template.agentRagius * 2) + 2; //plus some extra

                foreach (var pos in nearObstacleSet) {
                    var dataValue = data[pos.index];;
                    if(dataValue.pass >= (sbyte)Passability.Crouchable && dataValue.GetState(VoxelState.VoxelAreaFlagJumpOuter) == false) {
                        CaptureArea(pos, AreaType.Jump, 
                            sqrArea, VoxelState.CheckForVoxelAreaFlag, 
                            doubleAreaSqr, VoxelState.VoxelAreaFlagJumpOuter, true, true);
                    }
                }

                if (profiler != null) profiler.AddLog("end capturing areas for jump");
            }

            if (doCover) {
                if (profiler != null) profiler.AddLog("agent can cover. start generating covers");
                //important to check it before growth
                GenerateCovers(template.agentRagius + template.coverExtraSamples);
                if (profiler != null) profiler.AddLog("end generating covers");
            }

            if (doCrouch) {
                if (profiler != null) profiler.AddLog("agent can cover. start generating cover obstacles");

                GrowthObstacles(
                    nearCrouchSet,
                    template.agentRagius * template.agentRagius,
                    (DataPos_DirectIndex vp) => { return data[vp.index].pass == (byte)Passability.Crouchable; },
                    (DataPos_DirectIndex vp) => { return data[vp.index].pass == (byte)Passability.Walkable; },
                    (DataPos_DirectIndex vp) => { data[vp.index].pass = (byte)Passability.Crouchable; },
                    template);

                if (profiler != null) profiler.AddLog("end generating cover obstacles");
            }


            if (profiler != null) profiler.AddLog("start growing obstacles");
            GrowthObstacles(
                nearObstacleSet,
                template.agentRagius * template.agentRagius,
                (DataPos_DirectIndex vp) => { return data[vp.index].pass < (byte)Passability.Crouchable; },
                (DataPos_DirectIndex vp) => { return data[vp.index].pass >= (byte)Passability.Crouchable; },
                (DataPos_DirectIndex vp) => { data[vp.index].pass = (byte)Passability.Unwalkable; },
                template);

            if (profiler != null) profiler.AddLog("end growing obstacles");

            if (template.doBattleGrid) {
                if (profiler != null) profiler.AddLog("agent use battle grid. start creating battle grid");
                BattleGrid();
                if (profiler != null) profiler.AddLog("end creating battle grid");
            }

            //int extra = template.extraOffset;
            //sbyte minPass = (sbyte)Passability.Crouchable;
            //for (int x = extra; x < sizeX - extra; x++) {
            //    for (int z = extra; z < sizeZ - extra; z++) {
            //        int index = GetIndex(x, z);
            //        DataCollum dc = collums[index];
            //        for (int i2 = 0; i2 < dc.count; i2++) {
            //            var curValue = data[dc.index + i2];
            //            if(curValue.area != 1 & curValue.pass >= minPass)
            //                dataLayers[i2].data[index].SetData(curValue.y, AreaPassabilityHashData.GetAreaHash(curValue.area, curValue.pass), curValue.flags);
            //        }
            //    }
            //}


            if (maxColumCount > 2 | doCover) { //cover check because there will be artifacts even on single layer on diagonals
                //var sw = new System.Diagnostics.Stopwatch();
                //sw.Start();
                SplitToLayers(out layersCount); //overal it will take ~5% of time
                //sw.Stop();
                //var elapsed = sw.Elapsed;
                //Debug.Log("split: " + elapsed);
            }
            else {
                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        var collum = collums[(z * sizeX) + x];
                        for (short layer = 0; layer < collum.count; layer++) {
                            data[collum.index + layer].layer = layer;
                        }
                    }
                }
                layersCount = (short)maxColumCount;
            }


#if UNITY_EDITOR
            if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
                if (profiler != null) profiler.AddLog("start adding volumes to debuger");
                Debuger_K.AddVolumes(template, this);
                if (profiler != null) profiler.AddLog("end adding volumes to debuger");
            }
#endif

            Collector3.ShapeCollector.ReturnToPool(ref shape);            
            //DebugCompactData();
        }      

        public void SetHeightFlags() {
            float agentHeight = template.properties.height;

            if (doCrouch) {
                float crouchHeight = template.properties.crouchHeight;

                for (int i = 0; i < flattenedSize; i++) {
                    var curValue = arrayData[i];

                    if (curValue.next != -2) {
                        int curIndex = i;
                        while (true) {
                            int nextIndex = arrayData[curIndex].next;
                            if (nextIndex == -1)
                                break;

                            if (arrayData[curIndex].pass > (sbyte)Passability.Slope) {
                                float diff = arrayData[nextIndex].min - arrayData[curIndex].max;

                                if (diff < agentHeight) {
                                    if (diff < crouchHeight)
                                        arrayData[curIndex].pass = (sbyte)Passability.Unwalkable;
                                    else
                                        arrayData[curIndex].pass = (sbyte)Passability.Crouchable;
                                }
                            }
                            curIndex = nextIndex;
                        }
                    }
                }
            }
            else {
                for (int i = 0; i < flattenedSize; i++) {
                    var curValue = arrayData[i];

                    if (curValue.next != -2) {
                        int curIndex = i;
                        while (true) {
                            int nextIndex = arrayData[curIndex].next;
                            if (nextIndex == -1)
                                break;

                            if (arrayData[curIndex].pass > (sbyte)Passability.Slope) {
                                if (arrayData[nextIndex].min - arrayData[curIndex].max < agentHeight) {
                                    arrayData[curIndex].pass = (sbyte)Passability.Unwalkable;
                                }
                            }
                            curIndex = nextIndex;
                        }
                    }
                }
            }
        }
        
        private void MakeCompactData() {
            collums = new DataCollum[flattenedSize];

            data = new Data[shape.filledIndexes];

 


            int counter = 0;
            for (int z = 0; z < sizeZ; z++) {
                for (int x = 0; x < sizeX; x++) {
                    int i = GetIndex(x, z);

                    int start = counter;

                    if (arrayData[i].next != -2) {
                        int curIndex = i;
                        var curValue = arrayData[i];

                        while (true) {                        
                            if (curValue.pass > (sbyte)Passability.Slope && curValue.area != 1) {
                                data[counter++] = new Data(curValue.max, (byte)curValue.pass, curValue.area);
                            }

                            curIndex = arrayData[curIndex].next;
                            if (curIndex == -1)
                                break;

                            curValue = arrayData[curIndex];
                        }
                    }


                    int count = counter - start;
                    if(count > sbyte.MaxValue) {
                        Debug.LogError("volume have too much layers");
                    }

                    collums[i] = new DataCollum(start, (sbyte)count);
                    if (count > maxColumCount)
                        maxColumCount = count;
                }
            }
        }

#if UNITY_EDITOR
        void DebugCompactData() {
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    DataCollum collum = collums[GetIndex(x, z)];
                    if (collum.count == 0)
                        continue;

                    for (int i = 0; i < collum.count; i++) {
                        Data value = data[collum.index + i];

                        Vector3 p1 = GetPos(x, z, value.y);

                        Color dColor;
                        switch ((Passability)value.pass) {
                            case Passability.Unwalkable:
                                dColor = Color.red;
                                break;
                            case Passability.Slope:
                                dColor = Color.magenta;
                                break;
                            case Passability.Crouchable:
                                dColor = template.hashData.areaByIndex[value.area].color;
                                dColor = new Color(dColor.r * 0.5f, dColor.g * 0.5f, dColor.b * 0.5f, 1f);
                                break;
                            case Passability.Walkable:
                                dColor = template.hashData.areaByIndex[value.area].color;
                                break;
                            default:
                                dColor = Color.white;
                                break;
                        }
                        Debuger_K.AddDot(p1, dColor, 0.02f);
                        //Debuger_K.AddLabel(p1, i);
                    }
                }
            }
        }
#endif

        private void SetConnections() {
            float connectionDistance = template.properties.maxStepHeight;

            //x
            for (int x = 0; x < sizeX - 1; x++) {
                for (int z = 0; z < sizeZ; z++) {
                    DataCollum collum = collums[GetIndex(x, z)];
                    if (collum.count == 0)
                        continue;

                    DataCollum collumPlusX = collums[GetIndex(x + 1, z)];
                    if (collumPlusX.count == 0)
                        continue;

                    for (sbyte i = 0; i < collum.count; i++) {
                        Data value1 = data[collum.index + i];
                        float closestStep = float.MaxValue;
                        sbyte closestIndex = 0;
                        for (sbyte i2 = 0; i2 < collumPlusX.count; i2++) {
                            Data value2 = data[collumPlusX.index + i2];
                            float curStep = Mathf.Abs(value1.y - value2.y);
                            if (curStep < closestStep) {
                                closestStep = curStep;
                                closestIndex = i2;
                            }
                        }
                        if (closestStep <= connectionDistance) {
                            data[collum.index + i].xPlus = closestIndex;
                            data[collumPlusX.index + closestIndex].xMinus = i;
                            //Vector3 p1 = GetPos(x, z, data[collum.index + i].y);
                            //Vector3 p2 = GetPos(x + 1, z, data[collumPlusX.index + closestIndex].y);
                            //Debuger_K.AddLine(p1, p2);
                        }
                    }
                }
            }

            //z
            for (int x = 0; x < sizeX; x++) {
                for (int z = 0; z < sizeZ - 1; z++) {
                    DataCollum collum = collums[GetIndex(x, z)];
                    if (collum.count == 0)
                        continue;

                    DataCollum collumPlusZ = collums[GetIndex(x, z + 1)];
                    if (collumPlusZ.count == 0)
                        continue;

                    for (sbyte i = 0; i < collum.count; i++) {
                        Data value1 = data[collum.index + i];
                        float closestStep = float.MaxValue;
                        sbyte closestIndex = 0;
                        for (sbyte i2 = 0; i2 < collumPlusZ.count; i2++) {
                            Data value2 = data[collumPlusZ.index + i2];
                            float curStep = Mathf.Abs(value1.y - value2.y);
                            if (curStep < closestStep) {
                                closestStep = curStep;
                                closestIndex = i2;
                            }
                        }
                        if (closestStep <= connectionDistance) {
                            data[collum.index + i].zPlus = closestIndex;
                            data[collumPlusZ.index + closestIndex].zMinus = i;
                            //Vector3 p1 = GetPos(x, z, data[collum.index + i].y);
                            //Vector3 p2 = GetPos(x, z + 1, data[collumPlusZ.index + closestIndex].y);
                            //Debuger_K.AddLine(p1, p2);
                        }
                    }
                }
            }


            //debug
            //for (int x = 0; x < sizeX; x++) {
            //    for (int z = 0; z < sizeZ; z++) {
            //        var collum = collums[GetIndex(x, z)];

            //        Vector3 p0 = GetPos(x, z, 0);
            //        //Debuger_K.AddLabel(p0, collum.count);

            //        for (int i = 0; i < collum.count; i++) {
            //            var value = data[collum.index + i];

            //            Vector3 p1 = GetPos(x, z, value.y);
            //            //Debuger_K.AddLabel(p1, i);
            //            if (value.xPlus != -1) {
            //                var connection = data[collums[GetIndex(x + 1, z)].index + value.xPlus];
            //                Vector3 p2 = GetPos(x + 1, z, connection.y);
            //                Debuger_K.AddLine(p1, p2, Color.red);
            //            }

            //            if (value.xMinus != -1) {
            //                var connection = data[collums[GetIndex(x - 1, z)].index + value.xMinus];
            //                Vector3 p2 = GetPos(x - 1, z, connection.y);
            //                Debuger_K.AddLine(p1, p2, Color.blue, 0.015f);
            //            }

            //            if (value.zPlus != -1) {
            //                var connection = data[collums[((z + 1) * sizeX) + x].index + value.zPlus];
            //                Vector3 p2 = GetPos(x, z + 1, connection.y);
            //                Debuger_K.AddLine(p1, p2, Color.magenta, 0.03f);
            //            }

            //            if (value.zMinus != -1) {
            //                var connection = data[collums[GetIndex(x, z - 1)].index + value.zMinus];
            //                Vector3 p2 = GetPos(x, z - 1, connection.y);
            //                Debuger_K.AddLine(p1, p2, Color.cyan, 0.045f);
            //            }
            //        }
            //    }
            //}
        }

        private void CreateNearObstaclesSet() {
            //near obstacles
            nearObstacleSet = new HashSet<DataPos_DirectIndex>();


            if (doCrouch) {
                nearCrouchSet = new HashSet<DataPos_DirectIndex>();
                sbyte crouchVal = (sbyte)Passability.Crouchable;
                sbyte walkVal = (sbyte)Passability.Walkable;

                for (int x = 1; x < sizeX - 1; x++) {
                    for (int z = 1; z < sizeZ - 1; z++) {
                        var collum = collums[GetIndex(x, z)];

                        for (int i = 0; i < collum.count; i++) {
                            var value = data[collum.index + i];    

                            if(value.pass == walkVal) {
                                if(value.xMinus != Data.INVALID_CONNECTION && data[GetRealIndex(x - 1, z, value.xMinus)].pass == crouchVal)
                                    nearCrouchSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                                if (value.xPlus != Data.INVALID_CONNECTION && data[GetRealIndex(x + 1, z, value.xPlus)].pass == crouchVal)
                                    nearCrouchSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                                if (value.zMinus != Data.INVALID_CONNECTION && data[GetRealIndex(x, z - 1, value.zMinus)].pass == crouchVal)
                                    nearCrouchSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                                if (value.zPlus != Data.INVALID_CONNECTION && data[GetRealIndex(x, z + 1, value.zPlus)].pass == crouchVal)
                                    nearCrouchSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                            }

                            if (value.xMinus == Data.INVALID_CONNECTION | 
                                value.xPlus == Data.INVALID_CONNECTION | 
                                value.zMinus == Data.INVALID_CONNECTION | 
                                value.zPlus == Data.INVALID_CONNECTION)
                                nearObstacleSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                        }
                    }
                }
            }
            else {
                for (int x = 0; x < sizeX; x++) {
                    for (int z = 0; z < sizeZ; z++) {
                        var collum = collums[GetIndex(x, z)];

                        for (int i = 0; i < collum.count; i++) {
                            var value = data[collum.index + i];
                            if (value.xMinus == Data.INVALID_CONNECTION | 
                                value.xPlus == Data.INVALID_CONNECTION | 
                                value.zMinus == Data.INVALID_CONNECTION | 
                                value.zPlus == Data.INVALID_CONNECTION)
                                nearObstacleSet.Add(new DataPos_DirectIndex(x, z, collum.index + i));
                        }
                    }
                }
            }

            //foreach (var pos in nearObstacleSet) {
            //    var value = data[pos.index];
            //    Vector3 p1 = realChunkPos + offset + new Vector3((pos.x * template.voxelSize), value.y, (pos.z * template.voxelSize));
            //    Debuger_K.AddRay(p1, Vector3.up, Color.red, 0.2f);
            //}

            //foreach (var pos in nearCrouchSet) {
            //    var value = data[pos.index];
            //    Vector3 p1 = realChunkPos + offset + new Vector3((pos.x * template.voxelSize), value.y, (pos.z * template.voxelSize));
            //    Debuger_K.AddRay(p1, Vector3.up, Color.cyan, 0.2f);
            //}
        }

        private int GetRealIndex(int x, int z, int layer) {
            return collums[(z * sizeX) + x].index + layer;
        }

        public Vector3 GetPos(int x, int z, float y) {
            return realChunkPos + offset + new Vector3((x * template.voxelSize), y, (z * template.voxelSize));
        }

        private Vector3 GetPos(DataPos dp) {
            return realChunkPos + offset + new Vector3((dp.x * template.voxelSize), data[collums[GetIndex(dp.x, dp.z)].index + dp.layer].y, (dp.z * template.voxelSize));
        }

        private Data GetData(DataPos pos) {
            return data[collums[(pos.z * sizeX) + pos.x].index + pos.layer];
        }

        private Data GetData(int x, int z, int layer) {
            return data[collums[(z * sizeX) + x].index + layer];
        }

        //this ugly function are shift voxels in growth pattern from obstacles and crouch areas
        private void GrowthObstacles(
            HashSet<DataPos_DirectIndex> origins,
            float sqrDistance,
            System.Func<DataPos_DirectIndex, bool> positive,
            System.Func<DataPos_DirectIndex, bool> growTo,
            System.Action<DataPos_DirectIndex> grow,
            NavMeshTemplateCreation template) {

            Dictionary<DataPos_DirectIndex, DataPos_DirectIndex> originsDistionary = new Dictionary<DataPos_DirectIndex, DataPos_DirectIndex>();//position of value, position of origin
            HashSet<DataPos_DirectIndex> lastIteration = origins;

            foreach (var item in origins) {
                originsDistionary[item] = item;
                grow(item);
            }

            Dictionary<DataPos_DirectIndex, HashSet<DataPos_DirectIndex>> borderDictionary = new Dictionary<DataPos_DirectIndex, HashSet<DataPos_DirectIndex>>();

            while (true) {
                foreach (var lastIterationPos in lastIteration) {
                    var dataValue = data[lastIterationPos.index];

                    for (int i = 0; i < 4; i++) {
                        int conn = dataValue.GetConnection((Directions)i);
                        if (conn != -1) {                       
                            DataPos_DirectIndex value;
                            switch ((Directions)i) {
                                case Directions.xPlus:
                                    conn = collums[GetIndex(lastIterationPos.x + 1, lastIterationPos.z)].index + conn;
                                    value = new DataPos_DirectIndex(lastIterationPos.x + 1, lastIterationPos.z, conn);
                                    break;
                                case Directions.xMinus:
                                    conn = collums[GetIndex(lastIterationPos.x - 1, lastIterationPos.z)].index + conn;
                                    value = new DataPos_DirectIndex(lastIterationPos.x - 1, lastIterationPos.z, conn);
                                    break;
                                case Directions.zPlus:
                                    conn = collums[GetIndex(lastIterationPos.x, lastIterationPos.z + 1)].index + conn;
                                    value = new DataPos_DirectIndex(lastIterationPos.x, lastIterationPos.z + 1, conn);
                                    break;
                                case Directions.zMinus:
                                    conn = collums[GetIndex(lastIterationPos.x, lastIterationPos.z - 1)].index + conn;
                                    value = new DataPos_DirectIndex(lastIterationPos.x, lastIterationPos.z - 1, conn);
                                    break;
                                default:
                                    value = new DataPos_DirectIndex(lastIterationPos.x, lastIterationPos.z, -1);
                                    break;
                            }

                            if (positive(value) | growTo(value) == false)
                                continue;

                            if (borderDictionary.ContainsKey(value) == false)
                                borderDictionary.Add(value, new HashSet<DataPos_DirectIndex>());

                            borderDictionary[value].Add(originsDistionary[lastIterationPos]);
                        }
                    }
                }


                HashSet<DataPos_DirectIndex> newIteration = new HashSet<DataPos_DirectIndex>();

                foreach (var curPoint in borderDictionary) {
                    DataPos_DirectIndex? closest = null;
                    int dist = int.MaxValue;

                    foreach (var root in curPoint.Value) {
                        int curDist = SomeMath.SqrDistance(curPoint.Key.x, curPoint.Key.z, root.x, root.z);
                        if (curDist < sqrDistance & curDist < dist) {
                            dist = curDist;
                            closest = root;
                        }
                    }
                    if (closest.HasValue) {
                        newIteration.Add(curPoint.Key);
                        originsDistionary.Add(curPoint.Key, closest.Value);
                        grow(curPoint.Key);
                        //Vector3 curP = GetRealMax(curPoint.Key);
                        //Vector3 closestP = GetRealMax(closest.Value);
                        //Debuger_K.AddLine(curP, closestP, Color.cyan);
                    }
                }

                if (newIteration.Count == 0)
                    break;

                lastIteration = newIteration;
                borderDictionary.Clear();
            }
        }





        /// some more spaghetti code. used to rate fragments from 0 to 2 in cover capability
        /// 0 is no cover, 1 is half cover, 2 is full cover. goto used to exit nested loops
        private void GenerateCovers(int sampleDistance) {
            sampleDistance = Mathf.Max(2, sampleDistance);

            var pattern = CirclePatternPool.GetPattern(sampleDistance);
            int patternSize = pattern.size;
            int patternRadius = pattern.radius - 1;
            bool[] patternGrid = pattern.pattern;
            
            int minIndex = template.extraOffset;
            int maxIndexX = template.lengthX_extra - template.extraOffset;
            int maxIndexZ = template.lengthZ_extra - template.extraOffset;

            //generate initial map

            int extra = template.extraOffset;
            sbyte minPass = (sbyte)Passability.Crouchable;
            coverData = new DataCover[data.Length];
            for (int x = extra; x < sizeX - extra; x++) {
                for (int z = extra; z < sizeZ - extra; z++) {
                    var collum = collums[(z * sizeX) + x];
                    for (int i2 = 0; i2 < collum.count; i2++) {
                        coverData[collum.index + i2].hash =
                            data[collum.index + i2].pass >= minPass ?
                            AreaPassabilityHashData.COVER_HASH :
                            AreaPassabilityHashData.INVALID_COVER_HASH;
                    }
                }
            }

            //iterating through near obstacle set 
            foreach (var item in nearObstacleSet) {
                if (item.x < minIndex || item.z < minIndex || item.x > maxIndexX || item.z > maxIndexZ)
                    continue;
                
                coverData[item.index].hash = AreaPassabilityHashData.INVALID_COVER_HASH;

                float baseHeight = data[item.index].y;
                sbyte cover = 0;

                //float maxDif = 0f;

                for (int x_pattern = 0; x_pattern < patternSize; x_pattern++) {
                    for (int y_pattern = 0; y_pattern < patternSize; y_pattern++) {
                        if (patternGrid[(y_pattern * patternSize) + x_pattern]) {
                            int checkX = item.x - patternRadius + x_pattern;

                            if (checkX < 0 || checkX >= sizeX)
                                continue;

                            int checkZ = item.z - patternRadius + y_pattern;

                            if (checkZ < 0 || checkZ >= sizeZ)
                                continue;

                            int checkIndex = GetIndex(checkX, checkZ);

                            #region debug
                            //Debuger_K.AddLine(
                            //    GetPos(item.x, item.z, data[item.index].y), 
                            //    GetPos(checkX, checkZ, data[item.index].y), 
                            //    Color.red);
                            #endregion

                            var curValue = arrayData[checkIndex];

                            if (curValue.next != -2) {
                                int curIndex = checkIndex;
                                while (true) {
                                    curValue = arrayData[curIndex];

                                    if(curValue.min <= baseHeight) {
                                        float dif = curValue.max - baseHeight;

                                        //if (dif > maxDif)
                                        //    maxDif = dif;

                                        if (doHalfCover && dif > halfCover) {
                                            if (cover < 1)
                                                cover = 1;
                                        }

                                        if (dif > fullCover) {
                                            cover = 2;
                                            break;
                                        }
                                    }

                                    curIndex = arrayData[curIndex].next;
                                    if (curIndex == -1)
                                        break;
                                }
                            }
                        }

                        if (cover == 2) break;
                    }

                    if (cover == 2) break;
                }

                //float dLength;
                //switch (cover) {
                //    case 1:
                //        dLength = halfCover;
                //        break;
                //    case 2:
                //        dLength = fullCover;
                //        break;
                //    default:
                //        dLength = 0f;
                //        break;
                //}

                coverData[item.index].coverHeight = cover;
                //Debuger_K.AddLabel(GetPos(item.x, item.z, baseHeight), maxDif);
                //Debuger_K.AddRay(GetPos(item.x, item.z, baseMax), Vector3.up, Color.magenta, dLength);

                //break;
            }
        }



        // most of this code are about proper alighment in world
        // it's calculate all grid shifts in world space and then shift this grid to proper distance
        // (actualy code is far from optimal but it's not that bad so i never touch it)
        private void BattleGrid() {
            int density = template.battleGridDensity;
            var chunkPos = template.gridPosition;

            int fPosX = template.lengthX_central * chunkPos.x;
            int fPosZ = template.lengthZ_central * chunkPos.z;

            int lastGridLeftX = fPosX - (fPosX / template.battleGridDensity * template.battleGridDensity);
            int lastGridLeftZ = fPosZ - (fPosZ / template.battleGridDensity * template.battleGridDensity);

            int lastChunkLeftX = lastGridLeftX == 0 ? 0 : template.battleGridDensity - lastGridLeftX;
            int lastChunkLeftZ = lastGridLeftZ == 0 ? 0 : template.battleGridDensity - lastGridLeftZ;

            if (lastChunkLeftX > template.battleGridDensity) //negative chunk position
                lastChunkLeftX -= template.battleGridDensity;

            if (lastChunkLeftZ > template.battleGridDensity) //negative chunk position
                lastChunkLeftZ -= template.battleGridDensity;

            int lengthX = ((template.lengthX_central - lastChunkLeftX - 1) / template.battleGridDensity) + 1;
            int lengthZ = ((template.lengthZ_central - lastChunkLeftZ - 1) / template.battleGridDensity) + 1;

            int offsetX = template.extraOffset + lastChunkLeftX;
            int offsetZ = template.extraOffset + lastChunkLeftZ;

            Dictionary<DataPos, DataPos?[]> gridDic = new Dictionary<DataPos, DataPos?[]>();
            Dictionary<DataPos, BattleGridPoint> bgpDic = new Dictionary<DataPos, BattleGridPoint>();

            for (int x = 0; x < lengthX; x++) {
                for (int z = 0; z < lengthZ; z++) {
                    int curTargetX = offsetX + (x * density);
                    int curTargetZ = offsetZ + (z * density);
                    var collum = collums[GetIndex(curTargetX, curTargetZ)];

                    for (int i = 0; i < collum.count; i++) {
                        var curData = data[collum.index + i];
                        if(curData.pass >= (sbyte)Passability.Crouchable) {
                            DataPos curPos = new DataPos(curTargetX, curTargetZ, i);

                            gridDic.Add(curPos, new DataPos?[4]);
                            bgpDic.Add(curPos, new BattleGridPoint(GetPos(curTargetX, curTargetZ, curData.y), (Passability)curData.pass, new VectorInt.Vector2Int(x, z)));

                            //Debuger_K.AddDot(bgpDic[curPos].positionV3 + new Vector3(0, 0.2f, 0), Color.yellow);
                            //Debuger_K.AddLabel(bgpDic[curPos].positionV3 + new Vector3(0, 0.2f, 0), curPos);
                        }
                    }
                }
            }

            //x
            for (int x = 0; x < lengthX - 1; x++) {
                for (int z = 0; z < lengthZ; z++) {
                    int curTargetX = offsetX + (x * density);
                    int curTargetZ = offsetZ + (z * density);
                    var collum = collums[GetIndex(curTargetX, curTargetZ)];

                    for (int i = 0; i < collum.count; i++) {
                        var curData = data[collum.index + i];
                        if (curData.pass >= (sbyte)Passability.Crouchable) {
                            DataPos curPos = new DataPos(curTargetX, curTargetZ, i);
                            //Vector3 p1 = GetPos(curPos);

                            DataPos curChangedPos = curPos;
                            var curValue = GetData(curChangedPos);

                            bool isValid = true;
                            for (int i1 = 0; i1 < density; i1++) {
                                curChangedPos.x += 1;
                                curChangedPos.layer = curValue.xPlus;
                                curValue = GetData(curChangedPos);
                                if(curValue.xPlus == Data.INVALID_CONNECTION || curValue.pass < (sbyte)Passability.Crouchable) {
                                    isValid = false;
                                    break;
                                }
                                //Vector3 p2 = GetPos(curChangedPos);
                                //Debuger_K.AddLine(p1, p2 + new Vector3(0, 0.1f * i1, 0), Color.yellow);
                            }

                            if (isValid) {
                                //Vector3 p2 = GetPos(curChangedPos);
                                //Debuger_K.AddLine(p1, p2, Color.yellow, 0.2f);
                                gridDic[curPos][(int)Directions.xPlus] = curChangedPos;
                                gridDic[curChangedPos][(int)Directions.xMinus] = curPos;
                            }
                        }
                    }
                }
            }


            //z
            for (int x = 0; x < lengthX; x++) {
                for (int z = 0; z < lengthZ - 1; z++) {
                    int curTargetX = offsetX + (x * density);
                    int curTargetZ = offsetZ + (z * density);
                    var collum = collums[GetIndex(curTargetX, curTargetZ)];

                    for (int i = 0; i < collum.count; i++) {
                        var curData = data[collum.index + i];
                        if (curData.pass >= (sbyte)Passability.Crouchable) {
                            DataPos curPos = new DataPos(curTargetX, curTargetZ, i);
                            //Vector3 p1 = GetPos(curPos);
                            DataPos curChangedPos = curPos;
                            var curValue = GetData(curChangedPos);

                            bool isValid = true;
                            for (int i1 = 0; i1 < density; i1++) {
                                curChangedPos.z += 1;
                                curChangedPos.layer = curValue.zPlus;
                                curValue = GetData(curChangedPos);

                                if (curValue.zPlus == Data.INVALID_CONNECTION || curValue.pass < (sbyte)Passability.Crouchable) {
                                    isValid = false;
                                    break;
                                }
                                //Vector3 p2 = GetPos(curChangedPos);
                                //Debuger_K.AddLine(p1, p2 + new Vector3(0, 0.1f * i1, 0), Color.yellow);
                            }


                            if (isValid) {
                                //Vector3 p2 = GetPos(curChangedPos);
                                //Debuger_K.AddLine(p1, p2, Color.yellow, 0.2f);
                                gridDic[curPos][(int)Directions.zPlus] = curChangedPos;
                                gridDic[curChangedPos][(int)Directions.zMinus] = curPos;

                            }
                        }
                    }
                }
            }


            //transfer connections
            foreach (var fragKeyValue in gridDic) {
                var bgp = bgpDic[fragKeyValue.Key];
                var ar = fragKeyValue.Value;

                for (int i = 0; i < 4; i++) {
                    if (ar[i].HasValue)
                        bgp.neighbours[i] = bgpDic[ar[i].Value];
                }
            }

            battleGrid = new BattleGrid(lengthX, lengthZ, bgpDic.Values);

            //foreach (var item in battleGrid.points) {
            //    Debuger_K.AddDot(item.positionV3 + new Vector3(0, 0.2f, 0), Color.yellow);

            //    foreach (var nb in item.neighbours) {
            //        if(nb != null)
            //            Debuger_K.AddLine(item.positionV3, nb.positionV3, Color.yellow, 0.2f);
            //    }
            //}

//#if UNITY_EDITOR
//            //debug battle grid
//            if (Debuger_K.doDebug) {
//                //since it's just bunch of lines i transfer it as list of vector 3
//                List<Vector3> debugList = new List<Vector3>();
//                foreach (var fragKeyValue in gridDic) {
//                    var f = fragKeyValue.Key;
//                    var v = fragKeyValue.Value;

//                    for (int i = 0; i < 4; i++) {
//                        if (v[i].HasValue) {
//                            debugList.Add(bgpDic[f].positionV3);
//                            debugList.Add(bgpDic[v[i].Value].positionV3);
//                        }
//                    }
//                }
//                Debuger3.AddBattleGridConnection(template.chunk, template.properties, debugList);
//            }
//#endif
        }


        public VolumeArea CaptureArea(DataPos_DirectIndex targetPos, AreaType areaType, int sqrInnerAreaRange, VoxelState innerAreaFlag, int sqrOuterAreaRange, VoxelState outerAreaFlag, bool addOuterAreaFlag, bool debug = false) {
            var dataTarget = data[targetPos.index];

            VolumeArea areaObject = new VolumeArea(GetPos(targetPos.x, targetPos.z, dataTarget.y), areaType);

            
            HashSet<DataPos> lastAreaIteration = new HashSet<DataPos>();

            for (int i = 0; i < 4; i++) {
                int neighbourLayer = dataTarget.GetConnection((Directions)i);
                if (neighbourLayer > -1) {
                    switch ((Directions)i) {
                        case Directions.xPlus:
                            lastAreaIteration.Add(new DataPos(targetPos.x + 1, targetPos.z, neighbourLayer));
                            break;
                        case Directions.xMinus:
                            lastAreaIteration.Add(new DataPos(targetPos.x - 1, targetPos.z, neighbourLayer));
                            break;
                        case Directions.zPlus:
                            lastAreaIteration.Add(new DataPos(targetPos.x, targetPos.z + 1, neighbourLayer));
                            break;
                        case Directions.zMinus:
                            lastAreaIteration.Add(new DataPos(targetPos.x, targetPos.z - 1, neighbourLayer));
                            break;
                    }
                }
            }
            HashSet<DataPos> area = new HashSet<DataPos>(lastAreaIteration);
            HashSet<DataPos> doubleArea = new HashSet<DataPos>(lastAreaIteration);

            while (true) {
                if (lastAreaIteration.Count == 0)
                    break;

                HashSet<DataPos> newAxisIteration = new HashSet<DataPos>();

                foreach (var lastIterationPos in lastAreaIteration) {
                    //captured only further positions
                    int curDistance = SomeMath.SqrDistance(targetPos.x, targetPos.z, lastIterationPos.x, lastIterationPos.z);

                    var curData = data[collums[GetIndex(lastIterationPos.x, lastIterationPos.z)].index + lastIterationPos.layer];

                    for (int i = 0; i < 4; i++) {
                        int neighbourLayer = curData.GetConnection((Directions)i);

                        if (neighbourLayer > -1) {
                            DataPos neighbourPos = lastIterationPos;

                            switch ((Directions)i) {
                                case Directions.xPlus:
                                    neighbourPos.x += 1;
                                    break;
                                case Directions.xMinus:
                                    neighbourPos.x -= 1;
                                    break;
                                case Directions.zPlus:
                                    neighbourPos.z += 1;
                                    break;
                                case Directions.zMinus:
                                    neighbourPos.z -= 1;
                                    break;
                            }

                            neighbourPos.layer = neighbourLayer;

                            int neighbourDistance = SomeMath.SqrDistance(targetPos.x, targetPos.z, neighbourPos.x, neighbourPos.z);

                            if (neighbourDistance >= curDistance && neighbourDistance <= sqrOuterAreaRange && doubleArea.Add(neighbourPos)) {
                                newAxisIteration.Add(neighbourPos);
                                if (neighbourDistance <= sqrInnerAreaRange)
                                    area.Add(neighbourPos);
                            }
                        }
                    }
                }
                lastAreaIteration = newAxisIteration;
            }

            foreach (var pos in area) {
                int index = collums[GetIndex(pos.x, pos.z)].index + pos.layer;
                areaSet.Add(index, areaObject);
                data[index].SetState(innerAreaFlag, true);              
            }

            if (addOuterAreaFlag) {
                foreach (var pos in doubleArea) {
                    data[collums[GetIndex(pos.x, pos.z)].index + pos.layer].SetState(outerAreaFlag, true);
                }
            }

#if UNITY_EDITOR
            if (debug) {
                foreach (var item in area) {
                    Debuger_K.AddLine(areaObject.position, GetPos(item), Color.blue);
                }

                foreach (var item in doubleArea) {
                    Debuger_K.AddLine(areaObject.position, GetPos(item), Color.cyan, 0.1f);
                }

                //Debuger_K.AddLabel(basePosReal, string.Format("type: {0}\ncount: {1}\nsqr area: {2}\nsqr offset: {3}",
                //    areaType,
                //    area.Count,
                //    sqrArea, sqrOffset));
            }
#endif


            volumeAreas.Add(areaObject);
            return areaObject;
        }





        public bool GetClosestPos(Vector3 pos, out DataPos_DirectIndex closestPos) {
            closestPos = new DataPos_DirectIndex();
            float fragmentSize = template.voxelSize;
            Vector3 ajustedPos = pos - template.realOffsetedPosition;
            int x = Mathf.RoundToInt((ajustedPos.x - (fragmentSize * 0.5f)) / fragmentSize);
            int z = Mathf.RoundToInt((ajustedPos.z - (fragmentSize * 0.5f)) / fragmentSize);

            float curDist = float.MaxValue;       
            bool posDefined = false;
            
            VectorInt.Vector2Int[] vectorArray = GenericPoolArray<VectorInt.Vector2Int>.Take(9);
            vectorArray[0] = new VectorInt.Vector2Int(x - 1, z - 1);
            vectorArray[1] = new VectorInt.Vector2Int(x - 1, z);
            vectorArray[2] = new VectorInt.Vector2Int(x - 1, z + 1);
            vectorArray[3] = new VectorInt.Vector2Int(x, z - 1);
            vectorArray[4] = new VectorInt.Vector2Int(x, z);
            vectorArray[5] = new VectorInt.Vector2Int(x, z + 1);
            vectorArray[6] = new VectorInt.Vector2Int(x + 1, z - 1);
            vectorArray[7] = new VectorInt.Vector2Int(x + 1, z);
            vectorArray[8] = new VectorInt.Vector2Int(x + 1, z + 1);


            for (int vec = 0; vec < 9; vec++) {
                VectorInt.Vector2Int vector = vectorArray[vec];
                DataCollum collum = collums[GetIndex(vector.x, vector.y)];

                for (int i = 0; i < collum.count; i++) {
                    Vector3 tPos = GetPos(vector.x, vector.y, data[collum.index + i].y);
                    float dist = SomeMath.SqrDistance(tPos, pos);
                    if (dist < curDist) {
                        posDefined = true;
                        curDist = dist;
                        closestPos = new DataPos_DirectIndex(vector.x, vector.y, collum.index + i);
                    }
                }
            }

            GenericPoolArray<VectorInt.Vector2Int>.ReturnToPool(ref vectorArray);
            return posDefined;
        }

    

    }



    ///// <summary>
    ///// class for storing volume area
    ///// it stored position, it added to volume as reference and extracted due marching sqares iterate throu volume. 
    ///// then short edges also stored reference to that. and finaly after ramer douglas peuker algorithm colume area gets final set of edges it belong
    ///// </summary>
    //public class VolumeArea {
    //    public Vector3 position;
    //    public AreaType areaType;//cause there may be some types of area. jump points and cover points right now

    //    private HashSet<EdgeAbstract> _edges = new HashSet<EdgeAbstract>();

    //    public VolumeArea(Vector3 position, AreaType areaType) {
    //        this.position = position;
    //        this.areaType = areaType;
    //    }

    //    //add edges
    //    public void AddEdge(EdgeAbstract edge) {
    //        _edges.Add(edge);
    //    }
    //    public void AddEdge(IEnumerable<EdgeAbstract> edges) {
    //        foreach (var item in edges) {
    //            AddEdge(item);
    //        }
    //    }
    //    public void AddEdge(IEnumerable<EdgeTemp> edges) {
    //        foreach (var item in edges) {
    //            AddEdge(item);
    //        }
    //    }

    //    //acessor
    //    public IEnumerable<EdgeAbstract> edges {
    //        get { return _edges; }
    //    }
    //}
}