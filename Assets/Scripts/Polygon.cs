using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPolygon", menuName = "NetFolder/Polygon")]
public class Polygon : ScriptableObject
{
    public Vector2[] vertices;
}