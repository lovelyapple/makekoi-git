using System.Linq;
using UnityEditor;
using UnityEngine;

public class SoundsContainerEditorWindow : EditorWindow
{
    private SoundsContainer _container;
    private SerializedObject _serializedObject;
    private SerializedProperty _seResourcesProp;
    private SerializedProperty _bgmResourcesProp;

    private Vector2 _scrollPos;

    [MenuItem("Makekoi/Tools/Sounds Container")]
    public static void Open()
    {
        var window = GetWindow<SoundsContainerEditorWindow>("Sounds Container");
        window.LoadAsset();
        window.Show();
    }

    private void LoadAsset()
    {
        _container = AssetDatabase.LoadAssetAtPath<SoundsContainer>(
            "Assets/Resources/Sounds/SoundsContainer.asset");
        if (_container == null) return;

        _serializedObject = new SerializedObject(_container);
        _seResourcesProp = _serializedObject.FindProperty("SeResources");
        _bgmResourcesProp = _serializedObject.FindProperty("BgmResources");
    }

    private void OnEnable()
    {
        LoadAsset();
    }

    private void OnGUI()
    {
        if (_container == null || _serializedObject == null)
        {
            EditorGUILayout.HelpBox("SoundsContainer.asset が見つかりません\nAssets/Resources/Sounds/SoundsContainer.asset", MessageType.Warning);
            if (GUILayout.Button("作成する"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources/Sounds");
                var asset = ScriptableObject.CreateInstance<SoundsContainer>();
                asset.SeResources = System.Enum.GetNames(typeof(CommonSeType))
                    .Select(name => new SoundResource { SoundType = name })
                    .ToList();
                asset.BgmResources = new System.Collections.Generic.List<SoundResource>();
                AssetDatabase.CreateAsset(asset, "Assets/Resources/Sounds/SoundsContainer.asset");
                AssetDatabase.SaveAssets();
                LoadAsset();
            }
            if (GUILayout.Button("再読み込み")) LoadAsset();
            return;
        }

        _serializedObject.Update();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        EditorGUILayout.LabelField("Sounds Container", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_seResourcesProp, new GUIContent("SE Resources"), true);
        if (GUILayout.Button("Remove Empty")) _container.SeResources.RemoveAll(x => string.IsNullOrWhiteSpace(x.SoundType));
        if (GUILayout.Button("Sort")) _container.SeResources = _container.SeResources.OrderBy(x => x.SoundType).ToList();
        if (GUILayout.Button("Output Enum"))
        {
            var list = _container.SeResources.Select(x => x.SoundType).ToList();
            EnumFileGenerator.GenerateEnumFile(list, "CommonSeType", "Assets/Scripts/System/Generated/CommonSeType.cs");
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(_bgmResourcesProp, new GUIContent("BGM Resources"), true);
        if (GUILayout.Button("Remove Empty")) _container.BgmResources.RemoveAll(x => string.IsNullOrWhiteSpace(x.SoundType));
        if (GUILayout.Button("Sort")) _container.BgmResources = _container.BgmResources.OrderBy(x => x.SoundType).ToList();

        EditorGUILayout.EndScrollView();

        _serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(_container);
    }
}
