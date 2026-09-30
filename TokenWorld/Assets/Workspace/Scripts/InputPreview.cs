using System.Collections.Generic;
using UnityEngine;

// 展示の4画面（床・正面・左・右）を並べて表示する。床の映像と入力座標は同じ矩形で扱い、
// Gameビューのサイズ変更にも追従する。
[DefaultExecutionOrder(-600)]
[RequireComponent(typeof(InteractionInput))]
public class InputPreview : MonoBehaviour
{
    public struct ScreenLayout
    {
        public Rect floor, wall, left, right;
        public bool allScreens;
    }

    public InteractionInput input;
    [Tooltip("床（池）の映像。入力はこの画面だけ。")]
    public RenderTexture previewTexture;
    [Tooltip("正面（滝）の映像。")]
    public RenderTexture wallTexture;
    [Tooltip("左右の映像。左半分が左、右半分が右の画面。")]
    public RenderTexture sideTexture;
    public InteractionControl interactions;
    public BG_Control backgrounds;
    public bool showPanel = true;
    [Tooltip("正面・左・右も床と並べて表示する。")]
    public bool showAllScreens = true;

    const float UiScale = 1.5f;
    const float TwoRowPanelWidth = 980;
    // 以下の4つは画面ピクセル。
    const float TileGap = 8;
    const float SideMargin = 16 * UiScale;
    const float CaptionHeight = 24 * UiScale;
    const float MinTilesHeight = 40;
    const float StripShare = 0.3f;
    static readonly Rect WholeTexture = new Rect(0, 0, 1, 1);
    static readonly Rect LeftHalf = new Rect(0, 0, 0.5f, 1);
    static readonly Rect RightHalf = new Rect(0.5f, 0, 0.5f, 1);
    static readonly Color InputColor = new Color(0.4f, 1f, 0.86f);

    readonly List<Vector3> points = new List<Vector3>();
    readonly GUIContent content = new GUIContent();
    ScreenLayout layout;
    GUIStyle textStyle, titleStyle, buttonStyle, captionStyle, detailStyle, hintStyle;
    Font uiFont;

    public bool AllScreensAvailable { get { return wallTexture != null && sideTexture != null; } }

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
        if (Input.GetKeyDown(KeyCode.V)) showAllScreens = !showAllScreens;
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
        float top = showPanel ? (Screen.width / UiScale < TwoRowPanelWidth ? 124 : 92) * UiScale : 0;
        var area = new Rect(0, top, Screen.width, Mathf.Max(1, Screen.height - top - 26 * UiScale));
        layout = ComputeLayout(area, Aspect(previewTexture, 1), Aspect(wallTexture, 1), Aspect(sideTexture, 0.5f),
            showAllScreens && AllScreensAvailable);
        // OnGUIは左上原点、Input.mousePositionは左下原点。
        Rect floor = layout.floor;
        input.InputScreenRect = new Rect(floor.x, Screen.height - floor.yMax, floor.width, floor.height);
    }

    static float Aspect(Texture texture, float widthShare)
    {
        return texture == null ? 16f / 9f : texture.width * widthShare / texture.height;
    }

    // 上段に［左｜正面｜右］、下段に床。部屋の中から見た並びで、床の上端が正面側の辺になる。
    // 床は入力に使うため、他の画面や見出しと重ならないように置く。
    public static ScreenLayout ComputeLayout(Rect area, float floorAspect, float wallAspect, float sideAspect, bool allScreens)
    {
        var result = new ScreenLayout();
        float usable = area.height - CaptionHeight * 2 - TileGap * 2;
        if (!allScreens || usable < MinTilesHeight)
        {
            float width = Mathf.Min(area.width, area.height * floorAspect);
            float height = width / floorAspect;
            result.floor = new Rect(area.x + (area.width - width) * 0.5f, area.y + (area.height - height) * 0.5f, width, height);
            return result;
        }

        // 左右の端は見出しが画面の縁に付かないように空ける。
        float innerWidth = Mathf.Max(1, area.width - SideMargin * 2);
        float stripAspect = sideAspect * 2 + wallAspect;
        float stripHeight = Mathf.Min(Mathf.Max(1, innerWidth - TileGap * 2) / stripAspect, usable * StripShare);
        float floorWidth = Mathf.Min(innerWidth, (usable - stripHeight) * floorAspect);
        float floorHeight = floorWidth / floorAspect;

        float blockHeight = CaptionHeight + stripHeight + TileGap * 2 + CaptionHeight + floorHeight;
        float y = area.y + (area.height - blockHeight) * 0.5f + CaptionHeight;
        float x = area.x + (area.width - stripHeight * stripAspect - TileGap * 2) * 0.5f;
        result.allScreens = true;
        result.left = new Rect(x, y, stripHeight * sideAspect, stripHeight);
        result.wall = new Rect(result.left.xMax + TileGap, y, stripHeight * wallAspect, stripHeight);
        result.right = new Rect(result.wall.xMax + TileGap, y, stripHeight * sideAspect, stripHeight);
        float floorY = y + stripHeight + TileGap * 2 + CaptionHeight;
        result.floor = new Rect(area.x + (area.width - floorWidth) * 0.5f, floorY, floorWidth, floorHeight);
        return result;
    }

    void PrepareStyles()
    {
        if (textStyle != null) return;
        uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic UI", "Meiryo", "Arial" }, 15);
        textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 14, alignment = TextAnchor.MiddleLeft };
        textStyle.normal.textColor = new Color(0.84f, 0.89f, 0.92f);
        titleStyle = new GUIStyle(textStyle) { fontSize = 19, fontStyle = FontStyle.Bold };
        buttonStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter };
        // 見出しは狭い画面では横に切る。上下の余白をなくし、太字の上端が欠けないようにする。
        captionStyle = new GUIStyle(textStyle) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset() };
        detailStyle = new GUIStyle(textStyle) { fontSize = 12, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset() };
        detailStyle.normal.textColor = new Color(0.55f, 0.63f, 0.68f);
        hintStyle = new GUIStyle(textStyle) { wordWrap = false, clipping = TextClipping.Clip };
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

    // 枠を先に塗り、映像で内側を覆う。映像はアルファを使わない。
    void DrawScreen(Rect rect, Texture texture, Rect texCoords, Color frame)
    {
        Fill(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), frame);
        Fill(rect, Color.black);
        if (texture != null) GUI.DrawTextureWithTexCoords(rect, texture, texCoords, false);
    }

    // 見出しは画面の上に置く。rect は画面ピクセル、描画は GUI.matrix の縮尺で行う。
    void Caption(Rect screen, string title, string detail)
    {
        var rect = new Rect(screen.x / UiScale, (screen.y - CaptionHeight) / UiScale, screen.width / UiScale, CaptionHeight / UiScale);
        content.text = title;
        float titleWidth = Mathf.Min(rect.width, captionStyle.CalcSize(content).x);
        GUI.Label(new Rect(rect.x, rect.y, titleWidth, rect.height), title, captionStyle);
        GUI.Label(new Rect(rect.x + titleWidth + 8, rect.y, Mathf.Max(0, rect.width - titleWidth - 8), rect.height), detail, detailStyle);
    }

    static string Describe(Texture texture, string part, float widthShare)
    {
        if (texture == null) return "";
        return texture.name + part + "  ·  " + Mathf.RoundToInt(texture.width * widthShare) + "×" + texture.height;
    }

    void OnGUI()
    {
        if (input == null) return;
        PrepareStyles();
        GUI.depth = -100;
        Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0.025f, 0.04f, 0.055f));
        var frame = new Color(0.22f, 0.29f, 0.34f);
        if (layout.allScreens)
        {
            DrawScreen(layout.left, sideTexture, LeftHalf, frame);
            DrawScreen(layout.wall, wallTexture, WholeTexture, frame);
            DrawScreen(layout.right, sideTexture, RightHalf, frame);
        }
        // 水色の枠は入力できる画面（床）を示す。
        DrawScreen(layout.floor, previewTexture, WholeTexture, new Color(InputColor.r, InputColor.g, InputColor.b, 0.7f));

        Rect floor = layout.floor;
        foreach (var point in points)
        {
            var camera = input.interactionCamera != null ? input.interactionCamera : Camera.main;
            if (camera == null) continue;
            var uv = camera.WorldToViewportPoint(point);
            var p = new Vector2(floor.x + uv.x * floor.width, floor.yMax - uv.y * floor.height);
            Fill(new Rect(p.x - 10, p.y - 1, 20, 2), InputColor);
            Fill(new Rect(p.x - 1, p.y - 10, 2, 20), InputColor);
        }

        var previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(UiScale, UiScale, 1));
        float panelWidth = Screen.width / UiScale;
        float panelHeight = Screen.height / UiScale;
        if (layout.allScreens)
        {
            Caption(layout.left, "左", Describe(sideTexture, " 左半分", 0.5f));
            Caption(layout.wall, "正面（滝）", Describe(wallTexture, "", 1));
            Caption(layout.right, "右", Describe(sideTexture, " 右半分", 0.5f));
            Caption(layout.floor, "床（池）", "入力はこの画面だけ  ·  " + Describe(previewTexture, "", 1));
        }
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
            bool twoRows = panelWidth < TwoRowPanelWidth;
            float rowY = twoRows ? 86 : 46;
            float rowX = twoRows ? 16 : 536;
            GUI.Label(new Rect(rowX, rowY, 80, 32), "自動の人数", textStyle);
            GUI.enabled = input.CurrentMode == InteractionInput.Mode.Automatic;
            for (int i = 1; i <= 3; i++)
                if (Button(new Rect(rowX + 84 + (i - 1) * 37, rowY, 31, 32), i.ToString(), input.automaticPointCount == i)) input.automaticPointCount = i;
            GUI.enabled = true;
            float viewX = rowX + 205;
            GUI.Label(new Rect(viewX, rowY, 64, 32), "表示 [V]", textStyle);
            GUI.enabled = AllScreensAvailable;
            if (Button(new Rect(viewX + 68, rowY, 64, 32), "4画面", showAllScreens && AllScreensAvailable)) showAllScreens = true;
            if (Button(new Rect(viewX + 136, rowY, 64, 32), "床のみ", !showAllScreens || !AllScreensAvailable)) showAllScreens = false;
            GUI.enabled = true;
        }
        GUI.Label(new Rect(16, panelHeight - 25, panelWidth - 32, 24), "床の映像を左ボタンで押す・ドラッグ  /  V: 4画面・床のみ  /  Space: 入力停止  /  H: パネル表示  ·  水色の十字が検出位置", hintStyle);
        GUI.matrix = previousMatrix;
    }
}
