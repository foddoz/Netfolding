using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Diagnostics;

public class NetEditor : MonoBehaviour
{
    public TMP_Dropdown layoutDropdown; // assign in Inspector
    public NetLayout[] layoutOptions;   // assign multiple layouts in Inspector

    public NetFolder netFolder;

    private NetLayout layoutData; // The ScriptableObject to edit

    public GameObject rootFaceEntryPrefab; // Drag in via Inspector
    public GameObject faceEntryPrefab;

    public Transform listContainer;
    public Polygon[] availablePolygons; // Assign via inspector

    private List<GameObject> entryRows = new();

    void Start()
    {
        SetupLayoutDropdown();

        // Ensure layoutData is set from dropdown's initial value
        if (layoutOptions.Length > 0)
        {
            layoutDropdown.value = 0; // Trigger value to match default selection
            layoutData = layoutOptions[0];
            netFolder.layoutData = layoutData;
        }

        LoadLayout(); // Now safe to call
    }


    public void LoadLayout()
    {
        int preservedIndex = netFolder.currentFaceIndex;

        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        entryRows.Clear();

        for (int i = 0; i < layoutData.faceList.Count; i++)
        {
            FaceData faceData = layoutData.faceList[i];

            // Use root prefab for face 0, normal for others
            GameObject row = Instantiate(i == 0 ? rootFaceEntryPrefab : faceEntryPrefab, listContainer);
            row.transform.SetParent(listContainer, false);

            row.transform.Find("FaceIndex").GetComponent<TextMeshProUGUI>().text = $"Face {i + 1}";

            TMP_Dropdown polygonDropdown = row.transform.Find("PolygonDropdown").GetComponent<TMP_Dropdown>();
            SetupPolygonDropdown(polygonDropdown, faceData);

            if (i != 0) // Only setup hinging for non-root faces
            {
                IntStepper parentStepper = row.transform.Find("Hinging/ParentIndex").GetComponent<IntStepper>();
                IntStepper hingeEdgeStepper = row.transform.Find("Hinging/HingeEdge").GetComponent<IntStepper>();

                parentStepper.maxValue = Mathf.Max(0, i - 1);
                parentStepper.Value = Mathf.Clamp(faceData.parentIndex, 0, parentStepper.maxValue);

                Polygon parentPolygon = layoutData.faceList[parentStepper.Value].polygon;
                int vertCount = parentPolygon != null ? parentPolygon.vertices.Length : 0;

                int edgeIndex = faceData.hingeIndexA;
                hingeEdgeStepper.Value = Mathf.Clamp(edgeIndex, 0, vertCount - 1);
                hingeEdgeStepper.maxValue = Mathf.Max(0, vertCount - 1);

                parentStepper.onValueChanged = val =>
                {
                    int totalParents = layoutData.faceList.Count;
                    int candidateParentIndex = val;

                    FaceData self = faceData; // needed for IsHingeEdgeOccupied

                    // Try to find a parent with a free hinge
                    for (int attempt = 0; attempt < totalParents; attempt++)
                    {
                        Polygon parentPolygon = layoutData.faceList[candidateParentIndex].polygon;
                        int vertCount = parentPolygon?.vertices.Length ?? 0;

                        for (int i = 0; i < vertCount; i++)
                        {
                            int hingeA = i;
                            int hingeB = (i + 1) % vertCount;

                            if (!IsHingeEdgeOccupied(candidateParentIndex, hingeA, hingeB, self))
                            {
                                // Found valid parent + hinge
                                parentStepper.Value = candidateParentIndex;

                                hingeEdgeStepper.maxValue = Mathf.Max(0, vertCount - 1);
                                hingeEdgeStepper.Value = i;

                                faceData.parentIndex = candidateParentIndex;
                                faceData.hingeIndexA = hingeA;
                                faceData.hingeIndexB = hingeB;

                                OnHingeEdgeChanged(hingeEdgeStepper, parentStepper, faceData, 0);
                                return;
                            }
                        }

                        // Try next parent (with wraparound)
                        candidateParentIndex = (candidateParentIndex + 1) % totalParents;
                    }

                    // No valid parent + hinge found at all
                    UnityEngine.Debug.LogWarning("No free hinge found on any parent face.");
                };


                int previousHingeValue = hingeEdgeStepper.Value;

                hingeEdgeStepper.onValueChanged = newValue =>
                {
                    OnHingeEdgeChanged(hingeEdgeStepper, parentStepper, faceData, previousHingeValue);
                };

            }
            else
            {
                faceData.parentIndex = -1;
                faceData.hingeIndexA = -1;
                faceData.hingeIndexB = -1;
            }

            entryRows.Add(row);
        }
        

        // Reassign currentFaceIndex (only if still valid)
        netFolder.currentFaceIndex = preservedIndex >= 1 && preservedIndex < layoutData.faceList.Count ? preservedIndex : -1;

        RefreshNetVisual();
    }
    

void SetupLayoutDropdown()
    {
        layoutDropdown.ClearOptions();
        List<string> names = new();
        foreach (var layout in layoutOptions)
            names.Add(layout.name);
        layoutDropdown.AddOptions(names);

        layoutDropdown.onValueChanged.AddListener(index =>
        {
            layoutData = layoutOptions[index];  // change layout
            netFolder.layoutData = layoutData;
            LoadLayout();                       // reload UI and net
        });
    }

    /*
    int FindNextFreeHingeIndex(int parentIndex, FaceData faceData, int startIndex, out int hingeA, out int hingeB)
    {
        int count = faceData.polygon?.vertices.Length ?? 0;

        for (int offset = 0; offset < count; offset++)
        {
            int i = (startIndex + offset) % count;
            int tryA = i;
            int tryB = (i + 1) % count;

            if (!IsHingeEdgeOccupied(parentIndex, tryA, tryB, faceData))
            {
                hingeA = tryA;
                hingeB = tryB;
                return i;
            }
        }

        UnityEngine.Debug.LogWarning($"No free hinge edge found for face (parentIndex: {parentIndex})");
        hingeA = 0;
        hingeB = 1;
        return 0;
    }

    int FindPreviousFreeHingeIndex(int parentIndex, FaceData faceData, int startIndex, out int hingeA, out int hingeB)
    {
        int count = faceData.polygon?.vertices.Length ?? 0;

        for (int offset = 0; offset < count; offset++)
        {
            int i = (startIndex - offset + count) % count;
            int tryA = i;
            int tryB = (i + 1) % count;

            if (!IsHingeEdgeOccupied(parentIndex, tryA, tryB, faceData))
            {
                hingeA = tryA;
                hingeB = tryB;
                return i;
            }
        }

        UnityEngine.Debug.LogWarning($"No free hinge edge found (backwards) for face (parentIndex: {parentIndex})");

        hingeA = 0;
        hingeB = 1;
        return 0;
    }
    */

    int FindNextFreeHingeIndex(int parentIndex, FaceData faceData, int startIndex, out int hingeA, out int hingeB)
    {
        int count = layoutData.faceList[parentIndex].polygon?.vertices.Length ?? 0;

        for (int offset = 0; offset < count; offset++) // start from 1 instead of 0
        {
            int i = (startIndex + offset) % count;
            int tryA = i;
            int tryB = (i + 1) % count;

            if (!IsHingeEdgeOccupied(parentIndex, tryA, tryB, faceData))
            {
                hingeA = tryA;
                hingeB = tryB;
                return i;
            }
        }

        UnityEngine.Debug.LogWarning($"No free hinge edge found (forward) for face (parentIndex: {parentIndex})");

        hingeA = -1;
        hingeB = -1;
        return -1;
    }


    int FindPreviousFreeHingeIndex(int parentIndex, FaceData faceData, int startIndex, out int hingeA, out int hingeB)
    {
        int count = layoutData.faceList[parentIndex].polygon?.vertices.Length ?? 0;

        for (int offset = 0; offset < count; offset++)
        {
            int i = (startIndex - offset + count) % count;
            int tryA = i;
            int tryB = (i + 1) % count;

            if (!IsHingeEdgeOccupied(parentIndex, tryA, tryB, faceData))
            {
                hingeA = tryA;
                hingeB = tryB;
                return i;
            }
        }

        UnityEngine.Debug.LogWarning($"No free hinge edge found (backwards) for face (parentIndex: {parentIndex})");

        hingeA = -1;
        hingeB = -1;
        return -1;
    }


    void OnHingeEdgeChanged(IntStepper hingeStepper, IntStepper parentStepper, FaceData faceData, int previousValue)
    {
        int parentIndex = parentStepper.Value;
        int count = layoutData.faceList[parentIndex].polygon?.vertices.Length ?? 0;

        if (count < 2)
            return;

        int val = hingeStepper.Value;
        int hingeA = val;
        int hingeB = (val + 1) % count;

        UnityEngine.Debug.Log($"Going from {previousValue + 1} to {val + 1}.");

        if (!IsHingeEdgeOccupied(parentIndex, hingeA, hingeB, faceData))
        {
            faceData.hingeIndexA = hingeA;
            faceData.hingeIndexB = hingeB;
            //RefreshNetVisual();
            LoadLayout();
            return;
        }

        // Determine scroll direction
        bool scrolledForward = (val == (previousValue + 1) % count);

        int newIndex, newA, newB;

        if (scrolledForward)
        {
            UnityEngine.Debug.Log("Finding next free hinge");
            newIndex = FindNextFreeHingeIndex(parentIndex, faceData, val, out newA, out newB);
        }
        else
        {
            UnityEngine.Debug.Log("Finding previous free hinge");
            newIndex = FindPreviousFreeHingeIndex(parentIndex, faceData, val, out newA, out newB);
        }

        if (newIndex != -1)
        {
            faceData.hingeIndexA = newA;
            faceData.hingeIndexB = newB;
            hingeStepper.Value = newIndex;
        }

        //RefreshNetVisual();
        LoadLayout();
    }


    bool IsHingeEdgeOccupied(int parentIndex, int hingeA, int hingeB, FaceData self)
    {
        for (int j = 0; j < layoutData.faceList.Count; j++)
        {
            FaceData other = layoutData.faceList[j];

            if (parentIndex > 0 && hingeA == 0 && hingeB == 1) return true;

            if (ReferenceEquals(other, self)) continue; // Don't compare with self

            if (other.parentIndex == parentIndex)
            {
                bool matchesForward = other.hingeIndexA == hingeA && other.hingeIndexB == hingeB;
                bool matchesReverse = other.hingeIndexA == hingeB && other.hingeIndexB == hingeA;

                if (matchesForward || matchesReverse)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[IsHingeEdgeOccupied] Face {j} already uses edge [{hingeA}, {hingeB}] on parent {parentIndex + 1}. " +
                        $"It has [{other.hingeIndexA}, {other.hingeIndexB}]."
                    );
                    return true;
                }
            }
        }

        UnityEngine.Debug.Log($"[IsHingeEdgeOccupied] Hinge [{hingeA}, {hingeB}] on parent {parentIndex + 1} is free to use! ");

        return false;
    }
    
    /*
    bool IsHingeEdgeOccupied(int parentIndex, int hingeA, int hingeB, FaceData self)
    {
        // Step 1: Prevent using the edge the parent itself used
        if (parentIndex >= 0 && parentIndex < layoutData.faceList.Count)
        {
            FaceData parentFace = layoutData.faceList[parentIndex];

            // If the parent is NOT the root (i.e., it has its own parent)
            if (parentIndex != 0 && parentFace.parentIndex >= 0)
            {
                int pa = parentFace.hingeIndexA;
                int pb = parentFace.hingeIndexB;

                if ((pa == hingeA && pb == hingeB) || (pa == hingeB && pb == hingeA))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[IsHingeEdgeOccupied] Cannot use edge [{hingeA}, {hingeB}] because parent face {parentIndex} used it to hinge onto its own parent.");
                    return true;
                }
            }
        }

        // Step 2: Check if any other face already uses this edge on this parent
        foreach (FaceData other in layoutData.faceList)
        {
            if (ReferenceEquals(other, self)) continue;

            if (other.parentIndex == parentIndex)
            {
                bool matchForward = other.hingeIndexA == hingeA && other.hingeIndexB == hingeB;
                bool matchReverse = other.hingeIndexA == hingeB && other.hingeIndexB == hingeA;

                if (matchForward || matchReverse)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[IsHingeEdgeOccupied] Edge [{hingeA}, {hingeB}] on parent {parentIndex} is already used by Face {layoutData.faceList.IndexOf(other)}.");
                    return true;
                }
            }
        }

        return false;
    }
    */




    void RefreshNetVisual()
    {
        if (netFolder != null)
            netFolder.Initialise();
    }

    void SetupPolygonDropdown(TMP_Dropdown dropdown, FaceData data)
    {
        dropdown.ClearOptions();
        List<string> options = new();
        foreach (var poly in availablePolygons)
            options.Add(poly.name);
        dropdown.AddOptions(options);

        int index = System.Array.IndexOf(availablePolygons, data.polygon);
        dropdown.value = index >= 0 ? index : 0;

        dropdown.onValueChanged.AddListener(i =>
        {
            // Store previous polygon
            Polygon previousPolygon = data.polygon;

            // Update to new polygon
            data.polygon = availablePolygons[i];

            int changedFaceIndex = layoutData.faceList.IndexOf(data);
            int newVertCount = data.polygon?.vertices.Length ?? 0;
            int oldVertCount = previousPolygon?.vertices.Length ?? 0;

            // Only check child faces if new polygon has fewer vertices
            if (newVertCount < oldVertCount)
            {
                List<int> facesToRemove = new();

                for (int j = 0; j < layoutData.faceList.Count; j++)
                {
                    FaceData child = layoutData.faceList[j];
                    if (child.parentIndex == changedFaceIndex)
                    {
                        if (child.hingeIndexA >= newVertCount || child.hingeIndexB > newVertCount)
                        {
                            facesToRemove.Add(j);
                            UnityEngine.Debug.LogWarning(
                                $"Removed Face {j} from hinge [{child.hingeIndexA}, {child.hingeIndexB}] because the new vertex count of the parent is {newVertCount}.");
                        }
                        else if (child.hingeIndexB == newVertCount)
                        {
                            child.hingeIndexB = 0;
                        }
                    }
                }

                for (int k = facesToRemove.Count - 1; k >= 0; k--)
                {
                    layoutData.faceList.RemoveAt(facesToRemove[k]);
                }
            }
            
            else if (newVertCount > oldVertCount)
            {
                for (int j = 0; j < layoutData.faceList.Count; j++)
                {
                    FaceData child = layoutData.faceList[j];
                    if (child.parentIndex == changedFaceIndex)
                    {
                        if (child.hingeIndexB == 0)
                        {
                            UnityEngine.Debug.LogWarning(
                                $"Changed Face {j} from hinge [{child.hingeIndexA}, {child.hingeIndexB}] to hinge [{child.hingeIndexA}, {oldVertCount}].");
                            child.hingeIndexB = oldVertCount;
                        }
                    }
                }
            }

            LoadLayout(); // Reload UI and visuals
        });
    }


    public void SaveLayout()
    {
        // You could optionally mark the layoutData dirty if editing in editor
        UnityEditor.EditorUtility.SetDirty(layoutData);
    }

    public void AddFace()
    {
        FaceData newFace = new FaceData();

        newFace.polygon = layoutData.faceList.Count > 0
            ? layoutData.faceList[^1].polygon  // ^1 = last element
            : (availablePolygons.Length > 0 ? availablePolygons[0] : null);


        int parentCount = layoutData.faceList.Count;

        // === Handle root face creation ===
        if (parentCount == 0)
        {
            newFace.parentIndex = -1;
            newFace.hingeIndexA = -1;
            newFace.hingeIndexB = -1;

            layoutData.faceList.Add(newFace);
            LoadLayout();
            return;
        }

        // === Attach to any face with a free hinge ===
        for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
        {
            Polygon parentPolygon = layoutData.faceList[parentIndex].polygon;
            int vertexCount = parentPolygon?.vertices.Length ?? 0;

            for (int i = 0; i < vertexCount; i++)
            {
                int hingeA = i;
                int hingeB = (i + 1) % vertexCount;

                if (!IsHingeEdgeOccupied(parentIndex, hingeA, hingeB, newFace))
                {
                    newFace.parentIndex = parentIndex;
                    newFace.hingeIndexA = hingeA;
                    newFace.hingeIndexB = hingeB;

                    layoutData.faceList.Add(newFace);
                    LoadLayout();
                    return;
                }
            }
        }

        // === If no valid hinge found ===
        UnityEngine.Debug.LogError("AddFace failed: No free hinge found, this should not happen.");
    }




    public void RemoveLastFace()
    {
        int removedIndex = layoutData.faceList.Count - 1;

        if (layoutData.faceList.Count > 0)
        {
            layoutData.faceList.RemoveAt(removedIndex);

            // Notify NetFolder to check if its currentFaceIndex is still valid
            if (netFolder.currentFaceIndex == removedIndex)
            {
                netFolder.currentFaceIndex = -1; // deselect
            }

            LoadLayout();
        }
    }

}
