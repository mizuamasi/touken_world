using System.Collections.Generic;
using UnityEngine;

// 表示した映像と入力座標を同じ矩形で扱い、Gameビューのサイズ変更にも追従する。
[DefaultExecutionOrder(-600)]
[RequireComponent(typeof(InteractionInput))]
public class InputPreview : MonoBehaviour
{
    public InteractionInput input;
    public RenderTexture previewTexture;
    public InteractionControl interactions;
    public BG_Control backgrounds;
    public bool showPanel = true;

    const float UiScale = 1.5f;
    readonly List<Vector3> points = new List<Vector3>();
    Rect previewRect;
    GUIStyle textStyle, titleStyle, buttonStyle;
    Font uiFont;

    void OnEnable()
    {
        if (input == null) input = GetComponent<InteractionInput>();
        input.PositionUpdated.AddListener(RecordPoint);
        UpdateViewport();
    }

    void OnDisable() { if (input != null) input.PositionUpdated.RemoveListener(RecordPoint); }
    void OnDestroy() { if (uiFont != null) Destroy(uiFont); }
    void RecordPoint(Vector3 point) { points.Add(point); }

    void Update()
    {
        points.Clear();
        Cursor.visible = true;
        if (Input.GetKeyDown(KeyCode.H)) showPanel = !showPanel;
        if (Input.GetKeyDown(KeyCode.Alpha1)) input.SetSimulationMode(InteractionInput.Mode.Mouse);
        if (Input.GetKeyDown(KeyCode.Alpha2)) input.SetSimulationMode(InteractionInput.Mode.Automatic);
        if (Input.GetKeyDown(KeyCode.Space)) input.Paused = !input.Paused;
        UpdateViewport();
    }

    public void ClearInput()
    {
        input.ClearSimulation();
        input.Paused = true;
        if (interactions != null) interactions.ClearInteractions();
        points.Clear();
    }

    void UpdateViewport()
    {
        float top = showPanel ? (Screen.width / UiScale < 760 ? 124 : 92) * UiScale : 0;
        float aspect = previewTexture == null ? 16f / 9f : (float)previewTexture.width / previewTexture.height;
        float availableHeight = Mathf.Max(1, Screen.height - top - 26 * UiScale);
        float width = Mathf.Min(Screen.width, availableHeight * aspect);
        float height = width / aspect;
        previewRect = new Rect((Screen.width - width) * 0.5f, top + (availableHeight - height) * 0.5f, width, height);
        // OnGUIは左上原点、Input.mousePositionは左下原点。
        input.InputScreenRect = new Rect(previewRect.x, Screen.height - previewRect.yMax, width, height);
    }

    void PrepareStyles()
    {
        if (textStyle != null) return;
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 15);
        textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 14, alignment = TextAnchor.MiddleLeft };
        textStyle.normal.textColor = new Color(0.84f, 0.89f, 0.92f);
        titleStyle = new GUIStyle(textStyle) { fontSize = 19, fontStyle = FontStyle.Bold };
        buttonStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
    }

    void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    bool Button(Rect rect, string label, bool selected = false)
    {
        Fill(rect, selected ? new Color(0.08f, 0.40f, 0.40f) : new Color(0.15f, 0.20f, 0.24f));
        return GUI.Button(rect, label, buttonStyle);
    }

    void OnGUI()
    {
        if (input == null) return;
        PrepareStyles();
        GUI.depth = -100;
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.04f, 0.055f));
        if (previewTexture != null) GUI.DrawTexture(previewRect, previewTexture, ScaleMode.StretchToFill, false);

        foreach (var point in points)
        {
            var camera = input.interactionCamera != null ? input.interactionCamera : Camera.main;
            if (camera == null) continue;
            var uv = camera.WorldToViewportPoint(point);
            var p = new Vector2(previewRect.x + uv.x * previewRect.width, previewRect.yMax - uv.y * previewRect.height);
            var color = new Color(0.4f, 1f, 0.86f);
            Fill(new Rect(p.x - 10, p.y - 1, 20, 2), color);
            Fill(new Rect(p.x - 1, p.y - 10, 2, 20), color);
        }

        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
        float panelWidth = Screen.width / UiScale;
        float panelHeight = Screen.height / UiScale;
        if (showPanel)
        {
            GUI.Label(new Rect(16, 8, 340, 30), "TOKENWORLD  /  INPUT PREVIEW", titleStyle);
            string status = input.Paused ? "入力停止中" : input.CurrentMode == InteractionInput.Mode.Sensor ? "実機センサー" : "センサーなしで実行中";
            GUI.Label(new Rect(Mathf.Max(350, panelWidth - 270), 10, 255, 26), status + "  ·  検出 " + input.ActivePointCount, textStyle);
            bool sensor = input.CurrentMode == InteractionInput.Mode.Sensor;
            GUI.enabled = !sensor;
            if (Button(new Rect(16, 46, 86, 32), "マウス [1]", input.CurrentMode == InteractionInput.Mode.Mouse)) input.SetSimulationMode(InteractionInput.Mode.Mouse);
            if (Button(new Rect(108, 46, 86, 32), "自動 [2]", input.CurrentMode == InteractionInput.Mode.Automatic)) input.SetSimulationMode(InteractionInput.Mode.Automatic);
            GUI.enabled = true;
            if (Button(new Rect(200, 46, 112, 32), input.Paused ? "入力を再開" : "入力を止める")) input.Paused = !input.Paused;
            if (Button(new Rect(318, 46, 70, 32), "クリア")) ClearInput();
            GUI.enabled = backgrounds != null && !backgrounds._is_changeScene;
            if (Button(new Rect(394, 46, 126, 32), backgrounds != null && backgrounds._is_changeScene ? "切り替え中…" : "季節を変える [B]")) backgrounds.NextScene();
            GUI.enabled = true;
            float rowY = panelWidth < 760 ? 86 : 46;
            float rowX = panelWidth < 760 ? 16 : 536;
            GUI.Label(new Rect(rowX, rowY, 80, 32), "自動の人数", textStyle);
            GUI.enabled = input.CurrentMode == InteractionInput.Mode.Automatic;
            for (int i = 1; i <= 3; i++)
                if (Button(new Rect(rowX + 84 + (i - 1) * 37, rowY, 31, 32), i.ToString(), input.automaticPointCount == i)) input.automaticPointCount = i;
            GUI.enabled = true;
        }
        GUI.Label(new Rect(16, panelHeight - 25, panelWidth - 32, 24), "映像内を左ボタンで押す・ドラッグ  /  Space: 入力停止  /  H: パネル表示  ·  水色の十字が検出位置", textStyle);
        GUI.matrix = previousMatrix;
    }
}
