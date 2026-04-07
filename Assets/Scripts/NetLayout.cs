using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNetLayout", menuName = "NetFolder/Net Layout")]
public class NetLayout : ScriptableObject
{
    public List<FaceData> faceList;
}