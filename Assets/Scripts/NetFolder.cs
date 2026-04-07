using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using UnityEngine;

public class NetFolder : MonoBehaviour
{
    public NetLayout layoutData;

    public Material netMaterial;
    public Material currentFaceMaterial;

    private List<NetShape> net = new List<NetShape>();
    private List<Face> faces = new List<Face>();

    public int currentFaceIndex = 1;

    private float opacity = 1f; //fully opaque

    private GameObject faceLabel;

    void Start()
    {
        Initialise();
    }

    public void Initialise()
    {
        Clear();
        CreateFacesFromLayout();
        CreateNet();

        // === Reset ALL materials ===
        foreach (var netShape in net)
        {
            var renderer = netShape.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.material = netMaterial;
        }

        // === Apply current face highlight ===
        if (currentFaceIndex >= 1 && currentFaceIndex < net.Count)
        {
            net[0].GetComponent<MeshRenderer>().material = currentFaceMaterial;
            net[currentFaceIndex].GetComponent<MeshRenderer>().material = currentFaceMaterial;
            ShowFaceLabel(currentFaceIndex);
            UnityEngine.Debug.Log($"Current Face: {currentFaceIndex + 1}");
        }

        ApplyOpacity();
    }



    void Update()
    {
        if (faceLabel != null && Camera.main != null && currentFaceIndex >= 1 && currentFaceIndex < net.Count)
        {
            // Recompute center of updated face mesh
            Vector3[] verts = net[currentFaceIndex].GetLocalVertices();
            Vector3 center = Vector3.zero;
            foreach (var v in verts)
                center += v;
            center /= verts.Length;

            // Update label position + orientation
            faceLabel.transform.position = center;
            //faceLabel.transform.rotation = center;
            faceLabel.transform.rotation = Quaternion.LookRotation(center - Camera.main.transform.position);

            //faceLabel.SetActive(!IsAnyFaceActivelyFolding());
        }

        if (Input.GetKey(KeyCode.R))
        {
            ResetNet();
        }

        if(Input.GetKey(KeyCode.Z))
        {
            UpdateFaces(false);
            if(faceLabel != null)
                faceLabel.SetActive(false);
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            if (faceLabel != null)
                faceLabel.SetActive(true);
        }
        else if (Input.GetKey(KeyCode.X))
        {
            UpdateFaces(true);
            if (faceLabel != null)
                faceLabel.SetActive(false);
        }
        else if(Input.GetKeyUp(KeyCode.X))
        {
            if (faceLabel != null)
                faceLabel.SetActive(true);
        }

        if (Input.GetKey(KeyCode.C))
        {
            UpdateFace(false, currentFaceIndex, true);
            if (faceLabel != null)
                faceLabel.SetActive(false);
        }
        else if(Input.GetKeyUp(KeyCode.C))
        {
            if (faceLabel != null)
                faceLabel.SetActive(true);
        }
        else if (Input.GetKey(KeyCode.V))
        {
            UpdateFace(true, currentFaceIndex, true);
            if (faceLabel != null)
                faceLabel.SetActive(false);
        }
        else if(Input.GetKeyUp(KeyCode.V))
        {
            if (faceLabel != null)
                faceLabel.SetActive(true);
        }

            float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f && faces.Count > 1)
        {
            // Clear old material if valid
            if (currentFaceIndex >= 1 && currentFaceIndex < net.Count)
                net[currentFaceIndex].GetComponent<MeshRenderer>().material = netMaterial;

            // Scroll forward (skipping index 0)
            currentFaceIndex++;
            if (currentFaceIndex >= net.Count || currentFaceIndex == 0)
                currentFaceIndex = 1;

            net[currentFaceIndex].GetComponent<MeshRenderer>().material = currentFaceMaterial;
            ShowFaceLabel(currentFaceIndex);
            UnityEngine.Debug.Log($"Current Face: {currentFaceIndex + 1}");
        }
        else if (scroll < 0f && faces.Count > 1)
        {
            if (currentFaceIndex >= 1 && currentFaceIndex < net.Count)
                net[currentFaceIndex].GetComponent<MeshRenderer>().material = netMaterial;

            currentFaceIndex--;
            if (currentFaceIndex <= 0)
                currentFaceIndex = net.Count - 1;

            net[currentFaceIndex].GetComponent<MeshRenderer>().material = currentFaceMaterial;
            ShowFaceLabel(currentFaceIndex);
            UnityEngine.Debug.Log($"Current Face: {currentFaceIndex + 1}");
        }



        if (Input.GetKey(KeyCode.F))
        {
            opacity = Mathf.Max(0.1f, opacity - Time.deltaTime); // decrease to small opacity value. Don't want it to be invisible.
            ApplyOpacity();
        }
        else if (Input.GetKey(KeyCode.G))
        {
            opacity = Mathf.Min(1f, opacity + Time.deltaTime); // increase
            ApplyOpacity();
        }

    }

    bool IsAnyFaceActivelyFolding()
    {
        for (int i = 1; i < faces.Count; i++)
        {
            float angle = Mathf.Abs(faces[i].currentFoldAngle);
            if (angle > 0f && angle < 90f) // actively moving
                return true;
        }
        return false;
    }


    void ShowFaceLabel(int index)
    {
        // Clean up old label
        if (faceLabel != null)
            Destroy(faceLabel);

        if (index < 1 || index >= net.Count)
            return;

        Vector3[] verts = net[index].GetLocalVertices();
        Vector3 center = Vector3.zero;
        foreach (var v in verts) center += v;
        center /= verts.Length;

        
        faceLabel = CreateLabel((index+1).ToString(), center);
        faceLabel.transform.SetParent(net[index].transform, false);
    }

    GameObject CreateLabel(string text, Vector3 position)
    {
        GameObject labelObj = new GameObject("FaceLabel");
        TextMesh textMesh = labelObj.AddComponent<TextMesh>();

        textMesh.text = text;
        textMesh.fontSize = 50;
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.characterSize = 0.1f;

        labelObj.transform.position = position;
        return labelObj;
    }


    void Clear()
    {
        // Destroy all NetShape GameObjects
        foreach (NetShape shape in net)
        {
            if (shape != null)
                Destroy(shape.gameObject);
        }

        net.Clear();
        faces.Clear();
        //currentFaceIndex = 0; // reset selection
    }


    void CreateFacesFromLayout()
    {
        for (int i = 0; i < layoutData.faceList.Count; i++)
        {
            FaceData data = layoutData.faceList[i];

            Vector2[] verts2D = data.polygon.vertices;
            Vector3[] verts3D = ConvertToVector3XY(verts2D);

            if (i == 0)
                faces.Add(new Face(verts3D));
            else
                faces.Add(new Face(verts3D, faces[data.parentIndex], data.hingeIndexA, data.hingeIndexB));
        }
    }

    Vector3[] ConvertToVector3XY(Vector2[] input)
    {
        Vector3[] result = new Vector3[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = new Vector3(input[i].x, input[i].y, 0f);
        }
        return result;
    }

    Vector3[] ConvertToVector3XZ(Vector2[] input)
    {
        Vector3[] result = new Vector3[input.Length];
        for (int i = 0; i < input.Length; i++)
        {
            result[i] = new Vector3(input[i].x, 0f, input[i].y);
        }
        return result;
    }


    void CreateNet()
    {
        // === Create visual NetShapes ===
        foreach (Face face in faces)
        {
            GameObject faceObj = new GameObject("Face");

            //Add mesh components
            faceObj.AddComponent<MeshFilter>();
            faceObj.AddComponent<MeshRenderer>();

            faceObj.transform.SetParent(transform, false);
            NetShape ns = faceObj.AddComponent<NetShape>();
            ns.Setup(face, netMaterial);
            net.Add(ns);
        }
    }

    void ResetNet()
    {
        for (int i = 1; i < faces.Count; i++)
        {
            //Face face = faces[i];
            faces[i].currentFoldAngle = 0f;
            net[i].UpdateMesh(); // Update the corresponding mesh
            faces[i].folded = false;
        }
    }

    void UpdateFaces(bool foldClockwise)
    {
        for (int i = 1; i < faces.Count; i++)
        {
            UpdateFace(foldClockwise, i, false);
        }
    }


    void UpdateFace(bool foldClockwise, int i, bool singleFace)
    {
        float deltaAngle = 45f * Time.deltaTime;

        Face face = faces[i];

        float direction = foldClockwise ? 1 : -1;
        float flipped = face.flipped ? -1 : 1;
        float actualDirection = direction * flipped;

        // Only fold if it hasn't collided OR is folding in the opposite direction
        if (!(face.folded && Mathf.Sign(actualDirection) == Mathf.Sign(face.currentFoldAngle)))
        {
            face.currentFoldAngle += actualDirection * deltaAngle;

            net[i].UpdateMesh(); // Update the corresponding mes
            if (singleFace)
            {
                UpdateChildMeshes(i); // Update all children recursively
            }

            bool faceFolded = IsFaceColliding(face, net[i]);
            FaceIsFolded(face, faceFolded);
        }
    }

    void FaceIsFolded(Face face, bool folded)
    {
        face.folded = folded;

        if (face.parentFace != null)
        {
            FaceIsFolded(face.parentFace, folded);
        }
    }

    void UpdateChildMeshes(int parentIndex)
    {
        for (int i = parentIndex + 1; i < faces.Count; i++)
        {
            if (faces[i].parentFace == faces[parentIndex])
            {
                net[i].UpdateMesh();
                UpdateChildMeshes(i); // Recursive call
            }
        }
    }
    
    bool IsFaceColliding(Face face, NetShape shape)
    {
        Vector3[] testVerts = shape.GetLocalVertices();

        for (int i = 0; i < net.Count; i++)
        {
            if (faces[i] == face || faces[i].parentFace == face || face.parentFace == faces[i])
            {
                continue; // Skip self and related faces
            }

            Vector3[] otherVerts = net[i].GetLocalVertices();

            for (int j = 0; j < testVerts.Length; j++)
            {
                // Skip hinge vertices
                if (j == face.hingeA || j == face.hingeB)
                    continue;

                Vector3 v = testVerts[j];

                for (int k = 1; k < otherVerts.Length - 1; k++)
                {
                    Vector3 a = otherVerts[0];
                    Vector3 b = otherVerts[k];
                    Vector3 c = otherVerts[k + 1];

                    if (SharesAnyVertex(v, a, b, c))
                        continue;

                    if (IsPointInTriangle3D(v, a, b, c))
                    {
                        UnityEngine.Debug.Log($"Point-in-triangle collision at vertex {j} of face {faces.IndexOf(face)} with triangle on face {i}");
                        return true;
                    }
                }
            }
        }

        return false;
    }
    
    bool SharesAnyVertex(Vector3 v, Vector3 a, Vector3 b, Vector3 c)
    {
        const float epsilon = 0.0001f;
        return
            Vector3.Distance(v, a) < epsilon ||
            Vector3.Distance(v, b) < epsilon ||
            Vector3.Distance(v, c) < epsilon;
    }

    bool IsPointInTriangle3D(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 n = Vector3.Cross(b - a, c - a);
        float area = n.magnitude;
        if (area < 1e-6f) return false; // Degenerate

        float planeDist = Mathf.Abs(Vector3.Dot(n.normalized, p - a));
        if (planeDist > 0.005f) return false;

        // Project to 2D
        Vector3 u = (b - a).normalized;
        Vector3 v = Vector3.Cross(n.normalized, u);
        Vector2 p2 = new Vector2(Vector3.Dot(p - a, u), Vector3.Dot(p - a, v));
        Vector2 a2 = Vector2.zero;
        Vector2 b2 = new Vector2(Vector3.Dot(b - a, u), Vector3.Dot(b - a, v));
        Vector2 c2 = new Vector2(Vector3.Dot(c - a, u), Vector3.Dot(c - a, v));

        float denom = (b2.y - c2.y) * (a2.x - c2.x) + (c2.x - b2.x) * (a2.y - c2.y);
        float w1 = ((b2.y - c2.y) * (p2.x - c2.x) + (c2.x - b2.x) * (p2.y - c2.y)) / denom;
        float w2 = ((c2.y - a2.y) * (p2.x - c2.x) + (a2.x - c2.x) * (p2.y - c2.y)) / denom;
        float w3 = 1 - w1 - w2;

        return w1 >= -0.001f && w2 >= -0.001f && w3 >= -0.001f;
    }
    
    void ApplyOpacity()
    {
        //UnityEngine.Debug.Log($"[ApplyOpacity] Setting opacity = {opacity}");

        if (netMaterial.HasProperty("_Opacity"))
        {
            netMaterial.SetFloat("_Opacity", opacity);
        }
        else
        {
            UnityEngine.Debug.LogWarning("netMaterial does NOT have Opacity property");
        }

        if (currentFaceMaterial.HasProperty("_Opacity"))
        {
            currentFaceMaterial.SetFloat("_Opacity", opacity);
        }
        else
        {
            UnityEngine.Debug.LogWarning("currentFaceMaterial does NOT have Opacity property");
        }
    }
    
}