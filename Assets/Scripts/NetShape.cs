//using System.Diagnostics;
using System.Collections.Generic;
using UnityEngine;

public class NetShape : MonoBehaviour
{
    //public bool folded;

    private Mesh mesh;
    private Face face;

    public void Setup(Face face, Material material)
    {
        this.face = face;
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = material;

        mesh.vertices = face.localVertices;
        mesh.triangles = TriangulateConvexPolygon(face.localVertices);
        mesh.RecalculateNormals();
    }

    public void UpdatePolygonMesh(Vector2[] vertices2D)
    {
        Vector3[] verts3D = new Vector3[vertices2D.Length];
        for (int i = 0; i < vertices2D.Length; i++)
            verts3D[i] = new Vector3(vertices2D[i].x, vertices2D[i].y, 0f);

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        mesh.Clear();
        mesh.vertices = verts3D;
        mesh.triangles = TriangulateConvexPolygon(verts3D);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

    }


    int[] TriangulateConvexPolygon(Vector3[] verts)
    {
        List<int> tris = new List<int>();
        for (int i = 1; i < verts.Length - 1; i++)
        {
            tris.Add(0);
            tris.Add(i);
            tris.Add(i + 1);
        }
        return tris.ToArray();
    }

    public void UpdateMesh()
    {
        mesh.vertices = GetLocalVertices();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    

    public Vector3[] GetLocalVertices()
    {
        Vector3[] verts = (Vector3[])face.localVertices.Clone();

        ApplyOwnFolding(verts);

        if (face.parentFace != null)
            ApplyParentFolding(face.parentFace, verts);

        return verts;
    }

    

    void ApplyParentFolding(Face parent, Vector3[] vertices)
    {
        Vector3 hingeA = parent.localVertices[parent.hingeA];
        Vector3 hingeB = parent.localVertices[parent.hingeB];
        Vector3 hingeAxis = (hingeB - hingeA).normalized;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 relative = vertices[i] - hingeA;
            Quaternion rotation = Quaternion.AngleAxis(parent.currentFoldAngle, hingeAxis);
            vertices[i] = hingeA + rotation * relative;
        }

        if (parent.parentFace != null)
            ApplyParentFolding(parent.parentFace, vertices);
    }

    void ApplyOwnFolding(Vector3[] vertices)
    {
        // Apply folding rotation
        Vector3 hingeA = vertices[face.hingeA];
        Vector3 hingeB = vertices[face.hingeB];
        Vector3 hingeAxis = (hingeB - hingeA).normalized;

        for (int i = 0; i < vertices.Length; i++)
        {
            if (i == face.hingeA || i == face.hingeB)
                continue;

            if (face.hingeA == face.hingeB)
                Debug.LogWarning($"[Face] Skipping folding: hingeA == hingeB for face with {face.localVertices.Length} verts");

            Vector3 relative = vertices[i] - hingeA;
            Quaternion rotation = Quaternion.AngleAxis(face.currentFoldAngle, hingeAxis);
            vertices[i] = hingeA + rotation * relative;
        }
    }
}
