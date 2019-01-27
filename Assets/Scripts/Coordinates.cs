using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class IntVector
{
    public enum Rotation
    {
        e0,
        e90,
        e180,
        e270,
        eNumElem
    }

    public int x;
    public int y;

    public IntVector(IntVector other)
    {
        this.x = other.x;
        this.y = other.y;
    }

    public IntVector(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static IntVector operator+ (IntVector a, IntVector b)
    {
        return new IntVector(a.x + b.x, a.y + b.y);
    }

    public static IntVector operator- (IntVector a, IntVector b)
    {
        return new IntVector(a.x - b.x, a.y - b.y);
    }

    public static bool operator== (IntVector a, IntVector b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(IntVector a, IntVector b)
    {
        return !(a == b);
    }

    public static float GetRotationAngle (Rotation rot)
    {
        switch (rot)
        {
            case Rotation.e0:
                return 0;
            case Rotation.e90:
                return -90;
            case Rotation.e180:
                return -180;
            case Rotation.e270:
                return -270;
            default:
                Debug.LogError("BAD ROTATE");
                return 0;
        }
    }


    public IntVector RotateCentered(Rotation rot, IntVector center)
    {
        IntVector retVal = this - center;
        retVal = retVal.Rotate(rot);
        return retVal + center;
        
    }

    // rotate around 0,0
    private IntVector Rotate(Rotation rot)
    {
        switch (rot)
        {
            case Rotation.e0:
                return new IntVector(x, y);
            case Rotation.e90:
                return new IntVector(-y, x);
            case Rotation.e180:
                return new IntVector(-x, -y);
            case Rotation.e270:
                return new IntVector(y, -x);
            default:
                Debug.LogError("BAD ROTATE");
                return new IntVector(0, 0);
        }
    }

    // bounds are exclusive
    public bool IsInBounds(IntVector bounds)
    {
        return (x >= 0 && x < bounds.x && y >= 0 && y < bounds.y);
    }

}
