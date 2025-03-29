using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CNB
{
    public static class MathHelper
    {

        public static Vector3 EvaluateCurve(Vector3[] points, float t)
        {
            return EvaluateCurve(points[0], points[1], points[2], points[3], t);
        }

        public static Vector3 EvaluateCurveDerivative(Vector3[] points, float t)
        {
            return EvaluateCurveDerivative(points[0], points[1], points[2], points[3], t);
        }

        public static float EstimateCurveLength(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float controlNetLength = (p0 - p1).magnitude + (p1 - p2).magnitude + (p2 - p3).magnitude;
            float estimatedCurveLength = (p0 - p3).magnitude + controlNetLength / 2f;
            return estimatedCurveLength;
        }

        public static List<float> ExtremePointTimes(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 a = 3 * (-p0 + 3 * p1 - 3 * p2 + p3);
            Vector3 b = 6 * (p0 - 2 * p1 + p2);
            Vector3 c = 3 * (p1 - p0);

            List<float> times = new List<float>();
            times.AddRange(StationaryPointTimes(a.x, b.x, c.x));
            times.AddRange(StationaryPointTimes(a.y, b.y, c.y));
            times.AddRange(StationaryPointTimes(a.z, b.z, c.z));
            return times;
        }

        public static Vector3 TransformDirection(Vector3 p, Transform t)
        {
            var original = LockTransformToSpace(t);
            Vector3 transformedPoint = t.TransformDirection(p);
            original.SetTransform(t);
            return transformedPoint;
        }

        public static Vector3 TransformPoint(Vector3 p, Transform t)
        {
            var original = LockTransformToSpace(t);
            Vector3 transformedPoint = t.TransformPoint(p);
            original.SetTransform(t);
            return transformedPoint;
        }

        static Vector3 EvaluateCurve(Vector3 a1, Vector3 c1, Vector3 c2, Vector3 a2, float t)
        {
            t = Mathf.Clamp01(t);
            return (1 - t) * (1 - t) * (1 - t) * a1 + 3 * (1 - t) * (1 - t) * t * c1 + 3 * (1 - t) * t * t * c2 + t * t * t * a2;
        }

        
        static Vector3 EvaluateCurveDerivative(Vector3 a1, Vector3 c1, Vector3 c2, Vector3 a2, float t)
        {
            t = Mathf.Clamp01(t);
            return 3 * (1 - t) * (1 - t) * (c1 - a1) + 6 * (1 - t) * t * (c2 - c1) + 3 * t * t * (a2 - c2);
        }
       
        static IEnumerable<float> StationaryPointTimes(float a, float b, float c)
        {
            List<float> times = new List<float>();

            if (a != 0)
            {
                float discriminant = b * b - 4 * a * c;
                if (discriminant >= 0)
                {
                    float s = Mathf.Sqrt(discriminant);
                    float t1 = (-b + s) / (2 * a);
                    if (t1 >= 0 && t1 <= 1)
                    {
                        times.Add(t1);
                    }

                    if (discriminant != 0)
                    {
                        float t2 = (-b - s) / (2 * a);

                        if (t2 >= 0 && t2 <= 1)
                        {
                            times.Add(t2);
                        }
                    }
                }
            }
            return times;
        }

        static PosRotScale LockTransformToSpace(Transform t)
        {
            var original = new PosRotScale(t);

            t.eulerAngles = new Vector3(0, 0, t.eulerAngles.z);
            t.position = new Vector3(t.position.x, t.position.y, 0);

            float maxScale = Mathf.Max(t.lossyScale.x, t.lossyScale.y, t.lossyScale.z);

            t.localScale = Vector3.one * maxScale;

            return original;
        }
        
        class PosRotScale
        {
            public readonly Vector3 position;
            public readonly Quaternion rotation;
            public readonly Vector3 scale;

            public PosRotScale(Transform t)
            {
                position = t.position;
                rotation = t.rotation;
                scale = t.localScale;
            }

            public void SetTransform(Transform t)
            {
                t.position = position;
                t.rotation = rotation;
                t.localScale = scale;

            }
        }
    }
}