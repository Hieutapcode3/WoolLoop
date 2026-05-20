using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class GameEditor
{
    private const string sceneFolder = "Assets/WoolLoop/Scenes/";
    [MenuItem("GameEditor/Scenes/LoadingScene")]
    static void OpenLoadingScene()
    {
        OpenScene(sceneFolder + "LoadingScene.unity");
    }
    [MenuItem("GameEditor/Scenes/GameScene")]
    static void OpenMainScene()
    {
        OpenScene(sceneFolder + "GameScene.unity");
    }
    [MenuItem("GameEditor/Scenes/TestScene")]
    static void OpenTestScene()
    {
        OpenScene(sceneFolder + "TestScene.unity");
    }

    static void OpenScene(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path);
        }
    }
}