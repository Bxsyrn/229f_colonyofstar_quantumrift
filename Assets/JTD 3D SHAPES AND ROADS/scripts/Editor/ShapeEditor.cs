using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CNB
{
    [CustomEditor(typeof(ShapeCreator))]
    public class ShapeEditor : Editor
    {

        ShapeCreator shapeCreator;
        SelectionInfo selectionInfo;
        bool shapeChangedSinceLastRepaint;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            string helpMessage = "Push Create paths if you prefer rounded shapes wiht evenly spaced mesh vertices.\nYou can individually select to edit or delete the shapes created.\nPush Spawn props to spawn the prefabs preset in the Shape SO asset.";
            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

            int shapeDeleteIndex = -1;

            if (shapeCreator.autoUpdate && Event.current.type == EventType.Repaint)
            {
                shapeCreator.UpdateMeshDisplay();
            }

            if (GUILayout.Button("CreatePaths"))
            {
                for (int i = 0; i < shapeCreator.shapes.Count; i++)
                {
                    shapeCreator.CreatePath(i);
                }
            }

            for (int i = 0; i < shapeCreator.shapes.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Shape " + (i + 1));

                GUI.enabled = i != selectionInfo.selectedShapeIndex;
                if (GUILayout.Button("Select"))
                {
                    selectionInfo.selectedShapeIndex = i;
                }
                GUI.enabled = true;

                if (GUILayout.Button("Delete"))
                {
                    shapeDeleteIndex = i;
                }
               
                GUILayout.EndHorizontal();
            }
            
            if (shapeDeleteIndex != -1)
            {
                shapeCreator.shapes.RemoveAt(shapeDeleteIndex);
                selectionInfo.selectedShapeIndex = Mathf.Clamp(selectionInfo.selectedShapeIndex, 0, shapeCreator.shapes.Count - 1);
                if (shapeCreator.shapes.Count==0)
                {
                    DestroyImmediate(shapeCreator.transform.GetChild(0).gameObject);
                }
            }

            if (GUILayout.Button("Spawn props"))
            {
                shapeCreator.InitSpawner();
                shapeCreator.Spawn();
            }


            if (GUI.changed)
            {
                shapeChangedSinceLastRepaint = true;
                SceneView.RepaintAll();
            }
        }

        void OnSceneGUI()
        {
            Event guiEvent = Event.current;

            if (guiEvent.type == EventType.Repaint && shapeCreator.autoUpdate)
            {
                Draw();
            }
            else if (guiEvent.type == EventType.Layout)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else
            {
                HandleInput(guiEvent);
                if (shapeChangedSinceLastRepaint)
                {
                    HandleUtility.Repaint();
                }
            }
            
        }

        void CreateNewShape()
        {
            shapeCreator.shapes.Add(new Shape());
            selectionInfo.selectedShapeIndex = shapeCreator.shapes.Count - 1;
        }

        void CreateNewPoint(Vector3 position)
        {
            bool mouseIsOverSelectedShape = selectionInfo.mouseOverShapeIndex == selectionInfo.selectedShapeIndex;
            int newPointIndex = selectionInfo.mouseIsOverLine && mouseIsOverSelectedShape ? selectionInfo.lineIndex + 1 : SelectedShape.points.Count;
            SelectedShape.points.Insert(newPointIndex, position);
            selectionInfo.pointIndex = newPointIndex;
            selectionInfo.mouseOverShapeIndex = selectionInfo.selectedShapeIndex;
            shapeChangedSinceLastRepaint = true;

            SelectPointUnderMouse();
        }

        void DeletePointUnderMouse()
        {
            SelectedShape.points.RemoveAt(selectionInfo.pointIndex);
            selectionInfo.pointIsSelected = false;
            selectionInfo.mouseIsOverPoint = false;
            shapeChangedSinceLastRepaint = true;
        }

        void SelectPointUnderMouse()
        {
            selectionInfo.pointIsSelected = true;
            selectionInfo.mouseIsOverPoint = true;
            selectionInfo.mouseIsOverLine = false;
            selectionInfo.lineIndex = -1;

            selectionInfo.positionAtStartOfDrag = SelectedShape.points[selectionInfo.pointIndex];
            shapeChangedSinceLastRepaint = true;
        }

        void SelectShapeUnderMouse()
        {
            if (selectionInfo.mouseOverShapeIndex != -1)
            {
                selectionInfo.selectedShapeIndex = selectionInfo.mouseOverShapeIndex;
                shapeChangedSinceLastRepaint = true;
            }
        }

        void HandleInput(Event guiEvent)
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePosition = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
            {
                HandleShiftLeftMouseDown(mousePosition);
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.CapsLock)
            {
                HandleCapsLockLeftMouseDown(mousePosition);
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDown(mousePosition);
            }

            if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
            {
                HandleLeftMouseUp(mousePosition);
            }

            if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.None)
            {
                HandleLeftMouseDrag(mousePosition);
            }

            if (!selectionInfo.pointIsSelected)
            {
                UpdateMouseOverInfo(mousePosition);
            }

        }

        void HandleShiftLeftMouseDown(Vector3 mousePosition)
        {
            if (selectionInfo.mouseIsOverPoint)
            {
                SelectShapeUnderMouse();
                DeletePointUnderMouse();
            }
            else
            {
                CreateNewShape();
                CreateNewPoint(mousePosition);
            }
        }

        void HandleCapsLockLeftMouseDown(Vector3 mousePosition)
        {
            if (shapeCreator.shapes.Count == 0)
            {
                CreateNewShape();
            }

            SelectShapeUnderMouse();

            if (!selectionInfo.mouseIsOverPoint)
            {
                CreateNewPoint(mousePosition);
            }
        }

        void HandleLeftMouseDown(Vector3 mousePosition)
        {
            SelectShapeUnderMouse();

            if (selectionInfo.mouseIsOverPoint)
            {
                SelectPointUnderMouse();
            }
        }

        void HandleLeftMouseUp(Vector3 mousePosition)
        {
            if (selectionInfo.pointIsSelected)
            {
                SelectedShape.points[selectionInfo.pointIndex] = selectionInfo.positionAtStartOfDrag;
                SelectedShape.points[selectionInfo.pointIndex] = mousePosition;

                selectionInfo.pointIsSelected = false;
                selectionInfo.pointIndex = -1;
                shapeChangedSinceLastRepaint = true;
            }

        }

        void HandleLeftMouseDrag(Vector3 mousePosition)
        {
            if (selectionInfo.pointIsSelected)
            {
                SelectedShape.points[selectionInfo.pointIndex] = mousePosition;
                shapeChangedSinceLastRepaint = true;
            }

        }

        void UpdateMouseOverInfo(Vector3 mousePosition)
        {
            int mouseOverPointIndex = -1;
            int mouseOverShapeIndex = -1;
            for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
            {
                Shape currentShape = shapeCreator.shapes[shapeIndex];

                for (int i = 0; i < currentShape.points.Count; i++)
                {
                    if (Vector3.Distance(mousePosition, currentShape.points[i]) < shapeCreator.handleRadius)
                    {
                        mouseOverPointIndex = i;
                        mouseOverShapeIndex = shapeIndex;
                        break;
                    }
                }
            }

            if (mouseOverPointIndex != selectionInfo.pointIndex || mouseOverShapeIndex != selectionInfo.mouseOverShapeIndex)
            {
                selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
                selectionInfo.pointIndex = mouseOverPointIndex;
                selectionInfo.mouseIsOverPoint = mouseOverPointIndex != -1;

                shapeChangedSinceLastRepaint = true;
            }

            if (selectionInfo.mouseIsOverPoint)
            {
                selectionInfo.mouseIsOverLine = false;
                selectionInfo.lineIndex = -1;
            }
            else
            {
                int mouseOverLineIndex = -1;
                float closestLineDst = shapeCreator.handleRadius;
                for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
                {
                    Shape currentShape = shapeCreator.shapes[shapeIndex];

                    for (int i = 0; i < currentShape.points.Count; i++)
                    {
                        Vector3 nextPointInShape = currentShape.points[(i + 1) % currentShape.points.Count];
                        float dstFromMouseToLine = HandleUtility.DistancePointToLineSegment(mousePosition.ToXZ(), currentShape.points[i].ToXZ(), nextPointInShape.ToXZ());
                        if (dstFromMouseToLine < closestLineDst)
                        {
                            closestLineDst = dstFromMouseToLine;
                            mouseOverLineIndex = i;
                            mouseOverShapeIndex = shapeIndex;
                        }
                    }
                }

                if (selectionInfo.lineIndex != mouseOverLineIndex || mouseOverShapeIndex != selectionInfo.mouseOverShapeIndex)
                {
                    selectionInfo.mouseOverShapeIndex = mouseOverShapeIndex;
                    selectionInfo.lineIndex = mouseOverLineIndex;
                    selectionInfo.mouseIsOverLine = mouseOverLineIndex != -1;
                    shapeChangedSinceLastRepaint = true;
                }
            }
        }

        void Draw()
        {
            if (shapeCreator._shapeMesh)
            {
                float height = shapeCreator._shapeMesh.transform.position.y;

                for (int shapeIndex = 0; shapeIndex < shapeCreator.shapes.Count; shapeIndex++)
                {
                    Shape shapeToDraw = shapeCreator.shapes[shapeIndex];
                    bool shapeIsSelected = shapeIndex == selectionInfo.selectedShapeIndex;
                    bool mouseIsOverShape = shapeIndex == selectionInfo.mouseOverShapeIndex;
                    Color deselectedShapeColour = Color.grey;

                    for (int i = 0; i < shapeToDraw.points.Count; i++)
                    {
                        Vector3 newPosY = new Vector3(shapeToDraw.points[i].x, height, shapeToDraw.points[i].z);
                        Vector3 newPosYNext = new Vector3(shapeToDraw.points[(i + 1) % shapeToDraw.points.Count].x, height, shapeToDraw.points[(i + 1) % shapeToDraw.points.Count].z);
                        if (i == selectionInfo.lineIndex && mouseIsOverShape)
                        {
                            Handles.color = Color.red;
                            Handles.DrawLine(newPosY, newPosYNext);
                        }
                        else
                        {
                            Handles.color = shapeIsSelected ? Color.black : deselectedShapeColour;
                            Handles.DrawDottedLine(newPosY, newPosYNext, 4);
                        }

                        if (i == selectionInfo.pointIndex && mouseIsOverShape)
                        {
                            Handles.color = selectionInfo.pointIsSelected ? Color.black : Color.red;
                        }
                        else
                        {
                            Handles.color = shapeIsSelected ? Color.white : deselectedShapeColour;
                        }
                        Handles.DrawSolidDisc(newPosY, Vector3.up, shapeCreator.handleRadius);
                    }
                }
            }

            if (shapeChangedSinceLastRepaint)
            {
                shapeCreator.UpdateMeshDisplay();
            }

            shapeChangedSinceLastRepaint = false;
        }

        void OnEnable()
        {
            shapeChangedSinceLastRepaint = true;
            shapeCreator = target as ShapeCreator;
            selectionInfo = new SelectionInfo();
            Tools.hidden = true;
        }

        void OnDisable()
        {
            Tools.hidden = false;
        }

        Shape SelectedShape
        {
            get
            {
                return shapeCreator.shapes[selectionInfo.selectedShapeIndex];
            }
        }

        public class SelectionInfo
        {
            public int selectedShapeIndex;
            public int mouseOverShapeIndex;

            public int pointIndex = -1;
            public bool mouseIsOverPoint;
            public bool pointIsSelected;
            public Vector3 positionAtStartOfDrag;

            public int lineIndex = -1;
            public bool mouseIsOverLine;
        }

    }
}