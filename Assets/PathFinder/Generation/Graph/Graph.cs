using UnityEngine;
using System.Collections.Generic;
using System.Linq;

using K_PathFinder.EdgesNameSpace;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.CoverNamespace;
using System;
using K_PathFinder.VectorInt;
using System.Collections.ObjectModel;
using System.Text;
using K_PathFinder.Serialization;
using K_PathFinder.Graphs;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

public struct CellDataMapValue : IEquatable<CellDataMapValue> {
    public readonly Cell from, connection;
    public readonly CellContentData data;

    public CellDataMapValue(Cell source, Cell connection, CellContentData data) {
        this.from = source;
        this.connection = connection;
        this.data = data;
    }

    public static bool operator ==(CellDataMapValue a, CellDataMapValue b) {
        return a.from == b.from && a.connection == b.connection && a.data == b.data;
    }
    public static bool operator !=(CellDataMapValue a, CellDataMapValue b) {
        return !(a == b);
    }


    public override int GetHashCode() {
        return data.GetHashCode();
    }

    public override bool Equals(object obj) {
        if (obj == null || !(obj is CellDataMapValue))
            return false;
        return Equals((CellDataMapValue)obj);
    }

    public bool Equals(CellDataMapValue other) {
        return this == other;
    }

    public override string ToString() {
        return data.ToString();
    }
}

namespace K_PathFinder.Graphs {
    public class Graph {
        public ChunkData chunk { get; private set; }
        public AgentProperties properties { get; private set; }
        public bool canBeUsed { get; private set; }

        public BattleGrid battleGrid = null;
        public List<Cover> covers = new List<Cover>();
        public List<JumpPortalBase> portalBases = new List<JumpPortalBase>();

        private List<Cell> _cells = new List<Cell>();
        private List<Cell>[][] _mapCell;


        public CellDataMapValue[][][] dataMap;
        //public List<CellDataMapValue>[][] dataMap;
        private Dictionary<CellContentData, Cell> _contourLib;
        public List<KeyValuePair<CellContentData, Cell>>[] borderData = new List<KeyValuePair<CellContentData, Cell>>[4];
        private Graph[] _neighbours = new Graph[4];

        public Graph() {
            for (int i = 0; i < 4; i++) {
                borderData[i] = new List<KeyValuePair<CellContentData, Cell>>();
            }
        }

        public Graph(ChunkData chunk, AgentProperties properties) : this() {
            this.chunk = chunk;
            this.properties = properties;
        }

        //this function will disconnect graph and remove it's data from here and there
        public void OnDestroyGraph() {
            canBeUsed = false; 

            for (int i = 0; i < _cells.Count; i++) {
                _cells[i].OnGraphDestruction();
            }

            //connections
            //since right now connections are only made by Cell this part is easy. just take all connections and disconnect wich have interconnection flag
            HashSet<CellContent> interconnections = new HashSet<CellContent>();

            foreach (var cell in _cells) {
                foreach (var connection in cell.connections) {
                    if (connection.interconnection)
                        interconnections.Add(connection);
                }
            }

            //map
            foreach (var cellContent in interconnections) {
                cellContent.connection.graph.RemoveEdgeFromMap(cellContent.from, cellContent.cellData);
            }

            //cell connections
            foreach (var cellContent in interconnections) {
                cellContent.connection.RemoveAllConnections(cellContent.from);
            }

            //grid
            if (battleGrid != null) {
                for (int i = 0; i < 4; i++) {
                    //lots of cheking it even exist
                    Graph neighbourGraph;
                    if (TryGetNeighbour((Directions)i, out neighbourGraph) == false)
                        continue;

                    //actial disconnection
                    Directions oppositeDirection = Enums.Opposite((Directions)i);
                    var points = neighbourGraph.battleGrid.GetBorderLinePoints(oppositeDirection);

                    foreach (var point in points) {
                        point.neighbours[(int)oppositeDirection] = null;
                    }
                }
            }
        }

        //this function will be callse when graph are finished in unity main thread
        public void OnFinishGraph() {
            canBeUsed = true;

            for (int i = 0; i < _cells.Count; i++) {
                _cells[i].OnGraphGenerationEnd();
            }
        }


        public void SetBunchOfData(List<Cell> passedCells, List<Cell>[][] map, Dictionary<CellContentData, Cell> contourLib) {
            _cells.AddRange(passedCells);
            _mapCell = map;
            _contourLib = contourLib;

            //**********************TEMP**********************//
            for (int i = 0; i < passedCells.Count; i++) {
                passedCells[i].debugID = i;
            }

            //dataMap = new List<CellDataMapValue>[PathFinder.CELL_GRID_SIZE][];
            //for (int x = 0; x < PathFinder.CELL_GRID_SIZE; x++) {
            //    dataMap[x] = new List<CellDataMapValue>[PathFinder.CELL_GRID_SIZE];
            //}

            dataMap = new CellDataMapValue[PathFinder.CELL_GRID_SIZE][][];
            for (int x = 0; x < PathFinder.CELL_GRID_SIZE; x++) {
                dataMap[x] = new CellDataMapValue[PathFinder.CELL_GRID_SIZE][];
            }

            Vector2 chunkPos = chunk.realPositionV2;
      
            foreach (var cell in passedCells) {
                foreach (var pair in cell.dataContentPairs) {
                    if(pair.Value != null) {
                        if (pair.Value is CellContentGenericConnection)//skip jump connections since they dont participate in raycasting right now
                            AddEdgeToMap(cell, pair.Value.connection, pair.Key);
                    }
                    else {
                        AddEdgeToMap(cell, null, pair.Key);       
                    }
                }
            }




            //debug
            //float edgeMapPiselSize = PathFinder.CELL_GRID_SIZE / PathFinder.gridSize;
            //Vector3 realNotOffsetedPosition = chunk.realPositionV3;
            //for (int x = 0; x < PathFinder.CELL_GRID_SIZE + 1; x++) {
            //    Vector3 A = realNotOffsetedPosition + new Vector3(x * edgeMapPiselSize, 0, 0);
            //    Vector3 B = realNotOffsetedPosition + new Vector3(x * edgeMapPiselSize, 0, PathFinder.gridSize);
            //    Debuger_K.AddLine(A, B, Color.red);
            //}
            //for (int z = 0; z < PathFinder.CELL_GRID_SIZE + 1; z++) {
            //    Vector3 A = realNotOffsetedPosition + new Vector3(0, 0, z * edgeMapPiselSize);
            //    Vector3 B = realNotOffsetedPosition + new Vector3(PathFinder.gridSize, 0, z * edgeMapPiselSize);
            //    Debuger_K.AddLine(A, B, Color.red);
            //}
            //Vector3 chunkPosition = chunk.realPositionV3;
            //float chunkPixelSize = PathFinder.gridSize / PathFinder.CELL_GRID_SIZE;
            //for (int x = 0; x < PathFinder.CELL_GRID_SIZE; x++) {
            //    for (int z = 0; z < PathFinder.CELL_GRID_SIZE; z++) {
            //        Vector3 p = chunkPosition + new Vector3((x * chunkPixelSize) + (chunkPixelSize * 0.5f), 0, (z * chunkPixelSize) + (chunkPixelSize * 0.5f));
            //        if (dataMap[x][z] != null) {
            //            string s = "|";
            //            for (int i = 0; i < dataMap[x][z].Length; i++) {
            //                s += dataMap[x][z][i].connection == null ? "-" : "+";
            //            }
            //            Debuger_K.AddLabel(p, s);
            //        }
            //        else {
            //            Debuger_K.AddLabel(p, "null");
            //        }
            //        Debuger_K.AddDot(p);
            //    }
            //}
            //**********************TEMP**********************//


        }

        //**********************TEMP**********************//
        CellDataMapValue tempCellDataMapValue;
        Cell tempCellConnection;
        private void AddEdgeToMap(Cell origin, Cell connection, CellContentData data) {
            //Debuger_K.AddLine(origin.centerV3, SomeMath.MidPoint(data.leftV3, data.centerV3), Color.blue);
            //Debuger_K.AddLine(origin.centerV3, SomeMath.MidPoint(data.rightV3, data.centerV3), Color.red);

            tempCellDataMapValue = new CellDataMapValue(origin, connection, data);
            DDARasterization.DrawLine(data.a.x - chunk.realX, data.a.z - chunk.realZ, data.b.x - chunk.realX, data.b.z - chunk.realZ, PathFinder.CELL_GRID_SIZE / PathFinder.gridSize, AddEdgeToMapDelegate);
        }

        /// <summary>
        /// IMPORTANT:
        /// Sort so empty edges are at begining
        /// </summary>
        private void AddEdgeToMapDelegate(int x, int z) {
            x = SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, x);
            z = SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, z);

            CellDataMapValue[] arr = dataMap[x][z];
            if (arr == null)
                arr = new CellDataMapValue[0];
            CellDataMapValue[] newArr = new CellDataMapValue[arr.Length + 1];
            for (int i = 0; i < arr.Length; i++) {
                newArr[i] = arr[i];
            }
            newArr[arr.Length] = tempCellDataMapValue;
            Array.Sort(newArr, SortCellDataMapValue);            
            dataMap[x][z] = newArr;
            
            //if(dataMap[x][z] == null)
            //    dataMap[x][z] = new List<CellDataMapValue>();
            //dataMap[x][z].Add(tempCellDataMapValue);
            //dataMap[x][z].Sort(SortCellDataMapValue);//place connections with null at the end and with connection at begining
        }
        private int SortCellDataMapValue(CellDataMapValue x, CellDataMapValue y) {
            //Less than 0 x is less than y.
            //0 x equals y.
            //Greater than 0 x is greater than y
            if(x.connection != null) {
                if (y.connection != null)
                    return 0;
                else
                    return -1;
            }
            else {
                if (y.connection == null)
                    return 0;
                else
                    return +1;
            }
        }
        private void RemoveEdgeFromMap(Cell connection, CellContentData data) {
            tempCellConnection = connection;
            DDARasterization.DrawLine(data.a.x - chunk.realX, data.a.z - chunk.realZ, data.b.x - chunk.realX, data.b.z - chunk.realZ, PathFinder.CELL_GRID_SIZE / PathFinder.gridSize, RemoveEdgeFromMapDelegate);
        }
        private void RemoveEdgeFromMapDelegate(int x, int z) {
            x = SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, x);
            z = SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, z);
            //var list = dataMap[SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, x)][SomeMath.Clamp(0, PathFinder.CELL_GRID_SIZE - 1, z)];
            //Debug.Log(x + ":"+ z);
            //Debug.Log(dataMap == null);
            //Debug.Log(dataMap[x] == null);
            //Debug.Log(dataMap[x][z] == null);

            List<CellDataMapValue> tempList = new List<CellDataMapValue>(dataMap[x][z]);
            for (int i = tempList.Count - 1; i >= 0; i--) {
                if (tempList[i].connection == tempCellConnection)
                    tempList.RemoveAt(i);
            }
            dataMap[x][z] = tempList.ToArray();

            //for (int i = list.Count - 1; i >= 0; i--) {
            //    if(list[i].connection == tempCellConnection)
            //        list.RemoveAt(i);
            //}
        }
        //**********************TEMP**********************//


        //for serialization
        public List<Cell>[][] getCellMap {
            get { return _mapCell; }
        }
        public Dictionary<CellContentData, Cell> getContour {
            get { return _contourLib; }
        }

        public void SetChunkAndProperties(ChunkData chunk, AgentProperties properties) {
            this.chunk = chunk;
            this.properties = properties;
        }

        public Graph GetNeighbour(Directions direction) {
            return _neighbours[(int)direction];
        }
        public bool TryGetNeighbour(Directions direction, out Graph graph) {
            graph = _neighbours[(int)direction];
            return graph != null;
        }

        public void SetNeighbour(Directions direction, Graph graph) {
            _neighbours[(int)direction] = graph;
            graph._neighbours[(int)Enums.Opposite(direction)] = this;
        }


        public void FunctionsToFinishGraphInUnityThread() {   
            CheckJumpConnections();
            CheckCellsForAdvancedAreas();
        }

        public void FunctionsToFinishGraphInPathfinderMainThread() {   
            //setting neighbours
            for (int i = 0; i < 4; i++) {
                Graph neighbour;
                if(PathFinder.TryGetGraphFrom(gridPosition, (Directions)i, properties, out neighbour)) {
                    SetNeighbour((Directions)i, neighbour);
                }                
            }

            ConnectBattleGrid(properties.battleGridDensity * (PathFinder.gridSize / properties.voxelsPerChunk) * 1.732051f);
            CheckCovers();

            if (_cells.Count != 0)
                MakeBorders(properties.maxStepHeight);
        }


        #region functions that create graph
        //takes edges and axis. check if edge exist, if exist add closest point to cell
        public void AddPortal(IEnumerable<EdgeAbstract> edges, Vector3 axis) {
            Vector2 axisV2 = new Vector2(axis.x, axis.z);
            Dictionary<Cell, Vector3> cellMountPoints = new Dictionary<Cell, Vector3>();

            foreach (var abstractEdge in edges) {
                CellContentData data = new CellContentData(abstractEdge);

                Vector3 intersection;
                SomeMath.ClosestToSegmentTopProjection(data.a, data.b, axisV2, true, out intersection);

                foreach (var cell in _cells) {
                    if (cell.Contains(data)) {
                        if (cellMountPoints.ContainsKey(cell)) {
                            if (SomeMath.SqrDistance(cellMountPoints[cell], axis) > SomeMath.SqrDistance(intersection, axis))
                                cellMountPoints[cell] = intersection;
                        }
                        else
                            cellMountPoints.Add(cell, intersection);
                    }
                }
            }

            Vector2 normalRaw;

            switch (cellMountPoints.Count) {
                case 0:
                    return;
                case 1:
                    normalRaw = ToV2((cellMountPoints.First().Value - axis)).normalized * -1;
                    break;

                case 2:
                    normalRaw = (
                            ToV2(cellMountPoints.First().Value - axis).normalized +
                            ToV2(cellMountPoints.Last().Value - axis).normalized).normalized * -1;
                    break;
                default:
                    normalRaw = Vector2.left;
                    Dictionary<Cell, float> cellAngles = new Dictionary<Cell, float>();
                    Cell first = cellMountPoints.First().Key;
                    cellAngles.Add(first, 0f);

                    Vector3 firstDirV3 = cellMountPoints.First().Value - axis;
                    Vector2 firstDirV2 = ToV2(firstDirV3);

                    foreach (var pair in cellMountPoints) {
                        if (pair.Key == first)
                            continue;

                        Vector2 curDir = new Vector2(pair.Value.x - axis.x, pair.Value.z - axis.z);
                        cellAngles.Add(pair.Key, Vector2.Angle(firstDirV2, curDir) * Mathf.Sign(SomeMath.V2Cross(firstDirV2, curDir)));
                    }

                    normalRaw = (
                        ToV2(cellMountPoints[cellAngles.Aggregate((l, r) => l.Value > r.Value ? l : r).Key] - axis).normalized +
                        ToV2(cellMountPoints[cellAngles.Aggregate((l, r) => l.Value < r.Value ? l : r).Key] - axis).normalized).normalized * -1;
                    break;
            }

            portalBases.Add(new JumpPortalBase(cellMountPoints, axis, new Vector3(normalRaw.x, 0, normalRaw.y)));
        }

        public void AddCover(NodeCoverTemp coverInfo) {
            Cover cover = new Cover(coverInfo.positionV3, coverInfo.connection.positionV3, coverInfo.connectionType, coverInfo.normal);

            Vector3 agentNormalPoint = coverInfo.normal * properties.radius;

            foreach (var coverPoint in coverInfo.points) {
                Vector3 pointPlusAgentOffset = coverPoint.positionV3 + agentNormalPoint;
                HashSet<Cell> nearbyCells = new HashSet<Cell>();

                foreach (var edge in coverPoint.edges) {
                    CellContentData data = new CellContentData(edge);
                    foreach (var cell in _cells) {
                        if(cell.Contains(data))
                            nearbyCells.Add(cell);
                    } 
                }

                float closestSqrDistance = float.MaxValue;
                Cell closestCell = null;
                Vector3 closestPoint = Vector3.zero;

                foreach (var cell in nearbyCells) {
                    bool isOutside;
                    Vector3 currentPoint;
                    cell.GetClosestPointToCell(pointPlusAgentOffset, out currentPoint, out isOutside);

                    float curSqrDistance = SomeMath.SqrDistance(pointPlusAgentOffset, currentPoint);
                    if (curSqrDistance < closestSqrDistance) {
                        closestCell = cell;
                        closestPoint = currentPoint;
                        closestSqrDistance = curSqrDistance;
                    }
                }

                if (closestCell == null) {
                    //Debuger3.AddDot(coverPoint.positionV3, Color.red);
                    continue;
                }

                NodeCoverPoint coverNode = new NodeCoverPoint(coverPoint.positionV3, closestPoint, closestCell, cover);
                cover.AddCoverPoint(coverNode);
            }

            covers.Add(cover);
        }
        #endregion

        #region get closest
        public bool GetCell(float x, float y, float z, out Cell cell, out float resultY) {
            cell = null;
            resultY = 0;

            //if true then no result
            if (empty || _mapCell == null)        
                return false;            

            float scale = PathFinder.gridSize / PathFinder.CELL_GRID_SIZE;

            List<Cell> mapChunk = _mapCell
                [Mathf.Clamp((int)((x - chunk.realX) / scale), 0, PathFinder.CELL_GRID_SIZE - 1)]
                [Mathf.Clamp((int)((z - chunk.realZ) / scale), 0, PathFinder.CELL_GRID_SIZE - 1)];

            float sqrDist = float.MaxValue;

            //search cell in chunk are inside chunk
            if (mapChunk.Count > 0) {
                for (int i = 0; i < mapChunk.Count; i++) {
                    float curCellY;
                    //see if this point are inside cell and get position if are
                    if (mapChunk[i].GetPointInsideCell(x, z, out curCellY)) {
                        float curDiff = SomeMath.Difference(y, curCellY);
                        if (curDiff < sqrDist) {
                            sqrDist = curDiff;
                            cell = mapChunk[i];              
                            resultY = curCellY;
                        }
                    }
                }
            }
            
            return cell != null;
        }
        public bool GetCell(float x, float y, float z, out Cell cell, out Vector3 closestToCellPos) {
            float resultY;       
            bool result = GetCell(x, y, z, out cell, out resultY);
            closestToCellPos = new Vector3(x, resultY, z);
            return result;
        }
        public bool GetCell(Vector3 position, out Cell cell, out float resultY) {
            return GetCell(position.x, position.y, position.z, out cell, out resultY);
        }
        public bool GetCell(Vector3 position, out Cell cell, out Vector3 closestToCellPos) {
            float Y;
            bool result = GetCell(position.x, position.y, position.z, out cell, out Y);
            closestToCellPos = new Vector3(position.x, Y, position.z);      
            return result;
        }

        public bool GetClosestToHull(float x, float y, float z, out Cell cell, out Vector3 closestToOutlinePos) {
            cell = null;
            closestToOutlinePos = new Vector3();

            if (empty || _mapCell == null) 
                return false;            

            float sqrDist = float.MaxValue;

            foreach (var pair in _contourLib) {
                CellContentData val = pair.Key;
                Vector3 curNearest = pair.Key.NearestPoint(x, y, z);                
                float curSqrDist = SomeMath.SqrDistance(curNearest.x, curNearest.y, curNearest.z, x, y, z);

                if (curSqrDist < sqrDist) {
                    sqrDist = curSqrDist;
                    cell = pair.Value;
                    closestToOutlinePos = curNearest;
                }
            }
            return true;
        }
        public bool GetClosestToHull(Vector3 position, out Cell cell, out Vector3 closestToOutlinePos) {
            return GetClosestToHull(position.x, position.y, position.z, out cell, out closestToOutlinePos);
        }
                
        public bool GetClosestCell(float x, float y, float z, out Cell cell, out bool outsideCell, out Vector3 closestPoint) {
            if (empty || _mapCell == null) {
                outsideCell = true;
                cell = null;
                closestPoint = new Vector3();
                return false;
            }

            Cell getCell;
            Vector3 resultCell;

            if (GetCell(x, y, z, out getCell, out resultCell)) {
                Cell getHull;
                Vector3 resultHull;
                GetClosestToHull(x, y, z, out getHull, out resultHull);
                outsideCell = SomeMath.SqrDistance(x, y, z, resultCell.x, resultCell.y, resultCell.z) > SomeMath.SqrDistance(x, y, z, resultHull.x, resultHull.y, resultHull.z);

                if (outsideCell) {
                    closestPoint = resultHull;
                    cell = getHull;
                }
                else {
                    closestPoint = resultCell;
                    cell = getCell;
                }
            }
            else {
                GetClosestToHull(x, y, z, out cell, out closestPoint);
                outsideCell = true;
            }
            return true;
        }
        public bool GetClosestCell(Vector3 pos, out Cell cell, out bool outsideCell, out Vector3 closestPoint) {
            return GetClosestCell(pos.x, pos.y, pos.z, out cell, out outsideCell, out closestPoint);
        }
        #endregion

        #region acessors
        public ReadOnlyCollection<Cell> cells {
            get { return _cells.AsReadOnly(); }
        }

        public int x {
            get { return chunk.x; }
        }
        public int z {
            get { return chunk.z; }
        }
        public VectorInt.Vector2Int positionChunk {
            get { return chunk.position; }
        }

        public Vector3 positionCenter {
            get { return new Vector3(chunk.realX + (PathFinder.gridSize * 0.5f), 0, chunk.realZ + (PathFinder.gridSize * 0.5f)); }
        }

        public XZPosInt gridPosition {
            get { return chunk.xzPos; }
        }

        public bool empty {
            get { return _cells.Count == 0; }
        }
        #endregion        

        #region borders
        //we cant do this outside Graph so we store this data
        public void SetEdgeSide(CellContentData edge, Directions direction) {
            Cell cell = null;
            foreach (var curCell in cells) {
                if (curCell.Contains(edge)) {
                    cell = curCell;
                    break;
                }
            }

            if (cell == null)
                Debug.LogError("cell == null");

            borderData[(int)direction] .Add(new KeyValuePair<CellContentData, Cell>(edge, cell));
        }
        //version for deserialization cause we already know cell in that case
        public void SetEdgeSide(CellContentData edge, Directions direction, Cell cell) {
            borderData[(int)direction].Add(new KeyValuePair<CellContentData, Cell>(edge, cell));
        }

        private void MakeBorders(float yError) {
            for (int i = 0; i < 4; i++) {
                Directions directionFrom = (Directions)i;
                Directions directionTo = Enums.Opposite(directionFrom);

                Graph neighbourGraph;
                if (TryGetNeighbour(directionFrom, out neighbourGraph) == false)
                    continue;

                IEnumerable<KeyValuePair<CellContentData, Cell>> edgesFrom = GetBorderEdges(directionFrom);
                IEnumerable<KeyValuePair<CellContentData, Cell>> edgesTo = neighbourGraph.GetBorderEdges(directionTo);

                List<TempEdge> tempEdges = new List<TempEdge>();

                Axis projectionAxis;
                if(directionFrom == Directions.xMinus | directionFrom == Directions.xPlus) 
                    projectionAxis = Axis.z;                
                else
                    projectionAxis = Axis.x;


                foreach (var edgeFrom in edgesFrom) {
                    Cell cellFrom = edgeFrom.Value;

                    foreach (var edgeTo in edgesTo) {
                        Cell cellTo = edgeTo.Value;

                        CellContentData intersection;
                        if (CellContentData.Project(edgeFrom.Key, edgeTo.Key, yError, projectionAxis, out intersection) == false || intersection.pointed)
                            continue;

                        //Debuger_K.AddLine(cellFrom.centerV3, edgeFrom.edge.aPositionV3, Color.green);
                        //Debuger_K.AddLine(cellFrom.centerV3, edgeFrom.edge.bPositionV3, Color.green);

                        //Debuger_K.AddLine(intersection.aPositionV3, edgeFrom.edge.aPositionV3, Color.blue);
                        //Debuger_K.AddLine(intersection.bPositionV3, edgeFrom.edge.bPositionV3, Color.blue);

                        TempEdge curTempEdge = null;
                        for (int e = 0; e < tempEdges.Count; e++) {
                            if (tempEdges[e].from == cellFrom && tempEdges[e].to == cellTo)
                                curTempEdge = tempEdges[e];
                        }

                        if (curTempEdge == null) {
                            tempEdges.Add(new TempEdge(cellFrom, cellTo, intersection.a, intersection.b));
                        }
                        else {
                            curTempEdge.AddNodePos(intersection.a);
                            curTempEdge.AddNodePos(intersection.b);
                        }
                    }
                }
                foreach (var item in tempEdges) {
                    SetInterconnection(this, neighbourGraph, item.from, item.to, item.minus, item.plus);
                }
            }
        }

        private static void SetInterconnection(Graph graph1, Graph graph2, Cell cell1, Cell cell2, Vector3 node1, Vector3 node2) {
            Vector3 intersection;
            SomeMath.ClampedRayIntersectXZ(cell1.centerVector3, cell2.centerVector3 - cell1.centerVector3, node1, node2, out intersection);
            float cell1Cost = Vector3.Distance(cell1.centerVector3, intersection) * cell1.area.cost;
            float cell2Cost = Vector3.Distance(cell2.centerVector3, intersection) * cell2.area.cost;

            Vector3 leftPos, rightPos;

            if (SomeMath.LinePointSideMathf(new Vector2(node1.x, node1.z), new Vector2(node2.x, node2.z), cell1.centerVector2) > 0) {
                leftPos = node2;
                rightPos = node1;
            }
            else {
                leftPos = node1;
                rightPos = node2;
            }

            //Debuger_K.AddLabel(SomeMath.MidPoint(leftPos, cell1.centerV3), "L");
            //Debuger_K.AddLabel(SomeMath.MidPoint(rightPos, cell1.centerV3), "R");

            //Debuger_K.AddLabel(SomeMath.MidPoint(rightPos, cell2.centerV3), "L");
            //Debuger_K.AddLabel(SomeMath.MidPoint(leftPos, cell2.centerV3), "R");

            CellContentData C1C2data = new CellContentData(leftPos, rightPos);
            CellContentData C2C1data = new CellContentData(rightPos, leftPos);

            CellContentGenericConnection C1C2 = new CellContentGenericConnection(C1C2data, cell1, cell2, true, cell1Cost, cell2Cost, intersection);
            CellContentGenericConnection C2C1 = new CellContentGenericConnection(C2C1data, cell2, cell1, true, cell2Cost, cell1Cost, intersection);
            
            cell1.SetContent(C1C2);
            cell2.SetContent(C2C1);

            cell1.graph.AddEdgeToMap(cell1, cell2, C1C2data);
            cell2.graph.AddEdgeToMap(cell2, cell1, C2C1data);

#if UNITY_EDITOR
            if (Debuger_K.doDebug) {
                Debuger_K.AddEdgesInterconnected(graph1.x, graph1.z, graph1.properties, C1C2);
                Debuger_K.AddEdgesInterconnected(graph2.x, graph2.z, graph2.properties, C2C1);
            }
#endif
        }   
        
        //public for serialization
        public IEnumerable<KeyValuePair<CellContentData, Cell>> GetBorderEdges(Directions dir) {
            return borderData[(int)dir];
        }        
        #endregion

        private void CheckJumpConnections() {
            if (empty || portalBases.Count == 0)
                return;

            float rad = properties.radius;
            float radSqr = rad * rad;

            LayerMask mask = properties.includedLayers;

            float jumpUpSqr = properties.JumpUp * properties.JumpUp;
            float jumpDownSqr = properties.JumpDown * properties.JumpDown;
            float maxCheckDistance = Math.Max(jumpUpSqr, jumpDownSqr);

            float sampleStep = PathFinder.gridSize / properties.voxelsPerChunk;
            int sampleSteps = Mathf.RoundToInt(properties.radius / sampleStep) + 2;//plus some extra
            float bottomOffset = 0.2f;

            float agentHeightAjusted = properties.height - rad;
            float agentBottomAjusted = properties.radius + bottomOffset;
            RaycastHit hitCapsule, hitRaycast;

            if (agentHeightAjusted - agentBottomAjusted < 0) // somehow became spherical
                agentHeightAjusted = agentBottomAjusted;
      
            foreach (var portal in new List<JumpPortalBase>(portalBases)) {             
                Vector3 topAdd = new Vector3(0, agentBottomAjusted, 0);
                Vector3 bottomAdd = new Vector3(0, agentHeightAjusted, 0);
                Vector3 portalPosV3 = portal.positionV3;
                Vector3 mountPointBottom = portalPosV3 + topAdd;
                Vector3 mountPointTop = portalPosV3 + bottomAdd;

                if (Physics.CheckCapsule(mountPointBottom, mountPointTop, rad, properties.includedLayers) ||
                    (Physics.CapsuleCast(mountPointBottom, mountPointTop, rad, portal.normal, out hitCapsule, rad * 3, mask) &&
                    SomeMath.SqrDistance(ToV2(hitCapsule.point), portal.positionV2) < (rad * 3) * (rad * 3))) {
                    portalBases.Remove(portal);
                    continue;
                }

                for (int i = 0; i < sampleSteps; i++) {
                    Vector3 normalOffset = portal.normal * (properties.radius + (i * sampleStep));
                    Vector3 axisPoint = mountPointBottom + normalOffset;

                    if (Physics.Raycast(axisPoint, Vector3.down, out hitRaycast, maxCheckDistance, mask) == false)
                        continue;

                    Vector3 raycastHitPoint = hitRaycast.point;

                    if (Physics.CapsuleCast(axisPoint, mountPointTop + normalOffset, rad, Vector3.down, out hitCapsule, Mathf.Infinity, mask) == false)
                        continue;

                    if (SomeMath.SqrDistance(raycastHitPoint, hitCapsule.point) > radSqr)
                        continue;

                    if (SomeMath.SqrDistance(portal.positionV3 + normalOffset, hitCapsule.point) < radSqr || Vector3.Angle(Vector3.up, hitCapsule.normal) > properties.maxSlope)
                        continue;
                           
                    bool outside;
                    Cell closest;
                    Vector3 closestPos;

                    GetClosestCell(raycastHitPoint, out closest, out outside, out closestPos);

                    //Debuger3.AddLine(raycastHitPoint, closestPos, Color.red);
                    //Debuger3.AddRay(raycastHitPoint, Vector3.up, Color.magenta, 0.5f);

                    if (outside)
                        continue;
                    Cell cell;
                    bool outsideCell;
                    Vector3 closestPoint;
                    GetClosestCell(closestPos, out cell, out outsideCell, out closestPoint);

                    //Cell targetCell = GetClosestCellExpensive(raycastHitPoint, out closestPos);

                    if (SomeMath.SqrDistance(closestPos, hitCapsule.point) > radSqr) {
                        //portalBases.Remove(portal);
                        goto NEXT_PORTAL;
                    }

                    float fallSqrDistance = SomeMath.SqrDistance(portal.positionV3 + normalOffset, raycastHitPoint);

                  
                    if (fallSqrDistance < jumpUpSqr) {
                        foreach (var pair in portal.cellMountPoints) {
                            cell.SetContent(new CellContentPointedConnection(closestPos, raycastHitPoint, pair.Value, portal.positionV3, ConnectionJumpState.jumpUp, cell, pair.Key, false));
                            //cell.AddConnection(new CellJumpUpConnection(cell, pair.Key, closestPos, raycastHitPoint, portal.positionV3, pair.Value, false));
                            //cell.SetJumpUpConnection(pair.Key, closestPos, raycastHitPoint, portal.positionV3, pair.Value);
                        }
                    }

                    if (fallSqrDistance < jumpDownSqr) {
                        foreach (var pair in portal.cellMountPoints) {
                            pair.Key.SetContent(new CellContentPointedConnection(pair.Value, raycastHitPoint, closestPos, portal.positionV3, ConnectionJumpState.jumpDown, pair.Key, cell, false));
                            //pair.Key.AddConnection(new CellJumpDownConnection(pair.Key, cell, pair.Value, portal.positionV3, raycastHitPoint, closestPos, false));
                            //pair.Key.SetJumpDownConnection(cell, pair.Value, portal.positionV3, raycastHitPoint, closestPos);
                        }
                    }

                    goto NEXT_PORTAL;
                }


                //portalBases.Remove(portal);

                NEXT_PORTAL: {
                    continue;
                }
            }
        }

        private void CheckCellsForAdvancedAreas() {
            foreach (var cell in _cells) {
                if(cell.advancedAreaCell) {
                    (cell.area as AreaAdvanced).cells.Add(cell);
                }
            }
        }

        private void CheckCovers() {
            if (covers.Count == 0)
                return;

            for (int i = covers.Count - 1; i < 0; i--) {
                if (covers[i].coverPoints.Count == 0)
                    covers.RemoveAt(i);
            }
        }

        private void ConnectBattleGrid(float connectDistance) {       
            if (battleGrid == null)
                return;

            float connectionDistanceSqr = connectDistance * connectDistance;

            for (int i = 0; i < 4; i++) {
                Directions directionFrom = (Directions)i;
                Directions directionTo = Enums.Opposite(directionFrom);

                Graph neighbourGraph;
                if (TryGetNeighbour(directionFrom, out neighbourGraph) == false)
                    continue;

                BattleGrid neighbourBattleGrid = neighbourGraph.battleGrid;

                if (neighbourBattleGrid == null) {
                    Debug.LogWarningFormat("somehow i have battle grid but my neighbour is not. probably just empty graph. my positions is {0}", chunk.position.ToString());
                    continue;
                }     

                var ourBorder = battleGrid.GetBorderLinePoints(directionFrom);
                var neighbourBorder = neighbourBattleGrid.GetBorderLinePoints(directionTo);

                Axis projectionAxis = Axis.x;
                if (directionFrom == Directions.xPlus || directionFrom == Directions.xMinus)
                    projectionAxis = Axis.x;
                if (directionFrom == Directions.zPlus || directionFrom == Directions.zMinus)
                    projectionAxis = Axis.z;

                BattleGridPoint point;
                foreach (var ourBorderPoint in ourBorder) {
                    point = null;
                    float curClosestSqrDist = float.MaxValue;
                    foreach (var neighbourBorderPoint in neighbourBorder) {
                        if ((projectionAxis == Axis.x && ourBorderPoint.gridZ != neighbourBorderPoint.gridZ) ||
                            (projectionAxis == Axis.z && ourBorderPoint.gridX != neighbourBorderPoint.gridX))
                            continue;                 

                        float curSqrDist = SomeMath.SqrDistance(ourBorderPoint.positionV3, neighbourBorderPoint.positionV3);
              
                        if (curSqrDist < curClosestSqrDist && curSqrDist <= connectionDistanceSqr) {
                            point = neighbourBorderPoint;
                            curClosestSqrDist = curSqrDist;
                        }
                    }

                    if (point != null) {
                        ourBorderPoint.neighbours[(int)directionFrom] = point;
                        point.neighbours[(int)directionTo] = ourBorderPoint;
                    }
                }

                //for (int bi = 0; bi < ourBorder.Length; bi++) {
                //    foreach (var curPoint in ourBorder[bi]) {
                //        BattleGridPoint nbpn = null;
                //        float curClosestSqrDist = float.MaxValue;
                //        foreach (var neighbourPoint in neighbourBorder[bi]) {
                //            float curSqrDist = SomeMath.SqrDistance(curPoint.positionV3, neighbourPoint.positionV3);
                //            if(curSqrDist <= connectionDistanceSqr) {
                //                nbpn = neighbourPoint;
                //                curClosestSqrDist = curSqrDist;
                //            }
                //        }

                //        if(nbpn != null) {            
                //            curPoint.neighbours[(int)directionFrom] = nbpn;
                //            nbpn.neighbours[(int)directionTo] = curPoint;
                //        }
                //    }
                //}             
            }
        }
        

#if UNITY_EDITOR
        public void DebugGraph() {
            Debuger_K.AddCells(x, z, properties, cells);
            Debuger_K.AddCovers(x, z, properties, covers);
            Debuger_K.AddPortalBases(x, z, properties, portalBases);

            if (battleGrid != null)
                Debuger_K.AddBattleGrid(x, z, properties, battleGrid);

            if (Debuger_K.settings.autoUpdateSceneView)
                Debuger_K.UpdateSceneImportantThings();

        }
#endif
        private Vector2 ToV2(Vector3 v3) {
            return new Vector2(v3.x, v3.z);
        }
    }
}

