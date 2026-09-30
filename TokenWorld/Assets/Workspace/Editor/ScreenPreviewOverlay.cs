using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene ビューの隅に、展示の4画面の映像を小さく並べる。Play 前でも、物を動かすと映り方が更新される。
// Play 中は Game ビューが描いた映像を使い、Game ビューが隠れて描かれないときはここで描く。
// InputPreview のあるシーン（制作シーン）を開くと表示し、それ以外のシーンでは隠す。
[Overlay(typeof(SceneView), Id, "4画面プレビュー", true)]
public class ScreenPreviewOverlay : IMGUIOverlay
{
    const string Id = "tokenworld-screen-preview";
    const string LargeKey = "TokenWorld.ScreenPreviewOverlay.Large";
    const float Gap = 4;
    const float LabelHeight = 14;
    // 編集中にカメラを描き直す間隔（秒）。
    const double RenderInterval = 0.1;
    static readonly Rect WholeTexture = new Rect(0, 0, 1, 1);
    static readonly Rect LeftHalf = new Rect(0, 0, 0.5f, 1);
    static readonly Rect RightHalf = new Rect(0.5f, 0, 0.5f, 1);

    static bool renderingSelf;
    InputPreview preview;
    double nextRender, nextSearch, lastOtherRender;
    bool repaintQueued;

    public override void OnCreated()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        Camera.onPostRender += OnCameraRendered;
    }

    public override void OnWillBeDestroyed()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        Camera.onPostRender -= OnCameraRendered;
        EditorApplication.update -= RepaintWhenDue;
    }

    // Game ビューなど、この表示の外で床のカメラが描かれた時刻を覚える。
    void OnCameraRendered(Camera camera)
    {
        if (!renderingSelf && preview != null && camera.targetTexture != null && camera.targetTexture == preview.previewTexture)
            lastOtherRender = EditorApplication.timeSinceStartup;
    }

    void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        preview = null;
        nextSearch = 0;
        displayed = FindPreview() != null;
    }

    InputPreview FindPreview()
    {
        if (preview == null && EditorApplication.timeSinceStartup >= nextSearch)
        {
            nextSearch = EditorApplication.timeSinceStartup + 1;
            preview = Object.FindObjectOfType<InputPreview>();
        }
        return preview;
    }

    public override void OnGUI()
    {
        InputPreview current = FindPreview();
        if (current == null)
        {
            GUILayout.Label("制作シーン（InputPreview のあるシーン）で表示します", EditorStyles.miniLabel);
            return;
        }

        bool large = EditorPrefs.GetBool(LargeKey, false);
        GUILayout.BeginHorizontal();
        GUILayout.Label(Application.isPlaying ? "Play 中" : "編集中（Play 前）", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(large ? "小さく" : "大きく", EditorStyles.miniButton)) EditorPrefs.SetBool(LargeKey, !large);
        GUILayout.EndHorizontal();

        if (Event.current.type == EventType.Repaint) UpdateScreens(current);

        float width = large ? 640 : 380;
        RenderTexture floor = current.previewTexture, wall = current.wallTexture, side = current.sideTexture;
        float floorWidth = width * 0.55f;
        if (wall != null && side != null)
        {
            // 上段に［左｜正面｜右］、下段に床。Game ビューと同じ並び。
            float sideAspect = side.width * 0.5f / side.height, wallAspect = (float)wall.width / wall.height;
            float height = (width - Gap * 2) / (sideAspect * 2 + wallAspect);
            Rect row = GUILayoutUtility.GetRect(width, LabelHeight + height);
            var left = new Rect(row.x, row.y + LabelHeight, height * sideAspect, height);
            var center = new Rect(left.xMax + Gap, left.y, height * wallAspect, height);
            var right = new Rect(center.xMax + Gap, left.y, height * sideAspect, height);
            Tile(left, side, LeftHalf, "左");
            Tile(center, wall, WholeTexture, "正面（滝）");
            Tile(right, side, RightHalf, "右");
            floorWidth = Mathf.Max(center.width, floorWidth);
            GUILayoutUtility.GetRect(width, Gap);
        }
        float floorHeight = floor != null ? floorWidth * floor.height / floor.width : floorWidth * 9 / 16;
        Rect bottom = GUILayoutUtility.GetRect(width, LabelHeight + floorHeight);
        Tile(new Rect(bottom.x + (width - floorWidth) * 0.5f, bottom.y + LabelHeight, floorWidth, floorHeight), floor, WholeTexture, "床（池）  入力はここ");
    }

    // 編集中はカメラが毎フレーム描かれないため、ここで描き直す。間隔より短い変更も、間隔が過ぎたら描き直して反映する。
    void UpdateScreens(InputPreview current)
    {
        double now = EditorApplication.timeSinceStartup;
        bool playing = Application.isPlaying;
        if (playing && now - lastOtherRender < RenderInterval * 2) return;
        if (now >= nextRender)
        {
            nextRender = now + RenderInterval;
            renderingSelf = true;
            try { RenderScreens(current); }
            finally { renderingSelf = false; }
            // Play 中に自分で描くときは、次の間隔でも描き続ける。
            if (!playing) return;
        }
        if (!repaintQueued)
        {
            repaintQueued = true;
            EditorApplication.update += RepaintWhenDue;
        }
    }

    void RepaintWhenDue()
    {
        if (EditorApplication.timeSinceStartup < nextRender) return;
        EditorApplication.update -= RepaintWhenDue;
        repaintQueued = false;
        if (containerWindow != null) containerWindow.Repaint();
    }

    static void Tile(Rect rect, Texture texture, Rect texCoords, string name)
    {
        GUI.Label(new Rect(rect.x, rect.y - LabelHeight, rect.width + 60, LabelHeight), name, EditorStyles.miniLabel);
        EditorGUI.DrawRect(rect, Color.black);
        if (texture != null) GUI.DrawTextureWithTexCoords(rect, texture, texCoords, false);
    }

    // 3つの RenderTexture に描くカメラを、重ねる順（depth の小さい順）に描く。
    static void RenderScreens(InputPreview preview)
    {
        Camera[] cameras = Camera.allCameras;
        System.Array.Sort(cameras, (a, b) => a.depth.CompareTo(b.depth));
        foreach (var camera in cameras)
        {
            RenderTexture target = camera.targetTexture;
            if (target != null && (target == preview.previewTexture || target == preview.wallTexture || target == preview.sideTexture))
                camera.Render();
        }
    }
}
