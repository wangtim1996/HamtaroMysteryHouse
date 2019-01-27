using UnityEngine;
using System.Collections.Generic;
using System.Threading;

using K_PathFinder.Graphs;
using K_PathFinder.CoverNamespace;
using K_PathFinder.PFTools;
using System;
using K_PathFinder.Trees;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.PathGeneration {
    public class InfoTemplateCover : IThreadPoolWorkBatcherMember, IObjectPoolMember {
        HashSet<Cell> excludedCells = new HashSet<Cell>();
        HashSet<Graph> checkedGraphs = new HashSet<Graph>();
        HashSet<Graph> lastIteration = new HashSet<Graph>();
        HashSet<Graph> curIteration = new HashSet<Graph>();

        HeapStructed<HeapValue> heap = new HeapStructed<HeapValue>();

        public InfoTemplateCover() { }

        //IObjectPoolMember
        public void Clear() {
            //base.ClearBase();
            excludedCells.Clear();
            checkedGraphs.Clear();
            lastIteration.Clear();
            curIteration.Clear();
        }

        //IThreadPoolWorkBatcherMember
        public void PerformWork(object context) {
            WorkContext con = (WorkContext)context;
            PathFinderAgent agent = con.agent;
            AgentProperties properties = agent.properties;


        
            List<NodeCoverPoint> tempResult = Pool.GenericPool<List<NodeCoverPoint>>.Take();

            Vector3 start = agent.positionVector3;

            Cell cellStart;
            if (PathFinder.TryGetClosestCell(start, agent.properties, out cellStart) == false) {
                if (con.callBack != null)
                    con.callBack.Invoke();

                agent.ReceiveCovers(tempResult);
                tempResult.Clear();
                Pool.GenericPool<List<NodeCoverPoint>>.ReturnToPool(ref tempResult);
                return;
            }
            

            //base.SetBase(con.agent);
            //base.GetStartValues();
            
            float maxMoveCost = con.maxMoveCost;
            //int maxChunkDepth = con.maxChunkDepth;
            bool ignoreCrouchCost = con.ignoreCrouchCost;

            //checkedGraphs.Add(startGraph);
            //lastIteration.Add(startGraph);

            ////graphs
            //for (int i = 0; i < maxChunkDepth; i++) {
            //    curIteration.Clear();

            //    foreach (var lastGraph in lastIteration) {
            //        for (int n = 0; n < 4; n++) {
            //            Graph neighbourGraph;
            //            while (true) {
            //                if (PathFinder.GetGraphFrom(lastGraph.gridPosition, (Directions)n, properties, out neighbourGraph) && neighbourGraph.canBeUsed)
            //                    break;
            //                Thread.Sleep(20);
            //            }

            //            if (neighbourGraph != null && checkedGraphs.Add(neighbourGraph))
            //                curIteration.Add(neighbourGraph);
            //        }
            //    }

            //    lastIteration.Clear();
            //    foreach (var item in curIteration) {
            //        lastIteration.Add(item);
            //    }
            //}


            //actual cover search
            //just use Dijkstra to find nearest cover

            //CellPath path = new CellPath(startCell, start_v3);



            if (cellStart.covers != null)
                tempResult.AddRange(cellStart.covers);
            excludedCells.Add(cellStart);

            foreach (var connection in cellStart.connections) {
                heap.HeapAdd(new HeapValue(connection, connection.Cost(start, properties, ignoreCrouchCost)));
            }

            while (true) {
                if (heap.count == 0)
                    break;

                HeapValue cur = heap.HeapRemoveFirst();

                if (cur.value > maxMoveCost)
                    break;

                Cell currentCell = cur.content.connection;

                if (excludedCells.Contains(currentCell))
                    continue;
                else
                    excludedCells.Add(currentCell);

#if UNITY_EDITOR
                if (Debuger_K.debugPath)
                    Debuger_K.AddPath(cur.content.from.centerVector3, cur.content.connection.centerVector3, Color.green);
#endif

                if (currentCell.covers != null)
                    tempResult.AddRange(currentCell.covers);

                foreach (var connection in currentCell.connections) {
                    heap.HeapAdd(new HeapValue(connection, cur.value + connection.Cost(properties, ignoreCrouchCost)));
                }
            }

            agent.ReceiveCovers(tempResult);
            tempResult.Clear();
            Pool.GenericPool<List<NodeCoverPoint>>.ReturnToPool(ref tempResult);

            if (con.callBack != null)
                con.callBack.Invoke();
        }

        public struct WorkContext {
            public readonly PathFinderAgent agent;  
            public readonly float maxMoveCost;
            public readonly bool ignoreCrouchCost;
            public readonly Action callBack;       

            public WorkContext(PathFinderAgent agent, float maxMoveCost, bool ignoreCrouchCost, Action callBack) {
                this.agent = agent;   
                this.maxMoveCost = maxMoveCost;
                this.ignoreCrouchCost = ignoreCrouchCost;
                this.callBack = callBack;
            }
        }

        struct HeapValue : IComparable<HeapValue> {
            public CellContent content;
            public float value;

            public HeapValue(CellContent content, float value) {
                this.content = content;
                this.value = value;
            }

            public int CompareTo(HeapValue other) {
                if (value < other.value)
                    return 1;
                if (value > other.value)
                    return -1;
                return 0;
            }
        }
    }
}