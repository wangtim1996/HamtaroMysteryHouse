using K_PathFinder.Graphs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace K_PathFinder.Samples {
    //class to contain small tools that comon in examples
    public static class ExampleThings {

        //simple debug to show path
        //path started from agent position. 
        //information about agent stored in path
        //also path dont actualy remove points in just shift index. here are used shifted index to take actual points
        public static void PathToLineRenderer(LineRenderer line, Path path, float heightOffset) {
            if (path == null || path.count <= 0)
                return;

            Vector3 add = Vector3.up * heightOffset;//offset
            Vector3[] points = new Vector3[path.count + 1];//array for line renderer

            //Debug.Log(path.count);
            points[0] = path.owner.positionVector3; //add agent position
            for (int i = 0; i < path.count; i++) {
                points[i + 1] = path[i + path.currentIndex] + add;//path have acessor with indexes and node have implicit operator for vector2 and vector3. vector2 return (x,z)
            }
            //set values to line renderer
            line.positionCount = points.Length;
            line.SetPositions(points);
        }

        //create and return LineRenderer 
        public static LineRenderer GetLineRenderer(Material material, float width) {
            GameObject lineGO = new GameObject("line renderer");
            LineRenderer lineR = lineGO.AddComponent<LineRenderer>();
            lineR.startWidth = width;
            lineR.endWidth = width;
            lineR.material = material;
            return lineR;
        }
    }
}