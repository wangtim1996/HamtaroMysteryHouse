using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTile
{
    // Entrances are hallways that connect with rooms
    public enum Type
    {
        eRoom,
        eHallway,
        eEntrance,
        eNone
    }

    // just for entrances to match rooms
    public int id;

    public Type type = Type.eNone;

    public IntVector coords;

    public MapTile()
    {
        type = Type.eNone;
    }

    public MapTile(MapTile other)
    {
        this.coords = other.coords;
        this.type = other.type;
        this.id = other.id;
    }

    public MapTile(IntVector coords)
    {
        this.coords = coords;
        this.type = Type.eNone;
    }

    public MapTile(int x, int y)
    {
        this.coords = new IntVector(x, y);
        this.type = Type.eNone;
    }

}
