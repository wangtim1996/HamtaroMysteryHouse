using System.Collections.Generic;
using UnityEngine;

namespace K_PathFinder.Graphs {
    //class dedicated to take data only about cells, edges and nodes and composte it to final graph
    public class GraphCombiner{
        public NavMeshTemplateCreation template;        
        Dictionary<CellContentData, GenerationEdgeInfo> edges = new Dictionary<CellContentData, GenerationEdgeInfo>();
        List<GenerationCellInfo> cells = new List<GenerationCellInfo>();
        Graph graph;
        public NavmeshProfiler profiler;

        public GraphCombiner(NavMeshTemplateCreation template, Graph graph) {
            this.graph = graph;
            this.template = template;
            this.profiler = template.profiler;
        }

        public void ReduceCells() {
            Dictionary<Cell, GenerationCellInfo> cellsDic = new Dictionary<Cell, GenerationCellInfo>();

            foreach (var item in cells) {
                item.CalculateAngles();
                cellsDic.Add(item.cell, item);
            }
            #region Debug
            //foreach (var item in cells) {
            //    foreach (var c in item.connections) {
            //        Debuger_K.AddLine(c.from.centerV3, c.to.centerV3);
            //    }
            //}

            //foreach (var item in cells) {
            //    foreach (var n in item.nodes) {
            //        Debuger_K.AddLabel(n, item.Angle(n));
            //    }
            //}
            #endregion
            int breaker = 0;
            goto MR_LOOP;

            MR_LOOP:
            {
                breaker++;
                if (breaker > 50) {//no more than 50 iterations
                    //Debug.Log("breaker > 50");
                    return;
                }

                foreach (var thisCellInfo in cells) {
                    Cell thisCell = thisCellInfo.cell;

                    foreach (var connection in thisCellInfo.connections) {
                        Cell otherCell = connection.connection;
                        GenerationCellInfo otherCellInfo = cellsDic[otherCell];

                        //connect only of all important properties are equal
                        if (thisCell.area != otherCell.area ||
                            thisCell.layer != otherCell.layer ||
                            thisCell.graph != otherCell.graph ||
                            thisCell.passability != otherCell.passability)
                            continue;

                        Vector3 left = connection.leftV3;
                        Vector3 right = connection.rightV3;

                        // merging cells
                        if ((thisCellInfo.Angle(left) + otherCellInfo.Angle(left)) < 180f &&
                            (thisCellInfo.Angle(right) + otherCellInfo.Angle(right)) < 180f) {            
         
                            //collecting all edges
                            List<GenerationEdgeInfo> newEdges = new List<GenerationEdgeInfo>();
                            newEdges.AddRange(thisCellInfo.edges);
                            newEdges.AddRange(otherCellInfo.edges);

                            //we dont need edge where we connect cells so here we remove it
                            GenerationEdgeInfo removeMe = GetEdge(connection.cellData);
                            newEdges.Remove(removeMe);
                            newEdges.Remove(removeMe);                            

                            HashSet<Vector3> newNodes = new HashSet<Vector3>();
                            newNodes.UnionWith(thisCellInfo.nodes);
                            newNodes.UnionWith(otherCellInfo.nodes);               

                            thisCellInfo.connections.ForEach(con => cellsDic[con.connection].connections.RemoveAll(nb => nb.connection == thisCell));
                            otherCellInfo.connections.ForEach(con => cellsDic[con.connection].connections.RemoveAll(nb => nb.connection == otherCell));

                            cells.Remove(thisCellInfo);
                            cells.Remove(otherCellInfo);
                            cellsDic.Remove(thisCell);
                            cellsDic.Remove(otherCell);

                            GenerationCellInfo newInfo = new GenerationCellInfo(thisCell.area, thisCell.passability, thisCell.layer, graph, newNodes, newEdges);
                            newInfo.CalculateAngles();
                            cellsDic.Add(newInfo.cell, newInfo);
                            cells.Add(newInfo);                       

                            foreach (var edge in newEdges) {
                                edge.SetCellToEdge(newInfo);
                            }
                            //Debug.Log("simplifyed");                      

                            goto MR_LOOP; //begin new cycle if we do something
                        }
                    }
                }
                return;
            }
        }

        //public void DebugMe() {
        //    foreach (var cell in cells) {
        //        foreach (var edge in cell.edges) {
        //            PFDebuger.Debuger_K.AddLine(cell.centerV3, SomeMath.MidPoint(edge.data.leftV3, edge.data.centerV3), Color.blue);
        //            PFDebuger.Debuger_K.AddLine(cell.centerV3, SomeMath.MidPoint(edge.data.rightV3, edge.data.centerV3), Color.red);
        //        }
        //    }
        //}

        public void CombineGraph() {
            if (cells.Count == 0)
                return;            

            List<Cell> combinedCells = new List<Cell>();

            if (profiler != null) profiler.AddLogFormat("Raw Cells {0}", cells.Count);

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();

            foreach (var rawCell in cells) {
                Cell cell = rawCell.cell;
                        
                //setting connection data
                foreach (var connection in rawCell.connections) {
                    cell.SetContent(connection);
                }

                //setting edges to have empty data
                stopWatch.Start();
                foreach (var e in rawCell.edges) {
                    cell.TryAddData(e.data);
                }
                stopWatch.Stop();

                combinedCells.Add(cell);
            }

            if (profiler != null) profiler.AddLogFormat("Cells adding time {0}", stopWatch.Elapsed);



            List<Cell>[][] cellMap = GenerateCellMap(PathFinder.CELL_GRID_SIZE, combinedCells);
            Dictionary<CellContentData, Cell> contourLib = GenerateContourLib();

            if (profiler != null) profiler.AddLog("Setting bunch of data to graph");
            graph.SetBunchOfData(combinedCells, cellMap, contourLib);
        }

        public Dictionary<CellContentData, Cell> GenerateContourLib() {
            Dictionary<CellContentData, Cell> lib = new Dictionary<CellContentData, Cell>();
            foreach (var cellInfo in cells) {
                foreach (var c in cellInfo.cell.dataContentPairs) {
                    if (c.Value == null)//lead to nowere
                        lib.Add(c.Key, cellInfo.cell);
                }
            }
            return lib;
        }
        
        public bool AddCell(List<Vector3> nodes, Area area, Passability passability, int layer) {
            CellContentData d = new CellContentData(nodes[0], nodes[1]);

            if (GetEdge(d).CellsContains(nodes) == false) {
                List<GenerationEdgeInfo> edges = new List<GenerationEdgeInfo>();

                for (int i = 0; i < nodes.Count - 1; i++) {
                    edges.Add(GetEdge(new CellContentData(nodes[i], nodes[i + 1])));
                }

                edges.Add(GetEdge(new CellContentData(nodes[nodes.Count - 1], nodes[0])));

                GenerationCellInfo newCell = new GenerationCellInfo(area, passability, layer, graph, nodes, edges);
                cells.Add(newCell);

                foreach (var edge in edges) {
                    edge.SetCellToEdge(newCell);
                }
                return true;
            }
            else
                return false;
        }
        
        GenerationEdgeInfo GetEdge(CellContentData d) {
            GenerationEdgeInfo result;
            if (edges.TryGetValue(d, out result) == false) {
                result = new GenerationEdgeInfo(d);
                edges.Add(d, result);
            }
            return result;
        }

        public List<Cell>[][] GenerateCellMap(int librarySize, List<Cell> cells) {
            bool[] tempMap = new bool[librarySize * librarySize];    
            List<Cell>[][] cellMap = new List<Cell>[librarySize][];
            Vector2 chunkPos = template.chunkData.realPositionV2;
            float totalSize = PathFinder.gridSize;
            float pixelSize = totalSize / librarySize;

            for (int x = 0; x < librarySize; x++) {
                cellMap[x] = new List<Cell>[librarySize];
                for (int z = 0; z < librarySize; z++) {
                    cellMap[x][z] = new List<Cell>();
                }
            }

            foreach (var cell in cells) {
                foreach (var edge in cell.data) {
                    //DDARasterization.Rasterize(chunkPos, ref tempMap, librarySize, pixelSize, cell.centerV2, edge.leftV2, edge.rightV2);
                    DDARasterization.Rasterize(tempMap, librarySize, pixelSize, 
                        cell.centerVector2.x, cell.centerVector2.y, 
                        edge.leftV2.x, edge.leftV2.y, 
                        edge.rightV2.x, edge.rightV2.y,
                        chunkPos.x, chunkPos.y);
                }

                for (int x = 0; x < librarySize; x++) {
                    for (int z = 0; z < librarySize; z++) { 
                                          
                        if (tempMap[x * librarySize + z]) {                    
                            cellMap[x][z].Add(cell);
                            tempMap[x * librarySize + z] = false;
                        }
                    }
                }
            }

            //for (int x = 0; x < librarySize + 1; x++) {
            //    Vector3 A = template.chunkData.realPositionV3 + new Vector3(x * pixelSize, 0, 0);
            //    Vector3 B = template.chunkData.realPositionV3 + new Vector3(x * pixelSize, 0, totalSize);
            //    PFDebuger.Debuger_K.AddLine(A, B, Color.red);
            //}

            //for (int z = 0; z < librarySize + 1; z++) {
            //    Vector3 A = template.chunkData.realPositionV3 + new Vector3(0, 0, z * pixelSize);
            //    Vector3 B = template.chunkData.realPositionV3 + new Vector3(totalSize, 0, z * pixelSize);
            //    PFDebuger.Debuger_K.AddLine(A, B, Color.red);
            //}

            //for (int x = 0; x < librarySize; x++) {
            //    for (int z = 0; z < librarySize; z++) {
            //        Vector3 pixelPos = template.chunkData.realPositionV3 + (new Vector3(x + 0.5f, 0, z + 0.5f) * pixelSize);
            //        PFDebuger.Debuger_K.AddDot(pixelPos, Color.cyan);
            //        PFDebuger.Debuger_K.AddLabel(pixelPos, cellMap[x][z].Count);

            //        foreach (var cell in cellMap[x][z]) {
            //            PFDebuger.Debuger_K.AddLine(pixelPos, cell.centerV3, Color.cyan);
            //        }
            //    }
            //}

            return cellMap;
        }
        
        public AgentProperties properties {
            get { return template.properties; }
        }

        class GenerationEdgeInfo {
            public CellContentData data;
            public int direction = -1; //-1 mean none

            //public GenerationCellInfo right, left;
            public GenerationCellInfo upCell, downCell;
            public GenerationEdgeInfo(CellContentData data) {
                this.data = data;
                Vector3 m = data.centerV3;

                //Debuger_K.AddLabel(SomeMath.MidPoint(m, data.leftV3), "Left");
                //Debuger_K.AddLabel(SomeMath.MidPoint(m, data.rightV3), "Right");

            }
            public GenerationEdgeInfo(CellContentData data, int direction) : this(data) {       
                this.direction = direction;
            }

            public bool CellsContains(IEnumerable<Vector3> input) {
                return (upCell != null && upCell.nodes.SetEquals(input)) | (downCell != null && downCell.nodes.SetEquals(input));
            }

            public void SetCellToEdge(GenerationCellInfo cell) {
                Vector3 closest;
                SomeMath.ClosestToSegmentTopProjection(data.leftV3, data.rightV3, cell.centerV2, out closest);
        
                if (SomeMath.LinePointSideMathf(data.leftV2, data.rightV2, cell.centerV2) > 0) {
                    //Debuger_K.AddLine(closest, cell.centerV3, Color.white);
                    //Debuger_K.AddLabel(SomeMath.MidPoint(closest, cell.centerV3), "Up");
                    upCell = cell;            
                }
                else {
                    //Debuger_K.AddLine(closest, cell.centerV3, Color.white);
                    //Debuger_K.AddLabel(SomeMath.MidPoint(closest, cell.centerV3), "Down");
                    downCell = cell;
                }

                if (upCell != null & downCell != null) {
                    Vector3 intersection;
                    SomeMath.ClampedRayIntersectXZ(upCell.centerV3, downCell.centerV3 - upCell.centerV3, data.leftV3, data.rightV3, out intersection);
                    float upCellCost = Vector3.Distance(upCell.centerV3, intersection) * upCell.cell.area.cost;
                    float downCellCost = Vector3.Distance(downCell.centerV3, intersection) * downCell.cell.area.cost;

                    downCell.SetConnection(new CellContentData(data.leftV3, data.rightV3), upCell.cell, downCellCost, upCellCost, intersection);
                    upCell.SetConnection(new CellContentData(data.rightV3, data.leftV3), downCell.cell, upCellCost, downCellCost, intersection);
                }
            }
        }

        class GenerationCellInfo {
            public Cell cell;
            public HashSet<Vector3> nodes;
            public List<GenerationEdgeInfo> edges;
            public List<CellContentGenericConnection> connections = new List<CellContentGenericConnection>();
            Dictionary<Vector3, float> angles = new Dictionary<Vector3, float>();

            public GenerationCellInfo(Area area, Passability passability, int layer, Graph graph, IEnumerable<Vector3> nodes, List<GenerationEdgeInfo> edges) {
                List<CellContentData> originalEdges = new List<CellContentData>();
                for (int i = 0; i < edges.Count; i++) {
                    originalEdges.Add(edges[i].data);
                }

                cell = new Cell(area, passability, layer, graph, originalEdges);
                this.nodes = new HashSet<Vector3>(nodes);
                this.edges = edges;

                Vector3 cellCenter = SomeMath.MidPoint(nodes);

                if (edges.Count > 3){
                    Dictionary<GenerationEdgeInfo, float> triangleArea = new Dictionary<GenerationEdgeInfo, float>();
                    Dictionary<GenerationEdgeInfo, Vector3> centers = new Dictionary<GenerationEdgeInfo, Vector3>();

                    float areaSum = 0;
                    foreach (var item in edges) {
                        Vector3 curTriangleCenter = SomeMath.MidPoint(cellCenter, item.data.leftV3, item.data.rightV3);
                        centers.Add(item, curTriangleCenter);
                        float curArea = Vector3.Cross(item.data.leftV3 - curTriangleCenter, item.data.rightV3 - curTriangleCenter).magnitude * 0.5f;
                        areaSum += curArea;
                        triangleArea.Add(item, curArea);
                    }

                    Vector3 actualCenter = Vector3.zero;

                    foreach (var item in edges) {
                        actualCenter += (centers[item] * (triangleArea[item] / areaSum));
                    }

                    cellCenter = actualCenter;
                }

                cell.SetCenter(cellCenter);
            }

            public Vector3 centerV3 {
                get { return cell.centerVector3; }
            }
            public Vector2 centerV2 {
                get { return cell.centerVector2; }
            }

            public void SetConnection(CellContentData data, Cell connectTo, float costFrom, float costTo, Vector3 intersection) {
                connections.Add(new CellContentGenericConnection(data, cell, connectTo, false, costFrom, costTo, intersection));
            }
                 
            public void CalculateAngles() {
                foreach (var node in nodes) {
                    CellContentData? data1 = null, data2 = null;
                    bool data1Full = false;

                    foreach (var edge in edges) {
                        CellContentData curData = edge.data;
                        if (curData.Contains(node)) {
                            if (!data1Full) {
                                data1 = curData;
                                data1Full = true;
                            }
                            else {
                                data2 = curData;
                                break;//bouth data full
                            }
                        }
                    }               
                
                    Vector3 a = data1.Value.leftV3 == node ? data1.Value.rightV3 : data1.Value.leftV3;
                    Vector3 b = data2.Value.leftV3 == node ? data2.Value.rightV3 : data2.Value.leftV3;
                    
                    angles[node] = Vector2.Angle(new Vector2(a.x - node.x, a.z - node.z), new Vector2(b.x - node.x, b.z - node.z));

                    //Vector3 u = new Vector3(0, 0.1f, 0);
                    //Debuger_K.AddDot(node, Color.red);
                    //Debuger_K.AddLine(node + u, a, Color.blue);
                    //Debuger_K.AddLine(node + u, b, Color.red);
                    //Debuger_K.AddLabel(node, angles[node]);
                }
            }  

            public float Angle(Vector3 node) {
                return angles[node];
            }
        }
    }
}