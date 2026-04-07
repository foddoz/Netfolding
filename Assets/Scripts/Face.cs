
using System.Diagnostics;
using UnityEngine;

public class Face
{
    public Vector3[] localVertices;
    public int hingeA, hingeB;

    public float currentFoldAngle;

    public Face parentFace; //Attach this face to another face!
    public bool isFolding;

    public bool flipped;

    public bool folded;

    public Face(Vector3[] localVerts) //Root face constructor, does not fold
    {
        localVertices = localVerts;
        this.hingeA = 0;
        this.hingeB = 1;
        parentFace = null;

        currentFoldAngle = 0f;

        isFolding = false;
        folded = false;
    }

    /*
    public Face(Vector3[] localVerts, Face parent, int parentHinge)
    {
        // === 1. Compute parent hinge vertices ===
        int parentHingeA = (parentHinge - 1 + parent.localVertices.Length) % parent.localVertices.Length;
        int parentHingeB = parentHinge % parent.localVertices.Length;

        Vector3 pA = parent.localVertices[parentHingeA];
        Vector3 pB = parent.localVertices[parentHingeB];

        flipped = false;

        // === 2. Attach and align child to parent hinge ===
        localVerts = AttachToParentEdge(localVerts, parent, parentHingeA, parentHingeB);

        
        
        // === 3. Flip if child and parent normals point the same way
        if (ShouldFlipFace(localVerts, parent, parentHingeA, parentHingeB))
        {
            Vector3 hingeAxis = (pB - pA).normalized;

            // === 1. Rotate 180° around hinge axis ===
            for (int i = 0; i < localVerts.Length; i++)
            {
                Vector3 relative = localVerts[i] - pA;
                Quaternion rotation = Quaternion.AngleAxis(180f, hingeAxis);
                localVerts[i] = pA + rotation * relative;
            }

            flipped = true;
            UnityEngine.Debug.Log("Face is flipped");
        }
        else
        {
            flipped = false;
            UnityEngine.Debug.Log("Face is not flipped");
        }

        // === 5. Final setup
        this.hingeA = 0;
        this.hingeB = 1;
        this.localVertices = localVerts;
        this.parentFace = parent;
        this.currentFoldAngle = 0f;

        isFolding = true;
        folded = false;
    }
    */

    public Face(Vector3[] localVerts, Face parent, int parentHingeA, int parentHingeB)
    {
        // === 1. Compute parent hinge vertices ===
        //int parentHingeA = (parentHinge - 1 + parent.localVertices.Length) % parent.localVertices.Length;
        //int parentHingeB = parentHinge % parent.localVertices.Length;

        Vector3 pA = parent.localVertices[parentHingeA];
        Vector3 pB = parent.localVertices[parentHingeB];

        flipped = false;

        // === 2. Attach and align child to parent hinge ===
        localVerts = AttachToParentEdge(localVerts, parent, parentHingeA, parentHingeB);



        // === 3. Flip if child and parent normals point the same way
        if (ShouldFlipFace(localVerts, parent, parentHingeA, parentHingeB))
        {
            Vector3 hingeAxis = (pB - pA).normalized;

            // === 1. Rotate 180° around hinge axis ===
            for (int i = 0; i < localVerts.Length; i++)
            {
                Vector3 relative = localVerts[i] - pA;
                Quaternion rotation = Quaternion.AngleAxis(180f, hingeAxis);
                localVerts[i] = pA + rotation * relative;
            }

            flipped = true;
            //UnityEngine.Debug.Log("Face is flipped");
        }
        else
        {
            flipped = false;
            //UnityEngine.Debug.Log("Face is not flipped");
        }

        // === 5. Final setup
        this.hingeA = 0;
        this.hingeB = 1;
        this.localVertices = localVerts;
        this.parentFace = parent;
        this.currentFoldAngle = 0f;

        isFolding = true;
    }




    Vector3[] AttachToParentEdge(Vector3[] localVerts, Face parent, int parentHingeA, int parentHingeB)
    {
        // === 1. Reference hinge on parent ===
        Vector3 pA = parent.localVertices[parentHingeA];
        Vector3 pB = parent.localVertices[parentHingeB];
        Vector3 parentHingeDir = (pB - pA).normalized;
        float parentHingeLength = Vector3.Distance(pA, pB);

        // === 2. Child hinge is assumed to be from [0] to [1]
        Vector3 lA = localVerts[0];
        Vector3 lB = localVerts[1];
        Vector3 localHingeDir = (lB - lA).normalized;
        float localHingeLength = Vector3.Distance(lA, lB);

        // === 3. Normalize localVerts to origin and scale to match parent hinge length ===
        Vector3[] transformed = new Vector3[localVerts.Length];
        for (int i = 0; i < localVerts.Length; i++)
        {
            transformed[i] = localVerts[i] - lA; // move hinge start to origin
            transformed[i] *= (parentHingeLength / localHingeLength); // scale uniformly
        }

        // === 4. Rotate child hinge to match parent hinge direction ===
        Quaternion alignRotation = Quaternion.FromToRotation(localHingeDir, parentHingeDir);
        for (int i = 0; i < transformed.Length; i++)
            transformed[i] = alignRotation * transformed[i];

        // === 5. Translate to match parent's hinge start
        for (int i = 0; i < transformed.Length; i++)
            transformed[i] += pA;

        // === 6. Check winding and apply flip if needed
        if (transformed.Length >= 3)
        {
            Vector3 hingeDir = (transformed[1] - transformed[0]).normalized;
            Vector3 test = transformed[2] - transformed[0];
            Vector3 normal = Vector3.Cross(hingeDir, test);

            if (Vector3.Dot(normal, Vector3.forward) > 0f)
            {
                Quaternion flip = Quaternion.AngleAxis(180f, hingeDir);
                for (int i = 0; i < transformed.Length; i++)
                {
                    Vector3 relative = transformed[i] - transformed[0];
                    transformed[i] = transformed[0] + flip * relative;
                }
                //UnityEngine.Debug.Log("[AttachToParentEdge] Face was flipped");
                flipped = true;
            }
        }

        return transformed;
    }

    
    private bool ShouldFlipFace(Vector3[] newVerts, Face parent, int parentHingeA, int parentHingeB)
    {
        if (newVerts.Length < 3 || parent.localVertices.Length < 3)
            return false;

        Vector3 parentA = parent.localVertices[parentHingeA];
        Vector3 parentB = parent.localVertices[parentHingeB];
        Vector3 parentEdge = (parentB - parentA).normalized;

        // Get normal of parent face using consistent winding
        Vector3 parentRight = parentEdge;
        Vector3 parentUp = parent.localVertices[(parentHingeB + 1) % parent.localVertices.Length] - parentB;
        Vector3 parentNormal = Vector3.Cross(parentRight, parentUp).normalized;

        // Get normal of child face
        Vector3 childNormal = Vector3.Cross(newVerts[1] - newVerts[0], newVerts[2] - newVerts[0]).normalized;

        // If child and parent normals point in the same direction, flip is needed
        return Vector3.Dot(parentNormal, childNormal) > 0.1f;
    }
}