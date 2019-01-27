using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using K_PathFinder.VectorInt ;

using System;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder;
using K_PathFinder.CoverNamespace;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

////FIX: RPD do again shit with indexes

//namespace K_PathFinder.GraphGeneration {    
//    public class GraphGenerator {
//        private NavMeshTemplateCreation template;
//        private VolumeContainer volumeContainer;

//        private int nodesSizeX, nodesSizeZ;
//        private List<NodeTemp>[][] nodes; //x, z, y_list
//        //private List<NodeTemp> nodesAll = new List<NodeTemp>();

//        private HashSet<NodeTemp> RDPkeypoints = new HashSet<NodeTemp>();
//        private Vector3 chunkRealPosition;

//        private float fragmentSize, fragmentStep;
//        private Dictionary<Vector3, NodeCoverTemp> _coverDictionary = new Dictionary<Vector3, NodeCoverTemp>();
//        private List<VolumeArea> volumeAreas;

//        //some temp values
//        List<NodeTemp> tempNodes = new List<NodeTemp>();
//        List<EdgeTemp> tempEdges = new List<EdgeTemp>();
//        HashSet<VolumeArea> startArea = new HashSet<VolumeArea>();
//        HashSet<VolumeArea> collectedArea = new HashSet<VolumeArea>();

//        //fix ramer douglas peuker temp values
//        List<VectorInt.Vector2Int> removeConnectionsListOne = new List<VectorInt.Vector2Int>();
//        List<VectorInt.Vector2Int> removeConnectionsListTwo = new List<VectorInt.Vector2Int>();
//        HashSet<NodeTemp> removeList = new HashSet<NodeTemp>();

//        NavmeshProfiler profiler;

//        public GraphGenerator(VolumeContainer volumeContainer, NavMeshTemplateCreation template) {
//            this.volumeContainer = volumeContainer;
//            this.template = template;
//            this.profiler = template.profiler;

//            //areas = volumeContainer.areas;
//            chunkRealPosition = new Vector3(template.chunkData.realX, 0, template.chunkData.realZ);

//            fragmentSize = template.voxelSize;
//            fragmentStep = template.properties.maxStepHeight;

//            nodesSizeX = template.lengthX_extra * 2 + 1;
//            nodesSizeZ = template.lengthZ_extra * 2 + 1;
//            nodes = new List<NodeTemp>[nodesSizeX][];

//            for (int i = 0; i < nodesSizeX; i++)
//                nodes[i] = new List<NodeTemp>[nodesSizeZ];

//            volumeAreas = volumeContainer.volumeAreas;
//        }

//        public Graph MakeGraph() {
//            Graph graph = new Graph(template.chunkData, template.properties);
//            //nothing to do just return empty graph
//            if (volumeContainer.volumesAmount == 0)
//                return graph;

//            //there is reason why it here. it's create captured areas in process so it always must be before next part
//            if (template.doCover) {
//                if (profiler != null) profiler.AddLog("agent can cover. start making cover graph");

//                foreach (var volume in volumeContainer.volumes)
//                    SetCoverGraph(volume);

//                if (profiler != null) profiler.AddLog("end making cover graph. start Ramer–Douglas–Peucker");
//                RamerDouglasPeuckerCover(template.voxelSize * 2, template.voxelSize * 0.5f);
//                if (profiler != null) profiler.AddLogFormat("end Ramer–Douglas–Peucker. cover points: {0}", _coverDictionary.Count);

//                if (_coverDictionary.Count > 0) {
//                    if (profiler != null) profiler.AddLog("start setuping cover points");
//                    foreach (var cover in _coverDictionary.Values) {
//                        if (cover.connection != null)
//                            SetupCoverPoints(cover);
//                    }
//                    if (profiler != null) profiler.AddLog("end setuping cover points");
//                }
//            }
              
//            if (template.doNavMesh) {
//                //here is marching squares used   
//                if (profiler != null) profiler.AddLog("creating contour for NavMesh");
//                foreach (var volume in volumeContainer.volumes) {
//                    foreach (var area in volume.containsAreas) {
//                        SetGraph(volume, area, Passability.Walkable);
//                        if (template.canCrouch)
//                            SetGraph(volume, area, Passability.Crouchable);
//                    }
//                }
//                if (profiler != null) profiler.AddLog("end creating contour");

//                //add border flags, shift near border to border nodes and insert corner nodes
//                if (profiler != null) profiler.AddLog("start moving contour to  chunk border");
//                ProcessFlagsAndBordersForRDP();
//                if (profiler != null) profiler.AddLog("end moving contour to  chunk border");

//#if UNITY_EDITOR
//                if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
//                    if (profiler != null) profiler.AddLog("debug raw nodes");                    
//                    Debuger_K.AddNodesTempPreRDP(template.gridPosX, template.gridPosZ, template.properties, GetAllNodes());
//                    if (profiler != null) profiler.AddLog("end debug raw nodes");
//                }
//#endif

//                //Ramer-Douglas-Peucker
//                if (profiler != null) profiler.AddLog("start Ramer–Douglas–Peucker");
//                RamerDouglasPeuckerHull(template.voxelSize * 0.70f);
//                if (profiler != null) profiler.AddLog("end Ramer–Douglas–Peucker");

//#if UNITY_EDITOR
//                if (Debuger_K.doDebug && Debuger_K.debugOnlyNavMesh == false) {
//                    if (profiler != null) profiler.AddLog("debug nodes");
//                    Debuger_K.AddNodesTemp(template.gridPosX, template.gridPosZ, template.properties, GetAllNodes());
//                    if (profiler != null) profiler.AddLog("end debug nodes");
//                }
//#endif
//            }

//            //remove unused areas
//            if (template.canJump | template.doCover) {
//                for (int i = volumeAreas.Count - 1; i >= 0; i--) {
//                    if (volumeAreas[i].edges.Count() == 0)
//                        volumeAreas.RemoveAt(i);
//                }
//            }

//            if (template.doNavMesh) {
//                if (profiler != null) profiler.AddLog("start triangulating");
//                GraphTriangulator triangulator = new GraphTriangulator(this, template);
//                GraphCombiner combiner = new GraphCombiner(template, graph);
//                triangulator.Triangulate(template.gridPosition, template.properties, ref combiner, template);
//                if (profiler != null) profiler.AddLog("end triangulating");
                
//                ////////////
//                if (profiler != null) profiler.AddLog("start reducing cell count");
//                combiner.ReduceCells();
//                if (profiler != null) profiler.AddLog("end reducing cell count");
//                ////////////
//                if (profiler != null) profiler.AddLog("start combine graph");
//                combiner.CombineGraph();
//                if (profiler != null) profiler.AddLog("end combine graph");
//                ////////////

//                //if (profiler != null) profiler.AddLog("start reducing cell count");
//                //graph.MergeCells();//trying to reduce amount of cells
//                //if (profiler != null) profiler.AddLog("end reducing cell count");

//                if (profiler != null) profiler.AddLog("start setting graph edges");
//                foreach (var node in GetAllNodes()) { //get all cause not much nodes remain
//                    if (node.border == false)
//                        continue;

//                    foreach (var edge in node.getEdges) {
//                        var nextNode = edge.connection;
//                        if (node.GetFlag(NodeTempFlags.xMinusBorder) & nextNode.GetFlag(NodeTempFlags.xMinusBorder))
//                            graph.SetEdgeSide(new CellContentData(edge.aPositionV3, edge.bPositionV3), Directions.xMinus);
//                        else if (node.GetFlag(NodeTempFlags.xPlusBorder) & nextNode.GetFlag(NodeTempFlags.xPlusBorder))
//                            graph.SetEdgeSide(new CellContentData(edge.aPositionV3, edge.bPositionV3), Directions.xPlus);
//                        else if (node.GetFlag(NodeTempFlags.zMinusBorder) & nextNode.GetFlag(NodeTempFlags.zMinusBorder))
//                            graph.SetEdgeSide(new CellContentData(edge.aPositionV3, edge.bPositionV3), Directions.zMinus);
//                        else if (node.GetFlag(NodeTempFlags.zPlusBorder) & nextNode.GetFlag(NodeTempFlags.zPlusBorder))
//                            graph.SetEdgeSide(new CellContentData(edge.aPositionV3, edge.bPositionV3), Directions.zPlus);
//                    }
//                }
//                if (profiler != null) profiler.AddLog("end setting graph edges");

//                if (template.canJump) {
//                    if (profiler != null) profiler.AddLog("start adding jump portals to graph");
//                    foreach (var area in volumeAreas) {
//                        if (area.areaType == AreaType.Jump)
//                            graph.AddPortal(area.edges, area.position);
//                    }
//                    if (profiler != null) profiler.AddLog("end adding jump portals");
//                }
//            }

//            if (template.doCover) {
//                if (profiler != null) profiler.AddLog("start adding covers to graph");
//                foreach (var node in _coverDictionary.Values) {
//                    if (node.connection != null) {
//                        graph.AddCover(node);
//                    }
//                }
//                if (profiler != null) profiler.AddLog("end adding covers");
//            }

//            graph.battleGrid = volumeContainer.battleGrid;
//            return graph;
//        }


//        //cause search in all nodes are expensive i combine all nodes into list end return it when it's really needed
//        private List<NodeTemp> GetAllNodes() {
//            List<NodeTemp> result = new List<NodeTemp>();
//            for (int x = 0; x < nodesSizeX; x++) {
//                for (int z = 0; z < nodesSizeZ; z++) {
//                    if(nodes[x][z] != null)
//                        result.AddRange(nodes[x][z]);  
//                }
//            }
//            return result;
//        }

//        //marching squares usage
//        private void SetGraph(Volume volume, Area targetArea, Passability passability) {
//            int targetHash = template.hashData.GetAreaHash(targetArea, passability); 

//            int start = template.extraOffset;
//            int endX = start + template.lengthX_central;
//            int endZ = start + template.lengthZ_central;

//            int curID = volume.id;

//            int[] hashMap = volume.hashMap;
//            VolumeDataPoint[] data = volume.data;

//            int breaker = (template.lengthX_extra + template.lengthZ_extra) * 2 * 10; //some big enough number

//            for (int x = start; x < endX; x++) {
//                for (int z = start; z < endZ; z++) {
//                    int index = volume.GetIndex(x, z);
//                    if (data[index].exist && //skip if not exist
//                        hashMap[index] == targetHash && //only target hash can be start point
//                        volume.GetState(x, z, VoxelState.MarchingSquareArea) == false && //if we already was here then skip
//                        (hashMap[volume.GetIndex(x + 1, z)] != targetHash | hashMap[volume.GetIndex(x, z + 1)] != targetHash)) { //start iteration if some of plus voxels are not equal to target

//                        MarchingSquaresIterator iterator = new MarchingSquaresIterator(volume, x, z, targetHash, MarchingSquaresIteratorMode.area);

//                        Vector2Int1Float startPosition;

//                        startArea.Clear();
//                        iterator.GetExitVector(out startPosition, ref startArea);

//                        int startX = startPosition.x;
//                        int startZ = startPosition.z;

//                        Vector2Int1Float curPosition;
//                        Vector2Int1Float lastAdded = startPosition;            

//                        int loopBreak = 0;
//                        while (true) {
//                            loopBreak++;
//                            if (loopBreak > breaker) {
//                                Debug.LogError("to long line. probably loop");
//                                break;
//                            }

//                            if (iterator.Iterate() == false) {
//                                Debug.LogError("fail to itterate in MarchingSquaresIterator");
//                                break;
//                            }

//                            //use ref to pass out hashset and just clear it before iteration and that will save some GC time
//                            collectedArea.Clear();
//                            iterator.GetExitVector(out curPosition, ref collectedArea);

//                            if (startX == curPosition.x & startZ == curPosition.z)
//                                break;

//                            SetEdge(lastAdded, curPosition, curID, targetHash, collectedArea);
//                            lastAdded = curPosition;
//                        }
//                        SetEdge(lastAdded, startPosition, curID, targetHash, startArea);
//                    }   
//                }
//            }
//        }
                
//        //works just as function above. just use less intresting data and use diferent function to output information
//        public void SetCoverGraph(Volume volume) {
//            int targetHash = MarchingSquaresIterator.COVER_HASH;

//            int start = template.extraOffset;
//            int endX = template.lengthX_extra - template.extraOffset - 1;
//            int endZ = template.lengthZ_extra - template.extraOffset - 1;

//            int[] coverHashMap = volume.coverHashMap;
//            VolumeDataPoint[] data = volume.data;
            
//            int breaker = (template.lengthX_extra + template.lengthZ_extra) * 2 * 10; //some big enough number

//            for (int x = start; x < endX; x++) {
//                for (int z = start; z < endZ; z++) {
//                    int index = volume.GetIndex(x, z);
//                    if (data[index].exist && //skip if not exist
//                        coverHashMap[index] == targetHash && //only target hash can be start point
//                        volume.GetState(x, z, VoxelState.MarchingSquareArea) == false && //if we already was here then skip
//                        (coverHashMap[volume.GetIndex(x + 1, z)] != targetHash | coverHashMap[volume.GetIndex(x, z + 1)] != targetHash)) { //start iteration if some of plus voxels are not equal to target

//                        MarchingSquaresIterator iterator = new MarchingSquaresIterator(volume, x, z, targetHash, MarchingSquaresIteratorMode.cover);

//                        int startCover, curCover;
//                        Vector2Int1Float startPosition, curPosition;
//                        iterator.GetExitVectorCover(out startPosition, out startCover);

//                        int startX = startPosition.x;
//                        int startZ = startPosition.z;

//                        Vector2Int1Float lastAdded = startPosition;

//                        int loopBreak = 0;
//                        while (true) {
//                            loopBreak++;
//                            if (loopBreak > breaker) {
//                                Debug.LogError("to long loop");
//                                break;
//                            }

//                            if (iterator.Iterate() == false) {
//                                Debug.LogError("fail to itterate in MarchingSquaresIterator");
//                                break;
//                            }

//                            iterator.GetExitVectorCover(out curPosition, out curCover);

//                            if (startX == curPosition.x & startZ == curPosition.z)
//                                break;

//                            SetCoverEdge(lastAdded, curPosition, curCover);
//                            lastAdded = curPosition;
//                        }
//                        SetCoverEdge(lastAdded, startPosition, startCover);
//                    }
//                }
//            }
//        }
        
//        private void ProcessFlagsAndBordersForRDP() {
//            //welp
//            //i hate do manual indexes but since all grid are shifted there is only one way

//            int borderStart = template.extraOffset * 2;
 
//            int borderEndX = nodesSizeX - borderStart - 1; 
//            int borderEndZ = nodesSizeZ - borderStart - 1;    

//            //first row
//            for (int x = borderStart; x < borderEndX; x++) {
//                if (nodes[x][borderStart] != null)
//                    nodes[x][borderStart].ForEach(node => node.SetFlag(NodeTempFlags.zMinusBorder, true));
//                if (nodes[x][borderEndZ] != null)
//                    nodes[x][borderEndZ].ForEach(node => node.SetFlag(NodeTempFlags.zPlusBorder, true));
//            }

//            for (int z = borderStart; z < borderEndZ; z++) {
//                if (nodes[borderStart][z] != null)
//                    nodes[borderStart][z].ForEach(node => node.SetFlag(NodeTempFlags.xMinusBorder, true));

//                if (nodes[borderEndX][z] != null)
//                    nodes[borderEndX][z].ForEach(node => node.SetFlag(NodeTempFlags.xPlusBorder, true));
//            }

//            //second row
//            //also it move second row to border since it cant be possible to have there another list on that position
//            for (int x = borderStart + 1; x < borderEndX - 1; x++) {
//                if(nodes[x][borderStart + 1] != null) {
//                    Vector3 newPos = chunkRealPosition + NodeLocalPosition(new Vector2Int1Float(x, 0, borderStart));//with 0 height
//                    foreach (var node in nodes[x][borderStart + 1]) {
//                        node.SetFlag(NodeTempFlags.zMinusBorder, true);
//                        node.SetPosition(x, borderStart, newPos + new Vector3(0, node.y, 0));
//                    }
//                    nodes[x][borderStart] = nodes[x][borderStart + 1];
//                    nodes[x][borderStart + 1] = null;
//                }

//                if (nodes[x][borderEndZ - 1] != null) {
//                    Vector3 newPos = chunkRealPosition + NodeLocalPosition(new Vector2Int1Float(x, 0, borderEndZ));//with 0 height
//                    foreach (var node in nodes[x][borderEndZ - 1]) {
//                        node.SetFlag(NodeTempFlags.zPlusBorder, true);
//                        node.SetPosition(x, borderEndZ, newPos + new Vector3(0, node.y, 0));
//                    }
//                    nodes[x][borderEndZ] = nodes[x][borderEndZ - 1];
//                    nodes[x][borderEndZ - 1] = null;
//                }
//            }

//            for (int z = borderStart + 1; z < borderEndZ - 1; z++) {
//                if (nodes[borderStart + 1][z] != null) {
//                    Vector3 newPos = chunkRealPosition + NodeLocalPosition(new Vector2Int1Float(borderStart, 0, z));//with 0 height
//                    foreach (var node in nodes[borderStart + 1][z]) {
//                        node.SetFlag(NodeTempFlags.xMinusBorder, true);
//                        node.SetPosition(borderStart, z, newPos + new Vector3(0, node.y, 0));
//                    }
//                    nodes[borderStart][z] = nodes[borderStart + 1][z];
//                    nodes[borderStart + 1][z] = null;
//                }

//                if (nodes[borderEndX - 1][z] != null) {
//                    Vector3 newPos = chunkRealPosition + NodeLocalPosition(new Vector2Int1Float(borderEndX, 0, z));//with 0 height
//                    foreach (var node in nodes[borderEndX - 1][z]) {
//                        node.SetFlag(NodeTempFlags.xPlusBorder, true);
//                        node.SetPosition(borderEndX, z, newPos + new Vector3(0, node.y, 0));
//                    }
//                    nodes[borderEndX][z] = nodes[borderEndX - 1][z];
//                    nodes[borderEndX - 1][z] = null;
//                }
//            }

//            Dictionary<NodeTemp, HashSet<NodeTemp>> nodeConnections = new Dictionary<NodeTemp, HashSet<NodeTemp>>();
//            HashSet<NodeTemp> nearBorder = new HashSet<NodeTemp>();

//            var allNodes = GetAllNodes();

//            foreach (var node in allNodes)
//                nodeConnections.Add(node, new HashSet<NodeTemp>());

//            foreach (var node in allNodes) {            
//                foreach (var edge in node.getEdges) {
//                    NodeTemp neighbour = edge.connection;

//                    if (node.border | neighbour.border) {
//                        if (neighbour.border && !node.border)
//                            nearBorder.Add(neighbour);
//                        else if (node.border && !neighbour.border)
//                            nearBorder.Add(node);
//                        else {
//                            if (
//                                node.GetFlag(NodeTempFlags.zPlusBorder) & neighbour.GetFlag(NodeTempFlags.xMinusBorder) |
//                                node.GetFlag(NodeTempFlags.xPlusBorder) & neighbour.GetFlag(NodeTempFlags.zPlusBorder) |
//                                node.GetFlag(NodeTempFlags.zMinusBorder) & neighbour.GetFlag(NodeTempFlags.xPlusBorder) |
//                                node.GetFlag(NodeTempFlags.xMinusBorder) & neighbour.GetFlag(NodeTempFlags.zMinusBorder)) {
//                                nearBorder.Add(node);
//                                nearBorder.Add(neighbour);
//                            }
//                        }
//                    }

//                    nodeConnections[node].Add(neighbour);
//                    nodeConnections[neighbour].Add(node);
//                }
//            }


//            var intersections = from pair in nodeConnections where pair.Value.Count > 2 select pair.Key;
//            foreach (var item in intersections) {
//                RDPkeypoints.Add(item);
//                item.SetFlag(NodeTempFlags.keyMarker, true);
//                item.SetFlag(NodeTempFlags.Intersection, true);
//            }

//            foreach (var item in nearBorder) {
//                RDPkeypoints.Add(item);
//                item.SetFlag(NodeTempFlags.keyMarker, true);
//                item.SetFlag(NodeTempFlags.nearBorder, true);

//            }

//            //corners
//            //X-, Z+
//            if (nodes[borderStart][borderEndZ - 1] != null) {
//                foreach (var node in nodes[borderStart][borderEndZ - 1]) {
//                    var nextNode = node.getEdges.First().connection;//since all connections on corners are clocwise we can take any edge
//                    NodeTemp cornerNode = Get(new Vector2Int1Float(borderStart, (node.y + nextNode.y) * 0.5f, borderEndZ));
//                    cornerNode.SetFlag(NodeTempFlags.xMinusBorder, true);
//                    cornerNode.SetFlag(NodeTempFlags.zPlusBorder, true);
//                    InsertNodeBetween(node, nextNode, cornerNode);
//                    RDPkeypoints.Add(cornerNode);
//                    cornerNode.SetFlag(NodeTempFlags.keyMarker, true);
//                }
//            }

//            //Z +, X +
//            if (nodes[borderEndX - 1][borderEndZ] != null) {
//                foreach (var node in nodes[borderEndX - 1][borderEndZ]) {
//                    var nextNode = node.getEdges.First().connection;//since all connections on corners are clocwise we can take any edge
//                    NodeTemp cornerNode = Get(new Vector2Int1Float(borderEndX, (node.y + nextNode.y) * 0.5f, borderEndZ));
//                    cornerNode.SetFlag(NodeTempFlags.zPlusBorder, true);
//                    cornerNode.SetFlag(NodeTempFlags.xPlusBorder, true);
//                    InsertNodeBetween(node, nextNode, cornerNode);
//                    RDPkeypoints.Add(cornerNode);
//                    cornerNode.SetFlag(NodeTempFlags.keyMarker, true);
//                }
//            }


//            //Z-, X+
//            if (nodes[borderEndX][borderStart + 1] != null) {
//                foreach (var node in nodes[borderEndX][borderStart + 1]) {
//                    var nextNode = node.getEdges.First().connection;//since all connections on corners are clocwise we can take any edge
//                    NodeTemp cornerNode = Get(new Vector2Int1Float(borderEndX, (node.y + nextNode.y) * 0.5f, borderStart));
//                    cornerNode.SetFlag(NodeTempFlags.zMinusBorder, true);
//                    cornerNode.SetFlag(NodeTempFlags.xPlusBorder, true);
//                    InsertNodeBetween(node, nextNode, cornerNode);
//                    RDPkeypoints.Add(cornerNode);
//                    cornerNode.SetFlag(NodeTempFlags.keyMarker, true);
//                }
//            }

//            //Z-, X-
//            if (nodes[borderStart + 1][borderStart] != null) {
//                foreach (var node in nodes[borderStart + 1][borderStart]) {
//                    var nextNode = node.getEdges.First().connection;//since all connections on corners are clocwise we can take any edge
//                    NodeTemp cornerNode = Get(new Vector2Int1Float(borderStart, (node.y + nextNode.y) * 0.5f, borderStart));
//                    cornerNode.SetFlag(NodeTempFlags.xMinusBorder, true);
//                    cornerNode.SetFlag(NodeTempFlags.zMinusBorder, true);
//                    InsertNodeBetween(node, nextNode, cornerNode);
//                    RDPkeypoints.Add(cornerNode);
//                    cornerNode.SetFlag(NodeTempFlags.keyMarker, true);
//                }
//            }
//        }

//        #region manage
//        //nodes
//        private NodeTemp Get(Vector2Int1Float pos) {
//            if (nodes[pos.x][pos.z] == null)
//                nodes[pos.x][pos.z] = new List<NodeTemp>();

//            foreach (var node in nodes[pos.x][pos.z])
//                if (Mathf.Abs(node.y - pos.y) <= fragmentStep) //return value within step
//                    return node;

//            NodeTemp newNode = new NodeTemp(chunkRealPosition + NodeLocalPosition(pos), pos.x, pos.z);
//            nodes[pos.x][pos.z].Add(newNode);   
//            return newNode;
//        }

//        //this became nearly unredable but reason is voxel map are need to have +2 indexes to cover marching squares result
//        private Vector3 NodeLocalPosition(Vector2Int1Float pos) {
//            return new Vector3(
//                (pos.x * 0.5f * fragmentSize) - (fragmentSize * template.extraOffset),
//                pos.y,
//                (pos.z * 0.5f * fragmentSize) - (fragmentSize * template.extraOffset));
//        }

//        private void InsertNodeBetween(NodeTemp A, NodeTemp B, NodeTemp insertedNode) {
//            List<EdgeTemp> AB = A.GetConnectionsToNode(B);
//            List<EdgeTemp> BA = B.GetConnectionsToNode(A);
//            AB.ForEach(x => SetEdge(A, insertedNode, x.volume, x.hash));
//            AB.ForEach(x => SetEdge(insertedNode, B, x.volume, x.hash));

//            BA.ForEach(x => SetEdge(B, insertedNode, x.volume, x.hash));
//            BA.ForEach(x => SetEdge(insertedNode, A, x.volume, x.hash));
//        }
     
//        private void Remove(NodeTemp node) {
//            if (nodes[node.mapX][node.mapZ].Remove(node) == false)
//                Debug.LogWarning("node not presented in _tempNodes");
//        }
//        private void Remove(IEnumerable<NodeTemp> nodes) {
//            foreach (var node in nodes) 
//                Remove(node);            
//        }           
//        private void Remove(IEnumerable<NodeTemp> nodes, Predicate<NodeTemp> predicate) {
//            foreach (var node in nodes) {
//                if (predicate(node)) 
//                    Remove(node);                
//            }
//        }
        
//        public Vector3 GetGraphRealPosition(int x, float y, int z) {
//            return chunkRealPosition + new Vector3(x * 0.5f * fragmentSize, y, z * 0.5f * fragmentSize);
//        }
//        public Vector3 GetGraphRealPosition(Vector2Int1Float pos) {
//            return GetGraphRealPosition(pos.x, pos.y, pos.z);
//        }

//        //edges
//        //thats where all bugs from :P
//        public EdgeTemp SetEdge(NodeTemp left, NodeTemp right, int volume, int flag, IEnumerable<VolumeArea> volumeAreas) {
//            if (volumeAreas != null) {
//                foreach (var item in volumeAreas)
//                    right.capturedVolumeAreas.Add(item);
//            }
//            return left.SetNode(volume, flag, right);
//        }
//        public EdgeTemp SetEdge(NodeTemp left, NodeTemp right, int volume, int flag) {
//            return SetEdge(left, right, volume, flag, null);
//        }

//        public EdgeTemp SetEdge(Vector2Int1Float left, Vector2Int1Float right, int layer, int hash, IEnumerable<VolumeArea> volumeAreas) {
//            return SetEdge(Get(left), Get(right), layer, hash, volumeAreas);
//        }
//        public EdgeTemp SetEdge(Vector2Int1Float left, Vector2Int1Float right, int layer, int hash) {
//            return SetEdge(Get(left), Get(right), layer, hash, null);
//        }

//        public void SetCoverEdge(Vector2Int1Float left, Vector2Int1Float right, int size) {
//            if (size == 0)
//                return;

//            var posL = chunkRealPosition + NodeLocalPosition(left);
//            var posR = chunkRealPosition + NodeLocalPosition(right);

//            NodeCoverTemp nodeL, nodeR;
//            if (_coverDictionary.TryGetValue(posL, out nodeL) == false) {
//                nodeL = new NodeCoverTemp(posL);
//                _coverDictionary.Add(posL, nodeL);
//            }

//            if (_coverDictionary.TryGetValue(posR, out nodeR) == false) {
//                nodeR = new NodeCoverTemp(posR);
//                _coverDictionary.Add(posR, nodeR);
//            }

//            nodeL.SetConnection(nodeR, size);
//        }
//        #endregion

//        #region Douglas-Peucker
//        //generate segments, than loops to simplify them then fix them
//        private void RamerDouglasPeuckerHull(float epsilon) {
//            //foreach (var item in RDPkeypoints) {
//            //    Debuger3.AddLine(item.positionV3, item.positionV3 + Vector3.up, Color.red);
//            //}

//            #region segments
//            foreach (var node in RDPkeypoints) {
//                foreach (var pair in node.getData) {
//                    int volume = pair.Key.x;
//                    int hash = pair.Key.y;

//                    tempNodes.Clear();//current line                    
//                    tempNodes.Add(node);

//                    NodeTemp current = node;            
//                    while (true) {
//                        EdgeTemp currentConnection = current[volume, hash];
//                        current = currentConnection.connection;
//                        tempNodes.Add(current);

//                        if (current.GetFlag(NodeTempFlags.keyMarker) | currentConnection.GetFlag(EdgeTempFlags.DouglasPeukerMarker))
//                            break;
//                    }

//                    if (tempNodes.Count == 2)
//                        pair.Value.SetFlag(EdgeTempFlags.DouglasPeukerMarker, true);
//                    else
//                        SetupDouglasPeuckerHull(tempNodes, epsilon * 0.25f, epsilon);
//                }
//            }

//            #endregion

//            #region loops
//            int breakerMain = 0;
//            int breakerLoopFinish = 0;

//            //int loops = 0;
//            while (true) {
//                EdgeTemp target = null;
                
//                for (int x = 0; x < nodesSizeX; x++) {
//                    for (int z = 0; z < nodesSizeZ; z++) {
//                        if (nodes[x][z] == null)
//                            continue;

//                        for (int i = 0; i < nodes[x][z].Count; i++) {
//                            if (nodes[x][z][i].GetFlag(NodeTempFlags.Intersection))
//                                continue;

//                            foreach (var edge in nodes[x][z][i].getEdges) {
//                                if (edge.GetFlag(EdgeTempFlags.DouglasPeukerMarker) == false) {
//                                    target = edge;
//                                    break;
//                                }
//                            }
//                        }
//                    }
//                }

//                if (target == null)
//                    break;

//                tempNodes.Clear();//current loop
//                NodeTemp origin = target.origin;
//                tempNodes.Add(origin);

//                breakerMain++;
//                if (breakerMain > 3000) {
//#if UNITY_EDITOR
//                    PFDebuger.Debuger_K.AddErrorLine(tempNodes[0].positionV3, tempNodes[0].positionV3 + SmallV3(1f), Color.red);
//#endif
//                    UnityEngine.Debug.LogError("stuck in loop while fixing douglas peuker circles. count: " + tempNodes.Count);
//                    break;
//                }

//                breakerLoopFinish = 0;
//                while (true) {
//                    tempNodes.Add(tempNodes.Last().GetNode(target.volume, target.hash));

//                    //region for fancy bugs
//                    #region fancy bug region
//                    breakerLoopFinish++;
//                    if (breakerLoopFinish > 5000) {
//                        UnityEngine.Debug.LogError("probably stuck in loop while serching loop finish");
//                        break;
//                    }
//                    if (tempNodes.Last() == null) {
//                        UnityEngine.Debug.LogError(tempNodes.Count + ":" + target.volume + " : " + target.hash);

//#if UNITY_EDITOR                
//                        PFDebuger.Debuger_K.AddErrorLine(origin.positionV3, origin.positionV3 + SmallV3(1f), Color.red);
//                        PFDebuger.Debuger_K.AddErrorLine(target.origin.positionV3, target.connection.positionV3 + SmallV3(1f), Color.red);

//                        for (int i = 0; i < tempNodes.Count - 2; i++) {
//                            PFDebuger.Debuger_K.AddErrorLine(tempNodes[i].positionV3 + SmallV3(0.03f * breakerMain), tempNodes[i + 1].positionV3 + SmallV3(0.03f * breakerMain), Color.red);
//                        }                        
//#endif
//                    }
//                    #endregion
//                    if (tempNodes.Last() == origin | tempNodes.Last().GetFlag(NodeTempFlags.Intersection))
//                        break;
//                }
//                //Debuger_K.AddDot(Color.red, (from val in tempNodes select val.positionV3).ToArray());
//                SetupDouglasPeuckerHull(tempNodes, epsilon * 0.25f, epsilon);
//            }
//            #endregion





//            #region fixing result
//            //since nodes after RDP can have loops around too small gaps there is thing to fix it.
//            //cause other things even worse
//            //i tried to rearange some code three times but this is work all time better so ther code are work around that

//            for (int x = 0; x < nodesSizeX; x++) {
//                for (int z = 0; z < nodesSizeZ; z++) {
//                    if (nodes[x][z] == null)
//                        continue;
//                    for (int i = 0; i < nodes[x][z].Count; i++) {
//                        NodeTemp n = nodes[x][z][i];

//                        foreach (var item in n.getData) {
//                            int volume = item.Key.x;
//                            int hash = item.Key.y;

//                            NodeTemp nn = n[volume, hash].connection;
//                            NodeTemp nnn = nn[volume, hash].connection;

//                            if (ReferenceEquals(n, nn))//oh dog there is connection looped to itself. what a strange thing to deal with
//                                removeConnectionsListOne.Add(new VectorInt.Vector2Int(volume, hash));
//                            else if (ReferenceEquals(n, nnn))//we have loop and this is not good
//                                removeConnectionsListTwo.Add(new VectorInt.Vector2Int(volume, hash));
//                        }

//                        if (removeConnectionsListOne.Count > 0) {
//                            foreach (var item in removeConnectionsListOne) {
//                                n.RemoveConnention(item.x, item.y);
//                                if (n.getData.Count() == 0)
//                                    removeList.Add(n);
//                            }
//                            removeConnectionsListOne.Clear();
//                        }

//                        if (removeConnectionsListTwo.Count > 0) {
//                            NodeTemp nn = null;
//                            foreach (var item in removeConnectionsListTwo) {
//                                nn = n.GetNode(item.x, item.y);
//                                n.RemoveConnention(item.x, item.y);
//                                nn.RemoveConnention(item.x, item.y);

//                                if (n.getData.Count() == 0)
//                                    removeList.Add(n);

//                                if (nn.getData.Count() == 0)
//                                    removeList.Add(nn);
//                            }
//                            removeConnectionsListTwo.Clear();
//                        }
//                    }
//                }
//            }


//            Remove(removeList);
//            #endregion
//        }
//        //used for segments and then for loops
//        private void SetupDouglasPeuckerHull(List<NodeTemp> target, float firstEpsilon, float secondEpsilon) {
//            NodeTemp A = target[0];
//            NodeTemp B = target[1];

//            List<EdgeTemp> AB = A.GetConnectionsToNode(B);
//            List<EdgeTemp> BA = B.GetConnectionsToNode(A);
//            List<NodeAbstract> newValues = null;

//            var newTarget = DouglasPeucker(target.Cast<NodeAbstract>().ToList(), 0, target.Count - 1, firstEpsilon);
//            newValues = DouglasPeucker(newTarget, 0, newTarget.Count - 1, secondEpsilon);

//            List<NodeTemp> newValuesCasted = newValues.Cast<NodeTemp>().ToList();

//            for (int i = 0; i < newValuesCasted.Count; i++) {
//                newValuesCasted[i].SetFlag(NodeTempFlags.DouglasPeuckerWasHere, true);
//            }

//            Remove(target, (NodeTemp node) => { return node.GetFlag(NodeTempFlags.DouglasPeuckerWasHere) == false; });

//            for (int i = 0; i < newValues.Count - 1; i++) {
//                AB.ForEach(x => SetEdge(newValuesCasted[i], newValuesCasted[i + 1], x.volume, x.hash).SetFlag(EdgeTempFlags.DouglasPeukerMarker, true));
//                BA.ForEach(x => SetEdge(newValuesCasted[i + 1], newValuesCasted[i], x.volume, x.hash).SetFlag(EdgeTempFlags.DouglasPeukerMarker, true));
//            }

//            //iterating throu points, look wich areas they have, then add to that areas new edges
//            if (template.canJump | template.doCover) {
//                //a bit more readable variables for transfering captured areas information
//                List<NodeTemp> originalNodes = target;
//                List<NodeTemp> newNodes = newValuesCasted;

//                //cause first and last node in loops are same we cant use IndexOf
//                int index = 0;
//                int lastIndex = 0;

//                for (int nn = 0; nn < newNodes.Count - 1; nn++) {
//                    tempEdges.Clear();//AB and BA edges in same list

//                    foreach (var edge in newNodes[nn].getEdges) {
//                        if (edge.connection == newNodes[nn + 1])
//                            tempEdges.Add(edge);
//                    }
//                    foreach (var edge in newNodes[nn + 1].getEdges) {
//                        if (edge.connection == newNodes[nn])
//                            tempEdges.Add(edge);
//                    }

//                    for (index = lastIndex; index < originalNodes.Count; index++) {
//                        if (newNodes[nn + 1] == originalNodes[index])
//                            break;
//                    }

//                    for (int on = lastIndex; on < index; on++) {
//                        foreach (var va in originalNodes[on].capturedVolumeAreas) {
//                            foreach (var edge in tempEdges) {
//                                va.AddEdge(edge);
//                            }
//                        }
//                    }

//                    lastIndex = index;                     
//                }
//            }
//        }

//        //generate segments, than loops to simplify them
//        private void RamerDouglasPeuckerCover(params float[] epsilonSiquence) {
//            #region segments
//            List<NodeCoverTemp> roots = new List<NodeCoverTemp>(_coverDictionary.Values);
//            foreach (var node in _coverDictionary.Values) {
//                if (node.connection == null) 
//                    roots.Remove(node);                
//                else if (node.connectionType == node.connection.connectionType)
//                    roots.Remove(node.connection);       
//            }

//            foreach (var root in roots) {
//                //Debuger3.AddLine(root.positionV3, (root.positionV3 + Vector3.up * 5), Color.magenta);
//                List<NodeCoverTemp> line = new List<NodeCoverTemp>();
//                line.Add(root);
//                int type = root.connectionType;
    
     
//                while (true) {
//                    var last = line.Last();

//                    if (last.connection == null)
//                        break;       

//                    line.Add(last.connection);

//                    if (last.connection.connectionType != type)
//                        break;
//                }

//                SetupDouglasPeuckerCover(line, type, epsilonSiquence);
//            }
//            #endregion

//            #region Loops
//            List<NodeCoverTemp> nodeList = new List<NodeCoverTemp>(_coverDictionary.Values);
//            foreach (var node in nodeList) {
//                if (node.dpWasHere)
//                    continue;

//                List<NodeCoverTemp> line = new List<NodeCoverTemp>();
//                line.Add(node);
//                int type = node.connectionType;

//                int count = 0;


//                while (true) {
//                    //if (line.Last().connection == null) {
//                    //    Debuger3.AddRay(line.Last().positionV3, Vector3.up, Color.blue);
//                    //}

//                    line.Add(line.Last().connection);
//                    if (line.Last() == node)
//                        break;

//                    count++;
//                    if (count > 1000) {
//                        UnityEngine.Debug.LogError("loop over 1000 in cover");
//                        break;
//                    }
//                }

//                SetupDouglasPeuckerCover(line, type, epsilonSiquence);
//            }

//            #endregion

//            #region hide
//            //Vector3 add1 = Vector3.up * 0.01f;
//            //Vector3 add2 = Vector3.up * 0.02f;

//            //Vector3 add3 = Vector3.forward * 0.01f;
//            //Vector3 add4 = Vector3.back * 0.01f;
//            //Vector3 add5 = Vector3.left * 0.01f;
//            //Vector3 add6 = Vector3.right * 0.01f;




//            //foreach (var node in coverDictionary.Values) {
//            //    if (node.connection != null) {
//            //        Debuger3.AddLine(node.positionV3 + (Vector3.up * node.connectionType), node.connection.positionV3 + (Vector3.up * node.connectionType), Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + (Vector3.up * node.connectionType) + add1, node.connection.positionV3 + (Vector3.up * node.connectionType) + add1, Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + (Vector3.up * node.connectionType) + add2, node.connection.positionV3 + (Vector3.up * node.connectionType) + add2, Color.magenta);

//            //        Debuger3.AddLine(node.positionV3, node.connection.positionV3, Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + add1, node.connection.positionV3 + add1, Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + add2, node.connection.positionV3 + add2, Color.magenta);

//            //        Debuger3.AddLine(node.positionV3, node.positionV3 + (Vector3.up * node.connectionType), Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + add3, node.positionV3 + (Vector3.up * node.connectionType) + add3, Color.magenta);
//            //        Debuger3.AddLine(node.positionV3 + add6, node.positionV3 + (Vector3.up * node.connectionType) + add6, Color.magenta);

//            //        Debuger3.AddLine(node.connection.positionV3, node.connection.positionV3 + (Vector3.up * node.connectionType), Color.magenta);
//            //        Debuger3.AddLine(node.connection.positionV3 + add3, node.connection.positionV3 + (Vector3.up * node.connectionType) + add3, Color.magenta);
//            //        Debuger3.AddLine(node.connection.positionV3 + add6, node.connection.positionV3 + (Vector3.up * node.connectionType) + add6, Color.magenta);
//            //    }

//            //    if (node.dpWasHere) {
//            //        Debuger3.AddDot(node.positionV3 + (Vector3.up * node.connectionType), 0.05f, Color.magenta);
//            //        if(node.connection != null)
//            //            Debuger3.AddDot(node.connection.positionV3 + (Vector3.up * node.connectionType), 0.05f, Color.magenta);
//            //    }
//            //}
//            #endregion
//        }
//        //used for segments and then for loops
//        private void SetupDouglasPeuckerCover(List<NodeCoverTemp> target, int coverType, params float[] epsilonSiquence) {
//            List<NodeAbstract> values = target.Cast<NodeAbstract>().ToList();

//            foreach (var epsilon in epsilonSiquence) 
//                values = DouglasPeucker(values, 0, values.Count - 1, epsilon);            

//            List<NodeCoverTemp> newValuesCasted = values.Cast<NodeCoverTemp>().ToList();           

//            for (int i = 0; i < values.Count - 1; i++) {
//                newValuesCasted[i].SetConnection(newValuesCasted[i + 1], coverType);
//            }

//            target.ForEach(x => x.dpWasHere = true);

//            foreach (var noUsedNode in target.Except(newValuesCasted)) {
//                _coverDictionary.Remove(noUsedNode.positionV3);
//            }
//        }

//        //general for DouglasPeuckerCover and SetupDouglasPeuckerCover
//        private List<NodeAbstract> DouglasPeucker(List<NodeAbstract> nodesPoints, int startIndex, int lastIndex, float epsilon) {
//            float distanceMax = 0f;
//            int index = startIndex;
            
//            for (int i = index + 1; i < lastIndex; ++i) {
//                float distance = Vector3.Distance(SomeMath.NearestPointOnLine(nodesPoints[startIndex].positionV3, nodesPoints[lastIndex].positionV3, nodesPoints[i].positionV3), nodesPoints[i].positionV3);
//                if (distance > distanceMax) {
//                    index = i;
//                    distanceMax = distance;
//                }
//            }

//            if (distanceMax > epsilon) {
//                var result = new List<NodeAbstract>(DouglasPeucker(nodesPoints, startIndex, index, epsilon));
//                result.RemoveAt(result.Count - 1);
//                result.AddRange(DouglasPeucker(nodesPoints, index, lastIndex, epsilon));
//                return result;
//            }
//            else {
//                return new List<NodeAbstract>(new NodeAbstract[] { nodesPoints[startIndex], nodesPoints[lastIndex] });
//            }
//        }
//        #endregion

//        private void SetupCoverPoints(NodeCoverTemp cover) {
//            List<Vector3> points = new List<Vector3>();

//            float agentRadius = template.agentRadiusReal;
//            float agentDiameter = agentRadius * 2;
//            int sqrAgentRadiusOnVolumeMap = template.agentRagius * template.agentRagius;

//            Vector2 leftV2 = cover.positionV2;
//            Vector2 rightV2 = cover.connection.positionV2;

//            Vector3 leftV3 = cover.positionV3;
//            Vector3 rightV3 = cover.connection.positionV3;

//            Vector2 dir = (rightV2 - leftV2).normalized;
//            Vector3 normal = new Vector3(dir.y, 0, -dir.x);
//            cover.SetNormal(normal);

//            float distance = Vector2.Distance(leftV2, rightV2);
//            int mountPoints = (int)(distance / agentDiameter); // aprox how much agents are fit in that length of cover

//            if (mountPoints < 2) {//single point in middle
//                points.Add((leftV3 + rightV3) * 0.5f);
//            }
//            else if (mountPoints == 2) {//two points on agent radius distance near ends
//                points.Add(Vector3.Lerp(leftV3, rightV3, agentRadius / distance));
//                points.Add(Vector3.Lerp(leftV3, rightV3, (distance - agentRadius) / distance));
//            }
//            else {//whole bunch of points
//                points.Add(Vector3.Lerp(leftV3, rightV3, agentRadius / distance));
//                points.Add(Vector3.Lerp(leftV3, rightV3, (distance - agentRadius) / distance));

//                float startVal = agentDiameter / distance;
//                float step = (distance - agentDiameter - agentDiameter) / distance / (mountPoints - 2);

//                //Debuger3.AddLabel(
//                //    (nodeLeft.positionV3 + nodeRight.positionV3) * 0.5f,
//                //    "distance: " + distance + " / " + agentDiameter + " = " + distance / agentDiameter + " : " + mountPoints + "/n"
//                //    );

//                for (int i = 0; i < mountPoints - 2; i++) {
//                    points.Add(Vector3.Lerp(leftV3, rightV3, startVal + (step * 0.5f) + (step * i)));
//                }
//            }

//            //Vector3 add1 = Vector3.up * 0.01f;
//            //Vector3 add2 = Vector3.up * 0.02f;
//            //foreach (var item in points) {
//            //    Debuger3.AddRay(item + Vector3.up, normal, Color.magenta, 0.5f);
//            //    //Debuger3.AddVector(item + Vector3.up + add1, normal + add1, 0.5f, Color.magenta);
//            //    //Debuger3.AddVector(item + Vector3.up + add2, normal + add2, 0.5f, Color.magenta);

//            //    Debuger3.AddDot(item, Color.green, 0.03f);
//            //    Debuger3.AddDot(item + Vector3.up, Color.red, 0.03f);
//            //    Debuger3.AddDot(item + Vector3.up + (normal * 0.5f), Color.red, 0.03f);
//            //    Debuger3.AddDot(item + (normal * 0.5f), Color.green, 0.03f);

//            //    Debuger3.AddLine(item + (normal * 0.5f), item + Vector3.up + (normal * 0.5f), Color.green);
//            //}

//            //find closest point and capture area around it to pass it throu all next parts
//            foreach (var point in points) {
//                VolumePos pos;
//                if (volumeContainer.GetClosestPos(point, out pos)) {
//                    VolumeArea coverArea = volumeContainer.CaptureArea(pos, sqrAgentRadiusOnVolumeMap, sqrAgentRadiusOnVolumeMap, false, AreaType.Cover);
//                    coverArea.position = point;
//                    cover.AddCover(coverArea);

//                    //Debuger3.AddLine(volumeContainer.GetRealMax(pos), point, Color.red);
//                    //Debuger3.AddDot(volumeContainer.GetRealMax(pos), Color.red);
//                }
//            }
//        }
        
//        public IEnumerable<Volume> getVolumes {
//            get { return volumeContainer.volumes; }
//        }
//        public IEnumerable<NodeTemp> getNodes {
//            get { return GetAllNodes(); }
//        }
        
//        private static Vector3 SmallV3(float value) {
//            return Vector3.up * value;
//        }
//    }
//}
