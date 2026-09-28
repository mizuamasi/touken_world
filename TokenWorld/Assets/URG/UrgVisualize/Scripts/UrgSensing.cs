using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UrgSensing : SingletonMonoBehaviour<UrgSensing>
{
    [Header("sensing params")]
    [Range(0.1f, 2.0f)] public float objThreshold = 0.5f;
    public float minWidth = 0.01f;
    public Bounds sensingArea;

    [Header("sensing result")]
    // Published on the main thread. Consumers may keep a snapshot, but must not edit it.
    public List<SensedObject> sensedObjs = new List<SensedObject>();
    public Material mat;
    public bool drawDetectionMesh = true;

    readonly object lockObj = new object();
    List<SensedObject> pendingSensorObjects;
    bool simulationEnabled;
    bool inputPaused;
    int inputGeneration;

    public bool SimulationEnabled
    {
        get { lock (lockObj) return simulationEnabled; }
    }

    UrgDeviceEthernet urg;
    UrgControl urgControl;

    Mesh sensedObjMesh
    {
        get
        {
            if (_mesh == null)
            {
                _mesh = new Mesh();
                _mesh.vertices = Enumerable.Repeat(Vector3.zero, 5).ToArray();
                _mesh.SetIndices(
                    new[] {
                        0, 1, 4,
                        4, 1, 3,
                        3, 1, 2
                    },MeshTopology.Triangles, 0);
                _mesh.MarkDynamic();
            }
            return _mesh;
        }
    }
    Mesh _mesh;
    ComputeBuffer verticesBuffer;
    List<Vector3> verticesData;

    private void Start()
    {
        if (sensedObjs == null)
            sensedObjs = new List<SensedObject>();
        verticesData = new List<Vector3>();
        urgControl = GetComponent<UrgControl>();
        urg = GetComponent<UrgDeviceEthernet>();
        if (urg != null)
        {
            urg.onReadMD += OnReadMD;
            urg.onReadME += OnReadME;
        }
    }

    private void Update()
    {
        lock (lockObj)
        {
            if (!simulationEnabled && !inputPaused && pendingSensorObjects != null)
                sensedObjs = pendingSensorObjects;
            pendingSensorObjects = null;
        }
        DrawMesh();
    }

    /// <summary>
    /// Selects detected-position simulation. This does not simulate URG raw ranges or TCP.
    /// Call from the main thread before UrgControl.Start when enabling simulation.
    /// </summary>
    public void SetSimulationEnabled(bool enabled)
    {
        lock (lockObj)
        {
            simulationEnabled = enabled;
            inputGeneration++;
            pendingSensorObjects = null;
            sensedObjs = new List<SensedObject>();
        }
    }

    public void SetInputPaused(bool paused)
    {
        lock (lockObj)
        {
            if (inputPaused == paused)
                return;
            inputPaused = paused;
            inputGeneration++;
            pendingSensorObjects = null;
            sensedObjs = new List<SensedObject>();
        }
    }

    /// <summary>Publishes a copied snapshot of simulated local XZ detection positions.</summary>
    public void SetSimulationPoints(IList<Vector3> localPoints, float width = 0.5f)
    {
        lock (lockObj)
        {
            if (!simulationEnabled)
                return;

            var objects = new List<SensedObject>();
            if (!inputPaused && localPoints != null)
            {
                var halfWidth = Vector3.right * Mathf.Max(0.01f, width) * 0.5f;
                for (int i = 0; i < localPoints.Count; i++)
                {
                    Vector3 point = localPoints[i];
                    point.y = sensingArea.center.y;
                    if (sensingArea.Contains(point))
                        objects.Add(new SensedObject { center = point, p0 = point - halfWidth, p1 = point + halfWidth });
                }
            }
            sensedObjs = objects;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(sensingArea.center, sensingArea.size);
		Gizmos.color = Color.green;
        if (sensedObjs != null)
        {
            for (var i = 0; i < sensedObjs.Count; i++)
            {
                var so = sensedObjs[i];
                Gizmos.DrawLine(so.p0, so.center);
                Gizmos.DrawLine(so.center, so.p1);
            }
        }

		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(Vector3.zero, 0.4f);
	}

    private void OnDestroy()
    {
        if (urg != null)
        {
            urg.onReadMD -= OnReadMD;
            urg.onReadME -= OnReadME;
        }
        if (verticesBuffer != null)
            verticesBuffer.Release();
        if (_mesh != null)
            Destroy(_mesh);
    }

    void DrawMesh()
    {
        if (!drawDetectionMesh || mat == null || !mat.enableInstancing || sensedObjs == null || sensedObjs.Count == 0 ||
            verticesData == null || !SystemInfo.supportsInstancing || !SystemInfo.supportsComputeShaders)
            return;

        verticesData.Clear();
        int count = Mathf.Min(sensedObjs.Count, 1023);
        for (var i = 0; i < count; i++)
            verticesData.AddRange(sensedObjs[i].vertices);
        if (verticesBuffer == null || verticesBuffer.count < verticesData.Count)
        {
            if (verticesBuffer != null)
                verticesBuffer.Release();
            verticesBuffer = new ComputeBuffer(Mathf.Max(1080, verticesData.Count), sizeof(float) * 3);
        }
        verticesBuffer.SetData(verticesData);
        mat.SetInt("_VCount", sensedObjMesh.vertexCount);
        mat.SetBuffer("_VBuffer", verticesBuffer);
        var matrices = Enumerable.Repeat(transform.localToWorldMatrix, count).ToList();
        Graphics.DrawMeshInstanced(sensedObjMesh, 0, mat, matrices);
    }

    void GetPointFromDistance(int step, float distance, ref Vector3 pos)
    {
        var angle = step * urgControl.angleDelta - urgControl.angleOffset + 90f;
        pos.x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
        pos.z = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
    }
    void OnReadMD(List<long> distances)
    {
        if (distances == null || distances.Count < 1)
            return;

        int generation;
        lock (lockObj)
        {
            if (simulationEnabled || inputPaused)
                return;
            generation = inputGeneration;
        }

        Vector3 prevP = Vector3.zero;
        Vector3 checkP = Vector3.zero;
        Vector3 currentP = Vector3.zero;
        Vector3 accum = Vector3.zero;
        int accumCount = 0;
        bool isObj = false;

        var detectedObjects = new List<SensedObject>();

        GetPointFromDistance(0, distances[0], ref prevP);
        for (var i = 0; i < distances.Count; i++)
        {
            var d = distances[i] * urgControl.DistancesSca;
            GetPointFromDistance(i, d, ref currentP);

            if (isObj)
            {
                if (objThreshold * objThreshold < (currentP - prevP).sqrMagnitude && sensingArea.Contains(currentP))//new obj
                {
                    if (minWidth * minWidth < (prevP - checkP).sqrMagnitude)
                        detectedObjects.Add(new SensedObject() { p0 = checkP, p1 = prevP, center = accum / accumCount });
                    checkP = currentP;
                    accum = currentP;
                    isObj = true;
                    accumCount = 1;
                }
                else if (!sensingArea.Contains(currentP))//lost obj
                {
                    if (minWidth * minWidth < (prevP - checkP).sqrMagnitude)
                        detectedObjects.Add(new SensedObject() { p0 = checkP, p1 = prevP, center = accum / accumCount });
                    isObj = false;
                    accumCount = 0;
                }
                else//continue obj
                {
                    accum += currentP;
                    accumCount++;
                }
            }
            else
            {
                if (objThreshold * objThreshold < (currentP - prevP).sqrMagnitude && sensingArea.Contains(currentP))//new obj
                {
                    checkP = currentP;
                    accum = currentP;
                    isObj = true;
                    accumCount = 1;
                }
            }
            prevP = currentP;
        }

        // The receive thread never changes the list being enumerated by scene scripts.
        lock (lockObj)
        {
            if (!simulationEnabled && !inputPaused && generation == inputGeneration)
                pendingSensorObjects = detectedObjects;
        }
    }
    void OnReadME(List<long> distances, List<long> strengths)
    {
        OnReadMD(distances);
    }

    [System.Serializable]
    public struct SensedObject
    {
        public Vector3 p0;
        public Vector3 p1;
        public Vector3 center;

        public Vector3[] vertices
        {
            get
            {
                if (_vs == null)
                    _vs = new Vector3[5];
                var width = (p1 - p0).magnitude;
                _vs[0] = p0;
                _vs[1] = center;
                _vs[2] = p1;
                _vs[3] = p1 + center.normalized * width * 0.5f;
                _vs[4] = p0 + center.normalized * width * 0.5f;
                return _vs;
            }
        }
        Vector3[] _vs;
    }

	public Vector3 GetWorldPostion(SensedObject obj) {
		return transform.localToWorldMatrix.MultiplyPoint(obj.center);
	}
}
