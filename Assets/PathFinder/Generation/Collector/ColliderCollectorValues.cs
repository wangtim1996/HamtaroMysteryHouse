using UnityEngine;
using System.Collections.Generic;
using System;
using K_PathFinder.Collector;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

//class to store temp values
namespace K_PathFinder {
    //normal colliders
    public class MeshColliderInfo {
        public string name;
        public Vector3[] verts;
        public int[] tris;
        public Area area;
        public Matrix4x4 matrix;
        public Bounds bounds;
        public ColliderInfoMode infoMode;

        
        //public string tag;
        //public int layer;
        //public UserfulFilter filter;
        //public int priority;

        public MeshColliderInfo(
            Vector3[] verts, int[] tris, Area area, Matrix4x4 matrix, Bounds bounds, //very userful
            string name, ColliderInfoMode infoMode, string tag, int layer) {//semi userful
            this.verts = verts;
            this.tris = tris;
            this.area = area;
            this.matrix = matrix;
            this.bounds = bounds;
            this.name = name;
            this.infoMode = infoMode;
            //this.tag = tag;
            //this.layer = layer;
        }

        public MeshColliderInfo(
             Vector3[] verts, int[] tris, Area area, Matrix4x4 matrix, Bounds bounds, //very userful
             string name, ColliderInfoMode infoMode, string tag, int layer,  int priority) {//semi userful
            this.verts = verts;
            this.tris = tris;
            this.area = area;
            this.matrix = matrix;
            this.bounds = bounds;
            this.name = name;
            this.infoMode = infoMode;

            //this.tag = tag;
            //this.layer = layer;
            //this.filter = filter;
            //this.priority = priority;
        }
    }

    public class TerrainColliderInfoMesh {
        public float[,] heightMap;
        public int hSizeX, hSizeZ, resolution;
        public Matrix4x4 heightMatrix;

        public Terrain terrain;
        public List<ShapeDataAbstract> trees;

        public int[][] areaMap;
        public Area[] possibleArea;

        //bunch of values to calculate alpha map in thread
        public float[,,] alphaMap = null;

        //setted outside constrictor
        public TerrainNavmeshSettings settings;
        public int
            startXClamp, endXClamp,
            startZClamp, endZClamp,
            terrainStartX, terrainStartZ,
            alphaWidth, alphaHeight,
            alphaStartX, alphaStartZ,
            alphaSizeX, alphaSizeZ;

        public float terrainSizeX, terrainSizeZ;

        public TerrainColliderInfoMesh(Terrain terrain) {
            this.terrain = terrain;
        }
    }
 

    //trees
    public class TerrainTreeColliderData {
        public Vector3 worldPosition { get; private set; }
        public Bounds bounds { get; private set; }
        float heightScale, widthScale, colliderHeight, colliderRadius;
        int direction;
        Vector3 center;

        public TerrainTreeColliderData(Vector3 worldPosition, Bounds bounds, CapsuleCollider collider, TreeInstance instance) {
            this.worldPosition = worldPosition;
            this.bounds = bounds;
            this.colliderHeight = collider.height;
            this.colliderRadius = collider.radius;
            this.direction = collider.direction;
            this.center = collider.center;
            this.heightScale = instance.heightScale;
            this.widthScale = instance.widthScale;
        }

        public Vector3[] MoveThisCapsuleToWorldPosAndReturnPonts(Vector3[] capsulePoints) {
            Vector3[] result = new Vector3[capsulePoints.Length];
            Vector3[] points = capsulePoints;

            float scaledHeight = colliderHeight * heightScale;
            float scaledRadius = colliderRadius * widthScale;
            Quaternion rotation = Quaternion.Euler(direction == 2 ? 90f : 0f, 0f, direction == 0 ? 90f : 0f);
            Matrix4x4 rawMatrix = Matrix4x4.TRS(worldPosition + (center * heightScale), rotation, new Vector3(scaledRadius * 2f, scaledRadius * 2f, scaledRadius * 2f));
            float num = Mathf.Max(1f, scaledHeight / (scaledRadius * 2f)) - 2f;

            for (int i = 0; i < points.Length; i++) {
                result[i] = points[i];
                if (result[i].y > 0f)
                    result[i].y = Mathf.Max(result[i].y + (num * 0.5f), 0f);
                else if (result[i].y < 0f)
                    result[i].y = Mathf.Min(result[i].y - (num * 0.5f), 0f);

                result[i] = rawMatrix.MultiplyPoint(result[i]);
            }

            return result;
        }
    }

    

}
