using K_PathFinder.Graphs;
using K_PathFinder.PFTools;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using K_PathFinder.PFDebuger;


namespace K_PathFinder.PathGeneration {
    public class InfoTemplateAreaSearch : InfoTemplateAbstractWithHeap {
        //int MAX_RAYS = 7;

        PathFinderAgent agent;
        Area target;
        float maxSearchCost;
        bool searchToArea;
        Vector3 start;
        bool snapToNavMesh;
        Action callBack;
        bool applyRaycast;
        bool ignoreCrouchCost;

        AgentProperties properties;
        Path path;

        HashSet<Cell> closed = new HashSet<Cell>();
        List<Node> all = new List<Node>();
        List<Cell> cellListForRaycasting = new List<Cell>();
        List<CellContent> cellPath = new List<CellContent>();
        List<CellContentData> gateTempList = new List<CellContentData>();
        List<CellContentGenericConnection> gateSiquence = new List<CellContentGenericConnection>();

        public override void Clear() {
            agent = null;
            target = null;
            properties = null;
            callBack = null;
            path = null;

            closed.Clear();     
            all.Clear();
            cellPath.Clear();
            gateSiquence.Clear();
            cellListForRaycasting.Clear();
            gateTempList.Clear();

            base.Clear();
        }


        public override void PerformWork(object context) {
            WorkContext con = (WorkContext)context;
            agent = con.agent;
            properties = agent.properties;
            target = con.target;
            maxSearchCost = con.maxSearchCost;
            searchToArea = con.searchToArea;
            start = con.start;
            snapToNavMesh = con.snapToNavMesh;
            callBack = con.callBack;
            applyRaycast = con.applyRaycast;
            ignoreCrouchCost = con.ignoreCrouchCost;

            path = Path.pathPool.Rent();
            path.owner = agent;

            //start parameters
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


            //Debuger_K.ClearGeneric();
            //Debuger_K.AddLine(start, cellStart.centerVector3, Color.red);

            AddMove(start, cellStart);

            if (snapToNavMesh && outsideCell) {
                start = closestPoint;
                AddMove(closestPoint, cellStart);
            }

            if (searchToArea) {
                if (cellStart.area == target) {
                    path.pathType = PathResultType.Valid;    //if there is no issues at this point then path is valid
                    Finish();
                    return;
                }
            }
            else {
                if (cellStart.area != target) {
                    Finish();
                    return;
                }
            }
            #endregion

            closed.Add(cellStart);
            foreach (var ccon in cellStart.connections) {
                AddNode(-1, ccon, ccon.Cost(start, properties, ignoreCrouchCost));
            }

            for (int i = 0; i < 5000; i++) {
                if (heapCount == 0)
                    break;

                Node curNode = HeapRemoveFirst();
                if(curNode.g > maxSearchCost) {
                    path.pathType = PathResultType.InvalidNoPath;
                    Finish();
                    return;
                }

                Cell curNodeCell = curNode.content.connection;

                //var c = curNode.content;
                //Cell c1 = c.from;
                //Cell c2 = c.connection;
                //Vector3 v1 = c1.centerVector3;
                //Vector3 v2 = c2.centerVector3;
                //Debuger_K.AddLine(v1, v2, Color.red, 0.1f);
                //Debuger_K.AddLabel(SomeMath.MidPoint(v1, v2), curNode.gh);

                if (searchToArea) {
                    if (curNodeCell.area == target) {
                        Restore(curNode.index);
                        break;
                    }
                }
                else {
                    if (curNodeCell.area != target) {
                        Restore(curNode.index);
                        break;
                    }
                }

                if (closed.Add(curNodeCell) == false)
                    continue;

                foreach (var ccon in curNodeCell.connections) {
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

#if UNITY_EDITOR
            //for (int cpi = 0; cpi < cellPath.Count; cpi++) {
            //    var f = cellPath[cpi];
            //    Vector3 ff = f.from.centerVector3;
            //    Vector3 fc = f.connection.centerVector3;
            //    Debuger_K.AddLine(ff, fc, Color.blue, 0.2f);
            //}
#endif
            Vector3 targetPos = cellPath[cellPath.Count - 1].connection.centerVector3;

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
                    DoFunnelIteration(path.lastV3, curKeyGate == cellPath.Count ? targetPos : (cellPath[curKeyGate] as CellContentPointedConnection).enterPoint, gateSiquence);
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
                            AddMove(ju.enterPoint, ju.from);
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

            AddMove(targetPos, cellPath[cellPath.Count - 1].connection);

            path.pathType = PathResultType.Valid;    //if there is no issues at this point then path is valid
            Finish();
        }

        private void AddNode(int root, CellContent content, float globalCost) {
            Node node = new Node(all.Count, root, globalCost, 0, content);
            all.Add(node);
            HeapAdd(node);
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


        #region path adding
        private void AddMove(Vector3 pos, Cell cell) {
            path.AddMove(pos, (MoveState)(int)cell.passability);

            if (applyRaycast)
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
            if (path.count > 1)
                path.MoveToNextNode();

            agent.ReceivePath(path);
            if (callBack != null)
                callBack.Invoke();
        }

        #endregion

        public struct WorkContext {
            public readonly PathFinderAgent agent;
            public readonly Area target;
            public readonly float maxSearchCost;
            public readonly bool searchToArea;
            public readonly Vector3 start;
            public readonly bool snapToNavMesh;
            public readonly Action callBack;
            public readonly bool applyRaycast;
            public readonly bool ignoreCrouchCost;

            public WorkContext(PathFinderAgent agent, Area target, float maxSearchCost, bool searchToArea, Vector3 start, bool snapToNavMesh, Action callBack, bool applyRaycast, bool ignoreCrouchCost) {
                this.agent = agent;
                this.target = target;
                this.maxSearchCost = maxSearchCost;
                this.searchToArea = searchToArea;
                this.start = start;
                this.ignoreCrouchCost = ignoreCrouchCost;
                this.snapToNavMesh = snapToNavMesh;
                this.applyRaycast = applyRaycast;
                this.callBack = callBack;
            }
        }
    }
}