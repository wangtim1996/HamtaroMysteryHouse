using UnityEngine;
using System;
using System.Collections.Generic;

using K_PathFinder.VectorInt;
using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;
using K_PathFinder;
using K_PathFinder.CoverNamespace;
using System.Text;

namespace K_PathFinder.Serialization {
    public class NavmeshLayserSerializer {
        Dictionary<Cell, int> cells = new Dictionary<Cell, int>();
        List<Graph> targetGraphs = new List<Graph>();

        Dictionary<BattleGridPoint, int> bgIDs = new Dictionary<BattleGridPoint, int>();
        Dictionary<GameObject, int> gameObjectLibraryIDs;

        //create cell dictionary
        public NavmeshLayserSerializer(Dictionary<GeneralXZData, Graph> chunkData, Dictionary<XZPosInt, YRangeInt> chunkRangeData, Dictionary<GameObject, int> gameObjectLibraryIDs, AgentProperties properties) {
            int cellCounter = 0, bgpCounter = 0;

            this.gameObjectLibraryIDs = gameObjectLibraryIDs;

            //creating cell dictionary cause connections are lead outside chunk

            foreach (var graph in chunkData.Values) {
                if (graph.empty || graph.properties != properties)
                    continue;

                targetGraphs.Add(graph);

                foreach (var cell in graph.cells) {
                    cells.Add(cell, cellCounter++);
                }

                BattleGrid bg = graph.battleGrid;
                if (bg != null) {
                    foreach (var p in bg.points) {
                        bgIDs.Add(p, bgpCounter++);
                    }
                }
            }
        }
        
        public SerializedNavmesh Serialize() {      
            SerializedNavmesh serializedNM = new SerializedNavmesh();   
            serializedNM.serializedGraphs = new List<SerializedGraph>();

            foreach (var graph in targetGraphs) {
                GraphSerializer serializer = new GraphSerializer(this, graph);
                serializedNM.serializedGraphs.Add(serializer.Serialize());   
            }

            serializedNM.cellCount = cells.Count;
            serializedNM.bgPointsCount = bgIDs.Count;

            Debug.LogFormat("saved {0} graphs", serializedNM.serializedGraphs.Count);
            return serializedNM;
        }

        public int GetCellID(Cell cell) {
            return cells[cell];
        }
        public int GetBattleGridID(BattleGridPoint p) {
            return bgIDs[p];
        }
        public int GetGameObjectID(GameObject go) {
            int result;

            if (gameObjectLibraryIDs.TryGetValue(go, out result) == false) {
                result = gameObjectLibraryIDs.Count;
                gameObjectLibraryIDs.Add(go, result);
            }
            return result;
        }
    }


    public class NavmeshLayerDeserializer {
        SerializedNavmesh _serializedData;
        Cell[] _cells;
        BattleGridPoint[] _points;
        AgentProperties _properties;

        public Cell[] cellPool;
        public BattleGridPoint[] bgPool = null;
        public GameObject[] gameObjectLibrary;

        public NavmeshLayerDeserializer(SerializedNavmesh serializedData, AgentProperties properties, GameObject[] gameObjectLibrary) {
            _properties = properties;
            _serializedData = serializedData;
            cellPool = new Cell[serializedData.cellCount];
            if (properties.battleGrid && serializedData.bgPointsCount != 0)
                bgPool = new BattleGridPoint[serializedData.bgPointsCount];

            this.gameObjectLibrary = gameObjectLibrary;         
        }

        public Area GetArea(int value) {
            Area result = PathFinder.GetArea(value);
            if (result == null)
                result = PathFinder.settings.getDefaultArea;
            return result;
        }

        public GameObject GetGameObject(int index) {
            return gameObjectLibrary[index];
        }

        public SerializedNavmesh serializedData {
            get { return _serializedData; }
        }
        
        public List<DeserializationResult> Deserialize() {
            List<GraphDeserializer> graphDeserializers = new List<GraphDeserializer>();

            foreach (var graph in _serializedData.serializedGraphs) {
                graphDeserializers.Add(new GraphDeserializer(graph, this, _properties));
            }

            //now we have populate cell dictionary and have all cells in layer
            foreach (var gd in graphDeserializers) {
                gd.DeserializeCells();
            }
 
            //now all cells have connections
            foreach (var gd in graphDeserializers) {
                gd.DeserializeConnections(_properties.canJump);
            }

            if (_properties.canCover) {
                foreach (var gd in graphDeserializers) {
                    gd.DeserializeCovers();
                }
            }

            if (_properties.battleGrid && bgPool != null) {
                foreach (var gd in graphDeserializers) {
                    gd.DeserializeBattleGridPoints();
                }
                foreach (var gd in graphDeserializers) {
                    gd.ConnectBattleGridPoints();
                }
            }
    
            List<DeserializationResult> result = new List<DeserializationResult>();
            foreach (var item in graphDeserializers) {
                result.Add(new DeserializationResult(item.GetGraph(), item.serializedGraph.chunkPos, item.serializedGraph.minY, item.serializedGraph.maxY));
            }
    
            return result;
        }
    }

    public struct DeserializationResult {
        public XZPosInt chunkPosition;
        public int chunkMinY, chunkMaxY;
        public Graph graph;

        public DeserializationResult(Graph graph, XZPosInt chunkPosition, int minY, int maxY) {
            this.graph = graph;
            this.chunkPosition = chunkPosition;
            this.chunkMinY = minY;
            this.chunkMaxY = maxY;
        }
    }

    public class GraphSerializer {
        public Dictionary<CellContentData, int> edges = new Dictionary<CellContentData, int>();
        //public Dictionary<NodeAbstract, int> nodes = new Dictionary<NodeAbstract, int>();

        NavmeshLayserSerializer ns;
        Graph graph;

        public GraphSerializer(NavmeshLayserSerializer ns, Graph graph) {
            this.ns = ns;
            this.graph = graph;
        }

        public SerializedGraph Serialize() {
            SerializedGraph serializedGraph = new SerializedGraph();
            serializedGraph.posX = graph.chunk.x;
            serializedGraph.posZ = graph.chunk.z;
            serializedGraph.minY = graph.chunk.min;
            serializedGraph.maxY = graph.chunk.max;

            List<SerializedCell> serializedCells = new List<SerializedCell>();
            List<SerializedCover> serializedCovers = new List<SerializedCover>();

            foreach (var cell in graph.cells) {
                serializedCells.Add(new SerializedCell(ns, this, cell));
            }

            foreach (var cover in graph.covers) {
                serializedCovers.Add(new SerializedCover(ns, cover));
            }

            serializedGraph.serializedCells = serializedCells;
            serializedGraph.serializedCovers = serializedCovers;

            //battlegrid
            if (graph.battleGrid != null)
                serializedGraph.battleGrid = new SerializedBattleGrid(ns, graph.battleGrid);
            else
                serializedGraph.battleGrid = null;

            //cell map
            var cellMap = graph.getCellMap;
            if (cellMap != null) {
                List<SerializableVector3Int> serializedCellMap = new List<SerializableVector3Int>();

                for (int x = 0; x < cellMap.Length; x++) {
                    for (int z = 0; z < cellMap[x].Length; z++) {
                        for (int id = 0; id < cellMap[x][z].Count; id++) {
                            serializedCellMap.Add(new SerializableVector3Int(x, z, ns.GetCellID(cellMap[x][z][id])));
                        }
                    }
                }


                //Debug.Log("write cell map");
                serializedGraph.cellMapData = serializedCellMap;
                //Debug.Log(serializedGraph.cellMapData == null);
            }
            else {
                Debug.Log("cell map null");
                serializedGraph.cellMapData = null;
            }
            
            //contour dictionary      
            if (graph.getContour != null) {
                List<SerializedContourData> contour = new List<SerializedContourData>();
                foreach (var pair in graph.getContour) {
                    contour.Add(new SerializedContourData(pair.Key.a, pair.Key.b, ns.GetCellID(pair.Value)));
                }
                serializedGraph.contour = contour;
            }
            else {
                serializedGraph.contour = null;
            }

            //border         
            List<SerializedBorderData> serializedBorder = new List<SerializedBorderData>();

            for (int i = 0; i < 4; i++) {
                var curSide = graph.GetBorderEdges((Directions)i);
                foreach (var pair in curSide) {
                    serializedBorder.Add(new SerializedBorderData(pair.Key.a, pair.Key.b, ns.GetCellID(pair.Value), i));
                }
            }
            serializedGraph.borderData = serializedBorder;

            //foreach (var cell in serializedCells) {
            //    Debug.Log(cell.ToString());
            //}
            return serializedGraph;
        }
    }

    public class GraphDeserializer {
        public List<Cell> cells = new List<Cell>();
        public List<Cover> covers = new List<Cover>();

        public SerializedGraph serializedGraph;
        private Graph targetGraph;
        private NavmeshLayerDeserializer deserializer;

        Cell[] cellPool; //shortcut to cell pool

        public GraphDeserializer(SerializedGraph serializedGraph, NavmeshLayerDeserializer deserializer, AgentProperties properties) {
            this.serializedGraph = serializedGraph;
            this.deserializer = deserializer;
            targetGraph = new Graph(serializedGraph.chunkData, properties);

            //nodes = new Node[this.serializedGraph.serializedNodes.Count];
            //edges = new EdgeGraph[this.serializedGraph.serializedEdges.Count];

            cellPool = deserializer.cellPool;
        }

        //before all next
        public void DeserializeCells() {
            //cells
            List<SerializedCell> serializedCells = serializedGraph.serializedCells;

            foreach (var curSerializedCell in serializedCells) {
                Area cellArea;
                if (curSerializedCell.isAdvancedAreaCell) {
                    GameObject targetGO = deserializer.GetGameObject(curSerializedCell.area);
                    if(targetGO == null) {
                        Debug.LogWarning("Deserializer cant find GameObject so Cell area became default area");
                        cellArea = PathFinder.settings.getDefaultArea;
                    }
                    else {
                        AreaWorldMod areaWorldMod = targetGO.GetComponent<AreaWorldMod>();
                        if(areaWorldMod == null) {
                            Debug.LogWarning("Deserializer cant find AreaModifyer on gameObject so Cell area became default area");
                            cellArea = PathFinder.settings.getDefaultArea;
                        }
                        else {
                            if(areaWorldMod.useAdvancedArea == false) {
                                Debug.LogWarning("Area Modifyer don't use advanced area so Cell area became default area");
                                cellArea = PathFinder.settings.getDefaultArea;
                            }
                            else {
                                cellArea = areaWorldMod.advancedArea;
                            }
                        }
                    }
                }
                else {
                    cellArea = deserializer.GetArea(curSerializedCell.area);
                }
            

                Cell newC = new Cell(cellArea, (Passability)curSerializedCell.passability, curSerializedCell.layer, targetGraph, curSerializedCell.originalEdges);
                newC.SetCenter(curSerializedCell.center);

                foreach (var data in curSerializedCell.data) {
                    newC.TryAddData(data);
                }

                cellPool[curSerializedCell.id] = newC;
                cells.Add(newC);


                //Vector3 CC = c.center;
                //foreach (var data in c.data) {
                //    Vector3 DC = data.centerV3;
                //    PFDebuger.Debuger_K.AddLine(CC, DC, Color.red);
                //    Vector3 CCDC = SomeMath.MidPoint(CC, DC);
                //    PFDebuger.Debuger_K.AddLine(CCDC, data.rightV3, Color.blue);
                //    PFDebuger.Debuger_K.AddLine(CCDC, data.leftV3, Color.cyan);
                //}           
            }

            //map
            var serializedCellMap = serializedGraph.cellMapData;

            List<Cell>[][] cellMap = new List<Cell>[PathFinder.CELL_GRID_SIZE][];
            for (int x = 0; x < PathFinder.CELL_GRID_SIZE; x++) {
                cellMap[x] = new List<Cell>[PathFinder.CELL_GRID_SIZE];
                for (int z = 0; z < PathFinder.CELL_GRID_SIZE; z++) {
                    cellMap[x][z] = new List<Cell>();
                }
            }

            foreach (var data in serializedCellMap) {
                cellMap[data.x][data.y].Add(cellPool[data.z]);
            }

            //contour        
            var serializedContour = serializedGraph.contour;
            Dictionary<CellContentData, Cell> contour = new Dictionary<CellContentData, Cell>();
            foreach (var c in serializedContour) {
                contour.Add(new CellContentData(c.a, c.b), cellPool[c.cell]);
            }

            targetGraph.SetBunchOfData(cells, cellMap, contour);
   
            //borders  
            var serializedBorderData = serializedGraph.borderData;    
            if (serializedBorderData != null) {
                foreach (var bd in serializedBorderData) {
                    targetGraph.SetEdgeSide(new CellContentData(bd.a, bd.b), (Directions)bd.direction, cellPool[bd.cell]);
                }
            }

            //debug
            //Debug.Log(cells.Count);
            //string s = "";
            //for (int x = 0; x < cellMap.Length; x++) {
            //    for (int z = 0; z < cellMap[x].Length; z++) {
            //        s += cellMap[x][z].Count;
            //    }
            //    s += "\n";
            //}
            //Debug.Log(s);
            //Debug.Log(contour.Count);
        }

        //after deserializing cells
        public void DeserializeConnections(bool deserializeJumpConnections) {
            foreach (var cell in serializedGraph.serializedCells) {
                Cell fromCell = cellPool[cell.id];

                foreach (var connection in cell.serializedNormalConnections) {
                    CellContentGenericConnection newCon = new CellContentGenericConnection(
                        connection.data,
                        fromCell,
                        cellPool[connection.connectedCell],                        
                        connection.interconnection,         
                        connection.costFrom,
                        connection.costTo,
                        connection.intersection);

                    fromCell.SetContent(newCon);
                }

                if (deserializeJumpConnections) {
                    foreach (var connection in cell.serializedJumpConnections) {
                        CellContentPointedConnection newCon = new CellContentPointedConnection(
                            connection.enterPoint,
                            connection.lowerStandingPoint,
                            connection.exitPoint,
                            connection.axis,
                            (ConnectionJumpState)connection.jumpState,
                            fromCell,
                            cellPool[connection.connectedCell],           
                            connection.interconnection);

                        fromCell.SetContent(newCon);
                    }
                }
            }
        }
        public void DeserializeCovers() {
            foreach (var cover in serializedGraph.serializedCovers) {
                Cover newCover = new Cover(cover.left, cover.right, cover.coverType, cover.normal);
                foreach (var point in cover.coverPoints) {
                    NodeCoverPoint newCoverPoint = new NodeCoverPoint(point.position, point.cellPosition, cellPool[point.cell], newCover);
                    newCover.AddCoverPoint(newCoverPoint);
                }
                targetGraph.covers.Add(newCover);
                covers.Add(newCover);
            }
        }

        //battle grid
        public void DeserializeBattleGridPoints() {
            BattleGridPoint[] pool = deserializer.bgPool;
            List<BattleGridPoint> points = new List<BattleGridPoint>();

            foreach (var p in serializedGraph.battleGrid.points) {
                BattleGridPoint newP = new BattleGridPoint(p.position, (Passability)p.passability, new VectorInt.Vector2Int(p.gridX, p.gridZ));
                pool[p.id] = newP;
                points.Add(newP);
            }

            targetGraph.battleGrid = new BattleGrid(serializedGraph.battleGrid.lengthX, serializedGraph.battleGrid.lengthZ, points);
        }
        public void ConnectBattleGridPoints() {
            BattleGridPoint[] pool = deserializer.bgPool;
            foreach (var p in serializedGraph.battleGrid.points) {
                BattleGridPoint curP = pool[p.id];

                for (int i = 0; i < 4; i++) {
                    if (p.neighbours[i] != -1) {
                        curP.neighbours[i] = pool[p.neighbours[i]];
                    }
                }
            }
        }  

        public Graph GetGraph() {
            return targetGraph;
        }
    }
    
    [Serializable]
    public class SerializedNavmesh {
        public string pathFinderVersion;
        public int cellCount, bgPointsCount;
        public List<SerializedGraph> serializedGraphs;
    }

    [Serializable]
    public class SerializedGraph {
        public int posX, posZ, minY, maxY;
        public List<SerializedCell> serializedCells;
        [SerializeField]
        public List<SerializedCover> serializedCovers;
        [SerializeField]
        public SerializedBattleGrid battleGrid;

        [SerializeField]
        public List<SerializableVector3Int> cellMapData; //x,z,ids

        [SerializeField]
        public List<SerializedContourData> contour;
        [SerializeField]
        public List<SerializedBorderData> borderData;

        public XZPosInt chunkPos {
            get { return new XZPosInt(posX, posZ); }
        }

        public ChunkData chunkData {
            get { return new ChunkData(posX, posZ, minY, maxY); }
        }
    }

    [Serializable]
    public class SerializedCell {
        public int id, layer, area, passability;
        public bool isAdvancedAreaCell;
        public Vector3 center;
        public List<CellContentData> data = new List<CellContentData>();
        public List<CellContentData> originalEdges = new List<CellContentData>();
        public List<SerializedNormalConnection> serializedNormalConnections = new List<SerializedNormalConnection>();
        public List<SerializedJumpConnection> serializedJumpConnections = new List<SerializedJumpConnection>();
        
        public SerializedCell(NavmeshLayserSerializer ns, GraphSerializer gs, Cell cell) {
            id = ns.GetCellID(cell);
            layer = cell.layer;

            isAdvancedAreaCell = cell.advancedAreaCell;
            if (cell.advancedAreaCell) {
                AreaAdvanced aa = cell.area as AreaAdvanced;
                area = ns.GetGameObjectID(aa.container.gameObject);
            }
            else {
                area = cell.area.id;
            }
   
            passability = (int)cell.passability;
            center = cell.centerVector3;            

            data = new List<CellContentData>(cell.data);
            originalEdges = new List<CellContentData>(cell.originalEdges);

            foreach (var connection in cell.connections) {
                if (connection is CellContentGenericConnection)
                    serializedNormalConnections.Add(new SerializedNormalConnection(ns, gs, connection as CellContentGenericConnection));

                if (connection is CellContentPointedConnection)
                    serializedJumpConnections.Add(new SerializedJumpConnection(ns, connection as CellContentPointedConnection));
            }
        }


        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("ID: {0}, Layer: {1}, Area: {2}, Passability: {3}\n", id, layer, area, passability);
            sb.AppendFormat("Center: {0}\n", center);
            sb.AppendFormat("Data ({0})\n:", data.Count);
            foreach (var d in data) {
                sb.AppendLine(d.ToString());
            }
            sb.AppendFormat("Normal Connections ({0})\n:", serializedNormalConnections.Count);
            foreach (var c in serializedNormalConnections) {
                sb.AppendLine(c.ToString());
            }
            sb.AppendFormat("Jump Connections ({0})\n:", serializedJumpConnections.Count);
            foreach (var c in serializedJumpConnections) {
                sb.AppendLine(c.ToString());
            }

            return sb.ToString();
        }
    }

    //[Serializable]
    //public class SerializedNode {
    //    public float x, y, z;

    //    public SerializedNode(Node node) {
    //        x = node.x;
    //        y = node.y;
    //        z = node.z;
    //    }

    //    public Node Deserialize() {
    //        return new Node(x, y, z);
    //    }

    //    public Vector3 position {
    //        get { return new Vector3(x, y, z); }
    //    }
    //}

    //[Serializable]
    //public class SerializedEdge {
    //    public int nodeA, nodeB, rightCell, leftCell, direction;

    //    public SerializedEdge(NavmeshLayserSerializer ns, GraphSerializer gs, EdgeGraph edge) {
    //        nodeA = gs.nodes[edge.a];
    //        nodeB = gs.nodes[edge.b];
    //        rightCell = edge.right == null ? -1 : ns.GetCellID(edge.right);
    //        leftCell = edge.left == null ? -1 : ns.GetCellID(edge.left);
    //        direction = edge.direction;
    //    }
    //}

    [Serializable]
    public class SerializedNormalConnection {
        public bool interconnection;
        public int fromCell, connectedCell;
        public float costFrom, costTo;
        public Vector3 intersection;
        public CellContentData data;

        public SerializedNormalConnection(NavmeshLayserSerializer ns, GraphSerializer gs, CellContentGenericConnection connection) {
            interconnection = connection.interconnection;
            fromCell = ns.GetCellID(connection.from);
            connectedCell = ns.GetCellID(connection.connection);
            data = connection.cellData;
            intersection = connection.intersection;
            costFrom = connection.costFrom;
            costTo = connection.costTo;
        }
    }

    [Serializable]
    public class SerializedJumpConnection {
        public Vector3 enterPoint, lowerStandingPoint, exitPoint, axis;
        public bool interconnection;
        public int connectedCell, jumpState;  

        public SerializedJumpConnection(NavmeshLayserSerializer ns, CellContentPointedConnection connection) {
            interconnection = connection.interconnection;
            connectedCell = ns.GetCellID(connection.connection);
            enterPoint = connection.enterPoint;
            lowerStandingPoint = connection.lowerStandingPoint;
            exitPoint = connection.exitPoint;
            axis = connection.axis;
            jumpState = (int)connection.jumpState;
        }
    }
    
    [Serializable]
    public class SerializedCover {
        public List<SerializedCoverPoint> coverPoints = new List<SerializedCoverPoint>();
        public Vector3 left, right, normal;
        public float leftX, leftY, leftZ, rightX, rightY, rightZ, normalX, normalY, normalZ;
        public int coverType;

        public SerializedCover(NavmeshLayserSerializer ns, Cover cover) {
            coverType = cover.coverType;
            left = cover.left;
            right = cover.right;
            normal = cover.normalV3;

            foreach (var p in cover.coverPoints) {
                coverPoints.Add(new SerializedCoverPoint(p, ns.GetCellID(p.cell)));
            }
        }
    }

    [Serializable]
    public class SerializedCoverPoint {
        public Vector3 position, cellPosition;
        public int cell;

        public SerializedCoverPoint(NodeCoverPoint point, int cell) {
            position = point.positionV3;
            cellPosition = point.cellPos;
            this.cell = cell;
        }
    }

    [Serializable]
    public class SerializedBattleGridPoint {
        public Vector3 position;
        public int gridX, gridZ, id, passability;
        public int[] neighbours;

        public SerializedBattleGridPoint(NavmeshLayserSerializer ns, BattleGridPoint point) {
            position = point.positionV3;
            gridX = point.gridX;
            gridZ = point.gridZ;
            id = ns.GetBattleGridID(point);
            passability = (int)point.passability;
            neighbours = new int[4];
            for (int i = 0; i < 4; i++) {
                neighbours[i] = point.neighbours[i] != null ? ns.GetBattleGridID(point.neighbours[i]) : -1;
            }
        }
    }

    [Serializable]
    public class SerializedBattleGrid {
        public List<SerializedBattleGridPoint> points;
        public int lengthX, lengthZ;

        public SerializedBattleGrid(NavmeshLayserSerializer ns, BattleGrid bg) {
            lengthX = bg.lengthX;
            lengthZ = bg.lengthZ;

            points = new List<SerializedBattleGridPoint>();
            foreach (var p in bg.points) {
                points.Add(new SerializedBattleGridPoint(ns, p));
            }
        }
    }

    [Serializable]
    public struct SerializedBorderData {
        public Vector3 a, b;
        public int cell, direction;
        public SerializedBorderData(Vector3 a, Vector3 b, int cell, int direction) {
            this.a = a;
            this.b = b;
            this.cell = cell;
            this.direction = direction;
        }
    }

    [Serializable]
    public struct SerializedContourData {
        public Vector3 a, b;
        public int cell;
        public SerializedContourData(Vector3 a, Vector3 b, int cell) {
            this.a = a;
            this.b = b;
            this.cell = cell;
        }
    }

    [Serializable]
    public struct SerializableVector3Int {
        public int x, y, z;

        public SerializableVector3Int(int x, int y, int z) {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }

}