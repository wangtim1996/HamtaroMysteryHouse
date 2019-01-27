using K_PathFinder.Graphs;
using K_PathFinder.PFTools;
using System;
using System.Collections.Generic;
using UnityEngine;
//using PathFinder3.Debuger;

namespace K_PathFinder.PathGeneration {
    public class InfoTemplateBattleGrid : InfoTemplateAbstract, IThreadPoolWorkBatcherMember, IObjectPoolMember {
        //int depth;
        //Vector3[] points;

        public InfoTemplateBattleGrid() { }

        //IObjectPoolMember
        public void Clear() {
            base.ClearBase();
        }

        //IThreadPoolWorkBatcherMember
        public void PerformWork(object context) {
            WorkContext con = (WorkContext)context;
            Vector3[] points = con.points;
            int depth = con.depth;

            SetBase(con.agent);

            HashSet<BattleGridPoint> result = new HashSet<BattleGridPoint>();

            for (int i = 0; i < points.Length; i++) {
                Cell cellStart;
                if (PathFinder.TryGetClosestCell(points[i], agent.properties, out cellStart) == false) 
                    continue;                

                Graph curGraph = cellStart.graph;
                if (curGraph != null && curGraph.battleGrid != null)
                    result.Add(curGraph.battleGrid.GetClosestPoint(points[i]));
            }

            HashSet<BattleGridPoint> lastIteration = new HashSet<BattleGridPoint>();
            foreach (var item in result) {
                lastIteration.Add(item);
            }

            HashSet<BattleGridPoint> curIteration;

            for (int i = 0; i < depth; i++) {
                curIteration = new HashSet<BattleGridPoint>();

                foreach (var item in lastIteration) {
                    foreach (var nb in item.neighbours) {
                        if (nb == null)
                            continue;

                        if (result.Add(nb))
                            curIteration.Add(nb);
                    }
                }
                lastIteration = curIteration;
            }

            agent.RecieveBattleGrid(result);

            if(con.callBack != null)
                con.callBack.Invoke();
        }

        public struct WorkContext {
            public readonly PathFinderAgent agent;
            public readonly int depth;
            public readonly Vector3[] points;
            public readonly Action callBack;

            public WorkContext(PathFinderAgent agent, int depth, Vector3[] points, Action callBack) {
                this.agent = agent;
                this.depth = depth;
                this.points = points;
                this.callBack = callBack;
            }
        }
    }
}