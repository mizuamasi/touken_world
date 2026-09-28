using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorkspaceSceneTools
{
    public const string ScenePath = "Assets/Workspace/Scenes/Workspace.unity";

    [MenuItem("TokenWorld/Open development scene")]
    public static void OpenWorkspace()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(ScenePath);
    }
}