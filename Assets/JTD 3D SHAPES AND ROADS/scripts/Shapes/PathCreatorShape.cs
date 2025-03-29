using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace CNB
{
    public class PathCreatorShape : MonoBehaviour
    {
        [HideInInspector]
        public PathC path;

        [HideInInspector]
        public List<Vector3> waypoints = new List<Vector3>();
        [HideInInspector]
        public List<Vector3> evenlySpacedPoints = new List<Vector3>();

        public void CreatePath()
        {
            path = new PathC(transform.position);
        }
        
        void Reset()
        {
            CreatePath();
        }

        public void AddWayPointsFromPath(int step)
        {
            waypoints.Clear();
            evenlySpacedPoints = path.CalculateEvenlySpacedPoints().ToList();

            for (int i = 0; i < evenlySpacedPoints.Count; i += step % evenlySpacedPoints.Count)
            {
                Vector3 newPos = new Vector3(evenlySpacedPoints[i].x, this.transform.localPosition.y, evenlySpacedPoints[i].z);
                waypoints.Add(newPos);
            }

            
        }
    }
}