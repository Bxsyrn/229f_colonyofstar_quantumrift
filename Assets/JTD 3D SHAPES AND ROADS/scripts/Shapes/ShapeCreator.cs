using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CNB
{
    [ExecuteInEditMode]
    [System.Serializable]
    public class ShapeCreator : MonoBehaviour
    {
        [Header("SHAPE SKIN-APPEARENCE PRESET")]
        public ShapeSO shapeSO;
        [Header("TILLING IF PRESET MATERIALS HAVE TEXTURE")]
        public float _floorMaterialTileX = 20;
        public float _wallsMaterialTileX = 200;
        [HideInInspector]
        public bool autoUpdate = true;

        [HideInInspector]
        public List<Shape> shapes = new List<Shape>();
        [HideInInspector]
        public List<List<Vector3>> evenlySpacedPointsList = new List<List<Vector3>>();

        List<GameObject> prefabListToSpawn = new List<GameObject>();
        [Header("RADIUS OF THE EDITOR DRAGABLE DISK GIZMOS")]
        public float handleRadius = 3f;
        [Header("SHAPE WALLS HEIGHT")]
        public float _wallsHeight = 50;

        [Header("SPAWNER PARAMETERS")]
        public int _minRadius = 3;
        [HideInInspector]
        public int iterationStep = 10;
        public bool rotPrefab90;
        public float GOscale = 1;
        public float offSetVertical;

        [HideInInspector]
        public Vector2 regionSize;
        int rejectionSamples = 10;
        [HideInInspector]
        public List<Vector2> points;
        GameObject _holder;
        Bounds _bounds;

        MeshFilter viewMeshFilter;
        MeshRenderer viewMeshRenderer;
        MeshCollider viewMeshCollider;

        MeshFilter viewWallsMeshFilter;
        MeshRenderer viewWallsMeshRenderer;
        MeshCollider viewWallsMeshCollider;
        
        

        [HideInInspector]
        public float _wallsPerimeter;

        [SerializeField][HideInInspector]
        public List<PathCreatorShape> pathCreators = new List<PathCreatorShape>();

        [HideInInspector]
        public GameObject _shapeMesh;
        GameObject _shapeWallsMesh;

        [Header("SHAPE MESH RESOLUTION")]
        [Range(3, 50)]
        public int evenlySpacedPointsStep = 30;

        [ExecuteInEditMode]
        private void Start()
        {
            ResetMeshComponents();
            if (transform.position!=Vector3.zero)
            {
                transform.position = Vector3.zero;
            }
        }

        private void ResetMeshComponents()
        {
            if (_shapeMesh == null || viewMeshFilter == null || viewMeshRenderer == null)
            {
                if (transform.childCount > 0)
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
                    _shapeMesh.transform.tag = "SpawnableShape";
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

        public void InitSpawner()
        {
            _holder = GameObject.FindGameObjectWithTag("SpawnManager");
            if (_holder==null)
            {
                _holder = new GameObject("SpawnHolder");
                _holder.transform.tag = "SpawnManager";
            }
           
            UpdateBounds();
            points = GeneratePoints(_minRadius, regionSize, rejectionSamples);
            if (PoolManCNB.instance==null)
            {
                _holder.AddComponent<PoolManCNB>();
            }
        }

        public void UpdateMeshDisplay()
        {
            if (transform.position != Vector3.zero)
            {
                transform.position = Vector3.zero;
            }
            
            if (shapeSO)
            {
                ResetMeshComponents();

                if (_shapeMesh.transform.localPosition.x!=0 || _shapeMesh.transform.localPosition.z!=0)
                {
                    Vector3 newPos = new Vector3(0, _shapeMesh.transform.localPosition.y, 0);
                    _shapeMesh.transform.localPosition= newPos;
                }

                if (shapes.Count > 0)
                {
                    CompositeShape compShape = new CompositeShape(shapes);

                    viewMeshFilter.sharedMesh = compShape.GetMesh();
                    viewMeshCollider.sharedMesh = viewMeshFilter.sharedMesh;
                    viewMeshRenderer.material = shapeSO._floorMaterial;
                    viewMeshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, 1) * _floorMaterialTileX;

                    Vector2 meshMin = new Vector2(float.MaxValue, float.MaxValue);
                    Vector2 meshMax = new Vector2(float.MinValue, float.MinValue);
                    if (compShape.vertices.Length > 0)
                    {
                        CalculateMaxMinV2sForUVs(ref meshMax, ref meshMin, compShape.vertices.ToList());
                        Vector2[] uvs = new Vector2[compShape.vertices.Length];
                        for (int i = 0; i < compShape.vertices.Length; i++)
                        {
                            uvs[i].x = Mathf.InverseLerp(meshMin.x, meshMax.x, compShape.vertices[i].x);
                            uvs[i].y = Mathf.InverseLerp(meshMin.y, meshMax.y, compShape.vertices[i].z);
                        }
                        viewMeshFilter.sharedMesh.uv = uvs;
                    }

                    viewWallsMeshFilter.sharedMesh = CreateWallMesh(shapes);
                    viewWallsMeshRenderer.material = shapeSO._wallsMaterial;
                    viewWallsMeshRenderer.sharedMaterial.mainTextureScale = new Vector2(1, 1 / _wallsPerimeter) * _wallsMaterialTileX;
                    viewWallsMeshCollider.sharedMesh = viewWallsMeshFilter.sharedMesh;


                    if (viewMeshFilter.sharedMesh)
                    {
                        _bounds = viewMeshFilter.sharedMesh.bounds;
                    }
                }
                else if (pathCreators.Count == 0)
                {
                    viewMeshFilter.mesh = viewWallsMeshFilter.mesh = viewMeshCollider.sharedMesh = viewWallsMeshCollider.sharedMesh = null;
                }


                for (int i = 0; i < pathCreators.Count; i++)
                {
                    if (shapes.Count - 1 < i)
                    {
                        DestroyImmediate(pathCreators[i]);
                        pathCreators.RemoveAt(i);
                    }
                }
            }
            else
            {
                Debug.LogError("Link a shapeSO scriptable object asset in the editor");
            }
        }

        public void Spawn()
        {
            prefabListToSpawn = shapeSO.prefabsToSpawn;

            int objsToPoolCalculation = points.Count / (iterationStep == 0 ? 1 : iterationStep);
            bool hitSpawnableShape = false;
            Vector3 newPosY;
            Vector3 rayCastPos;

            PoolManCNB.instance.FillPoolDictionary();

            foreach (var item in prefabListToSpawn)
            {
                if (item != null)
                {
                    PoolManCNB.instance.CreatePool(item, objsToPoolCalculation / prefabListToSpawn.Count);
                }
            }

            if (points != null && prefabListToSpawn.Count > 0)
            {
                for (int i = 0; i < points.Count; i += iterationStep % points.Count)
                {
                    int index = Random.Range(0, prefabListToSpawn.Count);
                    rayCastPos = new Vector3(points[i].x, _shapeMesh.transform.position.y, points[i].y);
                    SpawnCheck(rayCastPos, ref hitSpawnableShape);
                    if (prefabListToSpawn[index] != null && hitSpawnableShape)
                    {
                        GameObject newGO = PoolManCNB.instance.ReuseObject(prefabListToSpawn[index], transform.localPosition, Quaternion.identity);

                        if (newGO != null)
                        {
                            newGO.transform.rotation = Quaternion.Euler(rotPrefab90 == true ? 90 : 0, 0, 0);
                            newGO.transform.Rotate(Vector3.right * -90);

                            newGO.transform.localPosition = rayCastPos;
                            newPosY = new Vector3(newGO.transform.localPosition.x, _shapeMesh.transform.position.y + offSetVertical, newGO.transform.localPosition.z);

                            newGO.transform.localScale = Vector3.one * (GOscale == 0 ? 1f : GOscale);
                            newGO.transform.localPosition = newPosY;
                        }
                    }
                    hitSpawnableShape = false;
                }
            }
        }

        void UpdateBounds()
        {
            regionSize = new Vector2(_bounds.size.x, _bounds.size.z);
        }

        List<Vector2> GeneratePoints(float radius, Vector2 sampleRegionSize, int numSamplesBeforeRejection = 30)
        {
            if (radius == 0)
            {
                radius = 5;
            }

            float cellSize = radius / Mathf.Sqrt(2);

            int[,] grid = new int[Mathf.CeilToInt(sampleRegionSize.x / cellSize), Mathf.CeilToInt(sampleRegionSize.y / cellSize)];
            List<Vector2> points = new List<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2>();

            spawnPoints.Add(sampleRegionSize / 2);
            while (spawnPoints.Count > 0)
            {
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                Vector2 spawnCentre = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < numSamplesBeforeRejection; i++)
                {
                    float angle = Random.value * Mathf.PI * 2;
                    Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                    Vector2 candidate = spawnCentre + dir * Random.Range(radius, 2 * radius);
                    if (IsValid(candidate, sampleRegionSize, cellSize, radius, points, grid))
                    {
                        points.Add(candidate);
                        spawnPoints.Add(candidate);
                        grid[(int)(candidate.x / cellSize), (int)(candidate.y / cellSize)] = points.Count;
                        candidateAccepted = true;
                        break;
                    }
                }
                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                }
            }
            Vector2 transformPos = new Vector2(_bounds.center.x-_bounds.size.x/2, _bounds.center.z- _bounds.size.z / 2);
            for (int i = 0; i < points.Count; i++)
            {
                points[i] += transformPos;
            }
            return points;
        }

        bool IsValid(Vector2 candidate, Vector2 sampleRegionSize, float cellSize, float radius, List<Vector2> points, int[,] grid)
        {
            if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
            {
                int cellX = (int)(candidate.x / cellSize);
                int cellY = (int)(candidate.y / cellSize);
                int searchStartX = Mathf.Max(0, cellX - 2);
                int searchEndX = Mathf.Min(cellX + 2, grid.GetLength(0) - 1);
                int searchStartY = Mathf.Max(0, cellY - 2);
                int searchEndY = Mathf.Min(cellY + 2, grid.GetLength(1) - 1);

                for (int x = searchStartX; x <= searchEndX; x++)
                {
                    for (int y = searchStartY; y <= searchEndY; y++)
                    {
                        int pointIndex = grid[x, y] - 1;
                        if (pointIndex != -1)
                        {
                            float sqrDst = (candidate - points[pointIndex]).sqrMagnitude;
                            if (sqrDst < radius * radius)
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }

        void SpawnCheck(Vector3 pos, ref bool hitSpawnableShape)
        {
            int offset = 100;
            (bool, RaycastHit) result = RayCheck(pos, offset);

            if (result.Item1)
            {
                if (result.Item2.collider.tag == "SpawnableShape" && result.Item2.collider.gameObject.transform.parent.name == this.name)
                {
                    hitSpawnableShape = true;
                }
            }
        }

        (bool, RaycastHit) RayCheck(Vector3 pos, int offSet)
        {
            bool hit = false;
            RaycastHit hitInfo;
            Vector3 rayDir = Vector3.down;
            Vector3 origin = pos - rayDir * offSet;
            Ray ray = new Ray(origin, rayDir);
            hit = Physics.Raycast(ray, out hitInfo, Mathf.Infinity);
            return (hit, hitInfo);
        }


        public Mesh CreateWallMesh(List<Shape> shapes)
        {
            Vector3 _wallTransformApplied = Vector3.up * _wallsHeight;

            List<Vector3> wallVertices = new List<Vector3>();
            List<int> wallTriangles = new List<int>();
            Mesh wallMesh = new Mesh();

            foreach (Shape outline in shapes)
            {
                outline.points.Add(outline.points[0]);
                for (int i = 0; i < outline.points.Count - 1; i++)
                {
                    int startIndex = wallVertices.Count;
                    wallVertices.Add(outline.points[i + 1]); // right
                    wallVertices.Add(outline.points[i]); // left
                    wallVertices.Add(outline.points[i + 1] - _wallTransformApplied); // bottom right
                    wallVertices.Add(outline.points[i] - _wallTransformApplied); // bottom left

                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);

                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }
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

            foreach (Shape outline in shapes)
            {
                outline.points.RemoveAt(outline.points.Count-1);
            }
            return wallMesh;
        }

        public void CreatePath(int index)
        {
            if (shapes[index].points.Count>=2)
            {
                if (pathCreators.Count < index + 1 || pathCreators[index] == null)
                {
                    pathCreators.Add(gameObject.AddComponent<PathCreatorShape>());
                }
                pathCreators[index].path = new PathC(transform.position);
                pathCreators[index].path.points[0] = shapes[index].points[0];
                pathCreators[index].path.points[3] = shapes[index].points[1];

                for (int i = 2; i < shapes[index].points.Count; i++)
                {
                    pathCreators[index].path.AddSegment(shapes[index].points[i]);
                }

                pathCreators[index].path.IsClosed = true;
                pathCreators[index].path.AutoSetControlPoints = true;

                pathCreators[index].AddWayPointsFromPath(evenlySpacedPointsStep);
                shapes[index].points = pathCreators[index].waypoints;
            }
        }

        public void CalculateMaxMinV2sForUVs(ref Vector2 max, ref Vector2 min, List<Vector3> meshPoints)
        {
            for (int i = 0; i < meshPoints.Count; i++)
            {
                if (meshPoints[i].x<min.x)
                {
                    min.x = meshPoints[i].x;
                }
                if (meshPoints[i].x > max.x)
                {
                    max.x = meshPoints[i].x;
                }
                if (meshPoints[i].z < min.y)
                {
                    min.y = meshPoints[i].z;
                }
                if (meshPoints[i].z > max.y)
                {
                    max.y = meshPoints[i].z;
                }
            }
        }
    }
}