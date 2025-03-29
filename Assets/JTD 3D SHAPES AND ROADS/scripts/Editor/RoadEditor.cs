using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CNB
{
    [CustomEditor(typeof(RoadCreator))]
    public class RoadEditor : Editor
    {

        RoadCreator creator;

        void OnSceneGUI()
        {
            if (creator.autoUpdate && Event.current.type == EventType.Repaint)
            {
                creator.UpdateRoad();
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            string helpMessage = "Push Create new to create a new road. Be careful as the existing one will ne erased\nTo create a new aditional one drad and drop anpther ROAD PREFAB";
            EditorGUILayout.HelpBox(helpMessage, MessageType.Info);

            EditorGUI.BeginChangeCheck();
            
            if (GUILayout.Button("Create new"))
            {
                creator.path.CreatePath();
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                SceneView.RepaintAll();
            }
        }

        void OnEnable()
        {
            creator = (RoadCreator)target;
        }
    }
}