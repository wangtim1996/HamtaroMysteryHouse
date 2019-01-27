using UnityEngine;
using K_PathFinder.VectorInt ;
using System;
using K_PathFinder.Graphs;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif

namespace K_PathFinder {
//    public class PathFinderTemplateGraphFinish : PathFinderTemplate {
//        public Graph graph { get; private set; }
//        Action callBack;

//        public PathFinderTemplateGraphFinish(Graph graph) : base(graph.gridPosition, graph.properties) {
//            this.graph = graph;      
//        }

//        public void SetCallBack(Action callBack) {
//            this.callBack = callBack;
//        }

//        public override void Work() {        
//            if (graph == null) {
//                Debug.LogWarning("graph null");
//                callBack.Invoke();
//            }
        
//            graph.FunctionsToFinishGraphInPathfinderMainThread();
//            graph.FunctionsToFinishGraphInUnityThread();
//            callBack.Invoke();

//#if UNITY_EDITOR
//            if (Debuger_K.doDebug) 
//                graph.DebugGraph();            
//#endif
//        }  
//    }
}
