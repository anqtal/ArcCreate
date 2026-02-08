using ArcCreate.Utility;
using UnityEngine;

namespace ArcCreate.EditorScripts
{
    public class StorageEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Import test package"))
            {
                var importFrom = Shell.OpenFileDialog("ArcCreate Packaage", new[] { "arcpkg" }, "Import test package");
            }
        }
    }
}