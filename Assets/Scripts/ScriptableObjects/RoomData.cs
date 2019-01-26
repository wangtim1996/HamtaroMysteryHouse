using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    public IntVector dimensions;
    public List<IntVector> entrances;
}
