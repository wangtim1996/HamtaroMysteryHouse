using UnityEngine;
using System.Collections;
using K_PathFinder.VectorInt ;

namespace K_PathFinder {
    //public abstract class PathFinderTemplate {
    //    public XZPosInt gridPosition;
    //    public AgentProperties properties { get; private set; }
    //    public bool stop {get; private set; }

    //    public PathFinderTemplate(XZPosInt gridPosition, AgentProperties properties) {
    //        this.stop = false;
    //        this.gridPosition = gridPosition;
    //        this.properties = properties;
    //    }

    //    public abstract void Work();

    //    public bool Match(XZPosInt gridPosition, AgentProperties properties) {
    //        return this.gridPosition == gridPosition && this.properties == properties;
    //    }

    //    public override bool Equals(object obj) {
    //        PathFinderTemplate otherTemplate = (PathFinderTemplate)obj;

    //        if (otherTemplate == null)
    //            return false;

    //        return Match(otherTemplate.gridPosition, otherTemplate.properties);
    //    }
    //    public override int GetHashCode() {
    //        return base.GetHashCode();
    //    }

    //    public void Stop() {
    //        stop = true;
    //    }        

    //    public int gridPosX {
    //        get { return gridPosition.x; }
    //    }


    //    public int gridPosZ {
    //        get { return gridPosition.z; }
    //    }
    //}
}
