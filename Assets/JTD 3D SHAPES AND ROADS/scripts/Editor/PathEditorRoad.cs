using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CNB
{
    [CustomEditor(typeof(PathCreatorRoad))]
    public class PathEditorRoad : Editor
    {
        PathCreatorRoad creator;
        PathC Path
        {
            get
            {
                return creator.path;
            }
        }

        const float segmentSelectDistanceThreshold = 1f;
        int selectedSegmentIndex = -1;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUI.BeginChangeCheck();

            bool autoSetControlPoints = GUILayout.Toggle(Path.AutoSetControlPoints, "Auto Set Control Points");
            if (autoSetControlPoints != Path.AutoSetControlPoints)
            {
                Path.AutoSetControlPoints = autoSetControlPoints;
            }

            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        void OnSceneGUI()
        {
            Input();
            Draw();
        }

        void Input()
        {
            Event guiEvent = Event.current;
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float drawPlaneHeight = 0;
            float dstToDrawPlane = (drawPlaneHeight - mouseRay.origin.y) / mouseRay.direction.y;
            Vector3 mousePos = mouseRay.GetPoint(dstToDrawPlane);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
            {
                if (selectedSegmentIndex != -1)
                {
                    Path.SplitSegment(mousePos, selectedSegmentIndex);
                }
                else if (!Path.IsClosed)
                {
                    Path.AddSegment(mousePos);
                }
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.capsLock)
            {
                float minDstToAnchor = creator.anchorDiameter * .5f;
                int closestAnchorIndex = -1;

                for (int i = 0; i < Path.NumPoints; i += 3)
                {
                    float dst = Vector2.Distance(mousePos, Path[i]);
                    if (dst < minDstToAnchor)
                    {
                        minDstToAnchor = dst;
                        closestAnchorIndex = i;
                    }
                }

                if (closestAnchorIndex != -1)
                {
                    Path.DeleteSegment(closestAnchorIndex);
                }
            }

            if (guiEvent.type == EventType.MouseMove)
            {
                float minDstToSegment = segmentSelectDistanceThreshold;
                int newSelectedSegmentIndex = -1;

                for (int i = 0; i < Path.NumSegments; i++)
                {
                    Vector3[] points = Path.GetPointsInSegment(i);
                    float dst = HandleUtility.DistancePointBezier(mousePos, points[0], points[3], points[1], points[2]);
                    if (dst < minDstToSegment)
                    {
                        minDstToSegment = dst;
                        newSelectedSegmentIndex = i;
                    }
                }

                if (newSelectedSegmentIndex != selectedSegmentIndex)
                {
                    selectedSegmentIndex = newSelectedSegmentIndex;
                    HandleUtility.Repaint();
                }
            }

            HandleUtility.AddDefaultControl(0);
        }

        void Draw()
        {
            if (creator.roadCreator._shapeMesh)
            {
                float height = creator.roadCreator._shapeMesh.transform.position.y;
                for (int i = 0; i < Path.NumSegments; i++)
                {
                    Vector3[] points = Path.GetPointsInSegment(i);
                    Vector3 newPos;
                    for (int j = 0; j < points.Length; j++)
                    {
                        newPos = new Vector3(points[j].x, height, points[j].z);
                        points[j] = newPos;
                    }
                    if (creator.displayControlPoints)
                    {
                        Handles.color = Color.black;
                        Handles.DrawLine(points[1], points[0]);
                        Handles.DrawLine(points[2], points[3]);
                    }
                    Color segmentCol = i == selectedSegmentIndex && Event.current.shift ? creator.selectedSegmentCol : creator.segmentCol;
                    Handles.color = segmentCol;
                    Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
                }


                for (int i = 0; i < Path.NumPoints; i++)
                {
                    Vector3 newPosY = new Vector3(Path[i].x, height, Path[i].z);

                    if (i % 3 == 0 || creator.displayControlPoints)
                    {
                        Handles.color = i % 3 == 0 ? creator.anchorCol : creator.controlCol;
                        float handleSize = i % 3 == 0 ? creator.anchorDiameter : creator.controlDiameter;

                        Vector3 newPos = Handles.FreeMoveHandle(newPosY, handleSize, Vector3.zero, Handles.CylinderHandleCap);
                        if (Path[i] != newPos)
                        {
                            Path.MovePoint(i, newPos);
                        }
                    }
                }

                for (int i = 0; i < creator.waypoints.Count; i++)
                {
                    Vector3 newPosY = new Vector3(creator.waypoints[i].x, height, creator.waypoints[i].z);

                    Handles.color = Color.blue;
                    float handleSize = creator.waypointsDiameter;
                    Handles.DrawSolidDisc(newPosY, Vector3.up, handleSize);
                }
            }
        }

        void OnEnable()
        {
            creator = (PathCreatorRoad)target;
            if (creator.path == null)
            {
                creator.CreatePath();
            }
            Tools.hidden = true;
        }

        void OnDisable()
        {
            Tools.hidden = false;
        }

    }
}