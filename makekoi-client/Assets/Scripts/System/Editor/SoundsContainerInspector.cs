using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SoundsContainer))]
public class SoundsContainerEditor : Editor
{
    private SerializedProperty _seResourcesProp;
    private SerializedProperty _bgmResourcesProp;

    private void OnEnable()
    {
        _seResourcesProp = serializedObject.FindProperty("SeResources");
        _bgmResourcesProp = serializedObject.FindProperty("BgmResources");
    }

    public override void OnInspectorGUI()
    {
        var container = (SoundsContainer)target;
        serializedObject.Update();

        EditorGUILayout.LabelField("Sounds Container", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_seResourcesProp, new GUIContent("SE Resources"), includeChildren: true);
        if (GUILayout.Button("Remove Empty"))
            container.SeResources.RemoveAll(x => string.IsNullOrWhiteSpace(x.SoundType));
        if (GUILayout.Button("Sort"))
            container.SeResources = container.SeResources.OrderBy(x => x.SoundType).ToList();
        if (GUILayout.Button("Output Enum"))
        {
            var stringList = container.SeResources.Select(x => x.SoundType).ToList();
            EnumFileGenerator.GenerateEnumFile(stringList, "CommonSeType", "Assets/Scripts/System/Generated/CommonSeType.cs");
        }

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(_bgmResourcesProp, new GUIContent("BGM Resources"), includeChildren: true);
        if (GUILayout.Button("Remove Empty"))
            container.BgmResources.RemoveAll(x => string.IsNullOrWhiteSpace(x.SoundType));
        if (GUILayout.Button("Sort"))
            container.BgmResources = container.BgmResources.OrderBy(x => x.SoundType).ToList();

        serializedObject.ApplyModifiedProperties();
    }
}
