using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CNB
{
    [System.Serializable]
    public class BezierPath
    {

        [SerializeField, HideInInspector]
        List<Vector3> points;
        [SerializeField, HideInInspector]
        bool isClosed;
        [SerializeField, HideInInspector]
        float autoControlLength = .3f;
        [SerializeField, HideInInspector]
        bool boundsUpToDate;
        [SerializeField, HideInInspector]
        Bounds bounds;

        public BezierPath(IEnumerable<Vector3> points, bool isClosed = true)
        {
            Vector3[] pointsArray = points.ToArray();

            if (pointsArray.Length < 2)
            {
                Debug.LogError("Path requires at least 2 anchor points.");
            }
            else
            {
                this.points = new List<Vector3> { pointsArray[0], Vector3.zero, Vector3.zero, pointsArray[1] };

                for (int i = 2; i < pointsArray.Length; i++)
                {
                    AddSegmentToEnd(pointsArray[i]);
                }
            }
            IsClosed = isClosed;
        }

        public BezierPath(IEnumerable<Vector2> transforms, bool isClosed = true) :
            this(transforms.Select(p => new Vector3(p.x, p.y)), isClosed)
        { }

        public Vector3 this[int i]
        {
            get
            {
                return GetPoint(i);
            }
        }

        public Vector3 GetPoint(int i)
        {
            return points[i];
        }

        public void SetPoint(int i, Vector3 localPosition)
        {
            points[i] = localPosition;
        }

        public int NumPoints
        {
            get
            {
                return points.Count;
            }
        }

        public int NumAnchorPoints
        {
            get
            {
                return IsClosed ? points.Count / 3 : (points.Count + 2) / 3;
            }
        }

        public int NumSegments
        {
            get
            {
                return points.Count / 3;
            }
        }

        public bool IsClosed
        {
            get
            {
                return isClosed;
            }
            set
            {
                if (isClosed != value)
                {
                    isClosed = value;
                    UpdateClosedState();
                }
            }
        }

        void AddSegmentToEnd(Vector3 anchorPos)
        {
            if (isClosed)
            {
                return;
            }

            int lastAnchorIndex = points.Count - 1;
            Vector3 secondControlForOldLastAnchorOffset = points[lastAnchorIndex] - points[lastAnchorIndex - 1];
            Vector3 secondControlForOldLastAnchor = points[lastAnchorIndex] + secondControlForOldLastAnchorOffset;
            Vector3 controlForNewAnchor = (anchorPos + secondControlForOldLastAnchor) * .5f;

            points.Add(secondControlForOldLastAnchor);
            points.Add(controlForNewAnchor);
            points.Add(anchorPos);

            AutoSetAllAffectedControlPoints(points.Count - 1);

        }

        public Vector3[] GetPointsInSegment(int segmentIndex)
        {
            segmentIndex = Mathf.Clamp(segmentIndex, 0, NumSegments - 1);
            return new Vector3[] { this[segmentIndex * 3], this[segmentIndex * 3 + 1], this[segmentIndex * 3 + 2], this[LoopIndex(segmentIndex * 3 + 3)] };
        }

        public Bounds CalculateBoundsWithTransform(Transform transform)
        {
            MinMax3D minMax = new MinMax3D();

            for (int i = 0; i < NumSegments; i++)
            {
                Vector3[] p = GetPointsInSegment(i);
                for (int j = 0; j < p.Length; j++)
                {
                    p[j] = MathHelper.TransformPoint(p[j], transform);
                }

                minMax.AddValue(p[0]);
                minMax.AddValue(p[3]);

                List<float> extremePointTimes = MathHelper.ExtremePointTimes(p[0], p[1], p[2], p[3]);
                foreach (float t in extremePointTimes)
                {
                    minMax.AddValue(MathHelper.EvaluateCurve(p, t));
                }
            }

            return new Bounds((minMax.Min + minMax.Max) / 2, minMax.Max - minMax.Min);
        }

        public void SetAnchorNormalAngle(int anchorIndex, float angle)
        {
            angle = (angle + 360) % 360;
        }

        public Bounds PathBounds
        {
            get
            {
                if (!boundsUpToDate)
                {
                    UpdateBounds();
                }
                return bounds;
            }
        }

        void UpdateBounds()
        {
            if (boundsUpToDate)
            {
                return;
            }

            MinMax3D minMax = new MinMax3D();

            for (int i = 0; i < NumSegments; i++)
            {
                Vector3[] p = GetPointsInSegment(i);
                minMax.AddValue(p[0]);
                minMax.AddValue(p[3]);

                List<float> extremePointTimes = MathHelper.ExtremePointTimes(p[0], p[1], p[2], p[3]);
                foreach (float t in extremePointTimes)
                {
                    minMax.AddValue(MathHelper.EvaluateCurve(p, t));
                }
            }

            boundsUpToDate = true;
            bounds = new Bounds((minMax.Min + minMax.Max) / 2, minMax.Max - minMax.Min);
        }

        void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
        {
            for (int i = updatedAnchorIndex - 3; i <= updatedAnchorIndex + 3; i += 3)
            {
                if (i >= 0 && i < points.Count || isClosed)
                {
                    AutoSetAnchorControlPoints(LoopIndex(i));
                }
            }

            AutoSetStartAndEndControls();
        }

        void AutoSetAllControlPoints()
        {
            if (NumAnchorPoints > 2)
            {
                for (int i = 0; i < points.Count; i += 3)
                {
                    AutoSetAnchorControlPoints(i);
                }
            }

            AutoSetStartAndEndControls();
        }

        void AutoSetAnchorControlPoints(int anchorIndex)
        {
            Vector3 anchorPos = points[anchorIndex];
            Vector3 dir = Vector3.zero;
            float[] neighbourDistances = new float[2];

            if (anchorIndex - 3 >= 0 || isClosed)
            {
                Vector3 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
                dir += offset.normalized;
                neighbourDistances[0] = offset.magnitude;
            }
            if (anchorIndex + 3 >= 0 || isClosed)
            {
                Vector3 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
                dir -= offset.normalized;
                neighbourDistances[1] = -offset.magnitude;
            }

            dir.Normalize();

            for (int i = 0; i < 2; i++)
            {
                int controlIndex = anchorIndex + i * 2 - 1;
                if (controlIndex >= 0 && controlIndex < points.Count || isClosed)
                {
                    points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * autoControlLength;
                }
            }
        }

        void AutoSetStartAndEndControls()
        {
            if (isClosed)
            {
                if (NumAnchorPoints == 2)
                {
                    Vector3 dirAnchorAToB = (points[3] - points[0]).normalized;
                    float dstBetweenAnchors = (points[0] - points[3]).magnitude;
                    Vector3 perp = Vector3.Cross(dirAnchorAToB, Vector3.forward);
                    points[1] = points[0] + perp * dstBetweenAnchors / 2f;
                    points[5] = points[0] - perp * dstBetweenAnchors / 2f;
                    points[2] = points[3] + perp * dstBetweenAnchors / 2f;
                    points[4] = points[3] - perp * dstBetweenAnchors / 2f;

                }
                else
                {
                    AutoSetAnchorControlPoints(0);
                    AutoSetAnchorControlPoints(points.Count - 3);
                }
            }
        }

        void UpdateClosedState()
        {
            if (isClosed)
            {
                Vector3 lastAnchorSecondControl = points[points.Count - 1] * 2 - points[points.Count - 2];
                Vector3 firstAnchorSecondControl = points[0] * 2 - points[1];
                points.Add(lastAnchorSecondControl);
                points.Add(firstAnchorSecondControl);
            }

            else
            {
                points.RemoveRange(points.Count - 2, 2);

            }

            AutoSetStartAndEndControls();
        }

        int LoopIndex(int i)
        {
            return (i + points.Count) % points.Count;
        }

        public static Vector2 EvaluateQuadratic(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            Vector2 p0 = Vector2.Lerp(a, b, t);
            Vector2 p1 = Vector2.Lerp(b, c, t);
            return Vector2.Lerp(p0, p1, t);
        }

        public static Vector2 EvaluateCubic(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            Vector2 p0 = EvaluateQuadratic(a, b, c, t);
            Vector2 p1 = EvaluateQuadratic(b, c, d, t);
            return Vector2.Lerp(p0, p1, t);
        }

        public class MinMax3D
        {

            public Vector3 Min { get; private set; }
            public Vector3 Max { get; private set; }

            public MinMax3D()
            {
                Min = Vector3.one * float.MaxValue;
                Max = Vector3.one * float.MinValue;
            }

            public void AddValue(Vector3 v)
            {
                Min = new Vector3(Mathf.Min(Min.x, v.x), Mathf.Min(Min.y, v.y), Mathf.Min(Min.z, v.z));
                Max = new Vector3(Mathf.Max(Max.x, v.x), Mathf.Max(Max.y, v.y), Mathf.Max(Max.z, v.z));
            }
        }
    }
}