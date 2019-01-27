using UnityEngine;

using System.Linq;
using System.Collections.Generic;

using K_PathFinder.Graphs;

namespace K_PathFinder.PathGeneration {
    public class CellPath {
        public List<Cell> path;
        public List<CellContent> connections;
        float[] start;
        float[] end;
        //PathInformationRaw pathInfo;

        public float g { get; set; }
        public float h { get; set; }
        public float gh {
            get { return g + h; }
        }

        public CellPath(Cell first, Vector3 start) {
            this.path = new List<Cell>();
            this.path.Add(first);
            this.connections = new List<CellContent>();
            this.start = new float[3] { start.x, start.y, start.z };
        }
        public CellPath(CellPath existedPath, CellContent newConnection) {
            this.start = existedPath.start;
            this.connections = new List<CellContent>(existedPath.connections);
            this.connections.Add(newConnection);
            this.path = new List<Cell>(existedPath.path);
            this.path.Add(newConnection.connection);
        }

        public Cell last {
            get { return path.Last(); }
        }
        public Vector3 getStart {
            get { return new Vector3(start[0], start[1], start[2]); }
        }
        public Vector3 getEnd {
            get { return new Vector3(end[0], end[1], end[2]); }
        }

        public bool Contains(Cell cell) {
            return path.Contains(cell);
        }
        public void SetLastStop(Vector3 end) {
            this.end = new float[3] { end.x, end.y, end.z };
        }
    }    
}