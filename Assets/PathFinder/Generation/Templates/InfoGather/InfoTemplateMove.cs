using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using K_PathFinder.VectorInt;

using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.PFTools;
using K_PathFinder.Trees;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.PathGeneration {
    public class PathTemplateMove : InfoTemplateAbstractWithHeap {
        const float HEURISTIC_FACTOR = 1f;
        int MAX_RAYS = 7;
  
        Action callback;
        PathFinderAgent agent;
        AgentProperties properties;
        Vector3 start;
        Vector3 target; 
        bool snapToNavMesh;
        bool applyRaycast;
        bool ignoreCrouchCost;

        Path path;

        List<Node> all = new List<Node>();
  
        HashSet<Cell> closed = new HashSet<Cell>();
        List<CellContent> cellPath = new List<CellContent>();
        List<CellContentGenericConnection> gateSiquence = new List<CellContentGenericConnection>();
        List<CellContentData> gateTempList = new List<CellContentData>();
        List<Cell> cellListForRaycasting = new List<Cell>();

 

        public override void Clear() {
            agent = null;
            properties = null;
            callback = null;
            path = null;

            all.Clear();
            closed.Clear();
            gateSiquence.Clear();
            cellPath.Clear();
            gateTempList.Clear();
            cellListForRaycasting.Clear();

            base.Clear();
        }

        public override void PerformWork(object context) {
            WorkContext con = (WorkContext)context;
            //Debuger_K.ClearGeneric();

            agent = con.agent;
            properties = agent.properties;
            start = con.start;
            target = con.target;
            snapToNavMesh = con.snapToNavMesh;
            applyRaycast = con.applyRaycast;
            ignoreCrouchCost = con.ignoreCrouchCost;
            callback = con.callBack;

            path = Path.pathPool.Rent();
            path.owner = agent;

            Cell cellStart;
            bool outsideCell;
            Vector3 closestPoint;

            #region start parameters
            //find start point
            if (PathFinder.TryGetClosestCell(start, properties, out cellStart, out outsideCell, out closestPoint) == false) {
                if (con.callBack != null)
                    con.callBack.Invoke();

                path.pathType = PathResultType.InvalidAgentOutsideNavmesh;
                Finish();
                return;
            }

            AddMove(start, cellStart);

            if (snapToNavMesh && outsideCell) {
                start = closestPoint;
                AddMove(closestPoint, cellStart);
            }

            //find end point
            Cell cellEnd;
            if (PathFinder.TryGetClosestCell(target, properties, out cellEnd, out outsideCell, out closestPoint) == false) {
                if (con.callBack != null)
                    con.callBack.Invoke();

                path.pathType = PathResultType.InvalidAgentOutsideNavmesh;
            }

            if (outsideCell) {
                target = closestPoint;
            }

            if (cellStart == cellEnd) {
                AddMove(target, cellStart);
                path.pathType = PathResultType.Valid;
                Finish();
                return;
            }
            #endregion

            closed.Add(cellStart);
            foreach (var ccon in cellStart.connections) {
                if(ccon.connection.canBeUsed)
                    AddNode(-1, ccon, ccon.Cost(start, properties, ignoreCrouchCost));          
            }

            //Debuger_K.ClearGeneric();
            for (int i = 0; i < 5000; i++) {
                if (heapCount == 0)
                    break;

                Node curNode = HeapRemoveFirst();
                Cell curNodeCell = curNode.content.connection;

                if (curNodeCell.canBeUsed == false)
                    continue;

                //var c = curNode.content;
                //Cell c1 = c.from;
                //Cell c2 = c.connection;
                //Vector3 v1 = c1.centerVector3;
                //Vector3 v2 = c2.centerVector3;
                //Debuger_K.AddLine(v1, v2, Color.red, 0.1f);
                //Debuger_K.AddLabel(SomeMath.MidPoint(v1, v2), curNode.gh);

                if (curNodeCell == cellEnd) {
                    Restore(curNode.index);
                    break;
                }

                if (closed.Add(curNodeCell) == false)
                    continue;

                foreach (var ccon in curNodeCell.connections) {
                    if (ccon.connection.canBeUsed == false)
                        continue;

                    AddNode(curNode.index, ccon, curNode.g + ccon.Cost(properties, ignoreCrouchCost));
                }
            }

            if (cellPath.Count == 0) {
                if (con.callBack != null)
                    con.callBack.Invoke();

                path.pathType = PathResultType.InvalidNoPath;
                Finish();
                return;
            }

            //funnel
            cellPath.Reverse();

            if (cellPath[0].from.advancedAreaCell) {
                path.pathContent.AddRange(cellPath[0].from.pathContent);
            }

            for (int i = 0; i < cellPath.Count; i++) {
                if (cellPath[i].connection.advancedAreaCell) {
                    path.pathContent.AddRange(cellPath[i].connection.pathContent);
                }
            }

#if UNITY_EDITOR
            if (Debuger_K.debugPath) {
                for (int cpi = 0; cpi < cellPath.Count; cpi++) {
                    var f = cellPath[cpi];
                    Vector3 ff = f.from.centerVector3;
                    Vector3 fc = f.connection.centerVector3;
                    //Debuger_K.AddPath(ff, fc, Color.blue, 0.2f);
                    Debuger_K.AddLine(ff, fc, Color.blue, 0.2f);
                }
            }
#endif

            int keyGate = 0;
            while (true) {
                if (keyGate >= cellPath.Count)
                    break;

                int curKeyGate;

                gateSiquence.Clear();
                for (curKeyGate = keyGate; curKeyGate < cellPath.Count; curKeyGate++) {
                    var c = cellPath[curKeyGate];
                    if (c is CellContentGenericConnection)
                        gateSiquence.Add((CellContentGenericConnection)c);
                    else
                        break;
                }

                if (keyGate != curKeyGate) {
                    DoFunnelIteration(path.lastV3, curKeyGate == cellPath.Count ? target : (cellPath[curKeyGate] as CellContentPointedConnection).enterPoint, gateSiquence);
                }

                if (curKeyGate != cellPath.Count) {
                    if (cellPath[curKeyGate] is CellContentPointedConnection) {
                        CellContentPointedConnection ju = cellPath[curKeyGate] as CellContentPointedConnection;
                        if (ju.jumpState == ConnectionJumpState.jumpUp) {
                            AddMove(ju.lowerStandingPoint, ju.from);
                            AddJumpUp(ju.lowerStandingPoint, ju.axis);
                            AddMove(ju.exitPoint, ju.connection);
                        }
                        else {
                            AddMove(ju.enterPoint,ju.from);
                            AddMove(ju.axis, ju.from);
                            AddJumpDown(ju.axis, ju.lowerStandingPoint);
                            AddMove(ju.exitPoint, ju.connection);
                        }
                    }
                    else {
                        Debug.LogErrorFormat("idk type of CellConnectionAbstract node {0}", cellPath[curKeyGate].GetType().Name);
                    }
                }

                keyGate = curKeyGate + 1;
            }

            AddMove(target, cellPath[cellPath.Count - 1].connection);

            if (applyRaycast) {
                //in this case always done slight offset to direction of cell cause raycasting cant be perfect
                Vector2 start2d = new Vector2(start.x, start.z);
                Vector3 cellCenter = cellStart.centerVector3;
                Vector2 cellCenterDir = (cellStart.centerVector2 - start2d).normalized;

                Vector2 ajustedstart = start2d + (cellCenterDir * 0.001f);

                RaycastAllocatedData allocated = PathFinderMainRaycasting.Rent();//take allocated data to perform raycast

                Vector2 curDir = path[path.pathNodes.Count - 1].Vector2 - ajustedstart;
                float curMag = SomeMath.Magnitude(curDir) * 0.999f;

                if (curMag < 0.01f) {
                    Finish();
                    return;
                }

                RaycastHitNavMesh2 rhnm;
                //bool r = PathFinder.RaycastForMoveTemplate(ajustedstart.x, start.y, ajustedstart.y, curDir.x, curDir.y, curMag, cellStart, allocated, out rhnm);
                //Debug.Log(r);

                //Debuger_K.ClearGeneric();
                //Debuger_K.AddLine(agent.positionVector3, cellStart.centerVector3 + (Vector3.up * 0.2f), Color.green);
                //Debuger_K.AddLine(agent.positionVector3, cellEnd.centerVector3 + (Vector3.up * 0.3f), Color.magenta);
                //if (rhnm.lastCell != null) {
                //    Debuger_K.AddLine(agent.positionVector3, rhnm.lastCell.centerVector3 + (Vector3.up * 0.1f), Color.blue);
                //    Debuger_K.AddLabel(agent.positionVector3, rhnm.lastCell == cellEnd);
                //}


                if (PathFinder.RaycastForMoveTemplate(ajustedstart.x, start.y, ajustedstart.y, curDir.x, curDir.y, curMag, cellStart, allocated, out rhnm) == false & rhnm.lastCell == cellEnd) {
                    path.SetCurrentIndex(path.pathNodes.Count - 1);
                }
                else {
                    int lastVisible = 0;
                    for (int i = 1; i < Math.Min(MAX_RAYS, path.pathNodes.Count); i++) { //ignore last and first
                        Cell curCell = cellListForRaycasting[i];
                        if (curCell == null)
                            continue;

                        Vector2 curCellCenter = curCell.centerVector2;
                        Vector2 taretDirToCell = (curCellCenter - path[i].Vector2).normalized;
                        Vector2 ajustedTarget = (path[i].Vector2 + taretDirToCell * 0.1f);

                        curDir = ajustedTarget - ajustedstart;
                        curMag = SomeMath.Magnitude(curDir);

                        //Vector3 dir3d = new Vector3(curDir.x, 0, curDir.y);
                        if (PathFinder.RaycastForMoveTemplate(ajustedstart.x, start.y, ajustedstart.y, curDir.x, curDir.y, curMag, cellStart, allocated, out rhnm) == false & rhnm.lastCell == curCell) {
                            lastVisible = i;
                        }
                    }
                    path.SetCurrentIndex(lastVisible);
                }

            
                path.pathType = PathResultType.Valid;    //if there is no issues at this point then path is valid
                agent.ReceivePath(path);
                if (callback != null)
                    callback.Invoke();

                PathFinderMainRaycasting.Return(allocated);//return allocated data to pool
                return;
            }

            path.pathType = PathResultType.Valid;    //if there is no issues at this point then path is valid
            Finish();       
        }

        private void AddNode(int root, CellContent content, float globalCost) {
            Node node = new Node(all.Count, root, globalCost, GetH(content.connection.centerVector3 - target), content);
            all.Add(node);
            HeapAdd(node);
        }

        //        public void PerformWork(object context) {
        //            WorkContext con = (WorkContext)context;
        //            Debuger_K.ClearGeneric();

        //            agent = con.agent;
        //            properties = agent.properties;
        //            start = con.start;
        //            target = con.target;
        //            snapToNavMesh = con.snapToNavMesh;
        //            applyRaycast = con.applyRaycast;
        //            ignoreCrouchCost = con.ignoreCrouchCost;
        //            callback = con.callBack;

        //            result = pathPool.Rent();
        //            result.owner = agent;

        //            Cell cellStart;
        //            bool outsideCell;
        //            Vector3 closestPoint;

        //            #region start parameters
        //            //find start point
        //            if (PathFinder.TryGetClosestCell(agent, out cellStart, out outsideCell, out closestPoint) == false) {
        //                if (con.callBack != null)
        //                    con.callBack.Invoke();

        //                result.pathType = PathResultType.InvalidAgentOutsideNavmesh;
        //                Finish(result);
        //                return;
        //            }

        //            result.AddMove(start, (MoveState)(int)cellStart.passability);

        //            if (snapToNavMesh && outsideCell) {
        //                start = closestPoint;
        //                result.AddMove(closestPoint, (MoveState)(int)cellStart.passability);
        //            }

        //            //find end point
        //            Cell cellEnd;
        //            if (PathFinder.TryGetClosestCell(target, properties, out cellEnd, out outsideCell, out closestPoint) == false) {
        //                if (con.callBack != null)
        //                    con.callBack.Invoke();

        //                result.pathType = PathResultType.InvalidAgentOutsideNavmesh;
        //            }

        //            if (outsideCell) {
        //                target = closestPoint;
        //            }

        //            if(cellStart == cellEnd) {
        //                result.AddMove(target, (MoveState)(int)cellStart.passability);
        //                Finish(result);
        //            }
        //            #endregion

        //            foreach (var ccon in cellStart.connections) {
        //                Node n = new Node(all.Count, - 1, ccon.costSum, GetH(ccon.connection.centerVector3 - target), ccon);
        //                open.Add(n);
        //                all.Add(n);
        //            }
        //            closed.Add(cellStart);

        //            for (int i = 0; i < 1000; i++) {         
        //                Node curNode = open[0];

        //                for (int n = 1; n < open.Count; n++) {
        //                    if (open[n].g <= curNode.g && open[n].h < curNode.h) {              
        //                        curNode = open[n];                   
        //                    }
        //                }

        //                open.Remove(curNode);
        //                Cell curNodeCell = curNode.content.connection;
        //                closed.Add(curNodeCell);             

        //                if(curNodeCell == cellEnd) {        
        //                    Restore(curNode.index);
        //                    break;
        //                }

        //                foreach (var ccon in curNodeCell.connections) {
        //                    if (closed.Contains(ccon.connection))
        //                        continue;

        //                    float newG = curNode.g + ccon.costSum;
        //                    float newH = GetH(ccon.connection.centerVector3 - target);
        //                    Node n = new Node(all.Count, curNode.index, newG, newH, ccon);
        //                    open.Add(n);
        //                    all.Add(n);
        //                }
        //            }
        //            if(cellPath == null) {
        //                if (con.callBack != null)
        //                    con.callBack.Invoke();

        //                result.pathType = PathResultType.InvalidNoPath;
        //                Finish(result);
        //                return;
        //            }

        //            //funnel
        //            cellPath.Reverse();


        //#if UNITY_EDITOR
        //            if (Debuger_K.debugPath) {
        //                for (int cpi = 0; cpi < cellPath.Count; cpi++) {
        //                    var f = cellPath[cpi];
        //                    Vector3 ff = f.from.centerVector3;
        //                    Vector3 fc = f.connection.centerVector3;
        //                    Debuger_K.AddPath(ff, fc, Color.magenta, 0.1f);
        //                }
        //            }
        //#endif
        //            int keyGate = 0;


        //            while (true) {
        //                if (keyGate >= cellPath.Count)
        //                    break;

        //                int curKeyGate;

        //                gateSiquence.Clear();
        //                for (curKeyGate = keyGate; curKeyGate < cellPath.Count; curKeyGate++) {
        //                    var c = cellPath[curKeyGate];
        //                    if (c is CellContentGenericConnection)
        //                        gateSiquence.Add((CellContentGenericConnection)c);
        //                    else
        //                        break;
        //                }

        //                if (keyGate != curKeyGate) {
        //                    DoFunnelIteration(result.lastV3, curKeyGate == cellPath.Count ? target : (cellPath[curKeyGate] as CellContentPointedConnection).enterPoint, gateSiquence);
        //                }

        //                if (curKeyGate != cellPath.Count) {
        //                    if (cellPath[curKeyGate] is CellContentPointedConnection) {
        //                        CellContentPointedConnection ju = cellPath[curKeyGate] as CellContentPointedConnection;
        //                        if (ju.jumpState == ConnectionJumpState.jumpUp) {
        //                            result.AddMove(ju.lowerStandingPoint, (MoveState)(int)ju.from.passability);
        //                            result.AddJumpUp(ju.lowerStandingPoint, ju.axis);
        //                            result.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
        //                        }
        //                        else {
        //                            result.AddMove(ju.enterPoint, (MoveState)(int)ju.from.passability);
        //                            result.AddMove(ju.axis, (MoveState)(int)ju.from.passability);
        //                            result.AddJumpDown(ju.axis, ju.lowerStandingPoint);
        //                            result.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
        //                        }
        //                    }
        //                    else {
        //                        Debug.LogErrorFormat("idk type of CellConnectionAbstract node {0}", cellPath[curKeyGate].GetType().Name);
        //                    }
        //                }

        //                keyGate = curKeyGate + 1;
        //            }         

        //            result.AddMove(target, (MoveState)(int)cellPath[0].connection.passability);

        //            Debuger_K.AddLine(result.vectors, Color.red, false, 0.1f);

        //        }

        //private float GetH(Vector3 v) {
        //    float absX = Mathf.Abs(v.x);
        //    float absZ = Mathf.Abs(v.z);

        //    if (absX > absZ)
        //        return (absX + Mathf.Abs(v.y)) * 0.2f;
        //    else {
        //        return (absZ + Mathf.Abs(v.y)) * 0.2f;
        //    }
        //}

        private float GetH(Vector3 v) {
            return v.magnitude * HEURISTIC_FACTOR;
        }

        private void Restore(int index) {
            Node node = all[index];

            while (true) {
                cellPath.Add(node.content);
                if (node.root != -1)
                    node = all[node.root];
                else
                    break;               
            }
        }

        private void DoFunnelIteration(Vector3 startV3, Vector3 endV3, List<CellContentGenericConnection> targetConnections) {
            gateTempList.Clear();

            for (int i = 0; i < targetConnections.Count; i++) {
                gateTempList.Add(targetConnections[i].cellData);
            }

            gateTempList.Add(new CellContentData(endV3));
            gateTempList.Add(new CellContentData(endV3));

            int curCycleEnd = 0, curCycleStart = 0, curIterationGateCount = targetConnections.Count + 1;
            Vector2 left, right;

            int maxIterations = 200;

            for (int i = 0; i < maxIterations; i++) {
                if (curCycleEnd == curIterationGateCount)
                    break;

                Vector2 startV2 = path.lastV2;

                for (curCycleStart = curCycleEnd; curCycleStart < curIterationGateCount; curCycleStart++) {
                    if (gateTempList[curCycleStart].leftV2 != startV2 & gateTempList[curCycleStart].rightV2 != startV2)
                        break;
                }

                left = gateTempList[curCycleStart].leftV2;
                right = gateTempList[curCycleStart].rightV2;
                Vector2 lowestLeftDir = left - startV2;
                Vector2 lowestRightDir = right - startV2;
                float lowestAngle = Vector2.Angle(lowestLeftDir, lowestRightDir);

                #region gate iteration
                int stuckLeft = curCycleStart;
                int stuckRight = curCycleStart;
                Vector3? endNode = null;

                for (int curGate = curCycleStart; curGate < curIterationGateCount; curGate++) {
                    right = gateTempList[curGate].rightV2;
                    left = gateTempList[curGate].leftV2;
       
                    Vector2 curRightDir = right - startV2;
                    if (SomeMath.V2Cross(lowestLeftDir, curRightDir) >= 0) {
                        float currentAngle = Vector2.Angle(lowestLeftDir, curRightDir);

                        if (currentAngle < lowestAngle) {
                            lowestRightDir = curRightDir;
                            lowestAngle = currentAngle;
                            stuckRight = curGate;
                        }
                    }
                    else {
                        endNode = gateTempList[stuckLeft].leftV3;
                        curCycleEnd = stuckLeft;
                        break;
                    }

                    Vector2 curLeftDir = left - startV2;
                    if (SomeMath.V2Cross(curLeftDir, lowestRightDir) >= 0) {
                        float currentAngle = Vector2.Angle(curLeftDir, lowestRightDir);

                        if (currentAngle < lowestAngle) {
                            lowestLeftDir = curLeftDir;
                            lowestAngle = currentAngle;
                            stuckLeft = curGate;
                        }
                    }
                    else {
                        endNode = gateTempList[stuckRight].rightV3;
                        curCycleEnd = stuckRight;
                        break;
                    }
                }
                #endregion

                //flag to reach next point
                if (endNode.HasValue) {
                    if (curCycleStart != curCycleEnd) //move inside multiple cells
                        AddGate(curCycleStart, curCycleEnd, targetConnections, path.lastV3, endNode.Value);

                    AddMove(endNode.Value, targetConnections[curCycleEnd].from);
                }
            }

            if (curCycleEnd < gateTempList.Count)
                AddGate(curCycleEnd, targetConnections.Count, targetConnections, path.lastV3, endV3);
        }
        
        void AddGate(int startCycle, int endCycle, List<CellContentGenericConnection> gates, Vector3 startPos, Vector3 endPos) {
            for (int cycle = startCycle; cycle < endCycle; cycle++) {
                if (gates[cycle].from.passability == gates[cycle].connection.passability)
                    continue;

                Vector3 ccInt;
                SomeMath.LineIntersectXZ(gates[cycle].leftV3, gates[cycle].rightV3, startPos, endPos, out ccInt);

                AddMove(ccInt, gates[cycle].from);
            }
        }

        private void AddMove(Vector3 pos, Cell cell) {
            path.AddMove(pos, (MoveState)(int)cell.passability);

            if(applyRaycast)
                cellListForRaycasting.Add(cell);
            //Debuger_K.AddLine(pos, cell.centerVector3, Color.blue);
        }
        private void AddJumpUp(Vector3 pos1, Vector3 pos2) {
            path.AddJumpUp(pos1, pos2);
            if (applyRaycast) {
                cellListForRaycasting.Add(null);
                cellListForRaycasting.Add(null);
            }
        }
        private void AddJumpDown(Vector3 pos1, Vector3 pos2) {
            path.AddJumpDown(pos1, pos2);
            if (applyRaycast) {
                cellListForRaycasting.Add(null);
                cellListForRaycasting.Add(null);
            }
        }

        void Finish() {
            //Debug.Log("finished " + path.count);
            //Debuger_K.AddLine(path.vectors, Color.red, false, 0.1f);
            if(path.count > 1)
                path.MoveToNextNode();

            agent.ReceivePath(path);
            if (callback != null)
                callback.Invoke();
        }


        public struct WorkContext {
            public readonly PathFinderAgent agent;
            public readonly Vector3 target;
            public readonly Vector3 start;
            public readonly bool snapToNavMesh;
            public readonly bool applyRaycast;
            public readonly bool ignoreCrouchCost;
            public readonly Action callBack;

            public WorkContext(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, bool applyRaycastBeforeFunnel, bool ignoreCrouchCost, Action callBack) {
                this.agent = agent;
                this.target = target;
                this.start = start;
                this.ignoreCrouchCost = ignoreCrouchCost;
                this.snapToNavMesh = snapToNavMesh;
                this.applyRaycast = applyRaycastBeforeFunnel;
                this.callBack = callBack;
            }
        }
    }

//    public class PathTemplateMove2 : InfoTemplateAbstract, IThreadPoolWorkBatcherMember, IObjectPoolMember {
//        Vector3 end_v3;
//        Graph endGraph;
//        Cell endCell;

//        const int maxPaths = 1;           //!!!
//        const int maxIterations = 15;     //!!!

//        List<CellPath> potentialPaths = new List<CellPath>();
//        HashSet<Cell> excluded = new HashSet<Cell>();

//        //general flags
//        bool ignoreCrouchCost;

//        //funnel values
//        private Path funnelPath;
//        bool snapToNavMesh;

//        //raycast
//        bool applyRaycast;
//        int raycastIterations;

//        //IObjectPoolMember
//        public void Clear() {
//            funnelPath = null;
//            potentialPaths.Clear();
//            excluded.Clear();
//            base.ClearBase();
//        }

//        //public PathTemplateMove(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, 
//        //    bool applyRaycastBeforeFunnel,             
//        //    bool ignoreCrouchCost) : base(agent, start) {
//        //    this.ignoreCrouchCost = ignoreCrouchCost;
//        //    this.end_v3 = target;
//        //    this.snapToNavMesh = snapToNavMesh;
//        //    this.applyRaycast = applyRaycastBeforeFunnel;
//        //}


//        //IThreadPoolWorkBatcherMember
//        public void PerformWork(object context) {
//            WorkContext con = (WorkContext)context;

//            base.SetBase(con.agent);

//            ignoreCrouchCost = con.ignoreCrouchCost;
//            start_v3 = con.start;
//            end_v3 = con.target;
//            snapToNavMesh = con.snapToNavMesh;
//            applyRaycast = con.applyRaycast;

//            GetStartValues();
//            CheckChunkPath();

//            if (ReferenceEquals(startCell, endCell)) {
//                Path path = new Path(start_v3, (MoveState)(int)startCell.passability);
//                path.AddMove(end_v3, (MoveState)(int)startCell.passability);
//                path.RemoveFirst();
//                agent.ReceivePath(path);
//                if (con.callBack != null)
//                    con.callBack.Invoke();
//                return;
//            }

//            //if (applyRaycast) {
//            //    RaycastHitNavMesh2 rhnm;
//            //    Vector3 dir = end_v3 - start_v3;
//            //    if (PathFinder.RaycastForMoveTemplate(start_v3.x, start_v3.y, start_v3.z, dir.x, dir.z, SomeMath.Magnitude(dir) - 0.01f, startCell, startCell.area, startCell.passability, out rhnm) == false) {
//            //        Path path = new Path(start_v3, (MoveState)(int)startCell.passability);
//            //        path.AddMove(end_v3, (MoveState)(int)startCell.passability);
//            //        path.RemoveFirst();
//            //        agent.ReceivePath(path);
//            //        if (con.callBack != null)
//            //            con.callBack.Invoke();
//            //        return;
//            //    }
//            //}

//            GeneratePaths();

//            if (potentialPaths.Count == 0) {
//                Debug.Log("no path");
//                if (con.callBack != null)
//                    con.callBack.Invoke();
//                return;
//            }

//            //CellPath targetCellPath = potentialPaths.OrderBy(val => val.gh).First();
//            CellPath targetCellPath = potentialPaths[0];//right now only 1 path

//            funnelPath = new Path(start_v3, (MoveState)(int)targetCellPath.path.First().passability);
//            FunnelPath(targetCellPath, end_v3);

//            //if (applyRaycast) {
//            //    while (true) {
//            //        Vector3 curDir = funnelPath.firstV3 - start_v3;
//            //        float curMag = SomeMath.Magnitude(curDir) * 0.01f;
//            //        RaycastHitNavMesh2 rhnm;
//            //        if (PathFinder.RaycastForMoveTemplate(start_v3.x, start_v3.y, start_v3.z, curDir.x, curDir.z, curMag - 0.01f, startCell, startCell.area, startCell.passability, out rhnm) == false) {
//            //            funnelPath.RemoveFirst();
//            //        }
//            //        else
//            //            break;
//            //    }

//            //    if(funnelPath.count == 0) {
//            //        funnelPath.AddMove(end_v3, (MoveState)(int)endCell.passability);
//            //    }
//            //}

//            //FunnelPath(targetCellPath, end_v3);
//            agent.ReceivePath(funnelPath);

//            if (con.callBack != null)
//                con.callBack.Invoke();
//        }



//        private void CheckChunkPath() {
//            Vector3 closestPos;
//            GetChunkValues(end_v3, out endGraph, out endCell, out closestPos, snapToNavMesh);

//#if UNITY_EDITOR
//            if (Debuger_K.debugPath) {
//                Debuger_K.AddLine(end_v3, closestPos, Color.red);
//                Debuger_K.AddLine(end_v3, endCell.centerVector3, Color.cyan);
//            }
//#endif

//            end_v3 = closestPos;

//            if (PathFinder.ToChunkPosition(start_v3) == PathFinder.ToChunkPosition(end_v3))
//                return;

//            VectorInt.Vector2Int targetPosition = endGraph.positionChunk;
//            ClearGraphList();

//            AddGraphNode(new GraphPathSimple(startGraph, Vector2.Distance(start_v3, end_v3)));
//            HashSet<Graph> usedGraphs = new HashSet<Graph>();

//            for (int v = 0; v < 10; v++) {
//                if (base.linkedGraph.Count == 0) {
//                    UnityEngine.Debug.Log("no path. count");
//                    break;
//                }

//                GraphPathSimple current = TakeGraphNode();
//                Graph currentGraph = current.graph;

//                //Debuger3.AddLine(start_v3, currentGraph.positionWorldCenter, Color.red);
//                //Debuger3.AddLabel(currentGraph.positionWorldCenter, linkedGrap.Count);

//                if (currentGraph.positionChunk == targetPosition)
//                    break;

//                for (int dir = 0; dir < 4; dir++) {
//                    Graph neighbourGraph;
//                    while (true) {
//                        if (PathFinder.GetGraphFrom(currentGraph.gridPosition, (Directions)dir, properties, out neighbourGraph) && neighbourGraph.canBeUsed)
//                            break;
//                        Thread.Sleep(10);
//                    }

//                    if (neighbourGraph != null && usedGraphs.Contains(neighbourGraph) == false) {
//                        AddGraphNode(new GraphPathSimple(neighbourGraph, Vector3.Distance(end_v3, neighbourGraph.positionCenter)));
//                        usedGraphs.Add(neighbourGraph);
//                    }
//                }
//            }

//            if (endGraph == null) {
//                Debug.LogWarning("graph path > 500");
//                Debug.LogWarning("chunk path result are null");
//                return;
//            }
//        }

//        #region potential paths
//        private void GeneratePaths() {
//            CellPath path = new CellPath(startCell, start_v3);

//            if (startCell == endCell) {
//                potentialPaths.Add(path);
//                return;
//            }

//#if UNITY_EDITOR
//            float totalDist = Debuger_K.doDebug ? Vector3.Distance(start_v3, end_v3) : 0f;
//#endif

//            path.h = EuclideanDistance(start_v3);
//            excluded.Clear();
//            excluded.Add(startCell);

//            foreach (var connection in startCell.connections) {
//                CellPath newPath = new CellPath(path, connection);
//                newPath.g = connection.Cost(properties, ignoreCrouchCost);
//                if (connection is CellContentPointedConnection)
//                    newPath.h = EuclideanDistance((connection as CellContentPointedConnection).exitPoint);
//                else
//                    newPath.h = EuclideanDistance(connection.connection);
//                AddCellNode(newPath);
//            }

//            int limit = 0;
//            while (true) {
//                limit++;
//                if (limit > 1500) {
//                    Debug.Log("limit > 1500");
//                    break;
//                }

//                CellPath current = TakeCellNode();
//                if (current == null)
//                    break;

//                Cell currentCell = current.last;

//                if (currentCell == endCell) {
//                    potentialPaths.Add(current);
//#if UNITY_EDITOR
//                    if (Debuger_K.doDebug) {
//                        float lerped = Mathf.InverseLerp(0, totalDist, Vector3.Distance(end_v3, currentCell.centerVector3));
//                        Debuger_K.AddPath(current.path[current.path.Count - 2].centerVector3 + Vector3.up, current.path[current.path.Count - 1].centerVector3 + Vector3.up, new Color(lerped, 1 - lerped, 0, 1f));
//                    }
//#endif
//                    if (potentialPaths.Count >= maxPaths)
//                        break;
//                    else
//                        continue;
//                }

//                if (excluded.Contains(currentCell))
//                    continue;
//                else
//                    excluded.Add(currentCell);

//#if UNITY_EDITOR
//                if (Debuger_K.doDebug) {
//                    float lerped = Mathf.InverseLerp(0, totalDist, Vector3.Distance(end_v3, currentCell.centerVector3));
//                    Debuger_K.AddPath(current.path[current.path.Count - 2].centerVector3 + (Vector3.up * 0.3f), current.path[current.path.Count - 1].centerVector3 + (Vector3.up * 0.3f), new Color(lerped, 1 - lerped, 0, 1f));
//                }
//#endif

//                foreach (var connection in currentCell.connections) {
//                    Cell newCell = connection.connection;

//                    if (current.Contains(newCell) == false) {
//                        CellPath newPath = new CellPath(current, connection);
//#if UNITY_EDITOR
//                        if (Debuger_K.debugPath)
//                            Debuger_K.AddLabel(SomeMath.MidPoint(current.last.centerVector3, newCell.centerVector3), connection.Cost(properties, ignoreCrouchCost), DebugGroup.path);
//#endif

//                        newPath.g = current.g + connection.Cost(properties, ignoreCrouchCost);
//                        if (connection is CellContentPointedConnection) {
//                            newPath.h = EuclideanDistance((connection as CellContentPointedConnection).exitPoint);
//                            //Debuger3.AddLabel((connection as CellPointedConnection).exitPoint, newPath.h);
//                        }
//                        else
//                            newPath.h = EuclideanDistance(connection.connection);

//                        AddCellNode(newPath);
//                    }
//                }
//            }
//        }
//        #endregion

//        #region funnel
//        protected void FunnelPath(CellPath path, Vector3 endV3) {
//            List<Cell> cellPath = path.path;

//            List<CellContent> cellPathConnections = path.connections;
//#if UNITY_EDITOR
//            if (Debuger_K.debugPath) {
//                for (int i = 0; i < cellPath.Count - 1; i++)
//                    Debuger_K.AddPath(cellPath[i].centerVector3 + Vector3.up, cellPath[i + 1].centerVector3 + Vector3.up, Color.magenta, 0.1f);
//            }
//#endif
//            int keyGate = 0;

//            while (true) {
//                if (keyGate >= cellPathConnections.Count)
//                    break;

//                int curKeyGate;

//                List<CellContentGenericConnection> gateSiquence = new List<CellContentGenericConnection>();
//                for (curKeyGate = keyGate; curKeyGate < cellPathConnections.Count; curKeyGate++) {
//                    var c = cellPathConnections[curKeyGate];
//                    if (c is CellContentGenericConnection)
//                        gateSiquence.Add((CellContentGenericConnection)c);
//                    else
//                        break;
//                }

//                if (keyGate != curKeyGate) {
//                    DoFunnelIteration(funnelPath.lastV3, curKeyGate == cellPathConnections.Count ? endV3 : (cellPathConnections[curKeyGate] as CellContentPointedConnection).enterPoint, gateSiquence);
//                }

//                if (curKeyGate != cellPathConnections.Count) {
//                    if (cellPathConnections[curKeyGate] is CellContentPointedConnection) {
//                        CellContentPointedConnection ju = cellPathConnections[curKeyGate] as CellContentPointedConnection;
//                        if (ju.jumpState == ConnectionJumpState.jumpUp) {
//                            funnelPath.AddMove(ju.lowerStandingPoint, (MoveState)(int)ju.from.passability);
//                            funnelPath.AddJumpUp(ju.lowerStandingPoint, ju.axis);
//                            funnelPath.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
//                        }
//                        else {
//                            funnelPath.AddMove(ju.enterPoint, (MoveState)(int)ju.from.passability);
//                            funnelPath.AddMove(ju.axis, (MoveState)(int)ju.from.passability);
//                            funnelPath.AddJumpDown(ju.axis, ju.lowerStandingPoint);
//                            funnelPath.AddMove(ju.exitPoint, (MoveState)(int)ju.connection.passability);
//                        }
//                    }
//                    else {
//                        Debug.LogErrorFormat("idk type of CellConnectionAbstract node {0}", cellPathConnections[curKeyGate].GetType().Name);
//                    }
//                }

//                keyGate = curKeyGate + 1;
//            }

//            funnelPath.AddMove(endV3, (MoveState)(int)cellPath[cellPath.Count - 1].passability);

//#if UNITY_EDITOR
//            if (Debuger_K.debugPath) {
//                var resultNodes = funnelPath.nodes;
//                for (int i = 0; i < resultNodes.Count - 1; i++) {
//                    Debuger_K.AddPath(resultNodes[i].positionV3, resultNodes[i + 1].positionV3, Color.green);
//                }
//                //for (int i = 0; i < resultNodes.Count; i++) {
//                //    Debuger3.AddDot(resultNodes[i].positionV3, Color.green, 0.03f, DebugGroup.path);
//                //    Debuger3.AddLabel(resultNodes[i].positionV3, resultNodes[i].ToString(), DebugGroup.path);
//                //}
//            }
//#endif
//            funnelPath.RemoveFirst();
//        }

//        private void DoFunnelIteration(Vector3 startV3, Vector3 endV3, List<CellContentGenericConnection> targetConnections) {
//            List<CellContentData> gd = new List<CellContentData>();
//            for (int i = 0; i < targetConnections.Count; i++) {
//                gd.Add(targetConnections[i].cellData);
//            }

//            gd.Add(new CellContentData(endV3));
//            gd.Add(new CellContentData(endV3));

//            int curCycleEnd = 0, curCycleStart = 0, curIterationGateCount = targetConnections.Count + 1;
//            Vector2 left, right;

//            for (int i = 0; i < maxIterations; i++) {
//                if (curCycleEnd == curIterationGateCount)
//                    break;

//                Vector2 startV2 = funnelPath.lastV2;

//                for (curCycleStart = curCycleEnd; curCycleStart < curIterationGateCount; curCycleStart++) {
//                    if (gd[curCycleStart].leftV2 != startV2 & gd[curCycleStart].rightV2 != startV2)
//                        break;
//                }

//                left = gd[curCycleStart].leftV2;
//                right = gd[curCycleStart].rightV2;
//                Vector2 lowestLeftDir = left - startV2;
//                Vector2 lowestRightDir = right - startV2;
//                float lowestAngle = Vector2.Angle(lowestLeftDir, lowestRightDir);

//                #region gate iteration
//                int stuckLeft = curCycleStart;
//                int stuckRight = curCycleStart;
//                Vector3? endNode = null;

//                for (int curGate = curCycleStart; curGate < curIterationGateCount; curGate++) {
//                    right = gd[curGate].rightV2;
//                    left = gd[curGate].leftV2;

//                    Vector2 curLeftDir = left - startV2;
//                    Vector2 curRightDir = right - startV2;

//                    if (SomeMath.V2Cross(lowestLeftDir, curRightDir) >= 0) {
//                        float currentAngle = Vector2.Angle(lowestLeftDir, curRightDir);

//                        if (currentAngle < lowestAngle) {
//                            lowestRightDir = curRightDir;
//                            lowestAngle = currentAngle;
//                            stuckRight = curGate;
//                        }
//                    }
//                    else {
//                        endNode = gd[stuckLeft].leftV3;
//                        curCycleEnd = stuckLeft;
//                        break;
//                    }

//                    if (SomeMath.V2Cross(curLeftDir, lowestRightDir) >= 0) {
//                        float currentAngle = Vector2.Angle(curLeftDir, lowestRightDir);
//                        if (currentAngle < lowestAngle) {
//                            lowestLeftDir = curLeftDir;
//                            lowestAngle = currentAngle;
//                            stuckLeft = curGate;
//                        }
//                    }
//                    else {
//                        endNode = gd[stuckRight].rightV3;
//                        curCycleEnd = stuckRight;
//                        break;
//                    }
//                }
//                #endregion

//                //flag to reach next point
//                if (endNode.HasValue) {
//                    if (curCycleStart != curCycleEnd) //move inside multiple cells
//                        AddGate(curCycleStart, curCycleEnd, targetConnections, funnelPath.lastV3, endNode.Value);

//                    funnelPath.AddMove(endNode.Value, (MoveState)(int)targetConnections[curCycleEnd].from.passability);
//                }
//            }

//            if (curCycleEnd < gd.Count)
//                AddGate(curCycleEnd, targetConnections.Count, targetConnections, funnelPath.lastV3, endV3);
//        }


//        void AddGate(int startCycle, int endCycle, List<CellContentGenericConnection> gates, Vector3 startPos, Vector3 endPos) {
//            for (int cycle = startCycle; cycle < endCycle; cycle++) {
//                if (gates[cycle].from.passability == gates[cycle].connection.passability)
//                    continue;

//                Vector3 ccInt;
//                SomeMath.LineIntersectXZ(
//                    gates[cycle].leftV3,
//                    gates[cycle].rightV3,
//                    startPos, endPos,
//                    out ccInt);

//                funnelPath.AddMove(ccInt, (MoveState)(int)gates[cycle].from.passability);
//            }

//        }
//        #endregion

//        #region distance   
//        protected float ManhattanDistance(Cell cell) {
//            return (Math.Abs(cell.centerVector3.x - end_v3.x) + Math.Abs(cell.centerVector3.y - end_v3.y) + Math.Abs(cell.centerVector3.z - end_v3.z));
//        }
//        protected float ManhattanDistance(Vector3 pos) {
//            return (Math.Abs(pos.x - end_v3.x) + Math.Abs(pos.y - end_v3.y) + Math.Abs(pos.z - end_v3.z));
//        }
//        protected float EuclideanDistance(Cell cell) {
//            return Vector3.Distance(cell.centerVector3, end_v3);
//        }
//        protected float EuclideanDistance(Vector3 pos) {
//            return Vector3.Distance(pos, end_v3);
//        }


//        #endregion

//        //private bool Raycast(Vector3 origin, Vector3 direction, out RaycastHitNavMesh hit,
//        //    float length, int maxIterations, Passability expectedPassability, Area expectedArea, Cell cell) {
//        //    HashSet<CellContentData> raycastExclude = new HashSet<CellContentData>();
//        //    List<RaycastSomeData> raycastTempData = new List<RaycastSomeData>();
//        //    float maxLengthSqr = length * length;

//        //    for (int iteration = 0; iteration < maxIterations; iteration++) {
//        //        raycastTempData.Clear();//iteration data cleared

//        //        foreach (var pair in cell.dataContentPairs) {
//        //            CellContentData curData = pair.Key;
//        //            if (!raycastExclude.Add(curData))//mean it's already contain this
//        //                continue;

//        //            Vector3 intersect;
//        //            if (SomeMath.RayIntersectXZ(origin, direction, curData.leftV3, curData.rightV3, out intersect)) {
//        //                if (pair.Value != null) {
//        //                    Cell otherCell = pair.Value.connection;
//        //                    if (otherCell == cell | !otherCell.canBeUsed)
//        //                        continue;

//        //                    if (cell.passability != otherCell.passability || cell.area != otherCell.area) {
//        //                        hit = new RaycastHitNavMesh(intersect, SomeMath.SqrDistance(origin, intersect) < maxLengthSqr);//!!!
//        //                        return;
//        //                    }
//        //                    raycastTempData.Add(new RaycastSomeData(intersect, otherCell));
//        //                }
//        //                else
//        //                    raycastTempData.Add(new RaycastSomeData(intersect, null));
//        //            }
//        //        }

//        //        //check if there possible connection
//        //        for (int i = 0; i < raycastTempData.Count; i++) {
//        //            if (raycastTempData[i].cell != null) {
//        //                cell = raycastTempData[i].cell;
//        //                goto CONTINUE;
//        //            }
//        //        }

//        //        //now we definetly hit something and now find furthest
//        //        float furthestSqrDist = 0f;
//        //        Vector3 furthest = origin;
//        //        for (int i = 0; i < raycastTempData.Count; i++) {
//        //            float curSqrDist = SomeMath.SqrDistance(raycastTempData[i].point, origin);

//        //            if (curSqrDist > furthestSqrDist) {
//        //                furthestSqrDist = curSqrDist;
//        //                furthest = raycastTempData[i].point;
//        //            }
//        //        }

//        //        hit = new RaycastHitNavMesh(furthest, SomeMath.SqrDistance(origin, furthest) < maxLengthSqr);
//        //        return;

//        //        CONTINUE: { continue; }
//        //    }
//        //    hit = new RaycastHitNavMesh(origin, true, true);
//        //    return;
//        //}

//        public struct WorkContext {
//            public readonly PathFinderAgent agent;
//            public readonly Vector3 target;
//            public readonly Vector3 start;
//            public readonly bool snapToNavMesh;
//            public readonly bool applyRaycast;
//            public readonly bool ignoreCrouchCost;
//            public readonly Action callBack;

//            public WorkContext(PathFinderAgent agent, Vector3 target, Vector3 start, bool snapToNavMesh, bool applyRaycastBeforeFunnel, bool ignoreCrouchCost, Action callBack) {
//                this.agent = agent;
//                this.target = target;
//                this.start = start;
//                this.ignoreCrouchCost = ignoreCrouchCost;
//                this.snapToNavMesh = snapToNavMesh;
//                this.applyRaycast = applyRaycastBeforeFunnel;
//                this.callBack = callBack;
//            }
//        }

//    }
}