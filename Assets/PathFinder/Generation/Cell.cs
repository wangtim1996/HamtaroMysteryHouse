using UnityEngine;
using System.Collections.Generic;
using K_PathFinder.CoverNamespace;

namespace K_PathFinder.Graphs {
    //convex mesh with some additional data
    public class Cell {
        public int debugID;

        public int layer { get; private set; }//cell layer
        public Area area { get; private set; }//cell area reference

        public bool advancedAreaCell;
        public List<CellPathContentAbstract> pathContent = null;

        public Passability passability { get; private set; }
        public Graph graph { get; private set; }//graph it's belong. dont need outside generation but still can have some uses

        //cell center are center of it's mesh area
        public Vector3 centerVector3 { get; private set; }
        public Vector2 centerVector2 { get; private set; }

        public HashSet<NodeCoverPoint> covers { get; private set; }
        public bool canBeUsed; //if not it will be ignored in path generation

        Dictionary<CellContentData, CellContent> _contentDictionary = new Dictionary<CellContentData, CellContent>();//cell edges
        List<CellContent> _connections = new List<CellContent>();//cell connections
        List<CellContentData> _originalEdges; //just cell edges without jump spots to recreate cell if needed

        public Cell(Area area, Passability passability, int layer, Graph graph, IEnumerable<CellContentData> originalEdges) {
            this.area = area;
            this.passability = passability;
            this.layer = layer;
            this.graph = graph;
            _originalEdges = new List<CellContentData>(originalEdges);

            foreach (var oe in originalEdges) {
                _contentDictionary.Add(oe, null);
            }

            advancedAreaCell = area is AreaAdvanced;
            if (advancedAreaCell) {
                pathContent = new List<CellPathContentAbstract>();
            }
        }

        public void OnGraphDestruction() {
            lock (this) {
                if (advancedAreaCell) {
                    (area as AreaAdvanced).RemoveCell(this);
                }

                canBeUsed = false;
            }
        }

        public void OnGraphGenerationEnd() {
            lock (this) {
                if (advancedAreaCell) {
                    IEnumerable<CellPathContentAbstract> content;
                    (area as AreaAdvanced).AddCell(this, out content, out canBeUsed);
                    lock (content) {
                        pathContent.AddRange(content);
                    }
                }
                else {
                    canBeUsed = true;
                }              
            }
        }

        public void SetCanBeUsed(bool value) {
            lock (this)
                canBeUsed = value;
        }
        
        #region Cell Path Content
        public void AddPathContent(CellPathContentAbstract content) {
            lock (this) {
                pathContent.Add(content);
                content.OnAddingToCell(this);
            }
        }

        public void AddPathContent(IEnumerable<CellPathContentAbstract> content) {
            lock (this) {
                pathContent.AddRange(content);
                foreach (var item in content) {
                    item.OnAddingToCell(this);
                }
            }
        }

        public void RemovePathContent(CellPathContentAbstract content) {
            lock (this) {
                pathContent.Remove(content);
                content.OnRemovingFromCell(this);
            }
        }

        public void RemovePathContent(IEnumerable<CellPathContentAbstract> content) {
            lock (this) {
                foreach (var item in content) {
                    pathContent.Remove(item);
                    item.OnRemovingFromCell(this);
                }
            }
        }
        #endregion
        

        public void SetCenter(Vector3 center) {
            centerVector3 = center;
            centerVector2 = new Vector2(center.x, center.z);
        }
        public void AddCover(NodeCoverPoint cover) {
            if (covers == null)
                covers = new HashSet<NodeCoverPoint>();

            covers.Add(cover);
        }

        #region data
        public void RemoveAllConnections(Cell target) {         
            for (int i = _connections.Count - 1; i >= 0; i--) {
                if (_connections[i].connection == target) {
                    CellContentData data = _connections[i].cellData;                
                    _connections.RemoveAt(i);
                   
                    //if edge was presented before we add this connection then it will remain in dictionary
                    //else we remove it
                    if (_originalEdges.Contains(data)) {
                        _contentDictionary[data] = null;
                    }
                    else {
                        _contentDictionary.Remove(data);
                    }
                }
            }
        }
        public void TryAddData(CellContentData d) {
            if (!_contentDictionary.ContainsKey(d))
                _contentDictionary.Add(d, null);
        }
        public void TryAddData(IEnumerable<CellContentData> ds) {
            foreach (var d in ds) {
                if (!_contentDictionary.ContainsKey(d))
                    _contentDictionary.Add(d, null);
            }
        }
        public void SetContent(CellContent cc) {
            lock (this) {
                _contentDictionary.Remove(cc.cellData); //make sure sides are on correct side
                _contentDictionary.Add(cc.cellData, cc);
                _connections.Add(cc);
            }
        }
        public void Remove(CellContentData d) {
            CellContent c = _contentDictionary[d];
            if (c != null)
                _connections.Remove(c);
            if (!_contentDictionary.Remove(d)) {
                Debug.LogError("false");
            }
        }
        public bool Contains(CellContentData data) {
            return _contentDictionary.ContainsKey(data);
        }
        #endregion

        #region acessors
        public IEnumerable<CellContentData> originalEdges {
            get { return _originalEdges; }
        }
        public IEnumerable<KeyValuePair<CellContentData, CellContent>> dataContentPairs {
            get { return _contentDictionary; }
        }
        public IEnumerable<CellContentData> data {
            get { return _contentDictionary.Keys; }
        }
        public IEnumerable<CellContent> connections {
            get { return _connections; }
        }
        #endregion

        #region closest
        /// <summary>
        /// check all cell triangles and return Y position if point inside of any triangle
        /// </summary>
        /// <param name="y">result Y if point inside cell</param>
        /// <returns>true if point inside cell</returns>
        public bool GetPointInsideCell(float x, float z, out float y) {
            CellContentData edgeData;
            foreach (var pair in _contentDictionary) {
                edgeData = pair.Key;
                if (SomeMath.PointInTriangle(centerVector2.x, centerVector2.y, edgeData.xLeft, edgeData.zLeft, edgeData.xRight, edgeData.zRight, x, z)) {
                    y = SomeMath.CalculateHeight(edgeData.leftV3, edgeData.rightV3, centerVector3, x, z);
                    return true;
                }
            }
            y = 0;
            return false;
        }  
        public bool GetPointInsideCell(Vector2 targetPos, out float y) {
            return GetPointInsideCell(targetPos.x, targetPos.y, out y);
        }
        public bool GetPointInsideCell(Vector3 targetPos, out float y) {
            return GetPointInsideCell(targetPos.x, targetPos.z, out y);
        }
        
        //Vector2
        public void GetClosestPointToCell(Vector2 targetPos, out Vector3 closestPoint, out bool isOutsideCell) {
            float closestSqrDistance = float.MaxValue;
            closestPoint = Vector3.zero;

            foreach (var edgeData in _contentDictionary.Keys) {
                if (SomeMath.PointInTriangle(edgeData.leftV2, edgeData.rightV2, centerVector2, targetPos)) {
                    closestPoint = new Vector3(targetPos.x, SomeMath.CalculateHeight(edgeData.leftV3, edgeData.rightV3, centerVector3, targetPos.x, targetPos.y), targetPos.y);
                    isOutsideCell = false;
                    return;
                }
                else {
                    Vector3 curInte;
                    SomeMath.ClosestToSegmentTopProjection(edgeData.leftV3, edgeData.rightV3, targetPos, true, out curInte);
                    float curSqrDist = SomeMath.SqrDistance(targetPos.x, targetPos.y, curInte.x, curInte.z);

                    if (curSqrDist < closestSqrDistance) {
                        closestSqrDistance = curSqrDist;
                        closestPoint = curInte;
                    }                    
                }
            }
            isOutsideCell = true;
            return;
        }
        //vector3
        public void GetClosestPointToCell(Vector3 targetPos, out Vector3 closestPoint, out bool isOutsideCell) {
            GetClosestPointToCell(new Vector2(targetPos.x, targetPos.z), out closestPoint, out isOutsideCell);
        }     
        #endregion
    }
}
