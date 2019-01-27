using UnityEngine;
using System.Collections;

namespace K_PathFinder {
    public enum NavmeshRaycastResultType : int {
        BorderHit = 0,
        PassabilityChange = 1,
        AreaChange = 2,
        ReachMaxIteration = 3,
        ReachMaxDistance = 4,
        OutsideGraph = 5
    }

    public enum NavmeshRaycastResultType2 : int {
        Nothing = 0,            //return false 
        ReachMaxDistance = 1,   //return false
        OutsideGraph = 2,       //return true
        NavmeshBorderHit = 3,   //return true
        PassabilityChange = 4,  //return true
        AreaChange = 5          //return true
    }

    public enum TerrainCollectorType : int {
        CPU = 1,
        ComputeShader = 2
    }

    public enum ColliderCollectorType : int {
        CPU = 0,
        ComputeShader = 1
    }

    public enum MarchingSquaresIteratorMode {
        area,
        cover
    }

    public enum SquareEdge {
        Left,
        Right,
        Bottom,
        Top
    }

    public enum Directions : int {
        xPlus = 0, xMinus = 1, zPlus = 2, zMinus = 3
    };

    public enum Direction : int {
        Left = 0,
        Right = 1
    }

    public enum NavPointDirections : int {
        Left = -1,
        Middle = 0,
        Right = 1
    }

    public enum Axis : int {
        x = 0, z = 1
    };    

    public enum Passability : sbyte {
        Unwalkable = 0,
        Slope = 1,
        Crouchable = 2,
        Walkable = 3
    }

    public enum JumpPassability {
        Up = 1, Down = 2, UpDown = 3
    }

    public enum ConnectionJumpState : int {
        jumpUp = 0, jumpDown = 1
    }

    //byte flags
    public enum VoxelState : int {
        Delete = 1,
        MarchingSquareArea = 4,
        CheckForVoxelAreaFlag = 8,
        VoxelAreaFlagJumpOuter = 32,
        InterconnectionArea = 64,
        InterconnectionAreaflag = 128,
        MarchingSquareCover = 512,
        NearObstacle = 1024,
        NearCrouch = 2048,    
        CoverAreaNegtiveFlag = 4096,
        Tree = 8192,
        NearTree = 16384
    }

    public enum AreaType : int {
        Jump = 0,
        Cover = 1
    }

    public enum AgentDelegateMode {
        ThreadSafe,
        NotThreadSafe
    }

    public static class Enums {
        public static Directions Opposite(Directions dir) {
            switch (dir) {
                case Directions.xPlus:
                return Directions.xMinus;
                case Directions.xMinus:
                return Directions.xPlus;
                case Directions.zPlus:
                return Directions.zMinus;
                case Directions.zMinus:
                return Directions.zPlus;
                default:
                return Directions.xMinus;
            }
        }
    }

    //this enum used when fragments became graph hull
    public enum EdgeTempFlags : int {
        Directed = 1,
        Marker1 = 2,
        Marker2 = 4,
        DouglasPeukerMarker = 8,
        ConnectionsMarker = 16
    }

    //triangulator node with some aditional info
    public enum NodeTempFlags : int {
        Merge = 1,
        DouglasPeuckerWasHere = 2,
        Intersection = 4,
        simplyfierWasHere = 8,
        graphNode = 16,
        delete = 32,
        xPlusBorder = 64,
        xMinusBorder = 128,
        zPlusBorder = 256,
        zMinusBorder = 512,
        nearBorder = 1024,
        corner = 2048,
        keyMarker = 4096
    }
}
