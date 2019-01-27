using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/TileGenAsset", order = 3)]
public class TileGenAssets : ScriptableObject
{
    public GameObject deadend;
    public GameObject turn;
    public GameObject line;
    public GameObject tIntersect;
    public GameObject fourway;
    public float scale = 5;
}
