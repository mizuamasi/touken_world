using UnityEditor;
using UnityEngine;

// Scene ビューに、展示の各画面に映る範囲を名前付きの枠で描く。
// 床と正面は3Dカメラの範囲、左右と文字・暗転は UI キャンバスの位置。Gizmos を切ると消える。
public static class ScreenSceneGuides
{
    static readonly Color FloorColor = new Color(0.4f, 1f, 0.86f);
    static readonly Color WallColor = new Color(1f, 0.72f, 0.3f);
    static readonly Color SideColor = new Color(0.8f, 0.62f, 1f);
    static readonly Color UiColor = new Color(0.75f, 0.8f, 0.85f);
    // 正面のカメラの下端が床の高さに届かない場合に枠を描く距離。
    const float FallbackDistance = 15;
    static readonly Vector3[] corners = new Vector3[4];
    static readonly Vector3[] half = new Vector3[4];
    static GUIStyle labelStyle;
    static Texture2D labelBackground;

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void Draw(InputPreview preview, GizmoType gizmoType)
    {
        Plane ground = GroundPlane(preview);
        var canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var camera in Camera.allCameras)
        {
            RenderTexture target = camera.targetTexture;
            if (target == null) continue;
            if (!camera.orthographic)
            {
                if (target == preview.previewTexture) DrawFloor(camera, target, ground);
                else if (target == preview.wallTexture) DrawWall(camera, target, ground);
                continue;
            }
            foreach (var canvas in canvases)
            {
                if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != camera) continue;
                ((RectTransform)canvas.transform).GetWorldCorners(corners);
                if (target == preview.sideTexture) DrawSides();
                else if (target == preview.previewTexture) Area(corners, UiColor, 0.05f, "床の文字・暗転（UI）");
                else if (target == preview.wallTexture) Area(corners, UiColor, 0.05f, "正面の文字・暗転（UI）");
            }
        }
        DrawInputArea(preview);
    }

    // 入力（URG）の検出面。センサーがなければ高さ0の水平面。
    static Plane GroundPlane(InputPreview preview)
    {
        var sensing = preview.input != null ? preview.input.urgSensing : null;
        if (sensing == null) return new Plane(Vector3.up, Vector3.zero);
        return new Plane(sensing.transform.up, sensing.transform.TransformPoint(sensing.sensingArea.center));
    }

    // 画面の四隅へ向かうカメラの向き。u, v は 0〜1、順は左下・左上・右上・右下。
    static Vector3 Direction(Camera camera, RenderTexture target, float u, float v)
    {
        float tan = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float aspect = (float)target.width / target.height;
        Transform t = camera.transform;
        return t.forward + t.right * ((u * 2 - 1) * tan * aspect) + t.up * ((v * 2 - 1) * tan);
    }

    static readonly Vector2[] ViewCorners = { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

    static void DrawFloor(Camera camera, RenderTexture target, Plane ground)
    {
        for (int i = 0; i < 4; i++)
        {
            var ray = new Ray(camera.transform.position, Direction(camera, target, ViewCorners[i].x, ViewCorners[i].y));
            float distance;
            if (!ground.Raycast(ray, out distance)) return;
            corners[i] = ray.GetPoint(distance);
        }
        // 入力の高さより低い物（川底など）は枠の外まで、高い物は枠より内側だけが映る。四角錐で見分ける。
        Edges(camera.transform.position, FloorColor);
        Area(corners, FloorColor, 0.06f, "床（池）に映る範囲");
    }

    // 正面は奥行きがあるため、カメラから、映る範囲の下端が床の高さに届く距離までを四角錐で描く。
    static void DrawWall(Camera camera, RenderTexture target, Plane ground)
    {
        Vector3 origin = camera.transform.position;
        var bottom = new Ray(origin, Direction(camera, target, 0.5f, 0));
        float along;
        float depth = ground.Raycast(bottom, out along) ? along * Vector3.Dot(bottom.direction, camera.transform.forward) : FallbackDistance;
        for (int i = 0; i < 4; i++)
            corners[i] = origin + Direction(camera, target, ViewCorners[i].x, ViewCorners[i].y) * depth;
        Edges(origin, WallColor);
        Area(corners, WallColor, 0.05f, "正面（滝）に映る範囲");
    }

    // SideTex は左半分が左、右半分が右の画面。
    static void DrawSides()
    {
        Vector3 bottomMid = (corners[0] + corners[3]) * 0.5f, topMid = (corners[1] + corners[2]) * 0.5f;
        half[0] = corners[0]; half[1] = corners[1]; half[2] = topMid; half[3] = bottomMid;
        Area(half, SideColor, 0.05f, "左の画面（UI）");
        half[0] = bottomMid; half[1] = topMid; half[2] = corners[2]; half[3] = corners[3];
        Area(half, SideColor, 0.05f, "右の画面（UI）");
    }

    static void DrawInputArea(InputPreview preview)
    {
        var sensing = preview.input != null ? preview.input.urgSensing : null;
        if (sensing == null) return;
        Bounds area = sensing.sensingArea;
        Transform t = sensing.transform;
        Vector3 c = area.center, e = area.extents;
        var points = new[]
        {
            t.TransformPoint(c + new Vector3(-e.x, 0, -e.z)), t.TransformPoint(c + new Vector3(-e.x, 0, e.z)),
            t.TransformPoint(c + new Vector3(e.x, 0, e.z)), t.TransformPoint(c + new Vector3(e.x, 0, -e.z)),
        };
        using (new Handles.DrawingScope(FloorColor))
            Handles.DrawDottedLines(new[] { points[0], points[1], points[1], points[2], points[2], points[3], points[3], points[0] }, 4);
        Label(points[0], "入力できる範囲（センサー）", FloorColor);
    }

    // カメラから枠の四隅へ線を引く。Handles の色は描画する色に掛かるため、範囲を区切って戻す。
    static void Edges(Vector3 origin, Color color)
    {
        using (new Handles.DrawingScope(new Color(color.r, color.g, color.b, 0.5f)))
            for (int i = 0; i < 4; i++) Handles.DrawLine(origin, corners[i]);
    }

    // 面を薄く塗って縁取り、左上の角に名前を付ける。
    static void Area(Vector3[] quad, Color color, float fill, string name)
    {
        using (new Handles.DrawingScope(Color.white))
            Handles.DrawSolidRectangleWithOutline(quad, new Color(color.r, color.g, color.b, fill), color);
        Label(quad[1], name, color);
    }

    static void Label(Vector3 position, string text, Color color)
    {
        if (labelStyle == null || labelBackground == null)
        {
            labelBackground = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            labelBackground.SetPixel(0, 0, new Color(0.02f, 0.03f, 0.04f, 0.75f));
            labelBackground.Apply();
            labelStyle = new GUIStyle(EditorStyles.boldLabel) { padding = new RectOffset(6, 6, 2, 2) };
            labelStyle.normal.background = labelBackground;
        }
        labelStyle.normal.textColor = color;
        Handles.Label(position, text, labelStyle);
    }
}
