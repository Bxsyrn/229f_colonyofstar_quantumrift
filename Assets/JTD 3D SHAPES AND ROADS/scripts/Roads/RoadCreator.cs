using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CNB
{
    [ExecuteInEditMode]
    public class RoadCreator : MonoBehaviour
    {
        [Header("ROAD SKIN-APPEARENCE PRESET")]
        public RoadSO roadSO;
        [Header("TILLING IF PRESET MATERIALS HAVE TEXTURE")]
        public float _floorMaterialTileX = 1;
        public float _wallsMaterialTileX = 200;

        
        [HideInInspector]
        public bool autoUpdate = true;

        [HideInInspector]
        public PathCreatorRoad path;

        [HideInInspector]
        public GameObject _shapeMesh;
        MeshFilter viewMeshFilter;
        MeshRenderer viewMeshRenderer;
        MeshCollider viewMeshCollider;

        GameObject _shapeWallsMesh;
        MeshFilter viewWallsMeshFilter;
        MeshRenderer viewWallsMeshRenderer;
        MeshCollider viewWallsMeshCollider;

        [Header("ROAD WIDTH AND WALLS HEIGHT PARAMS")]
        public float _wallsHeight = 10;
        public float roadWidth = 5;

        Vector3[] roadMeshVerts;
        private float _wallsPerimeter;

        [ExecuteInEditMode]
        private void Awake()
        {
            ResetMeshComponents();
        }

        private void Start()
        {
            if (viewMeshFilter.sharedMesh)
            {
                viewMeshCollider.sharedMesh = viewMeshFilter.sharedMesh;
            }
            if (viewWallsMeshFilter.sharedMesh)
            {
                viewWallsMeshCollider.sharedMesh = viewWallsMeshFilter.sharedMesh;
            }

            if (transform.position != Vector3.zero)
            {
                transform.position = Vector3.zero;
            }
        }

        private void ResetMeshComponents()
        {
            if (_shapeMesh == null|| viewMeshFilter==null || viewMeshRenderer == null)
            {
                if (transform.childCount>0)
                {
                    _shapeMesh = transform.GetChild(0).gameObject;
                    viewMeshFilter = _shapeMesh.GetComponent<MeshFilter>();
                    viewMeshRenderer = _shapeMesh.GetComponent<MeshRenderer>();
                    viewMeshCollider = _shapeMesh.GetComponent<MeshCollider>();
                    viewMeshCollider.sharedMesh = viewMeshFilter.sharedMesh;
                }
                else
                {
                    _shapeMesh = new GameObject("shapeMesh");
                    viewMeshFilter = _shapeMesh.AddComponent<MeshFilter>();
                    viewMeshRenderer = _shapeMesh.AddComponent<MeshRenderer>();
                    viewMeshCollider = _shapeMesh.AddComponent<MeshCollider>();
                    viewMeshCollider.sharedMesh = viewMeshFilter.sharedMesh;
                    _shapeMesh.transform.parent = this.transform;
                }
                
            }

            if (_shapeWallsMesh == null || viewWallsMeshFilter == null || viewWallsMeshRenderer == null)
            {
                if (_shapeMesh.transform.childCount > 0)
                {
                    _shapeWallsMesh = _shapeMesh.transform.GetChild(0).gameObject;
                    viewWallsMeshFilter = _shapeWallsMesh.GetComponent<MeshFilter>();
                    viewWallsMeshRenderer = _shapeWallsMesh.GetComponent<MeshRenderer>();
                    viewWallsMeshCollider = _shapeWallsMesh.GetComponent<MeshCollider>();
                    viewWallsMeshCollider.sharedMesh = viewWallsMeshFilter.sharedMesh;
                }
                else
                {
                    _shapeWallsMesh = new GameObject("wallsMesh");
                    viewWallsMeshFilter = _shapeWallsMesh.AddComponent<MeshFilter>();
                    viewWallsMeshRenderer = _shapeWallsMesh.AddComponent<MeshRenderer>();
                    viewWallsMeshCollider = _shapeWallsMesh.AddComponent<MeshCollider>();
                    viewWallsMeshCollider.sharedMesh = viewWallsMeshFilter.sharedMesh;
                    _shapeWallsMesh.transform.parent = _shapeMesh.transform;
                }
            }
        }

        public void UpdateRoad()
        {
            if (roadSO)
            {
                if (transform.position != Vector3.zero)
                {
                    transform.position = Vector3.zero;
                }

                if (_shapeMesh.transform.localPosition.x != 0 || _shapeMesh.transform.localPosition.z != 0)
                {
                    Vector3 newPos = new Vector3(0, _shapeMesh.transform.localPosition.y, 0);
                    _shapeMesh.transform.localPosition = newPos;
                }

                ResetMeshComponents();

                if (roadSO._floorMaterial && viewMeshRenderer)
                {
                    viewMeshRenderer.sharedMaterial = roadSO._floorMaterial;
                }
                if (roadSO._wallsMaterial && viewWallsMeshRenderer)
                {
                    viewWallsMeshRenderer.sharedMaterial = roadSO._wallsMaterial;
                }

                if (path == null)
                {
                    path = GetComponent<PathCreatorRoad>();
                    if (path == null)
                    {
                        path = gameObject.AddComponent<PathCreatorRoad>();
                    }
                }

                if (path.path.CalculateEvenlySpacedPoints().Length > 0)
                {
                    path.AddWayPointsFromPath(path.evenlySpacedPointsStep, path.path._pathLenght);
                    viewMeshFilter.sharedMesh = CreateRoadMesh(path.waypoints.ToArray(), path.path.IsClosed);
                    viewWallsMeshFilter.sharedMesh = CreateWallMesh(TransformRoadVertsForWalls().ToList());
                    viewMeshCollider.sharedMesh = viewMeshFilter.sharedMesh;
                    viewWallsMeshCollider.sharedMesh = viewWallsMeshFilter.sharedMesh;
                    viewMeshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, 32) * _floorMaterialTileX;
                    viewWallsMeshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, 1 / _wallsPerimeter) * _wallsMaterialTileX;
                }

                path.displayControlPoints = path.path.AutoSetControlPoints == true ? false : true;
            }
            else
            {
                Debug.LogError("Link a RoadSO scriptable object asset in the editor");
            }
        }

        Mesh CreateRoadMesh(Vector3[] points, bool isClosed)
        {
            roadMeshVerts = new Vector3[points.Length * 2];
            Vector2[] uvs = new Vector2[roadMeshVerts.Length];
            int numTris = 2 * (points.Length - 1) + (isClosed ? 2 : 0);
            int[] tris = new int[numTris * 3];
            int vertIndex = 0;
            int triIndex = 0;

            for (int i = 0; i < points.Length; i++)
            {
                Vector3 forward = Vector3.zero;
                if (i < points.Length - 1 || isClosed)
                {
                    forward += points[(i + 1) % points.Length] - points[i];
                }
                if (i > 0 || isClosed)
                {
                    forward += points[i] - points[(i - 1 + points.Length) % points.Length];
                }

                forward.Normalize();
                Vector3 left = Quaternion.Euler(0, -90, 0) * forward;

                roadMeshVerts[vertIndex] = points[i] + left * roadWidth * .5f;
                roadMeshVerts[vertIndex + 1] = points[i] - left * roadWidth * .5f;

                float completionPercent = i / (float)(points.Length - 1);
                uvs[vertIndex] = new Vector2(0, completionPercent);
                uvs[vertIndex + 1] = new Vector2(1, completionPercent);

                if (i < points.Length - 1 || isClosed)
                {
                    tris[triIndex] = vertIndex;
                    tris[triIndex + 1] = (vertIndex + 2) % roadMeshVerts.Length;
                    tris[triIndex + 2] = vertIndex + 1;

                    tris[triIndex + 3] = vertIndex + 1;
                    tris[triIndex + 4] = (vertIndex + 2) % roadMeshVerts.Length;
                    tris[triIndex + 5] = (vertIndex + 3) % roadMeshVerts.Length;
                }

                vertIndex += 2;
                triIndex += 6;
            }
            
            if (isClosed)
            {
                Vector3 avg1 = (roadMeshVerts[roadMeshVerts.Length - 1] + roadMeshVerts[1]) / 2;
                Vector3 avg2 = (roadMeshVerts[roadMeshVerts.Length - 2] + roadMeshVerts[0]) / 2;

                roadMeshVerts[roadMeshVerts.Length - 1] = roadMeshVerts[1] = avg1;
                roadMeshVerts[roadMeshVerts.Length - 2] = roadMeshVerts[0] = avg2;
            }

            Mesh mesh = new Mesh();
            mesh.vertices = roadMeshVerts;
            mesh.triangles = tris;
            mesh.uv = uvs;

            return mesh;
        }

        List<Vector3> TransformRoadVertsForWalls()
        {
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> addLater = new List<Vector3>();
            for (int i = 0; i < roadMeshVerts.Length; i++)
            {
                if (i%2==0)
                {
                    verts.Add(roadMeshVerts[i]);
                }
                else
                {
                    addLater.Add(roadMeshVerts[i]);
                }
            }
            verts .AddRange(addLater);
            return verts;
        }

        public Mesh CreateWallMesh(List<Vector3> roadMeshPoints)
        {
            Vector3 _wallTransformApplied = Vector3.up * _wallsHeight;

            List<Vector3> wallVertices = new List<Vector3>();
            List<int> wallTriangles = new List<int>();
            Mesh wallMesh = new Mesh();

            roadMeshPoints.Add(roadMeshPoints[0]);

            for (int i = 0; i < roadMeshPoints.Count - 1; i++)
            {
                int startIndex = wallVertices.Count;
                wallVertices.Add(roadMeshPoints[i + 1]); 
                wallVertices.Add(roadMeshPoints[i]); 
                wallVertices.Add(roadMeshPoints[i + 1] - _wallTransformApplied); 
                wallVertices.Add(roadMeshPoints[i] - _wallTransformApplied); 

                wallTriangles.Add(startIndex + 0);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex + 0);
            }
            
            wallMesh.vertices = wallVertices.ToArray();
            wallMesh.triangles = wallTriangles.ToArray();
            wallMesh.RecalculateNormals();
            Vector2[] uvs = new Vector2[wallVertices.Count];

            float[] distanceToPreviousVertArr = new float[wallVertices.Count];
            float totalDistance = 0.0f;
            for (int i = 0; i < wallVertices.Count; i += 4)
            {
                float distanceToPreviousVert = Vector3.Distance(wallVertices[i + 1], wallVertices[i]);
                distanceToPreviousVertArr[i % (wallVertices.Count)] = distanceToPreviousVert;
                totalDistance += distanceToPreviousVert;
            }
            _wallsPerimeter = totalDistance;
            float distanceAccumulated = 0.0f;
            float distAcumPrev = 0.0f;
            for (int i = 0; i < wallVertices.Count && wallVertices.Count > 0; i += 4)
            {
                distanceAccumulated += distanceToPreviousVertArr[i % (wallVertices.Count)];

                float percentPrev = Mathf.InverseLerp(0, totalDistance, distAcumPrev);
                float percent = Mathf.InverseLerp(0, totalDistance, distanceAccumulated);

                uvs[(i) % wallVertices.Count] = new Vector2(percent, 0);
                uvs[(i + 1) % wallVertices.Count] = new Vector2(percentPrev, 0);
                uvs[(i + 2) % wallVertices.Count] = new Vector2(percent, _wallsHeight);
                uvs[(i + 3) % wallVertices.Count] = new Vector2(percentPrev, _wallsHeight);

                distAcumPrev = distanceAccumulated;

            }
            wallMesh.uv = uvs;
            roadMeshPoints.RemoveAt(roadMeshPoints.Count - 1);

            return wallMesh;
        }
    }
}