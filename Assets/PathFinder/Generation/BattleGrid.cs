using UnityEngine;
using System.Collections.Generic;
using K_PathFinder.VectorInt ;

namespace K_PathFinder {
    public class BattleGrid {
        public int lengthX { get; private set; }
        public int lengthZ { get; private set; }
        List<BattleGridPoint> _points;
        //List<BattleGridPoint>[] xPlus, xMinus, zPlus, zMinus;

        List<BattleGridPoint> 
            _xPlus = new List<BattleGridPoint>(),
            _xMinus = new List<BattleGridPoint>(),
            _zPlus = new List<BattleGridPoint>(),
            _zMinus = new List<BattleGridPoint>();

        public BattleGrid(int lengthX, int lengthZ, IEnumerable<BattleGridPoint> points) {
            this.lengthX = lengthX;
            this.lengthZ = lengthZ;

            foreach (var p in points) {
                if (p.gridZ == 0)
                    _zMinus.Add(p);

                if (p.gridZ == lengthZ - 1)
                    _zPlus.Add(p);

                if (p.gridX == 0)
                    _xMinus.Add(p);

                if (p.gridX == lengthX - 1)
                    _xPlus.Add(p);
            }

            _points = new List<BattleGridPoint>(points);

            //xPlus = new List<BattleGridPoint>[lengthX];
            //xMinus = new List<BattleGridPoint>[lengthX];
            //zPlus = new List<BattleGridPoint>[lengthZ];
            //zMinus = new List<BattleGridPoint>[lengthZ];

            //for (int x = 0; x < lengthX; x++) {
            //    zPlus[x] = new List<BattleGridPoint>();
            //    zMinus[x] = new List<BattleGridPoint>();
            //}
            //for (int z = 0; z < lengthZ; z++) {
            //    xPlus[z] = new List<BattleGridPoint>();
            //    xMinus[z] = new List<BattleGridPoint>();
            //}

            //foreach (var p in points) {
            //    if (p.gridZ == 0)
            //        zMinus[p.gridX].Add(p);

            //    if (p.gridZ == lengthZ - 1)
            //        zPlus[p.gridX].Add(p);

            //    if (p.gridX == 0)
            //        xMinus[p.gridZ].Add(p);

            //    if (p.gridX == lengthX - 1)
            //        xPlus[p.gridZ].Add(p);
            //}   
        }

        public BattleGridPoint GetClosestPoint(Vector3 pos) {
            float minDist = float.MaxValue;
            BattleGridPoint result = null;
            foreach (var p in _points) {
                float curDist = SomeMath.SqrDistance(pos, p.positionV3);
                if(curDist < minDist) {
                    minDist = curDist;
                    result = p;
                }
            }            

            return result;
        }

        public List<BattleGridPoint> GetBorderLinePoints(Directions dir) {
            switch (dir) {
                case Directions.xPlus:
                    return _xPlus;
                case Directions.xMinus:
                    return _xMinus;
                case Directions.zPlus:
                    return _zPlus;
                case Directions.zMinus:
                    return _zMinus;
                default:
                    return null;
            }
        }

        //public List<BattleGridPoint>[] GetBorderLinePoints(Directions dir) {
        //    switch (dir) {
        //        case Directions.xPlus:
        //            return xPlus;
        //        case Directions.xMinus:
        //            return xMinus;
        //        case Directions.zPlus:
        //            return zPlus;
        //        case Directions.zMinus:
        //            return zMinus;
        //        default:
        //            return null;
        //    }
        //}

        public List<BattleGridPoint> points {
           get { return _points; }
        }
    }

    public class BattleGridPoint : IGraphPoint {
        public BattleGridPoint[] neighbours = new BattleGridPoint[4];
        public VectorInt.Vector2Int gridPos { get; private set; }
        public Passability passability { get; private set; }
        private Vector3 _pos;

        public BattleGridPoint(Vector3 pos, Passability passability, VectorInt.Vector2Int gridPos) {
            this._pos = pos;
            this.passability = passability;
            this.gridPos = gridPos;
        }

        public Vector3 positionV3 {
            get { return _pos; }
        }

        public Vector2 positionV2 {
            get { return new Vector2(_pos.x, _pos.z); }
        }

        public float x {
            get { return _pos.x; }
        }

        public float y {
            get { return _pos.y; }
        }

        public float z {
            get { return _pos.z; }
        }

        public int gridX {
            get { return gridPos.x; }
        }

        public int gridZ {
            get { return gridPos.y; }
        }
    }    
}
