using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace CNB
{
    public class PathCreatorRoad : MonoBehaviour
    {
        [HideInInspector]
        public PathC path;
        [HideInInspector]
        public RoadCreator roadCreator;

        [Header("ROAD HEIGHT MAP PARAMS")]
        public AnimationCurve roadHeightMap;
        public int heightFactor = 20;
        public bool useCurve = false;

        [HideInInspector]
        public Color anchorCol = Color.red;
        [HideInInspector]
        public Color controlCol = Color.white;
        [HideInInspector]
        public Color segmentCol = Color.green;
        [HideInInspector]
        public Color selectedSegmentCol = Color.yellow;

        [Header("RADIUS OF THE EDITOR DRAGABLE DISK GIZMOS")]
        public float anchorDiameter = 4f;
        public float controlDiameter = 2f;

        [HideInInspector]
        public float waypointsDiameter = 1f;

        [HideInInspector]
        public bool displayControlPoints = true;

        public float Length { get; private set; }

        [HideInInspector]
        public List<Vector3> waypoints = new List<Vector3>();

        [Header("ROAD MESH RESOLUTION")]
        [Range(3,20)]
        public int evenlySpacedPointsStep = 5;

        [HideInInspector]
        public List<Vector3> evenlySpacedPoints = new List<Vector3>();

        public void CreatePath()
        {
            path = new PathC(transform.position,true);
            path.IsClosed = true;
            path.AutoSetControlPoints = true;
        }
        
        void Reset()
        {
            CreatePath();
        }

        
        public void AddWayPointsFromPath(int step, float pathLenght)
        {
            if (roadCreator==null)
            {
                roadCreator = GetComponent<RoadCreator>();
            }
            float lenghtBetweenPoints = step * pathLenght / evenlySpacedPoints.Count;
            waypoints.Clear();
            evenlySpacedPoints = path.CalculateEvenlySpacedPoints().ToList();

            for (int i = 0; i < evenlySpacedPoints.Count; i+=step% evenlySpacedPoints.Count)
            {
                if (useCurve)
                {
                    Vector3 newPos = new Vector3(evenlySpacedPoints[i].x, this.transform.localPosition.y + roadHeightMap.Evaluate(Mathf.InverseLerp(0, pathLenght, i * lenghtBetweenPoints / step)) * heightFactor, evenlySpacedPoints[i].z);
                    waypoints.Add(newPos);
                }
                else
                {
                    Vector3 newPos = new Vector3(evenlySpacedPoints[i].x, this.transform.localPosition.y, evenlySpacedPoints[i].z);
                    waypoints.Add(newPos);
                }
            }

            waypoints[waypoints.Count - 1] = waypoints[0];
        }
    }
}