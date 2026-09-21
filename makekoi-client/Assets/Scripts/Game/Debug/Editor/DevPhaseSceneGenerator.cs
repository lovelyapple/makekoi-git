using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Game.unity を元に、フェーズごとの開発用シーン（Assets/Scenes/Dev/Dev_xxx.unity）を生成する。
/// カメラ / Canvas / ResourceContainer / EventSystem など共通セットアップはそのまま引き継ぎ、
/// 対象フェーズの UIGameContentHolder だけを残し、他のページを取り除く。
/// Game.unity を編集したら再生成すれば追従する。
/// </summary>
public static class DevPhaseSceneGenerator
{
    private const string SourceScenePath = "Assets/Scenes/Game.unity";
    private const string DevSceneDirectory = "Assets/Scenes/Dev";

    [MenuItem("Makekoi/Dev Scene/Generate All Phase Scenes")]
    public static void GenerateAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        GenerateAllWithoutPrompt();
    }

    /// <summary>batchmode（CLI）用。保存確認ダイアログを出さずに生成する。</summary>
    public static void GenerateAllWithoutPrompt()
    {
        if (!Directory.Exists(DevSceneDirectory))
        {
            Directory.CreateDirectory(DevSceneDirectory);
            AssetDatabase.Refresh();
        }

        foreach (GamePhaseContentType phase in System.Enum.GetValues(typeof(GamePhaseContentType)))
        {
            Generate(phase);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        Debug.Log($"[DevPhaseSceneGenerator] {DevSceneDirectory} に開発用シーンを生成しました");
    }

    [MenuItem("Makekoi/Dev Scene/Open/Title")]
    private static void OpenTitle() => OpenDevScene(GamePhaseContentType.Title);

    [MenuItem("Makekoi/Dev Scene/Open/CharaSelect")]
    private static void OpenCharaSelect() => OpenDevScene(GamePhaseContentType.CharaSelect);

    [MenuItem("Makekoi/Dev Scene/Open/Shopping")]
    private static void OpenShopping() => OpenDevScene(GamePhaseContentType.Shopping);

    [MenuItem("Makekoi/Dev Scene/Open/Result")]
    private static void OpenResult() => OpenDevScene(GamePhaseContentType.Result);

    public static string GetDevScenePath(GamePhaseContentType phase) => $"{DevSceneDirectory}/Dev_{phase}.unity";

    private static void OpenDevScene(GamePhaseContentType phase)
    {
        var path = GetDevScenePath(phase);
        if (!File.Exists(path))
        {
            Debug.LogError($"[DevPhaseSceneGenerator] {path} がありません。Makekoi/Dev Scene/Generate All Phase Scenes を実行してください");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static void Generate(GamePhaseContentType phase)
    {
        var devScenePath = GetDevScenePath(phase);

        AssetDatabase.DeleteAsset(devScenePath);
        if (!AssetDatabase.CopyAsset(SourceScenePath, devScenePath))
        {
            Debug.LogError($"[DevPhaseSceneGenerator] {SourceScenePath} -> {devScenePath} のコピーに失敗しました");
            return;
        }

        var scene = EditorSceneManager.OpenScene(devScenePath, OpenSceneMode.Single);

        var targetHolder = FindHolder(phase);
        if (targetHolder == null)
        {
            Debug.LogError($"[DevPhaseSceneGenerator] {phase} の UIGameContentHolder が {SourceScenePath} に見つかりません");
            return;
        }

        // ScrollRect が実行時に Viewport のサイズを決めているため、ScrollView 階層はそのまま残す。
        // 対象以外のホルダー（ページ）だけを取り除き、参照リストを1件に詰め替える。
        var rootController = Object.FindFirstObjectByType<UIRootController>(FindObjectsInactive.Include);
        if (rootController == null)
        {
            Debug.LogError($"[DevPhaseSceneGenerator] UIRootController が {SourceScenePath} に見つかりません");
            return;
        }

        var serializedRootController = new SerializedObject(rootController);
        var scrollView = serializedRootController.FindProperty("_contentScrollView").objectReferenceValue;

        foreach (var holder in Object.FindObjectsByType<UIGameContentHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (holder != targetHolder)
            {
                Object.DestroyImmediate(holder.gameObject);
            }
        }

        if (scrollView != null)
        {
            // 手動セルモードのページ一覧。1件になればドラッグも GoToIndex も無効化される。
            SetSingleElementList(new SerializedObject(scrollView), "_inspectorCells", targetHolder.gameObject);
        }

        // 1ページでは GoToIndex が index 0 にクランプされ、ページ変更通知も飛ばないため
        // UIRootController は何もしない。フッター同期は DevPhaseSceneBootstrap が代行する。
        Object.DestroyImmediate(rootController);

        var bootstrapObject = new GameObject("DevPhaseSceneBootstrap");
        var bootstrap = bootstrapObject.AddComponent<DevPhaseSceneBootstrap>();
        var serializedBootstrap = new SerializedObject(bootstrap);
        serializedBootstrap.FindProperty("_targetHolder").objectReferenceValue = targetHolder;
        serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, devScenePath);
    }

    private static void SetSingleElementList(SerializedObject serializedObject, string propertyName, Object element)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError($"[DevPhaseSceneGenerator] {serializedObject.targetObject.GetType().Name}.{propertyName} が見つかりません");
            return;
        }

        property.arraySize = 1;
        property.GetArrayElementAtIndex(0).objectReferenceValue = element;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static UIGameContentHolder FindHolder(GamePhaseContentType phase)
    {
        var holders = Object.FindObjectsByType<UIGameContentHolder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var holder in holders)
        {
            if (holder.ButtonType == phase)
            {
                return holder;
            }
        }

        return null;
    }
}
