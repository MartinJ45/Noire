using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Splines;
using UnityEditor.UIElements;
using UnityEngine.Splines;
using UnityEngine.UIElements;
[Overlay(typeof(SceneView), "Road Builder", true)]
public class JunctionBuilderOverlay : Overlay
{

    Label SelectionInfoLabel;
    Button BuildJunctionButton;
    Button ClearJunctionbutton;
    Button ClearAllJunctionsButton;
    Button SaveRoadAsMeshAssetButton;
    VisualElement SliderArea;
    Intersection selectedIntersection;

    public static JunctionBuilderOverlay instance;

    public Action OnChangeValueEvent { get; internal set; }

    public override VisualElement CreatePanelContent()
    {
        instance = this;

        SelectionInfoLabel = new Label();

        BuildJunctionButton = new Button(OnBuildJunction);
        BuildJunctionButton.text = "Build Junction";
        BuildJunctionButton.SetEnabled(false);

        ClearJunctionbutton = new Button(OnClearJunction);
        ClearJunctionbutton.text = "Clear Junction";
        ClearJunctionbutton.visible = false;

        ClearAllJunctionsButton = new Button(OnClearAllJunction);
        ClearAllJunctionsButton.text = "Clear All Junctions";

        SaveRoadAsMeshAssetButton = new Button(OnSaveMesh);
        SaveRoadAsMeshAssetButton.text = "Save Mesh";

        var root = new VisualElement() { name = "My Toolbar Root" };
        root.Add(SelectionInfoLabel);
        root.Add(BuildJunctionButton);
        root.Add(ClearJunctionbutton);
        root.Add(ClearAllJunctionsButton);
        root.Add(SaveRoadAsMeshAssetButton);

        SliderArea = new VisualElement();
        root.Add(SliderArea);


        SplineToolUtility.RegisterSelectionChangedEvent();
        SplineToolUtility.Changed += OnSelectionChanged;

        return root;

    }

    private void OnBuildJunction()
    {
        List<SelectedSplineElementInfo> selection = SplineToolUtility.GetSelection();

        Intersection intersection = new Intersection();
        foreach(SelectedSplineElementInfo item in selection)
        {
            //Get the spline container;
            SplineContainer container = (SplineContainer)item.target;
            Spline spline = container.Splines[item.targetIndex];
            intersection.AddJunction(item.targetIndex, item.knotIndex, spline);
        }

        Selection.activeObject.GetComponent<SplineRoad>().AddJunction(intersection);
    }

    private void OnSelectionChanged()
    {
        BuildJunctionButton.SetEnabled(SplineToolUtility.GetSelection().Count > 1);
        selectedIntersection = null;
        ClearJunctionbutton.visible = false;
        SliderArea.visible = false;

        if (SplineToolUtility.GetSelection().Count>0)
            UpdateSelectionInfo();
        else
            ClearSelectionInfo();
    }

    private void ClearSelectionInfo()
    {
        SelectionInfoLabel.text = "";
    }

    private void UpdateSelectionInfo()
    {
        ClearSelectionInfo();

        List<SelectedSplineElementInfo> infos = SplineToolUtility.GetSelection();
        foreach(SelectedSplineElementInfo element in infos)
        {
            SelectionInfoLabel.text += $"Spline {element.targetIndex}, Knot {element.knotIndex} \n";
        }
    }

    public void ShowIntersection(Intersection intersection)
    {
        selectedIntersection = intersection;

        SelectionInfoLabel.text = "Selected Intersection";
        ClearJunctionbutton.visible = true;
        SliderArea.visible = true;

        SliderArea.Clear();

        for(int i=0; i<intersection.curves.Count; i++)
        {
            int value = i;
            Slider slider = new Slider($"Curve {i}", 0, 1, SliderDirection.Horizontal);
            slider.labelElement.style.minWidth = 60;
            slider.labelElement.style.maxWidth = 80;
            slider.value = intersection.curves[i];
            slider.RegisterValueChangedCallback((x) =>
            {
                intersection.curves[value] = x.newValue;
                OnChangeValueEvent.Invoke();
            });
            SliderArea.Add(slider);
        }
    }

    private void OnClearJunction()
    {
        if (selectedIntersection != null)
        {
            Selection.activeObject.GetComponent<SplineRoad>().RemoveJunction(selectedIntersection);
        }
    }

    private void OnClearAllJunction()
    {
        Selection.activeObject.GetComponent<SplineRoad>().RemoveAllJunctions();
    }

    private void OnSaveMesh()
    {
        Selection.activeObject.GetComponent<SplineRoad>().SaveMesh("RoadMesh");
    }
}