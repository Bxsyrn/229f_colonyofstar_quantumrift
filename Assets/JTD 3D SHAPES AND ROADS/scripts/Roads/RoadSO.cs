using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNB
{
    [CreateAssetMenu(fileName = "NewRoad", menuName = "RoadSO")]
    public class RoadSO : ScriptableObject
    {
        public Material _floorMaterial;
        public Material _wallsMaterial;
    }
}