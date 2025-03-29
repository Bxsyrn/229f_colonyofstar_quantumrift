using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNB
{
    [CreateAssetMenu(fileName = "NewShape", menuName = "ShapeSO")]
    public class ShapeSO : ScriptableObject
    {
        public List<GameObject> prefabsToSpawn = new List<GameObject>();
        
        public Material _floorMaterial;
        public Material _wallsMaterial;
    }
}