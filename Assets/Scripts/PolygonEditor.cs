using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using static System.Net.Mime.MediaTypeNames;

using UnityEngine.EventSystems;

public class PolygonEditor : MonoBehaviour
{
    public Polygon[] availablePolygons;
    public TMP_Dropdown polygonDropdown;

    public GameObject inputPrefab; // Prefab with two InputFields for X and Y
    public Transform listContainer; // Vertical layout container

    private Polygon polygon;

    public NetShape shapePreview;

    private List<GameObject> inputRows = new();

    private NetEditor netEditor;

    void Start()
    {
        if (netEditor == null)
            netEditor = FindFirstObjectByType<NetEditor>();

        polygon = availablePolygons[0];

        SetupDropdown();
        LoadPolygonVertices();
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0)) // left click released
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void LoadPolygonVertices()
    {
        foreach (Transform child in listContainer)
            Destroy(child.gameObject);
        inputRows.Clear();

        for (int i = 0; i < polygon.vertices.Length; i++)
        {
            Vector2 vert = polygon.vertices[i];
            GameObject row = Instantiate(inputPrefab, listContainer);
            row.transform.SetParent(listContainer, false);

            // Find and set the label
            TextMeshProUGUI label = row.transform.Find("VertexIndex").GetComponent<TextMeshProUGUI>();
            label.text = $"Vertex {i + 1}";

            // Find sliders and text fields
            Slider xSlider = row.transform.Find("XGroup/XSlider").GetComponent<Slider>();
            TextMeshProUGUI xText = row.transform.Find("XGroup/XValue").GetComponent<TextMeshProUGUI>();

            Slider ySlider = row.transform.Find("YGroup/YSlider").GetComponent<Slider>();
            TextMeshProUGUI yText = row.transform.Find("YGroup/YValue").GetComponent<TextMeshProUGUI>();

            xSlider.minValue = -1f;
            ySlider.minValue = -1f;

            xSlider.value = vert.x;
            ySlider.value = vert.y;

            xText.text = xSlider.value.ToString("0.00");
            yText.text = ySlider.value.ToString("0.00");

            xSlider.onValueChanged.AddListener(val =>
            {
                xText.text = val.ToString("0.00");
                ApplyChanges();
            });

            ySlider.onValueChanged.AddListener(val =>
            {
                yText.text = val.ToString("0.00");
                ApplyChanges();
            });

            inputRows.Add(row);
        }



        ApplyChanges();
    }

    void SetupDropdown()
    {
        polygonDropdown.ClearOptions();
        List<string> options = new();

        foreach (var poly in availablePolygons)
            options.Add(poly.name);

        polygonDropdown.AddOptions(options);

        int initialIndex = System.Array.IndexOf(availablePolygons, polygon);
        polygonDropdown.value = Mathf.Max(0, initialIndex);
        polygonDropdown.onValueChanged.AddListener(OnPolygonChanged);
    }

    void OnPolygonChanged(int index)
    {
        polygon = availablePolygons[index];
        LoadPolygonVertices(); // Reload sliders
    }

    public void ApplyChanges()
    {
        List<Vector2> newVerts = new();
        foreach (GameObject row in inputRows)
        {
            float x = row.transform.Find("XGroup/XSlider").GetComponent<Slider>().value;
            float y = row.transform.Find("YGroup/YSlider").GetComponent<Slider>().value;
            newVerts.Add(new Vector2(x, y));
        }

        polygon.vertices = newVerts.ToArray();
        UnityEngine.Debug.Log("Polygon updated!");

        //Update the visual mesh
        if (shapePreview != null)
            shapePreview.UpdatePolygonMesh(polygon.vertices);

        if (netEditor != null)
            netEditor.LoadLayout();
    }

    public void AddVertex()
    {
        GameObject row = Instantiate(inputPrefab, listContainer);
        row.transform.SetParent(listContainer, false);

        int index = inputRows.Count + 1;

        TextMeshProUGUI label = row.transform.Find("VertexIndex").GetComponent<TextMeshProUGUI>();
        label.text = $"Vertex {index}";

        // === Find sliders and value text
        Slider xSlider = row.transform.Find("XGroup/XSlider").GetComponent<Slider>();
        TextMeshProUGUI xText = row.transform.Find("XGroup/XValue").GetComponent<TextMeshProUGUI>();

        Slider ySlider = row.transform.Find("YGroup/YSlider").GetComponent<Slider>();
        TextMeshProUGUI yText = row.transform.Find("YGroup/YValue").GetComponent<TextMeshProUGUI>();

        xSlider.minValue = -1f;
        ySlider.minValue = -1f;

        xSlider.value = 0f;
        ySlider.value = 0f;

        xText.text = xSlider.value.ToString("0.00");
        yText.text = ySlider.value.ToString("0.00");

        // === Hook up value change listeners
        xSlider.onValueChanged.AddListener(val =>
        {
            xText.text = val.ToString("0.00");
            ApplyChanges();
        });

        ySlider.onValueChanged.AddListener(val =>
        {
            yText.text = val.ToString("0.00");
            ApplyChanges();
        });

        inputRows.Add(row);

        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
            scrollRect.verticalNormalizedPosition = 0f;

        ApplyChanges();
    }



    public void RemoveLastVertex()
    {
        if (inputRows.Count > 0)
        {
            Destroy(inputRows[^1]);
            inputRows.RemoveAt(inputRows.Count - 1);
        }

        ApplyChanges();
    }
}
