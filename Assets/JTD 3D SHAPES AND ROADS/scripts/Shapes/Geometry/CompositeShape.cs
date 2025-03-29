using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*
 * Processes array of shapes into a single mesh
 * Automatically determines which shapes are solid, and which are holes
 * Ignores invalid shapes (contain self-intersections, too few points, overlapping holes)
 */

namespace CNB
{
    public partial class CompositeShape
    {
        public Vector3[] vertices;
        public int[] triangles;

        Shape[] shapes;
        float height = 0;

        public CompositeShape(IEnumerable<Shape> shapes)
        {
            this.shapes = shapes.ToArray();
        }

        public Mesh GetMesh()
        {
            Process();

            return new Mesh()
            {
                vertices = vertices,
                triangles = triangles,
                normals = vertices.Select(x => Vector3.up).ToArray()
            };
        }

        public void Process()
        {
            // Generate array of valid shape data
            CompositeShapeData[] eligibleShapes = shapes.Select(x => new CompositeShapeData(x.points.ToArray())).Where(x => x.IsValidShape).ToArray();

            // Set parents for all shapes. A parent is a shape which completely contains another shape.
            for (int i = 0; i < eligibleShapes.Length; i++)
            {
                for (int j = 0; j < eligibleShapes.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (eligibleShapes[i].IsParentOf(eligibleShapes[j]))
                    {
                        eligibleShapes[j].parents.Add(eligibleShapes[i]);
                    }
                }
            }

            // Holes are shapes with an odd number of parents.
            CompositeShapeData[] holeShapes = eligibleShapes.Where(x => x.parents.Count % 2 != 0).ToArray();
            foreach (CompositeShapeData holeShape in holeShapes)
            {
                // The most immediate parent (i.e the smallest parent shape) will be the one that has the highest number of parents of its own. 
                CompositeShapeData immediateParent = holeShape.parents.OrderByDescending(x => x.parents.Count).First();
                immediateParent.holes.Add(holeShape);
            }

            // Solid shapes have an even number of parents
            CompositeShapeData[] solidShapes = eligibleShapes.Where(x => x.parents.Count % 2 == 0).ToArray();
            foreach (CompositeShapeData solidShape in solidShapes)
            {
                solidShape.ValidateHoles();

            }
            // Create polygons from the solid shapes and their associated hole shapes
            Polygon[] polygons = solidShapes.Select(x => new Polygon(x.polygon.points, x.holes.Select(h => h.polygon.points).ToArray())).ToArray();
  
            // Flatten the points arrays from all polygons into a single array, and convert the vector2s to vector3s.
            vertices = polygons.SelectMany(x => x.points.Select(v2 => new Vector3(v2.x, height, v2.y))).ToArray();

            // Triangulate each polygon and flatten the triangle arrays into a single array.
            List<int> allTriangles = new List<int>();
            int startVertexIndex = 0;
            for (int i = 0; i < polygons.Length; i++)
            {
                Triangulator triangulator = new Triangulator(polygons[i]);
                int[] polygonTriangles = triangulator.Triangulate();

                for (int j = 0; j < polygonTriangles.Length; j++)
                {
                    allTriangles.Add(polygonTriangles[j] + startVertexIndex);
                }
                startVertexIndex += polygons[i].numPoints;
            }

            triangles = allTriangles.ToArray();
        }

        public class CompositeShapeData
        {
            public readonly Vector2[] points;
            public readonly Polygon polygon;
            public readonly int[] triangles;

            public List<CompositeShapeData> parents = new List<CompositeShapeData>();
            public List<CompositeShapeData> holes = new List<CompositeShapeData>();
            public bool IsValidShape { get; private set; }

            public CompositeShapeData(Vector3[] points)
            {
                this.points = points.Select(v => v.ToXZ()).ToArray();
                IsValidShape = points.Length >= 3 && !IntersectsWithSelf();

                if (IsValidShape)
                {
                    polygon = new Polygon(this.points);
                    Triangulator t = new Triangulator(polygon);
                    triangles = t.Triangulate();
                }
            }

            // Removes any holes which overlap with another hole
            public void ValidateHoles()
            {
                for (int i = 0; i < holes.Count; i++)
                {
                    for (int j = i + 1; j < holes.Count; j++)
                    {
                        bool overlap = holes[i].OverlapsPartially(holes[j]);

                        if (overlap)
                        {
                            holes[i].IsValidShape = false;
                            break;
                        }
                    }
                }

                for (int i = holes.Count - 1; i >= 0; i--)
                {
                    if (!holes[i].IsValidShape)
                    {
                        holes.RemoveAt(i);
                    }
                }
            }

            // A parent is a shape which fully contains another shape
            public bool IsParentOf(CompositeShapeData otherShape)
            {
                if (otherShape.parents.Contains(this))
                {
                    return true;
                }
                if (parents.Contains(otherShape))
                {
                    return false;
                }

                // check if first point in otherShape is inside this shape. If not, parent test fails.
                // if yes, then continue to line seg intersection test between the two shapes

                // (this point test is important because without it, if all line seg intersection tests fail,
                // we wouldn't know if otherShape is entirely inside or entirely outside of this shape)
                bool pointInsideShape = false;
                if (triangles!=null)
                {
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        if (PointInTriangle(polygon.points[triangles[i]], polygon.points[triangles[i + 1]], polygon.points[triangles[i + 2]], otherShape.points[0]))
                        {
                            pointInsideShape = true;
                            break;
                        }
                    }
                }

                if (!pointInsideShape)
                {
                    return false;
                }

                // Check for intersections between line segs of this shape and otherShape (any intersections will fail the parent test)
                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment parentSeg = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = 0; j < otherShape.points.Length; j++)
                    {
                        LineSegment childSeg = new LineSegment(otherShape.points[j], otherShape.points[(j + 1) % otherShape.points.Length]);
                        if (LineSegmentsIntersect(parentSeg.a, parentSeg.b, childSeg.a, childSeg.b))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }

            // Test if the shapes overlap partially (test will fail if one shape entirely contains other shape, i.e. one is parent of the other).
            public bool OverlapsPartially(CompositeShapeData otherShape)
            {

                // Check for intersections between line segs of this shape and otherShape (any intersection will validate the overlap test)
                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment segA = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = 0; j < otherShape.points.Length; j++)
                    {
                        LineSegment segB = new LineSegment(otherShape.points[j], otherShape.points[(j + 1) % otherShape.points.Length]);
                        if (LineSegmentsIntersect(segA.a, segA.b, segB.a, segB.b))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            // Checks if any of the line segments making up this shape intersect
            public bool IntersectsWithSelf()
            {

                for (int i = 0; i < points.Length; i++)
                {
                    LineSegment segA = new LineSegment(points[i], points[(i + 1) % points.Length]);
                    for (int j = i + 2; j < points.Length; j++)
                    {
                        if ((j + 1) % points.Length == i)
                        {
                            continue;
                        }
                        LineSegment segB = new LineSegment(points[j], points[(j + 1) % points.Length]);
                        if (LineSegmentsIntersect(segA.a, segA.b, segB.a, segB.b))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            public struct LineSegment
            {
                public readonly Vector2 a;
                public readonly Vector2 b;

                public LineSegment(Vector2 a, Vector2 b)
                {
                    this.a = a;
                    this.b = b;
                }
            }

            public static bool LineSegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
            {
                float denominator = ((b.x - a.x) * (d.y - c.y)) - ((b.y - a.y) * (d.x - c.x));
                if (Mathf.Approximately(denominator, 0))
                {
                    return false;
                }

                float numerator1 = ((a.y - c.y) * (d.x - c.x)) - ((a.x - c.x) * (d.y - c.y));
                float numerator2 = ((a.y - c.y) * (b.x - a.x)) - ((a.x - c.x) * (b.y - a.y));

                if (Mathf.Approximately(numerator1, 0) || Mathf.Approximately(numerator2, 0))
                {
                    return false;
                }

                float r = numerator1 / denominator;
                float s = numerator2 / denominator;

                return (r > 0 && r < 1) && (s > 0 && s < 1);
            }

            public static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
            {
                float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
                float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
                float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);
                return s >= 0 && t >= 0 && (s + t) <= 1;

            }
        }
    }
}