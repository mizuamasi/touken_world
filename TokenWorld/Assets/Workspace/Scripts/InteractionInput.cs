using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Supplies detected positions to the same interaction path used by the URG sensor.
/// Mouse and automatic modes simulate detected objects, not raw URG measurements or TCP.
/// </summary>
[DefaultExecutionOrder(-500)]
public class InteractionInput : MonoBehaviour
{
    public enum Mode { Mouse, Automatic, Sensor }

    [Serializable]
    public class PositionEvent : UnityEvent<Vector3> { }

    [Header("Input source (select Sensor before entering Play mode)")]
    [SerializeField] Mode mode = Mode.Mouse;
    public UrgSensing urgSensing;
    public UrgControl urgControl;
    public Camera interactionCamera;

    [Header("Automatic simulation")]
    [Range(1, 3)] public int automaticPointCount = 2;
    [Min(0.01f)] public float automaticSpeed = 0.15f;
    [Min(0.01f)] public float simulatedWidth = 0.5f;

    [Header("Output (world coordinates)")]
    [Tooltip("入力点ごとに毎フレーム、ワールド座標を送ります。")]
    public PositionEvent PositionUpdated = new PositionEvent();
    [Tooltip("入力がなくなった時や停止・切替時に、一度だけ通知します。")]
    public UnityEvent InputCleared = new UnityEvent();

    static readonly IReadOnlyList<Vector3> EmptyPositions = Array.AsReadOnly(new Vector3[0]);
    IReadOnlyList<Vector3> positions = EmptyPositions;
    /// <summary>Latest read-only world-position snapshot, independent of the input device.</summary>
    public IReadOnlyList<Vector3> Positions { get { return positions; } }

    readonly List<Vector3> localPoints = new List<Vector3>();
    Vector2[] overridePoints;
    Rect inputScreenRect;
    Mode activeMode;
    bool initialized;
    bool paused;
    float automaticTime;

    public Mode CurrentMode { get { return initialized ? activeMode : mode; } }
    public int ActivePointCount { get; private set; }

    /// <summary>The displayed interaction image rectangle in bottom-left-origin screen pixels.</summary>
    public Rect InputScreenRect
    {
        get
        {
            return inputScreenRect.width > 0f && inputScreenRect.height > 0f
                ? inputScreenRect : new Rect(0f, 0f, Screen.width, Screen.height);
        }
        set { inputScreenRect = value; }
    }

    /// <summary>Stops input publication without disconnecting a physical sensor.</summary>
    public bool Paused
    {
        get { return paused; }
        set
        {
            paused = value;
            if (urgSensing != null)
                urgSensing.SetInputPaused(value);
            if (value)
                ClearSimulation();
        }
    }

    void Awake()
    {
        if (urgSensing == null)
            urgSensing = FindObjectOfType<UrgSensing>();
        if (urgControl == null && urgSensing != null)
            urgControl = urgSensing.GetComponent<UrgControl>();
        if (interactionCamera == null)
            interactionCamera = Camera.main;

        activeMode = mode;
        initialized = true;

        // All Awake calls precede UrgControl.Start: simulation never opens TCP.
        if (urgControl != null && activeMode != Mode.Sensor)
            urgControl.enabled = false;
        if (urgSensing != null)
        {
            urgSensing.SetSimulationEnabled(activeMode != Mode.Sensor);
            urgSensing.SetInputPaused(paused);
        }
    }

    void Update()
    {
        ResolveInteractionCamera();
        if (urgSensing == null || paused)
        {
            ClearOutput();
            return;
        }

        if (activeMode != Mode.Sensor)
        {
            localPoints.Clear();
            if (overridePoints != null)
            {
                for (int i = 0; i < overridePoints.Length; i++)
                    AddViewportPoint(overridePoints[i]);
            }
            else if (activeMode == Mode.Mouse)
            {
                Rect rect = InputScreenRect;
                Vector2 mouse = Input.mousePosition;
                if (Input.GetMouseButton(0) && rect.Contains(mouse))
                    AddViewportPoint(new Vector2((mouse.x - rect.x) / rect.width, (mouse.y - rect.y) / rect.height));
            }
            else
            {
                automaticTime += Time.deltaTime * automaticSpeed;
                int count = Mathf.Clamp(automaticPointCount, 1, 3);
                for (int i = 0; i < count; i++)
                {
                    float phase = automaticTime + i * Mathf.PI * 2f / count;
                    AddViewportPoint(new Vector2(0.5f + 0.27f * Mathf.Cos(phase), 0.5f + 0.24f * Mathf.Sin(phase * 0.8f + i)));
                }
            }
            urgSensing.SetSimulationPoints(localPoints, simulatedWidth);
        }

        var snapshot = urgSensing.sensedObjs;
        if (snapshot == null || snapshot.Count == 0)
        {
            ClearOutput();
            return;
        }

        // Publish the complete frame before notifying outputs, so callbacks can also read Positions.
        var worldPoints = new Vector3[snapshot.Count];
        for (int i = 0; i < snapshot.Count; i++)
            worldPoints[i] = urgSensing.GetWorldPostion(snapshot[i]);
        positions = Array.AsReadOnly(worldPoints);
        ActivePointCount = worldPoints.Length;
        if (PositionUpdated != null)
            for (int i = 0; i < worldPoints.Length && !paused && isActiveAndEnabled; i++)
                PositionUpdated.Invoke(worldPoints[i]);
    }

    void AddViewportPoint(Vector2 viewportPoint)
    {
        Vector3 worldPoint;
        if (TryViewportToWorld(viewportPoint, out worldPoint))
            localPoints.Add(urgSensing.transform.InverseTransformPoint(worldPoint));
    }

    /// <summary>Switches between simulation sources; physical sensor selection is fixed at startup.</summary>
    public bool SetSimulationMode(Mode nextMode)
    {
        if (nextMode == Mode.Sensor || CurrentMode == Mode.Sensor)
            return false;
        if (nextMode != Mode.Mouse && nextMode != Mode.Automatic)
            return false;
        mode = nextMode;
        activeMode = nextMode;
        ClearSimulation();
        return true;
    }

    public void ClearSimulation()
    {
        localPoints.Clear();
        if (urgSensing != null && CurrentMode != Mode.Sensor)
            urgSensing.SetSimulationPoints(localPoints, simulatedWidth);
        ClearOutput();
    }

    void ClearOutput()
    {
        bool hadPositions = positions.Count > 0;
        positions = EmptyPositions;
        ActivePointCount = 0;
        if (hadPositions && InputCleared != null)
            InputCleared.Invoke();
    }

    /// <summary>Replaces mouse/demo input with normalized viewport points from an external source.</summary>
    public void OverrideViewportPoints(params Vector2[] points)
    {
        overridePoints = points == null ? new Vector2[0] : (Vector2[])points.Clone();
    }

    public void ReleaseOverride()
    {
        overridePoints = null;
        ClearSimulation();
    }

    /// <summary>Maps a normalized image point to the URG detection plane and checks its bounds.</summary>
    public bool TryViewportToWorld(Vector2 viewportPoint, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        ResolveInteractionCamera();
        if (urgSensing == null || interactionCamera == null ||
            float.IsNaN(viewportPoint.x) || float.IsNaN(viewportPoint.y) ||
            viewportPoint.x < 0f || viewportPoint.x > 1f || viewportPoint.y < 0f || viewportPoint.y > 1f)
            return false;

        Transform sensingTransform = urgSensing.transform;
        Bounds area = urgSensing.sensingArea;
        Plane plane = new Plane(sensingTransform.up, sensingTransform.TransformPoint(area.center));
        Ray ray = interactionCamera.ViewportPointToRay(viewportPoint);
        float distance;
        if (!plane.Raycast(ray, out distance))
            return false;

        Vector3 localPoint = sensingTransform.InverseTransformPoint(ray.GetPoint(distance));
        localPoint.y = area.center.y;
        if (!area.Contains(localPoint))
            return false;
        worldPoint = sensingTransform.TransformPoint(localPoint);
        return true;
    }

    void ResolveInteractionCamera()
    {
        if (interactionCamera == null || !interactionCamera.isActiveAndEnabled)
            interactionCamera = Camera.main;
    }

    void OnDisable()
    {
        ClearSimulation();
    }
}
