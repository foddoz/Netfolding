//using System.Runtime.Remoting.Messaging;
using UnityEngine;

[System.Serializable]
public class FaceData
{
    [Header("Shape Type")]
    public Polygon polygon;

    [Header("Hinging")]
    public int parentIndex = -1;
    public int hingeIndexA = 0;
    public int hingeIndexB = 0;
}
