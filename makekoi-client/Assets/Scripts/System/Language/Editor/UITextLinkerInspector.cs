using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UITextLinker))]
public class UITextLinkerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Update string table"))
        {
            TempStringDataExporter.BuildString();
        }
    }
}
