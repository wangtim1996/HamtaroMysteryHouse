using UnityEngine;
using System.Collections;
using K_PathFinder.VectorInt ;
using System.Collections.Generic;
using System;

//namespace K_PathFinder.GraphGeneration {
//    public class MarchingSquaresIterator {
//        public const int COVER_HASH = int.MaxValue - 1;

//        MarchingSquaresIteratorMode mode;
 
//        SquareEdge exitDirection;
//        int connectionType;

//        Volume volume;
//        VolumeDataPoint[] data;
//        //float[] heightMap;
//        int[] hashMap;
//        bool[] interestMap;
//        int mainX, mainZ, targetHash; 
    
//        public MarchingSquaresIterator(Volume volume, int leftBottomX, int leftBottomZ, int targetHash, MarchingSquaresIteratorMode mode) {
//            this.volume = volume;
//            this.mode = mode;
//            this.targetHash = targetHash;
//            mainX = leftBottomX;
//            mainZ = leftBottomZ;
//            data = volume.data;

//            switch (mode) {
//                case MarchingSquaresIteratorMode.area:
//                    hashMap = volume.hashMap;
//                    interestMap = volume.heightInterest;
//                    break;
//                case MarchingSquaresIteratorMode.cover:
//                    hashMap = volume.coverHashMap;
//                    interestMap = volume.coverHeightInterest;
//                    break;
//            }

//            SetConnectionType();
//            //Debug.Log("start connction type: " + connectionType);
//            SetStartParameters();
//        }

//        private void SetConnectionType() {
//            //84
//            //21
//            connectionType = 0;
            
//            if (hashMap[volume.GetIndex(mainX, mainZ)] == targetHash) {
//                switch (mode) {
//                    case MarchingSquaresIteratorMode.area:
//                        volume.SetState(mainX, mainZ, VoxelState.MarchingSquareArea, true);           
//                        break;
//                    case MarchingSquaresIteratorMode.cover:
//                        volume.SetState(mainX, mainZ, VoxelState.MarchingSquareCover, true);
//                        break;
//                }
//                connectionType |= 2;
//            }
//            if (hashMap[volume.GetIndex(mainX + 1, mainZ)] == targetHash)
//                connectionType |= 1;
//            if (hashMap[volume.GetIndex(mainX, mainZ + 1)] == targetHash)
//                connectionType |= 8;
//            if (hashMap[volume.GetIndex(mainX +1, mainZ+1)] == targetHash)
//                connectionType |= 4;
//        }

//        //clockwise
//        private void SetStartParameters() {
//            //starts from left bottom
//            switch (connectionType) {
//                case 2:
//                //00
//                //10
//                exitDirection = SquareEdge.Left;
//                break;

//                case 3:
//                //00
//                //11
//                exitDirection = SquareEdge.Left;
//                break;

//                case 10:
//                //10
//                //10
//                exitDirection = SquareEdge.Top;
//                break;

//                case 6:
//                //01
//                //10
//                exitDirection = SquareEdge.Right;
//                break;

//                case 14:
//                //11
//                //10
//                exitDirection = SquareEdge.Right;
//                break;

//                case 11:
//                //10
//                //11
//                exitDirection = SquareEdge.Top;
//                break;

//                case 7:
//                //01
//                //11
//                exitDirection = SquareEdge.Left;
//                break;

//                default:
//                    Debug.Log(hashMap[volume.GetIndex(mainX, mainZ)] + " : " + targetHash);
//                    Debug.Log("hit default in begin. current connection type is " + connectionType + ", main x: " + mainX + ", z: " + mainZ);
//                break;
//            }
//        }

//        public bool Iterate() {
//            //return sucsess
//            bool result = true;
//            SetConnectionType();

//            if (connectionType == 0 || connectionType == 15)
//                UnityEngine.Debug.LogError("thing happen: " + connectionType);

//            //SquareEdge enterDirection = exitDirection;


//            switch (connectionType) {
//                case 1:
//                //00
//                //01    

//                if (exitDirection == SquareEdge.Bottom) 
//                    exitDirection = SquareEdge.Left;
//                else if (exitDirection == SquareEdge.Right)
//                    exitDirection = SquareEdge.Top;
//                else
//                    result = false;
//                break;
//                case 2:
//                //00
//                //10
//                if (exitDirection == SquareEdge.Left)
//                    exitDirection = SquareEdge.Top;     
//                else if (exitDirection == SquareEdge.Bottom) 
//                    exitDirection = SquareEdge.Right;
//                else
//                    result = false;
//                break;

//                case 4:
//                //01
//                //00
//                if (exitDirection == SquareEdge.Right) 
//                    exitDirection = SquareEdge.Bottom;
//                else if (exitDirection == SquareEdge.Top)
//                    exitDirection = SquareEdge.Left;                
//                else
//                    result = false;
//                break;

//                case 8:
//                //10
//                //00
//                if (exitDirection == SquareEdge.Top) 
//                    exitDirection = SquareEdge.Right;
//                else if (exitDirection == SquareEdge.Left)
//                    exitDirection = SquareEdge.Bottom;
//                else
//                    result = false;
//                break;

//                case 3:
//                //00
//                //11
//                if (exitDirection == SquareEdge.Left) 
//                    exitDirection = SquareEdge.Left;
//                else if (exitDirection == SquareEdge.Right) 
//                    exitDirection = SquareEdge.Right;
//                else
//                    result = false;
//                break;

//                case 5:
//                //01
//                //01

//                if (exitDirection == SquareEdge.Bottom)   
//                    exitDirection = SquareEdge.Bottom;                
//                else if (exitDirection == SquareEdge.Top)
//                    exitDirection = SquareEdge.Top;
                
//                else
//                    result = false;
//                break;

//                case 12:
//                //11
//                //00
//                if (exitDirection == SquareEdge.Right) 
//                    exitDirection = SquareEdge.Right;
//                else if (exitDirection == SquareEdge.Left)
//                    exitDirection = SquareEdge.Left;
//                else
//                    result = false;
//                break;

//                case 10:
//                //10
//                //10

//                if (exitDirection == SquareEdge.Top)
//                    exitDirection = SquareEdge.Top;
//                else if (exitDirection == SquareEdge.Bottom) 
//                    exitDirection = SquareEdge.Bottom;
//                else
//                    result = false;
//                break;

//                case 6:
//                //01
//                //10
//                switch (exitDirection) {
//                    case SquareEdge.Bottom:
//                           //exitDirection = SquareEdge.Right;
//                            exitDirection = SquareEdge.Left;
//                            break;
//                    case SquareEdge.Top:
//                            exitDirection = SquareEdge.Right;
//                            //exitDirection = SquareEdge.Left;
//                            break;
//                    case SquareEdge.Left:               
//                            exitDirection = SquareEdge.Bottom;
//                            //exitDirection = SquareEdge.Top;
//                            break;
//                    case SquareEdge.Right:
//                            exitDirection = SquareEdge.Top;
//                            //exitDirection = SquareEdge.Bottom;
//                            break;
//                }

//                break;

//                case 9:
//                //10
//                //01
//                switch (exitDirection) {
//                    case SquareEdge.Bottom:
//                    exitDirection = SquareEdge.Left;
//                    break;
//                    case SquareEdge.Top:
//                    exitDirection = SquareEdge.Right;
//                    break;
//                    case SquareEdge.Left:
//                    exitDirection = SquareEdge.Bottom;     
//                    break;
//                    case SquareEdge.Right:
//                    exitDirection = SquareEdge.Top;
//                    break;
//                }

//                break;

//                case 14:
//                //11
//                //10
//                if (exitDirection == SquareEdge.Right)   
//                    exitDirection = SquareEdge.Top;               
//                else if (exitDirection == SquareEdge.Bottom)   
//                    exitDirection = SquareEdge.Left;
//                else
//                    result = false;     
//                break;
//                case 13:
//                //11
//                //01


//                if (exitDirection == SquareEdge.Bottom)                
//                    exitDirection = SquareEdge.Right;                
//                else if (exitDirection == SquareEdge.Left)              
//                    exitDirection = SquareEdge.Top;                
//                else
//                    result = false;

//                break;
//                case 11:
//                //10
//                //11


//                if (exitDirection == SquareEdge.Top)         
//                    exitDirection = SquareEdge.Left;                
//                else if (exitDirection == SquareEdge.Right)    
//                    exitDirection = SquareEdge.Bottom;           
//                else
//                    result = false;
   
//                break;
//                case 7:
//                //01
//                //11

//                if (exitDirection == SquareEdge.Left)         
//                    exitDirection = SquareEdge.Bottom;            
//                else if (exitDirection == SquareEdge.Top)              
//                    exitDirection = SquareEdge.Right;                
//                else
//                    result = false;     
//                break;

//                default:
//                    UnityEngine.Debug.Log("hit default. current connection type is " + connectionType);
//                result = false;
//                break;
//            }

//            //z /\
//            //x   >
//            //Debuger.Debuger3.AddLabel(fragmentMap[mainX][mainZ].GetRealMax(template) + (Vector3.up * 0.05f), connectionType + " : " + enterDirection + " : " + exitDirection);
//            //Debuger.Debuger3.AddDot(fragmentMap[mainX][mainZ].GetRealMax(template), Color.red, 0.01f);
//            switch (exitDirection) {
//                case SquareEdge.Left:
//                mainX += 1;
//                break;
//                case SquareEdge.Right:
//                mainX -= 1;
//                break;
//                case SquareEdge.Bottom:
//                mainZ += 1;
//                break;
//                case SquareEdge.Top:
//                mainZ -= 1;
//                break;
//            }      
      

//            return result;
//        }


//        private Vector2Int1Float GetExitVector() {
//            //  Z
//            //0 T 0
//            //L   R  X
//            //1 B 0
//            int count = 0;
//            float y = 0;
//            Vector2Int1Float position;
//            //get values are just for counting how much samples we take to calculate height
//            switch (exitDirection) {
//                case SquareEdge.Left:
//                    if (GetValues(mainX, mainZ, ref y))
//                        count++;
//                    if (GetValues(mainX, mainZ + 1, ref y))
//                        count++;
//                    position = new Vector2Int1Float(mainX * 2, count > 0 ? (y / count) : 0, (mainZ * 2) + 1);
//                    break;

//                case SquareEdge.Right:
//                    if (GetValues(mainX + 1, mainZ, ref y))
//                        count++;
//                    if (GetValues(mainX + 1, mainZ + 1, ref y))
//                        count++;
//                    position = new Vector2Int1Float((mainX + 1) * 2, count > 0 ? (y / count) : 0, (mainZ * 2) + 1);
//                    break;

//                case SquareEdge.Bottom:
//                    if (GetValues(mainX, mainZ, ref y))
//                        count++;
//                    if (GetValues(mainX + 1, mainZ, ref y))
//                        count++;
//                    position = new Vector2Int1Float((mainX * 2) + 1, count > 0 ? (y / count) : 0, mainZ * 2);
//                    break;

//                case SquareEdge.Top:
//                    if (GetValues(mainX, mainZ + 1, ref y))
//                        count++;
//                    if (GetValues(mainX + 1, mainZ + 1, ref y))
//                        count++;
//                    position = new Vector2Int1Float((mainX * 2) + 1, count > 0 ? (y / count) : 0, (mainZ + 1) * 2);
//                    break;

//                default:
//                    position = Vector2Int1Float.zero;
//                    break;
//            }
//            position = new Vector2Int1Float(position.x + 1, position.y, position.z + 1);
//            return position;
//        }

//        public void GetExitVector(out Vector2Int1Float position, ref HashSet<VolumeArea> areaOutput) {
//            //  Z
//            //0 T 0
//            //L   R  X
//            //1 B 0
//            //int count = 0;
//            //float y = 0;

//            if(volume.GetState(mainX, mainZ, VoxelState.InterconnectionArea)) {
//                foreach (var a in volume.volumeArea[volume.GetIndex(mainX, mainZ)]) {
//                    areaOutput.Add(a);
//                } 
//            }
//            if (volume.GetState(mainX + 1, mainZ, VoxelState.InterconnectionArea)) {
//                foreach (var a in volume.volumeArea[volume.GetIndex(mainX + 1, mainZ)]) {
//                    areaOutput.Add(a);
//                }
//            }
//            if (volume.GetState(mainX, mainZ + 1, VoxelState.InterconnectionArea)) {
//                foreach (var a in volume.volumeArea[volume.GetIndex(mainX, mainZ + 1)]) {
//                    areaOutput.Add(a);
//                }
//            }
//            if (volume.GetState(mainX + 1, mainZ + 1, VoxelState.InterconnectionArea)) {
//                foreach (var a in volume.volumeArea[volume.GetIndex(mainX + 1, mainZ + 1)]) {
//                    areaOutput.Add(a);
//                }
//            }
//            position = GetExitVector();

//            ////get values are just for counting how much samples we take to calculate height
//            //switch (exitDirection) {
//            //    case SquareEdge.Left:
//            //    if (GetValues(mainX, mainZ, ref y))
//            //        count++;
//            //    if (GetValues(mainX, mainZ + 1, ref y))
//            //        count++;
//            //    position = new Vector2Int1Float(mainX * 2, count > 0 ? (y / count) : 0, (mainZ * 2) + 1);
//            //    break;

//            //    case SquareEdge.Right:
//            //    if (GetValues(mainX + 1, mainZ, ref y))
//            //        count++;
//            //    if (GetValues(mainX + 1, mainZ + 1, ref y))
//            //        count++;
//            //    position = new Vector2Int1Float((mainX + 1) * 2, count > 0 ? (y / count) : 0, (mainZ * 2) + 1);
//            //    break;

//            //    case SquareEdge.Bottom:
//            //    if (GetValues(mainX, mainZ, ref y))
//            //        count++;
//            //    if (GetValues(mainX + 1, mainZ, ref y))
//            //        count++;
//            //    position = new Vector2Int1Float((mainX * 2) + 1, count > 0 ? (y / count) : 0, mainZ * 2);
//            //    break;

//            //    case SquareEdge.Top:
//            //    if (GetValues(mainX, mainZ + 1, ref y))
//            //        count++;
//            //    if (GetValues(mainX + 1, mainZ + 1, ref y))
//            //        count++;
//            //    position = new Vector2Int1Float((mainX * 2) + 1, count > 0 ? (y / count) : 0, (mainZ + 1) * 2);
//            //    break;

//            //    default:
//            //    position = Vector2Int1Float.zero;
//            //    break;
//            //}
//            //position = new Vector2Int1Float(position.x + 1, position.y, position.z + 1);
//        }

//        public void GetExitVectorCover(out Vector2Int1Float position, out int coverType) {
//            coverType = 0;
//            coverType = Math.Max(coverType, volume.coverType[volume.GetIndex(mainX, mainZ)]);
//            coverType = Math.Max(coverType, volume.coverType[volume.GetIndex(mainX + 1, mainZ)]);
//            coverType = Math.Max(coverType, volume.coverType[volume.GetIndex(mainX, mainZ + 1)]);
//            coverType = Math.Max(coverType, volume.coverType[volume.GetIndex(mainX + 1, mainZ + 1)]);
//            position = GetExitVector();
//        }
        
//        private bool GetValues(int x, int z, ref float y) {
//            if (interestMap[volume.GetIndex(x, z)]) {
//                y += data[volume.GetIndex(x, z)].max;
//                return true;
//            }
//            else
//                return false;
//        }

//#if UNITY_EDITOR
//        #region debug
//        //NavMeshTemplateRecast template;

//        //public void AddDebug(NavMeshTemplateRecast template) {
//        //    this.template = template;
//        //}

//        //private Vector3 GetFansyPoint(Fragment frag, xDirections xDir, zDirections zDir) {
//        //    float val = template.fragmentSize * 0.5f;
//        //    Vector3 offset = Vector3.zero;
//        //    switch (xDir) {
//        //        case xDirections.xPlus:
//        //        offset += new Vector3(val, 0, 0);
//        //        break;
//        //        case xDirections.xMinus:
//        //        offset += new Vector3(-val, 0, 0);
//        //        break;      
//        //    }
//        //    switch (zDir) {
//        //        case zDirections.zPlus:
//        //        offset += new Vector3(0, 0, val);
//        //        break;
//        //        case zDirections.zMinus:
//        //        offset += new Vector3(0, 0, -val);
//        //        break;
//        //    }
//        //    return FragmentVector(frag) + offset;
//        //}

//        //private Vector3 FragmentVector(Fragment frag) {
//        //    return frag.GetRealMax(template);
//        //}

//        //private bool GetValues(int x, int z, ref float y, ref int cover) {
//        //    cover = Mathf.Max(coverMap[x][z], cover);
//        //    if (interestMap[x][z]) {
//        //        y += heightMap[x][z];
//        //        return true;
//        //    }
//        //    else
//        //        return false;
//        //}


//        #endregion
//#endif
//    }
//}