using UnityEngine;
using System.Collections.Generic;

using K_PathFinder.Graphs;
using K_PathFinder.NodesNameSpace;
using K_PathFinder.EdgesNameSpace;

namespace K_PathFinder.CoverNamespace {
    public class Cover {
        //public NodeCover nodeLeft, nodeRight;   
        public List<NodeCoverPoint> coverPoints = new List<NodeCoverPoint>();
        public int coverType;
        public Vector3 normalV3 { get; private set; }
        public Vector3 left { get; private set; }
        public Vector3 right { get; private set; }

        //public Cover(NodeCover nodeLeft, NodeCover nodeRight, int covertype, Vector3 coverNormal) {
        //    this.nodeLeft = nodeLeft;
        //    this.nodeRight = nodeRight;
        //    this.coverType = covertype;
        //    this.normal = coverNormal;
        //    this.coverPoints = new List<NodeCoverPoint>();

        //    nodeLeft.AddCover(this);
        //    nodeRight.AddCover(this);
        //}

        public Cover(Vector3 left, Vector3 right, int covertype, Vector3 coverNormal) {
            this.left = left;
            this.right = right;
            this.coverType = covertype;
            this.normalV3 = coverNormal;
        }

        public void AddCoverPoint(NodeCoverPoint point) {
            coverPoints.Add(point);
        }

        public Vector2 normalV2 {
            get { return new Vector2(normalV3.x, normalV3.z); }
        }
    }

    //public class NodeCover : NodeAbstract {
    //    Cover belongA, belongB;
    //    public NodeCover(Vector3 pos) : base(pos.x, pos.y, pos.z) { }

    //    public void AddCover(Cover cover) {
    //        if (belongA == null) {
    //            belongA = cover;
    //        }
    //        else {
    //            if (belongB != null)
    //                Debug.LogError("check this out");
    //            belongB = cover;
    //        }
    //    }
    //}

    public class NodeCoverPoint : NodeAbstract {
        public Cover cover { get; private set; }
        public Cell cell { get; private set; }
        public Vector3 cellPos { get; private set; }

        public NodeCoverPoint(Vector3 position, Vector3 cellPoint, Cell cell, Cover cover) : base(position) {
            this.cover = cover;
            this.cellPos = cellPoint;
            this.cell = cell;
            cell.AddCover(this);
        }

        public int coverType {
            get { return cover.coverType; }
        }

        public Vector3 normalV3 {
            get { return cover.normalV3; }
        }

        public Vector2 normalV2 {
            get { return cover.normalV2; }
        }
    }

    public class NodeCoverTemp : NodeAbstract {
        public NodeCoverTemp connection { get; private set; }
        public int connectionType { get; private set; }
        public bool dpWasHere = false;
        public Vector3 normal { get; private set; }
        public List<NodeCoverPointTemp> points = null;


        public NodeCoverTemp(Vector3 pos) : base(pos.x, pos.y, pos.z) {
            connectionType = 0;
        }

        public void SetConnection(NodeCoverTemp connection, int connectionType) {
            this.connection = connection;
            this.connectionType = connectionType;
        }

        public void SetNormal(Vector3 normal) {            
            this.normal = normal;
        }

        public void AddCover(VolumeArea spot) {
            if (points == null)
                points = new List<NodeCoverPointTemp>();

            points.Add(new NodeCoverPointTemp(spot.position, spot.edges));
        }
    }

    public class NodeCoverPointTemp : NodeAbstract {
        public IEnumerable<EdgeAbstract> edges { get; private set; }

        public NodeCoverPointTemp(Vector3 point, IEnumerable<EdgeAbstract> edges) : base(point) {
            this.edges = edges;
        }
    }


}





