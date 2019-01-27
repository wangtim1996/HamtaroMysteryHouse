using UnityEngine;
using System.Collections;
using K_PathFinder.VectorInt ;
using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using K_PathFinder.PFDebuger;
#endif
//namespace K_PathFinder {
//    //threadsafe template that creates chunks
//    public class ChunkTemplate : WorkTemplate {
//        //public Action<Chunk> callBack;

//        HashSet<AgentProperties> afterList = new HashSet<AgentProperties>();
//        Action<Chunk, AgentProperties> coolDelegate;
//        Action<Chunk> generalCallback;

//        /// <summary>
//        /// template to create chunk
//        /// </summary>
//        public ChunkTemplate(
//            Vector2Int pos, 
//            AgentProperties properties, 
//            Action<Chunk, AgentProperties> coolDelegate) : base(pos, properties) {
//            this.coolDelegate = coolDelegate;
//        }

//        public void SetGeneralCallBack(Action<Chunk> generalCallback) {
//            this.generalCallback = generalCallback;
//        }

//        public void AddChunkGenerationQueue(AgentProperties properties) {
//            afterList.Add(properties);
//        }

//        public override void Work() {
//            if (stop)
//                return;

//            //guessing height if not provided
//            float gridSize = PathFinder.gridSize;
//            float e = gridSize * 0.5f;
//            Vector3 v3e = new Vector3(e, e, e);

//            float currentX = gridPosition.x * gridSize + e;
//            float currentZ = gridPosition.y * gridSize + e;

//            float cyrMinY = PathFinder.gridLowest * gridSize;
//            float cyrMaxY = PathFinder.gridHighest * gridSize;
//            float castDistance = Math.Abs(cyrMaxY - cyrMinY);

//            Vector3 sMax = new Vector3(currentX, cyrMaxY, currentZ);

//            LayerMask mask = properties.includedLayers;   
//            int minY, maxY;

//            RaycastHit[] hits = Physics.BoxCastAll(sMax, v3e, Vector3.down, Quaternion.identity, castDistance, mask);

//            if (hits.Length > 0) {
//                float highest = hits[0].point.y;
//                float lowest = hits[0].point.y;

//                if (hits.Length > 1) {
//                    for (int i = 1; i < hits.Length; i++) {
//                        if (hits[i].point.y > highest)
//                            highest = hits[i].point.y;

//                        if (hits[i].point.y < lowest)
//                            lowest = hits[i].point.y;
//                    }
//                }

//                minY = (int)(lowest / gridSize) - 1;
//                maxY = (int)(highest / gridSize);
//            }
//            else {
//                minY = 0;
//                maxY = 0;
//            }

//            if (stop)
//                return;

//            //creating chunk
//            Chunk newChunk = new Chunk(gridPosition, minY, maxY);

//            //setting neighbours
//            Chunk neightbour;
//            for (int i = 0; i < 4; i++) {
//                Directions dir = (Directions)i;
//                if (PathFinder.TryGetChunk(newChunk, dir, out neightbour))
//                    newChunk.SetNeighbour(dir, neightbour);
//            }

//            generalCallback.Invoke(newChunk);

//            foreach (var prop in afterList) {
//                coolDelegate.Invoke(newChunk, prop);
//            }

//            //callBack.Invoke(newChunk);
//        } 
//    }
//}