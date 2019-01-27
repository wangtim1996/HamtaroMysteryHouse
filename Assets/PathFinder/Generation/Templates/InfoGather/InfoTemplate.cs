using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;

using K_PathFinder.VectorInt;
using K_PathFinder.Graphs;
using K_PathFinder.PFDebuger;
using K_PathFinder.PFTools;

namespace K_PathFinder.PathGeneration {
    public abstract class InfoTemplateAbstract {
        //general values
        protected PathFinderAgent agent;
        protected AgentProperties properties;

        //start values
        protected Vector3 start_v3;
        protected Cell startCell;
        protected Graph startGraph;

        protected LinkedList<CellPath> linkedPaths = new LinkedList<CellPath>();
        protected LinkedList<GraphPathSimple> linkedGraph = new LinkedList<GraphPathSimple>();

        protected void SetBase(PathFinderAgent agent) {
            this.agent = agent;
            properties = agent.properties;
        }

        protected void ClearBase() {
            agent = null;
            properties = null;
            startCell = null;
            startGraph = null;
            //Debug.Log("linkedPaths.Clear()");
            linkedPaths.Clear();
            //Debug.Log(linkedPaths.Count);
            //Debug.Log("linkedGraph.Clear()");
            linkedGraph.Clear();
            //Debug.Log(linkedGraph.Count);
        }        

        protected void GetStartValues() {
            Vector3 closest;        
            GetChunkValues(start_v3, out startGraph, out startCell, out closest);
            //Debuger_K.AddLine(start_v3, closest + Vector3.up);
        }

        protected void GetChunkValues(Vector3 pos, out Graph graph, out Cell cell, out Vector3 closestPoint, bool snapToNavMesh = false) {
            graph = null;
            cell = null;
            closestPoint = pos;

            HashSet<Graph> usedGraphs = new HashSet<Graph>();
            XZPosInt startPos = PathFinder.ToChunkPosition(pos.x, pos.z);

            ClearGraphList();

            //getting first graph
            Graph curGraph;     
            while (true) {
                if (PathFinder.GetGraph(startPos, properties, out curGraph) && curGraph.canBeUsed)                  
                    break;                    
                else
                    Thread.Sleep(30);
            }
            AddGraphNode(new GraphPathSimple(curGraph, Vector3.Distance(pos, curGraph.positionCenter)));
            usedGraphs.Add(curGraph);       

            for (int i = 0; i < 100; i++) {     
                var node = TakeGraphNode();
                graph = node.graph;

                if (graph.empty == false) {
                    bool outsideCell;
                    Vector3 ifOutsideCell;
                    graph.GetClosestCell(pos, out cell, out outsideCell, out ifOutsideCell);
                    if(snapToNavMesh | outsideCell)
                        closestPoint = ifOutsideCell;   
                }
      
                if (cell == null) {
                    for (int dir = 0; dir < 4; dir++) {
                        Graph neighbourGraph;
                        while (true) {
                            if (PathFinder.GetGraphFrom(graph.gridPosition, (Directions)dir, properties, out neighbourGraph) && neighbourGraph.canBeUsed)
                                break;
                            Thread.Sleep(10);
                        }

                        if (neighbourGraph != null && usedGraphs.Add(neighbourGraph)) 
                            AddGraphNode(new GraphPathSimple(neighbourGraph, Vector3.Distance(pos, neighbourGraph.positionCenter)));
                    }
                }
                else 
                    break;                
            }

            if (cell == null) {
                closestPoint = pos;
                Debug.LogError("there was 100 iterations and still no graph. are you trying to find path in middle of nowhere? if u r so smart than increase up number");
            } 
        }
        
        #region cellNode manage
        protected void AddCellNode(CellPath node) {
            LinkedListNode<CellPath> targetNode = null;

            for (LinkedListNode<CellPath> currentPath = linkedPaths.First;
                currentPath != null && currentPath.Value.gh < node.gh;
                currentPath = currentPath.Next) {
                targetNode = currentPath;
            }

            if (targetNode == null)
                linkedPaths.AddFirst(node);
            else
                linkedPaths.AddAfter(targetNode, node);
        }
        protected CellPath TakeCellNode() {
            LinkedListNode<CellPath> result = linkedPaths.First;
            linkedPaths.RemoveFirst();
            return result == null ? null : result.Value;
        }
        #endregion

        #region graphNode manage
        protected void AddGraphNode(GraphPathSimple node) {
            LinkedListNode<GraphPathSimple> targetNode = null;

            for (LinkedListNode<GraphPathSimple> currentPath = linkedGraph.First;
                currentPath != null && currentPath.Value.cost < node.cost;
                currentPath = currentPath.Next) {
                targetNode = currentPath;
            }

            if (targetNode == null)
                linkedGraph.AddFirst(node);
            else
                linkedGraph.AddAfter(targetNode, node);
        }
        protected GraphPathSimple TakeGraphNode() {
            LinkedListNode<GraphPathSimple> result = linkedGraph.First;
            linkedGraph.RemoveFirst();
            return result == null ? null : result.Value;
        }

        protected void ClearGraphList() {
            linkedGraph.Clear();
        }
        #endregion

        #region static
        protected static Vector2 ToVector2(Vector3 pos) {
            return new Vector2(pos.x, pos.z);
        }
        #endregion
    }
}
