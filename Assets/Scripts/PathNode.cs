using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    public PathNode prevNode;
    public MapTile tile;
    public int totalCost;

    public PathNode(PathNode prev, MapTile tile, int cost)
    {
        this.prevNode = prev;
        this.tile = tile;
        this.totalCost = cost;
    }
}
