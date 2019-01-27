using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using K_PathFinder.VectorInt;
using K_PathFinder.Settings;
using K_PathFinder.Graphs;
using K_PathFinder.PathGeneration;
using System.Text;
using K_PathFinder.Serialization;
using K_PathFinder.EdgesNameSpace;
using System.Linq;
using K_PathFinder.PFDebuger;
using System;

namespace K_PathFinder {
    /// <summary>
    /// class to store referencess. cause otherwise it cant be threaded.
    /// and it cant be threaded cause line drawing thing are have delegate as input. so all temporary data stored here and also passed to line drawing delegate
    /// </summary>
    public class RaycastAllocatedData {
        //reference types
        public bool[] raycastSamples = new bool[4];//which chunk sides should be cheked
        public CellDataMapValue[][][] dataMap;//current chunk map
        public CellDataMapValue[] dataArray;//current pixel array
        public Cell curCell, prevCell;//cells
        public Area expArea;//expected area

        //value types
        public Passability expPass;//expected passability
        public bool 
            raycastDone, //main flag in case something go wrong
            checkPass,   //do check passability chages
            checkArea;   //do check area changes
        public NavmeshRaycastResultType2 raycastType; //current raycast result
        public ChunkData currentChunkData; //currrent struct that represent chunk
        public float
            raycastResultX, raycastResultY, raycastResultZ, //result
            posX, posY, posZ,  //start
            rayDirX, rayDirY,  //ray direction
            chunkPixelSize,    //chunk pixel size
            maxSqrLength;      //maximum length

        public int 
            startIntX, startIntY, //start chunk position * 10 + start pixel
            curChunkIntX, curChunkIntY, //current chunk position * 10
            gridDistanceTreshold; //what sqr distance is too large
    }

    public static class PathFinderMainRaycasting {
        //private const bool DEBUG = false;
        private static CellContentData[] raycastSamplesTemplate = new CellContentData[4];

        private static Queue<RaycastAllocatedData> pool = new Queue<RaycastAllocatedData>();
        private static object poolLock = new object();

        //called externaly cause PathFinder.gridSize can be zero in that case
        public static void Init() {
            Vector2 A = new Vector2(0, 0);
            Vector2 B = new Vector2(PathFinder.gridSize, 0);
            Vector2 C = new Vector2(0, PathFinder.gridSize);
            Vector2 D = new Vector2(PathFinder.gridSize, PathFinder.gridSize);
            raycastSamplesTemplate[0] = new CellContentData(C, A);
            raycastSamplesTemplate[1] = new CellContentData(D, C);
            raycastSamplesTemplate[2] = new CellContentData(B, D);
            raycastSamplesTemplate[3] = new CellContentData(A, B);
        }

        public static void Raycast2Body2(float posX, float posY, float posZ, float rayDirX, float rayDirY, Cell cell,
          float maxLength, bool checkArea, bool checkPass, Area expArea, Passability expPass, 
          RaycastAllocatedData rad, out RaycastHitNavMesh2 hit) {
            if (SomeMath.SqrMagnitude(rayDirX, rayDirY) == 0f) {
                hit = new RaycastHitNavMesh2(posX, posY, posZ, NavmeshRaycastResultType2.ReachMaxDistance, cell);
                return;
            }

            rad.chunkPixelSize = PathFinder.gridSize / PathFinder.CELL_GRID_SIZE;

            //trick to fix case when raycast start on "near" edge
            //currently can't be on chunk edge so we dont care if chunk changed
            rad.currentChunkData = cell.graph.chunk;

            int curGridX = (int)((posX - rad.currentChunkData.realX) / rad.chunkPixelSize);
            int curGridY = (int)((posZ - rad.currentChunkData.realZ) / rad.chunkPixelSize);

            //if (curGridX < 0) curGridX = 0; else if (curGridX > 9) curGridX = 9; 
            //if (curGridY < 0) curGridY = 0; else if (curGridY > 9) curGridY = 9;

            rad.startIntX = (rad.currentChunkData.x * 10) + curGridX;
            rad.startIntY = (rad.currentChunkData.z * 10) + curGridY;

            var tempVal = maxLength / rad.chunkPixelSize;
            if(tempVal > 10000) {//too big number anyway
                rad.gridDistanceTreshold = SomeMath.Sqr(10000);
            }
            else {
                rad.gridDistanceTreshold = (int)SomeMath.Sqr(maxLength / rad.chunkPixelSize) + 1;
            }
 
            rad.dataArray = cell.graph.dataMap[curGridX][curGridY];

            if (rad.dataArray != null) {
                for (int i = 0; i < rad.dataArray.Length; i++) {
                    CellDataMapValue mapData = rad.dataArray[i];

                    if (mapData.from != cell)
                        continue;

                    if (mapData.data.RotateRightAndReturnDot(rayDirX, rayDirY) < 0) 
                        continue;                    

                    float sqrDist = SomeMath.SqrDistance(mapData.data.NearestPointXZ(posX, posZ), new Vector2(posX, posZ));

                    if (sqrDist < 0.0001f) {
                        Vector2 dirToCellCenter = (cell.centerVector2 - new Vector2(posX, posZ)).normalized * 0.001f;
                        posX += dirToCellCenter.x;
                        posZ += dirToCellCenter.y;
                        //if (dot < 0.001f) {//oh wow. start point exactly on edge and ray alond side
                        //}
                        break;
                    }
                }
            }


            //if (DEBUG) {
            //    Debuger_K.AddRay(new Vector3(posX, posY + 0.1f, posZ), new Vector3(rayDirX, 0, rayDirY), Color.gray);
            //}

            rad.posX = posX;
            rad.posY = posY;
            rad.posZ = posZ;
            rad.rayDirX = rayDirX;
            rad.rayDirY = rayDirY;
            rad.checkPass = checkPass;
            rad.checkArea = checkArea;
            rad.expPass = expPass;
            rad.expArea = expArea;
            rad.raycastType = NavmeshRaycastResultType2.Nothing;
            rad.maxSqrLength = SomeMath.Sqr(maxLength);
            rad.curCell = cell;
            rad.prevCell = null;
            rad.raycastDone = false;

            float
                chunkX,
                chunkZ,
                curHullX,
                curHullZ,
                lastHullX = posX,
                lastHullZ = posZ;

            for (int i = 0; i < 4; i++) {
                rad.raycastSamples[i] = raycastSamplesTemplate[i].RotateRightAndReturnDot(rayDirX, rayDirY) < 0;
            }

            int chunkIteration = 0;
            while (rad.raycastDone == false) {
                chunkIteration++;
                if (chunkIteration > 50) {
                    string s = string.Format("chunkIteration too large. x {0}, y {1}, z {2}, dx {3}, dy {4}, max {5}", posX, posY, posZ, rayDirX, rayDirY, maxLength);
                    //HandleTextFile.WriteString(s);
                    //Debuger_K.AddRay(new Vector3(posX, posY, posZ), Vector3.down, Color.cyan);
                    //Debuger_K.AddRay(new Vector3(posX, posY, posZ), new Vector3(rayDirX, 0, rayDirY), Color.yellow, 50);
                    //Debuger_K.UserfulPublicFlag = true;
                    Debug.LogError(s);
                    break;
                }

                rad.currentChunkData = rad.curCell.graph.chunk;
                rad.curChunkIntX = rad.currentChunkData.x * 10;
                rad.curChunkIntY = rad.currentChunkData.z * 10;
                rad.dataMap = rad.curCell.graph.dataMap;

                chunkX = rad.currentChunkData.realX;
                chunkZ = rad.currentChunkData.realZ;

                #region border points   
                curHullX = posX;
                curHullZ = posZ;
                for (int i = 0; i < 4; i++) {
                    if (rad.raycastSamples[i]) {
                        CellContentData curSide = raycastSamplesTemplate[i];
                        float rX, rZ;

                        if (SomeMath.RayIntersectSegment(posX, posZ, rayDirX, rayDirY, curSide.xLeft + chunkX, curSide.zLeft + chunkZ, curSide.xRight + chunkX, curSide.zRight + chunkZ, out rX, out rZ)) {
                            curHullX = rX;
                            curHullZ = rZ;
                        }
                        //if (DEBUG)
                        //    Debuger_K.AddLine(curSide.a, curSide.b, Color.red, chunkIteration);
                    }
                }

                #region debug
                //if (DEBUG) {
                //    Debuger_K.AddLine(new Vector3(curHullX, 0, curHullZ), new Vector3(lastHullX, 0, lastHullZ), Color.yellow, chunkIteration);

                //    for (int x = 0; x < PathFinder.CELL_GRID_SIZE + 1; x++) {
                //        Debuger_K.AddLine(
                //            currentChunkData.realPositionV3 + new Vector3(x * chunkPixelSize, 0, 0),
                //            currentChunkData.realPositionV3 + new Vector3(x * chunkPixelSize, 0, PathFinder.gridSize),
                //            Color.red);
                //    }
                //    for (int z = 0; z < PathFinder.CELL_GRID_SIZE + 1; z++) {
                //        Debuger_K.AddLine(
                //            currentChunkData.realPositionV3 + new Vector3(0, 0, z * chunkPixelSize),
                //            currentChunkData.realPositionV3 + new Vector3(PathFinder.gridSize, 0, z * chunkPixelSize),
                //            Color.red);
                //    }
                //}
                #endregion

                #endregion

                DDARasterization.DrawLine(
                    lastHullX - chunkX,
                    lastHullZ - chunkZ,
                    curHullX - chunkX,
                    curHullZ - chunkZ,
                    rad.chunkPixelSize,
                    rad,
                    RaycastDelegate);

                lastHullX = curHullX;
                lastHullZ = curHullZ;
            }

            hit = new RaycastHitNavMesh2(rad.raycastResultX, rad.raycastResultY, rad.raycastResultZ, rad.raycastType, rad.curCell);
        }

        private static bool RaycastDelegate(int x, int y, RaycastAllocatedData rad) {
            if (rad.raycastDone)
                return true;       
       
            if (x < 0) x = 0; else if (x > 9) x = 9; //x = SomeMath.Clamp(0, CELL_GRID_SIZE - 1, x);
            if (y < 0) y = 0; else if (y > 9) y = 9; //y = SomeMath.Clamp(0, CELL_GRID_SIZE - 1, y);
            if (SomeMath.SqrDistance(rad.startIntX, rad.startIntY, rad.curChunkIntX + x, rad.curChunkIntY + y) > rad.gridDistanceTreshold) {
                rad.raycastType = NavmeshRaycastResultType2.ReachMaxDistance;
                rad.raycastDone = true;
            }
            //IMPORTANT: edges in this list are sorted. "connection != null" at the begining and "connection == null" at the end.
            //some logic here based on this order                      
            rad.dataArray = rad.dataMap[x][y];

            if (rad.dataArray == null)
                return false;

            int dataArrayLength = rad.dataArray.Length;

            #region debug            
            //if (DEBUG) {
            //    Vector3 p = currentChunkData.realPositionV3 + new Vector3((x * chunkPixelSize) + (chunkPixelSize * 0.5f), 0, (y * chunkPixelSize) + (chunkPixelSize * 0.5f));
            //    Debuger_K.AddDot(curCell.centerV3, Color.cyan);
            //    Debuger_K.AddDot(p, Color.red, 0.05f);
            //    //list.ForEach(item => Debuger_K.AddLine(item.data.NearestPoint(p), p, Color.blue));
            //}
            #endregion

            int cellLoop = 0;
            bool doCellLoop = true;
            while (doCellLoop) {
                cellLoop++;
                if (cellLoop > 50) {
                    Debug.LogErrorFormat("cellLoop too large. x {0}, y {1}, z {2}, dx {3}, dy {4}, max {5}", rad.posX, rad.posY, rad.posZ, rad.rayDirX, rad.rayDirY, Mathf.Sqrt(rad.maxSqrLength));
                    break;
                }

                doCellLoop = false;
                for (int i = 0; i < dataArrayLength; i++) {
                    CellDataMapValue mapData = rad.dataArray[i];
                    if (mapData.from != rad.curCell)
                        continue;

                    CellContentData ccd = mapData.data;
                    if ((-(ccd.zRight - ccd.zLeft) * rad.rayDirX) + ((ccd.xRight - ccd.xLeft) * rad.rayDirY) < 0)
                        continue;

                    float ix, iy, iz;
                    if (SomeMath.RayIntersectXZ(rad.posX, rad.posZ, rad.rayDirX, rad.rayDirY, ccd.xLeft, ccd.yLeft, ccd.zLeft, ccd.xRight, ccd.yRight, ccd.zRight, out ix, out iy, out iz) == false)
                        continue;

                    rad.raycastResultX = ix;
                    rad.raycastResultY = iy;
                    rad.raycastResultZ = iz;
                    rad.prevCell = rad.curCell;

                    if (SomeMath.SqrDistance(rad.posX, rad.posY, rad.posZ, ix, iy, iz) >= rad.maxSqrLength) {
                        rad.raycastType = NavmeshRaycastResultType2.ReachMaxDistance;
                        rad.raycastDone = true;
                        return true;
                    }

                    if (mapData.connection != null) {
                        #region debug
                        //if (DEBUG) {
                        //    Vector3 p = currentChunkData.realPositionV3 + new Vector3((x * chunkPixelSize) + (chunkPixelSize * 0.5f), 0, (y * chunkPixelSize) + (chunkPixelSize * 0.5f));
                        //    //Debuger_K.AddLine(ToV3(curHullIntersection), resultVector);
                        //    if (prevCell != null) {
                        //        Vector3 p1p = SomeMath.MidPoint(curCell.centerV3, prevCell.centerV3);
                        //        //Vector3 p2p = SomeMath.MidPoint(p1p, p);
                        //        Debuger_K.AddLine(curCell.centerV3, prevCell.centerV3, Color.green);
                        //        Debuger_K.AddLine(p1p, p, Color.cyan);
                        //    }
                        //}
                        #endregion

                        doCellLoop = true;
                        rad.curCell = mapData.connection;

      
                        if (rad.checkPass && rad.curCell.passability != rad.expPass) {
                            rad.raycastType = NavmeshRaycastResultType2.PassabilityChange;
                            rad.raycastDone = true;
                            return true;
                        }
                        else if (rad.checkArea && rad.curCell.area != rad.expArea) {
                            rad.raycastType = NavmeshRaycastResultType2.AreaChange;
                            rad.raycastDone = true;
                            return true;
                        }
                    }
                    else {
                        rad.curCell = null;                   
                        rad.raycastType = NavmeshRaycastResultType2.NavmeshBorderHit;
                        rad.raycastDone = true;
                        return true;
                    }
                    break;
                }
            }
            return rad.raycastDone;
        }

        public static RaycastAllocatedData Rent() {
            lock (poolLock) {
                if (pool.Count == 0)
                    return new RaycastAllocatedData();
                else
                    return pool.Dequeue();
            }
        }
        public static void Return(RaycastAllocatedData data) {
            lock (poolLock) {
                pool.Enqueue(data);
            }
        }

        //public static void Raycast2Body2(float posX, float posY, float posZ, float rayDirX, float rayDirY, Cell cell,
        //          float maxLength, bool checkPass, bool checkArea, Passability expPass, Area expArea,
        //          out RaycastHitNavMesh2 hit) {

        //    chunkPixelSize = PathFinder.gridSize / PathFinder.CELL_GRID_SIZE;

        //    //trick to fix case when raycast start on "near" edge
        //    //currently can't be on chunk edge so we dont care if chunk changed
        //    currentChunkData = cell.graph.chunk;
        //    dataArray = cell.graph.dataMap[(int)((posX - currentChunkData.realX) / chunkPixelSize)][(int)((posZ - currentChunkData.realZ) / chunkPixelSize)];

        //    if (dataArray != null) {
        //        for (int i = 0; i < dataArray.Length; i++) {
        //            CellDataMapValue mapData = dataArray[i];

        //            //Vector3 papa = SomeMath.MidPoint(mapData.data.leftV3, mapData.data.centerV3);
        //            //Debuger_K.AddLabel(papa, mapData.from != cell);
        //            //Debuger_K.AddLine(cell.centerV3, papa, Color.green);

        //            if (mapData.from != cell)
        //                continue;

        //            float dot = mapData.data.RotateRightAndReturnDot(rayDirX, rayDirY);
        //            //string s = "dot " + dot + " : " + (dot < 0.001f) + " ";   

        //            if (dot < 0) {
        //                //Debuger_K.AddLabel(mapData.data.leftV3, s);
        //                continue;
        //            }

        //            float sqrDist = SomeMath.SqrDistance(mapData.data.NearestPointXZ(posX, posZ), new Vector2(posX, posZ));
        //            //s += "dqrDist " + sqrDist + " : " + (sqrDist < 0.000001f) + " ";
        //            //Debuger_K.AddLabel(mapData.data.leftV3, s);



        //            if (sqrDist < 0.000001f) {
        //                Vector2 dirToCellCenter = (cell.centerV2 - new Vector2(posX, posZ)).normalized * 0.001f;
        //                posX += dirToCellCenter.x;
        //                posZ += dirToCellCenter.y;

        //                //if (dot < 0.001f) {//oh wow. start point exactly on edge and ray alond side     

        //                //}
        //                //else {
        //                //    if (DEBUG && mapData.connection != null) Debuger_K.AddLine(cell.centerV3, mapData.connection.centerV3, Color.cyan);//debug
        //                //    cell = mapData.connection;                          

        //                //    if (cell == null) {
        //                //        hit = new RaycastHitNavMesh2(new Vector3(posX, posY, posZ), NavmeshRaycastResultType2.OutsideGraph, null);
        //                //        return;
        //                //    }
        //                //}
        //                break;
        //            }
        //        }
        //    }


        //    if (DEBUG) {
        //        Debuger_K.AddRay(new Vector3(posX, posY + 0.1f, posZ), new Vector3(rayDirX, 0, rayDirY), Color.gray);
        //    }

        //    PathFinderMainRaycasting.posX = posX;
        //    PathFinderMainRaycasting.posY = posY;
        //    PathFinderMainRaycasting.posZ = posZ;
        //    PathFinderMainRaycasting.rayDirX = rayDirX;
        //    PathFinderMainRaycasting.rayDirY = rayDirY;
        //    PathFinderMainRaycasting.checkPass = checkPass;
        //    PathFinderMainRaycasting.checkArea = checkArea;
        //    PathFinderMainRaycasting.expPass = expPass;
        //    PathFinderMainRaycasting.expArea = expArea;


        //    raycastType = NavmeshRaycastResultType2.Nothing;
        //    maxSqrLength = SomeMath.Sqr(maxLength);

        //    curCell = cell;
        //    prevCell = null;
        //    raycastDone = false;

        //    float
        //        chunkX,
        //        chunkZ,
        //        curHullX,
        //        curHullZ,
        //        lastHullX = posX,
        //        lastHullZ = posZ;

        //    for (int i = 0; i < 4; i++) {
        //        raycastSamples[i] = raycastSamplesTemplate[i].RotateRightAndReturnDot(rayDirX, rayDirY) < 0;
        //    }

        //    //int chunkIteration = 0;
        //    while (raycastDone == false) {
        //        //chunkIteration++;
        //        //if (chunkIteration > 50) {
        //        //    Debug.LogErrorFormat("chunkIteration too large. x {0}, y {1}, z {2}, dx {3}, dy {4}", posX, posY, posZ, rayDirX, rayDirY);
        //        //    break;
        //        //}

        //        currentChunkData = curCell.graph.chunk;
        //        dataMap = curCell.graph.dataMap;

        //        chunkX = currentChunkData.realX;
        //        chunkZ = currentChunkData.realZ;

        //        #region border points   
        //        curHullX = posX;
        //        curHullZ = posZ;
        //        for (int i = 0; i < 4; i++) {
        //            if (raycastSamples[i]) {
        //                CellContentData curSide = raycastSamplesTemplate[i];
        //                float rX, rZ;

        //                if (SomeMath.RayIntersect(posX, posZ, rayDirX, rayDirY, curSide.xLeft + chunkX, curSide.zLeft + chunkZ, curSide.xRight + chunkX, curSide.zRight + chunkZ, out rX, out rZ)) {
        //                    curHullX = rX;
        //                    curHullZ = rZ;
        //                }

        //                //if (DEBUG)
        //                //    Debuger_K.AddLine(curSide.a, curSide.b, Color.red, chunkIteration);
        //            }
        //        }

        //        #region debug
        //        //if (DEBUG) {
        //        //    Debuger_K.AddLine(new Vector3(curHullX, 0, curHullZ), new Vector3(lastHullX, 0, lastHullZ), Color.yellow, chunkIteration);

        //        //    for (int x = 0; x < PathFinder.CELL_GRID_SIZE + 1; x++) {
        //        //        Debuger_K.AddLine(
        //        //            currentChunkData.realPositionV3 + new Vector3(x * chunkPixelSize, 0, 0),
        //        //            currentChunkData.realPositionV3 + new Vector3(x * chunkPixelSize, 0, PathFinder.gridSize),
        //        //            Color.red);
        //        //    }
        //        //    for (int z = 0; z < PathFinder.CELL_GRID_SIZE + 1; z++) {
        //        //        Debuger_K.AddLine(
        //        //            currentChunkData.realPositionV3 + new Vector3(0, 0, z * chunkPixelSize),
        //        //            currentChunkData.realPositionV3 + new Vector3(PathFinder.gridSize, 0, z * chunkPixelSize),
        //        //            Color.red);
        //        //    }
        //        //}
        //        #endregion

        //        #endregion

        //        DDARasterization.DrawLine(
        //            lastHullX - chunkX,
        //            lastHullZ - chunkZ,
        //            curHullX - chunkX,
        //            curHullZ - chunkZ,
        //            chunkPixelSize,
        //            RaycastDelegate);

        //        lastHullX = curHullX;
        //        lastHullZ = curHullZ;
        //    }

        //    hit = new RaycastHitNavMesh2(new Vector3(raycastResultX, raycastResultY, raycastResultZ), raycastType, prevCell);
        //}

        //private static bool RaycastDelegate(int x, int y) {
        //    if (raycastDone)
        //        return true;

        //    //x = SomeMath.Clamp(0, CELL_GRID_SIZE - 1, x);
        //    //y = SomeMath.Clamp(0, CELL_GRID_SIZE - 1, y);
        //    if (x < 0) x = 0; else if (x > 9) x = 9;
        //    if (y < 0) y = 0; else if (y > 9) y = 9;

        //    //IMPORTANT: edges in this list are sorted. "connection != null" at the begining and "connection == null" at the end.
        //    //some logic here based on this order                      
        //    dataArray = dataMap[x][y];

        //    if (dataArray == null) 
        //        return false;            

        //    int dataArrayLength = dataArray.Length;

        //    #region debug            
        //    //if (DEBUG) {
        //    //    Vector3 p = currentChunkData.realPositionV3 + new Vector3((x * chunkPixelSize) + (chunkPixelSize * 0.5f), 0, (y * chunkPixelSize) + (chunkPixelSize * 0.5f));
        //    //    Debuger_K.AddDot(curCell.centerV3, Color.cyan);
        //    //    Debuger_K.AddDot(p, Color.red, 0.05f);
        //    //    //list.ForEach(item => Debuger_K.AddLine(item.data.NearestPoint(p), p, Color.blue));
        //    //}
        //    #endregion

        //    //int cellLoop = 0;
        //    bool doCellLoop = true;
        //    while (doCellLoop) {
        //        //cellLoop++;
        //        //if (cellLoop > 20) {
        //        //    Debug.LogErrorFormat("cellLoop too large. x {0}, y {1}, z {2}, dx {3}, dy {4}", posX, posY, posZ, rayDirX, rayDirY);              
        //        //    break;
        //        //}

        //        doCellLoop = false;
        //        for (int i = 0; i < dataArrayLength; i++) {
        //            CellDataMapValue mapData = dataArray[i];
        //            if (mapData.from != curCell)
        //                continue;

        //            CellContentData ccd = mapData.data;
        //            if ((-(ccd.zRight - ccd.zLeft) * rayDirX) + ((ccd.xRight - ccd.xLeft) * rayDirY) < 0)
        //                continue;

        //            float ix, iy, iz;
        //            if (SomeMath.RayIntersectXZ(posX, posZ, rayDirX, rayDirY, ccd.xLeft, ccd.yLeft, ccd.zLeft, ccd.xRight, ccd.yRight, ccd.zRight, out ix, out iy, out iz) == false)
        //                continue;

        //            raycastResultX = ix;
        //            raycastResultY = iy;
        //            raycastResultZ = iz;
        //            prevCell = curCell;

        //            if (mapData.connection != null) {
        //                #region debug
        //                //if (DEBUG) {
        //                //    Vector3 p = currentChunkData.realPositionV3 + new Vector3((x * chunkPixelSize) + (chunkPixelSize * 0.5f), 0, (y * chunkPixelSize) + (chunkPixelSize * 0.5f));
        //                //    //Debuger_K.AddLine(ToV3(curHullIntersection), resultVector);
        //                //    if (prevCell != null) {
        //                //        Vector3 p1p = SomeMath.MidPoint(curCell.centerV3, prevCell.centerV3);
        //                //        //Vector3 p2p = SomeMath.MidPoint(p1p, p);
        //                //        Debuger_K.AddLine(curCell.centerV3, prevCell.centerV3, Color.green);
        //                //        Debuger_K.AddLine(p1p, p, Color.cyan);
        //                //    }
        //                //}
        //                #endregion

        //                doCellLoop = true;                  
        //                curCell = mapData.connection;                      

        //                if (SomeMath.SqrDistance(posX, posY, posZ, ix, iy, iz) >= maxSqrLength) {
        //                    raycastType = NavmeshRaycastResultType2.ReachMaxDistance;
        //                    raycastDone = true;
        //                }
        //                else if (checkPass && curCell.passability != expPass) {
        //                    raycastType = NavmeshRaycastResultType2.PassabilityChange;
        //                    raycastDone = true;
        //                }
        //                else if (checkArea && curCell.area != expArea) {
        //                    raycastType = NavmeshRaycastResultType2.AreaChange;
        //                    raycastDone = true;
        //                }
        //            }
        //            else {              
        //                curCell = null;
        //                raycastDone = true;

        //                if (SomeMath.SqrDistance(posX, posY, posZ, ix, iy, iz) >= maxSqrLength) {
        //                    raycastType = NavmeshRaycastResultType2.ReachMaxDistance;
        //                }
        //                else {
        //                    raycastType = NavmeshRaycastResultType2.NavmeshBorderHit;
        //                }
        //            }
        //            break;
        //        }
        //    }
        //    return raycastDone;
        //}
    }
}