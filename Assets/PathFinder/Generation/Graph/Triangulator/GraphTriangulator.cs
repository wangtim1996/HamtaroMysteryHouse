using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using K_PathFinder;

using K_PathFinder.EdgesNameSpace;
using K_PathFinder.GraphGeneration;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder.GraphGeneration {
    public class GraphTriangulator {
        List<TriangulatorDataSet> data = new List<TriangulatorDataSet>();
        //Graph graph;
        GraphCombiner combiner;

        //debug only but can be userful
        //Chunk chunk;
#if UNITY_EDITOR
        AgentProperties properties;
        XZPosInt pos;//for debug
#endif

        public NavmeshProfiler profiler;

        //public GraphTriangulator(GraphGenerator generator, NavMeshTemplateCreation template) {
        //    var volumes = generator.getVolumes;
        //    var nodes = generator.getNodes;

        //    //layer, hash
        //    Dictionary<int, Dictionary<int, List<NodeAbstract>>> dicNodes = new Dictionary<int, Dictionary<int, List<NodeAbstract>>>();
        //    Dictionary<int, Dictionary<int, List<EdgeAbstract>>> dicEdges = new Dictionary<int, Dictionary<int, List<EdgeAbstract>>>();
        //    Dictionary<int, Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>> dicValues = new Dictionary<int, Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>>();

        //    foreach (var volume in volumes) {
        //        dicNodes.Add(volume.id, new Dictionary<int, List<NodeAbstract>>());
        //        dicEdges.Add(volume.id, new Dictionary<int, List<EdgeAbstract>>());
        //        dicValues.Add(volume.id, new Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>());

        //        //some hardcoded stuff
        //        foreach (var a in volume.containsAreas) {
        //            int crouchHash = PathFinder.GetAreaHash(a, Passability.Crouchable);
        //            int walkHash = PathFinder.GetAreaHash(a, Passability.Walkable);

        //            dicNodes[volume.id].Add(crouchHash, new List<NodeAbstract>());
        //            dicEdges[volume.id].Add(crouchHash, new List<EdgeAbstract>());
        //            dicValues[volume.id].Add(crouchHash, new Dictionary<NodeAbstract, TriangulatorNodeData>());

        //            dicNodes[volume.id].Add(walkHash, new List<NodeAbstract>());
        //            dicEdges[volume.id].Add(walkHash, new List<EdgeAbstract>());
        //            dicValues[volume.id].Add(walkHash, new Dictionary<NodeAbstract, TriangulatorNodeData>());
        //        }
        //    }

        //    foreach (var first in nodes) {
        //        foreach (var pair in first.getData) {
        //            int volume = pair.Key.x;
        //            int hash = pair.Key.y;

        //            dicNodes[volume][hash].Add(first);
        //            dicEdges[volume][hash].Add(first[volume, hash]);

        //            NodeTemp middle = first.GetNode(volume, hash);
        //            NodeTemp last = middle.GetNode(volume, hash);

        //            float cross = SomeMath.V2Cross(
        //                last.x - middle.x, last.z - middle.z,
        //                first.x - middle.x, first.z - middle.z);

        //            if (cross < 0) {
        //                Vector2 directionLast = new Vector2(last.x - middle.x, last.z - middle.z).normalized;
        //                Vector2 directionFirst = new Vector2(first.x - middle.x, first.z - middle.z).normalized;
        //                dicValues[volume][hash].Add(middle, new TriangulatorNodeData(cross, Vector2.Angle(directionLast, directionFirst), (directionLast + directionFirst).normalized * -1));
        //            }
        //            else
        //                dicValues[volume][hash].Add(middle, new TriangulatorNodeData(cross, 0, Vector2.zero));
        //        }
        //    }

        //    foreach (var volume in volumes) {
        //        foreach (var a in volume.containsAreas) {
        //            int crouchHash = PathFinder.GetAreaHash(a, Passability.Crouchable);
        //            data.Add(new TriangulatorDataSet(
        //                template,
        //                dicNodes[volume.id][crouchHash],
        //                dicEdges[volume.id][crouchHash],
        //                dicValues[volume.id][crouchHash],
        //                volume.id, a, Passability.Crouchable));


        //            int walkHash = PathFinder.GetAreaHash(a, Passability.Walkable);
        //            data.Add(new TriangulatorDataSet(
        //                template,
        //                dicNodes[volume.id][walkHash],
        //                dicEdges[volume.id][walkHash],
        //                dicValues[volume.id][walkHash],
        //                volume.id, a, Passability.Walkable));
        //        }
        //    }

        //    //data.RemoveAll(x => x.nodes.Count == 0);
        //}

        public GraphTriangulator(GraphGeneratorNew generator, NavMeshTemplateCreation template) {
            int maxLayers = generator.volumeContainer.layersCount;

            //var volumes = generator.dataLayers;
            var nodes = generator.getNodes;

            //DataLayer[] dataLayers = generator.dataLayers;
            //int dataLayersLength = dataLayers.Length;

            profiler = template.profiler;

         

            //layer, hash
            Dictionary<int, List<NodeAbstract>>[] dicNodes = new Dictionary<int, List<NodeAbstract>>[maxLayers];
            Dictionary<int, List<EdgeAbstract>>[] dicEdges = new Dictionary<int, List<EdgeAbstract>>[maxLayers];
            Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>[] dicValues = new Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>[maxLayers];

            for (int id = 0; id < maxLayers; id++) {
                dicNodes[id] = new Dictionary<int, List<NodeAbstract>>();
                dicEdges[id] = new Dictionary<int, List<EdgeAbstract>>();
                dicValues[id] = new Dictionary<int, Dictionary<NodeAbstract, TriangulatorNodeData>>();

                //DataLayer layer = dataLayers[id];

                //some hardcoded stuff
                //foreach (var hash in layer.allAreaHashes) {
                //    dicNodes[id].Add(hash, new List<NodeAbstract>());
                //    dicEdges[id].Add(hash, new List<EdgeAbstract>());
                //    dicValues[id].Add(hash, new Dictionary<NodeAbstract, TriangulatorNodeData>());
                //}
            }
            if (profiler != null) profiler.AddLog("start preparing data in GraphTriangulator");
            foreach (var first in nodes) {
                foreach (var pair in first.getData) {
                    int layer = pair.Key.x;
                    int hash = pair.Key.y;

                    var curDicNodes = dicNodes[layer];
                    var curDicEdges = dicEdges[layer];
                    var curDicValues= dicValues[layer];


                    if (curDicNodes.ContainsKey(hash) == false) {
                        curDicNodes.Add(hash, new List<NodeAbstract>());
                        curDicEdges.Add(hash, new List<EdgeAbstract>());
                        curDicValues.Add(hash, new Dictionary<NodeAbstract, TriangulatorNodeData>());
                    }

                    curDicNodes[hash].Add(first);
                    curDicEdges[hash].Add(first[layer, hash]);

                    NodeTemp middle = first.GetNode(layer, hash);
                    NodeTemp last = middle.GetNode(layer, hash);

                    float cross = SomeMath.V2Cross(
                        last.x - middle.x, last.z - middle.z,
                        first.x - middle.x, first.z - middle.z);

                    if (cross < 0) {
                        Vector2 directionLast = new Vector2(last.x - middle.x, last.z - middle.z).normalized;
                        Vector2 directionFirst = new Vector2(first.x - middle.x, first.z - middle.z).normalized;
                        curDicValues[hash].Add(middle, new TriangulatorNodeData(cross, Vector2.Angle(directionLast, directionFirst), (directionLast + directionFirst).normalized * -1));
                    }
                    else
                        curDicValues[hash].Add(middle, new TriangulatorNodeData(cross, 0, Vector2.zero));
                }
            }


            for (int id = 0; id < maxLayers; id++) {
                foreach (var hash in dicNodes[id].Keys) {
                    Area area;
                    Passability pass;
                    template.hashData.GetAreaByHash((short)hash, out area, out pass);

                    data.Add(new TriangulatorDataSet(
                        template,
                        dicNodes[id][hash],
                        dicEdges[id][hash],
                        dicValues[id][hash],
                        id, area, pass));
                }
            }
            if (profiler != null) profiler.AddLog("end preparing data in GraphTriangulator");
            //data.RemoveAll(x => x.nodes.Count == 0);
        }

        public void Triangulate(XZPosInt pos, AgentProperties properties, ref GraphCombiner combiner, NavMeshTemplateCreation template) {
#if UNITY_EDITOR
            //for debug
            this.pos = pos;
            this.properties = properties;
#endif

            this.combiner = combiner;

            //Debug.Log(chunk == null);
            //Debug.Log(properties == null);
            if (profiler != null) profiler.AddLog("Start make connections");       
            MakeConnections(template);
            if (profiler != null) profiler.AddLog("End make connections");

#if UNITY_EDITOR
            if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false)
                DebugDataSets(pos, properties);
#endif
            if (profiler != null) profiler.AddLog("Start Generate Cells");
            GenerateCells();
            if (profiler != null) profiler.AddLog("End Generate Cells");
        }

        //first part search for nearest non obstructed node in scope of angle
        //next part search take nodes wich fail first part and search closest to normal angle left and right nodes
        //will probably fail
        private void MakeConnections(NavMeshTemplateCreation template) {
            if (profiler != null) profiler.AddLogFormat("data sets count {0}", data.Count);
            foreach (var dataSet in data) {
                if (profiler != null) profiler.AddLogFormat("current data nodes Length {0}", dataSet.nodes.Length);

                if (dataSet.nodes.Length == 0) //cause it's empty
                    continue;




       

                dataSet.MakeConnections(template);




                //edges are clockwise right now
                //TEMP STUFF
                //var curNodes = dataSet.nodes;
                //var curEdges = dataSet.edges;

                //int nodesLength = curNodes.Length;
                //bool[] marks = new bool[nodesLength];


                //TriangulatorEdge[] temdStuffEdge = new TriangulatorEdge[curNodes.Length];
                //for (int i = 0; i < curEdges.Count; i++) {
                //    temdStuffEdge[curEdges[i].a] = curEdges[i];
                //}

                ////for (int i = 0; i < dataSet.nodes.Length; i++) {
                ////    Debuger_K.AddLabel(dataSet.NodePosV3(dataSet.nodes[i].id), dataSet.nodes[i].id);
                ////}

                ////foreach (var edge in dataSet.edges) {
                ////    Vector3 a = dataSet.NodePosV3(edge.a);
                ////    Vector3 b = dataSet.NodePosV3(edge.b);
                ////    Debuger_K.AddLabel(SomeMath.MidPoint(a, b), string.Format("{0}, {1}", edge.a, edge.b));
                ////}

                //Debug.Log(nodesLength);
                //int curNode = 1;


                //for (int i = 0; i < 5; i++) {     
                //    int nextNode = temdStuffEdge[curNode].b;
                //    Debug.Log(temdStuffEdge[curNode].a + " : " + temdStuffEdge[curNode].b);

                //    Vector2 startPos = curNodes[curNode].positionV2;
                //    Vector2 curDir = curNodes[nextNode].positionV2 - curNodes[curNode].positionV2;

                //    for (int b = 0; b < 5; b++) {
                //        nextNode = temdStuffEdge[nextNode].b;

                //        if(!IsVisible(curNode, nextNode, dataSet)) {
                //            goto NEXT;
                //        }

                //        Vector2 nextPos = curNodes[nextNode].positionV2;
                //        Vector2 nextDir = nextPos - startPos;

                //        float cross = SomeMath.V2Cross(curDir, nextDir);
                //        if(cross <= 0) {
                //            goto NEXT;
                //        }

                //        curDir = nextDir;

                //        Debuger_K.AddLabel(curNodes[nextNode].positionV3, cross);
                //        Debuger_K.AddLine(curNodes[curNode].positionV3, curNodes[nextNode].positionV3, Color.red);
                //    }

                //    NEXT:{
                //        curNode = nextNode;
                //        continue;
                //    }
                //}
                //TEMP STUFF

                //dataSet.GenerateEdgeMap(10, template);
                //dataSet.CreateAngleVisibilityField();

                //TriangulatorNode[] nodes = dataSet.nodes;
                //TriangulatorNodeData[] nodeData = dataSet.data;
                //List<int> nextIterationNodes = new List<int>();

                //for (int curNodeIndex = 0; curNodeIndex < nodes.Length; curNodeIndex++) {
                //    if (nodeData[curNodeIndex].cross >= 0)
                //        continue;

                //    var curVisible = dataSet.AngleVisibilityField(curNodeIndex);

                //    for (int i = 0; i < curVisible.Length; i++) {
                //        if (IsVisible(curNodeIndex, curVisible[i], dataSet)) {
                //            dataSet.AddEdge(curNodeIndex, curVisible[i], 0);
                //            goto NEXT;
                //        }
                //    }

                //    nextIterationNodes.Add(curNodeIndex);
                //    NEXT: { continue; }
                //}

                //List<T2Helper> helpers = new List<T2Helper>();
                //foreach (var nodeIndex in nextIterationNodes) {
                //    Vector2 nodePos = nodes[nodeIndex].positionV2;
                //    Vector2 normal = nodeData[nodeIndex].normal;
                //    float validAngle = 180f - (nodeData[nodeIndex].angle * 0.5f);

                //    helpers.Clear();
                //    foreach (var targetNode in Array.FindAll(nodes, x => Vector2.Angle(normal, x.positionV2 - nodePos) < validAngle)) {
                //        if (nodeIndex == targetNode.id)
                //            continue;

                //        Vector2 targetNodeDirection = (targetNode.positionV2 - nodePos).normalized;
                //        helpers.Add(new T2Helper(targetNode.id, Vector2.Angle(normal, targetNodeDirection) * Mathf.Sign(SomeMath.V2Cross(normal, targetNodeDirection))));
                //    }

                //    helpers.Sort((x, y) => { return (int)Mathf.Sign(Math.Abs(x.angle) - Math.Abs(y.angle)); });

                //    for (int i = 0; i < helpers.Count; i++) {
                //        if (helpers[i].angle < 0 && IsVisible(nodeIndex, helpers[i].node, dataSet)) {
                //            dataSet.AddEdge(nodeIndex, helpers[i].node, 0);
                //            break;
                //        }
                //    }

                //    for (int i = 0; i < helpers.Count; i++) {
                //        if (helpers[i].angle > 0 && IsVisible(nodeIndex, helpers[i].node, dataSet)) {
                //            dataSet.AddEdge(nodeIndex, helpers[i].node, 0);
                //            break;
                //        }
                //    }
                //}
            }
        }

#if UNITY_EDITOR
        public void DebugDataSets(XZPosInt pos, AgentProperties properties) {
            foreach (var dataSet in data)
                dataSet.DebugIt(pos, properties);
        }
#endif

        private void GenerateCells() {
            foreach (var dataSet in data) {
                TriangulatorNode[] nodes = dataSet.nodes;
                TriangulatorNodeData[] nodeData = dataSet.data;
                List<TriangulatorEdge> edges = dataSet.edges;
                List<int> edgesUsage = dataSet.edgesUsage;

                List<TriangulatorEdge>[] edgesDictionary = new List<TriangulatorEdge>[nodes.Length];//index are node index to know 
                for (int i = 0; i < nodes.Length; i++) {
                    edgesDictionary[i] = new List<TriangulatorEdge>();
                }

                foreach (var item in edges) {
                    edgesDictionary[item.a].Add(item);
                    edgesDictionary[item.b].Add(item);
                }

                //first iteration. we know if edge used once then it's directional and node "a" is left
                for (int i = 0; i < edgesUsage.Count; i++) {
                    if (edgesUsage[i] != 0)
                        continue;

                    TriangulatorEdge curentEdge = edges[i];

                    List<int> cellNodes;
                    List<TriangulatorEdge> cellEdges;

                    MakeCell(curentEdge, true, edgesDictionary, nodes, out cellNodes, out cellEdges);
                    foreach (var cellEdge in cellEdges) {
                        edgesUsage[cellEdge.id]++;
                    }

                    List<Vector3> cellNodePositions = new List<Vector3>();
                    foreach (var item in cellNodes) {
                        cellNodePositions.Add(nodes[item].positionV3);
                    }
                    combiner.AddCell(cellNodePositions, dataSet.area, dataSet.passability, dataSet.layer);
                }

                //yet again remain only with 1 usage edges, border edges are no more. next cell we dont know if it a left or right so we try bouth
                for (int i = 0; i < edgesUsage.Count; i++) {
                    if (edgesUsage[i] != 1)
                        continue;

                    TriangulatorEdge curentEdge = edges[i];

                    List<int> cellNodes;
                    List<TriangulatorEdge> cellEdges;

                    MakeCell(curentEdge, true, edgesDictionary, nodes, out cellNodes, out cellEdges);
                    foreach (var cellEdge in cellEdges) {
                        if (edgesUsage[cellEdge.id] > 1) {//mean this cell already was created
                            MakeCell(curentEdge, false, edgesDictionary, nodes, out cellNodes, out cellEdges);//key change here is false. mean left is node "b"
                            break;
                        }
                    }

                    foreach (var cellEdge in cellEdges) {
                        edgesUsage[cellEdge.id]++;
                    }

                    List<Vector3> cellNodePositions = new List<Vector3>();
                    foreach (var item in cellNodes) {
                        cellNodePositions.Add(nodes[item].positionV3);
                    }
                    combiner.AddCell(cellNodePositions, dataSet.area, dataSet.passability, dataSet.layer);
                }
            }
        }

        int harhar = 0;
        /// <summary>
        /// return used edges, 
        /// </summary>
        private void MakeCell(TriangulatorEdge target, bool aFirst, List<TriangulatorEdge>[] edgesDictionary, TriangulatorNode[] nodes, out List<int> cellNodes, out List<TriangulatorEdge> cellEdges) {
            cellNodes = new List<int>();
            cellEdges = new List<TriangulatorEdge>();
            cellEdges.Add(target);
            int startNode;

            if (aFirst) {//a are origin
                startNode = target.a;
                cellNodes.Add(target.a);
                cellNodes.Add(target.b);
            }
            else {
                startNode = target.b;
                cellNodes.Add(target.b);
                cellNodes.Add(target.a);
            }

            int limit = 0;
            harhar++;

            while (true) {
                limit++;
                if (limit > 50) {
#if UNITY_EDITOR
                    if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
                        for (int i = 0; i < cellNodes.Count - 1; i++) {
                            Vector3 a1 = nodes[cellNodes[i]].positionV3 + (Vector3.up * 0.02f * i);
                            Vector3 a2 = nodes[cellNodes[i + 1]].positionV3 + (Vector3.up * 0.02f * i);

                            Debuger_K.AddTriangulatorDebugLine(pos.x, pos.z, properties, a1, a2, Color.red);
                            //Debuger_K.AddTriangulatorDebugLabel(chunk, properties, SomeMath.MidPoint(a1, a2), i);
                        }
                    }
#endif
                    Debug.LogError("error while making cells " + harhar);
                    break;
                }

                int nodeMinus = cellNodes[cellNodes.Count - 2];
                int nodeCurrent = cellNodes[cellNodes.Count - 1];
                int? nodePlus = null;
                TriangulatorEdge? connectionPlus = null;

                Vector2 directionToMinus = (nodes[nodeMinus].positionV2 - nodes[nodeCurrent].positionV2).normalized;
                float lowestAngle = 180f;
                List<TriangulatorEdge> searchConnections = edgesDictionary[nodeCurrent];

                foreach (var connection in searchConnections) {
                    int potentialNodePlus = connection.GetOtherNode(nodeCurrent);

                    if (nodeMinus == potentialNodePlus)
                        continue;

                    Vector2 directionToPotentialPlus = (nodes[potentialNodePlus].positionV2 - nodes[nodeCurrent].positionV2).normalized;
                    float cross = SomeMath.V2Cross(directionToMinus, directionToPotentialPlus);
                    float currentAngle = Vector2.Angle(directionToMinus, directionToPotentialPlus);

                    if (cross > 0f & currentAngle != 180)
                        continue;

                    if (currentAngle > lowestAngle)
                        continue;

                    connectionPlus = connection;
                    nodePlus = potentialNodePlus;
                    lowestAngle = currentAngle;
                }

                if (nodePlus == null) {
                    #region error
#if UNITY_EDITOR
                    if (Debuger_K.doDebug) {
                        Debuger_K.AddTriangulatorDebugLine(pos.x, pos.z, properties, nodes[cellNodes[0]].positionV3, nodes[cellNodes[0]].positionV3 + SmallV3(0.3f), Color.green);

                        for (int i = 0; i < cellNodes.Count - 1; i++) {
                            Debuger_K.AddTriangulatorDebugLine(pos.x, pos.z, properties, nodes[cellNodes[i]].positionV3 + SmallV3(0.1f), nodes[cellNodes[i + 1]].positionV3 + SmallV3(0.1f), Color.red);
                        }
                        for (int i = 0; i < cellNodes.Count - 1; i++) {
                            Vector3 a1 = nodes[cellNodes[i]].positionV3 + SmallV3(0.02f * i);
                            Vector3 a2 = nodes[cellNodes[i + 1]].positionV3 + SmallV3(0.02f * i);
                            Debuger_K.AddTriangulatorDebugLine(pos.x, pos.z, properties, a1, a2, Color.red);
                        }

                        foreach (var connection in searchConnections) {
                            int potentialNodePlus = connection.GetOtherNode(nodeCurrent);

                            if (nodeMinus == potentialNodePlus)
                                continue;

                            Vector2 directionToPotentialPlus = (nodes[potentialNodePlus].positionV2 - nodes[nodeCurrent].positionV2).normalized;

                            float cross = SomeMath.V2Cross(directionToMinus, directionToPotentialPlus);

                            Debuger_K.AddLabel(nodes[potentialNodePlus].positionV3, cross);
                            Debuger_K.AddLabel(nodes[nodeCurrent].positionV3, Vector2.Angle(directionToMinus, directionToPotentialPlus));
                            //Debuger_K.AddTriangulatorDebugLabel(chunk, properties, nodes[potentialNodePlus].positionV3, cross);
                            //Debuger_K.AddTriangulatorDebugLabel(chunk, properties, nodes[nodeCurrent].positionV3, Vector2.Angle(directionToMinus, directionToPotentialPlus));
                        }
                    }
#endif
                    #endregion
                    Debug.LogError("nodePlus == null");
                    break;
                }

                cellEdges.Add(connectionPlus.Value);

                if (nodePlus == startNode)
                    break;

                cellNodes.Add(nodePlus.Value);
            }
        }
        private Vector3 SmallV3(float val) {
            return Vector3.up * val;
        }

        private struct T2Helper {
            public readonly int node;
            public readonly float angle;

            public T2Helper(int node, float angle) {
                this.node = node;
                this.angle = angle;
            }
        }

        private bool IsVisible(int a, int b, TriangulatorDataSet ds) {
            Vector2 aPos = ds.NodePosV2(a);
            Vector2 bPos = ds.NodePosV2(b);
            Vector2 dir = bPos - aPos;
            float curSqrDist = SomeMath.SqrDistance(aPos, bPos);

            foreach (var edge in ds.edges) {
                if (edge.Contains(a, b))
                    continue;

                Vector2 intersection;
                if (SomeMath.RayIntersectXZ(
                    aPos, dir, //from, direction
                    ds.NodePosV2(edge.a), ds.NodePosV2(edge.b), //a, b
                    out intersection)
                    && SomeMath.SqrDistance(aPos, intersection) < curSqrDist) {

                    //Debuger3.AddRay(new Vector3(intersection.x, 0, intersection.y), Vector3.up, Color.magenta);
                    //Debuger3.AddLine(ds.NodePosV3(a), new Vector3(intersection.x, 0, intersection.y), Color.magenta);
                    //Debuger3.AddLine(ds.NodePosV3(b), new Vector3(intersection.x, 0, intersection.y), Color.magenta);
                    return false;
                }
            }

            return true;
        }
    }
}