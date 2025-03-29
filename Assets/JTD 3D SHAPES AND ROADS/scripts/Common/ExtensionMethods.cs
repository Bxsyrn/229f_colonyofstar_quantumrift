using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNB
{
    public static class ExtensionMethods
    {

        public static Vector2 ToXZ(this Vector3 v3)
        {
            return new Vector2(v3.x, v3.z);
        }

    }
}