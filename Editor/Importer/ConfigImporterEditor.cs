using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Custom inspector for <see cref="ConfigImporter"/>: adds <c>Reload</c> and <c>Open in browser</c>
    /// buttons that drive the import pipeline from the editor. The data source and sheet entries are
    /// drawn through the default property field so new <see cref="IDataSource"/> implementations
    /// surface automatically.
    /// </summary>
    [CustomEditor(typeof(ConfigImporter))]
    public sealed class ConfigImporterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            ConfigImporter importer = (ConfigImporter)target;
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = importer.DataSource != null;
                if (GUILayout.Button("Reload", GUILayout.Height(28)))
                {
                    ImportResult result = importer.Reload();
                    LogResult(importer, result);
                }

                string browseUrl = importer.DataSource?.GetBrowseUrl();
                GUI.enabled = !string.IsNullOrEmpty(browseUrl);
                if (GUILayout.Button("Open in browser", GUILayout.Height(28)))
                {
                    Application.OpenURL(browseUrl);
                }

                GUI.enabled = true;
            }
        }

        private static void LogResult(ConfigImporter importer, ImportResult result)
        {
            string head = $"[ConfigImport] '{importer.name}' — imported={result.RowsImported}, skipped={result.RowsSkipped}, errors={(result.Errors == null ? 0 : result.Errors.Count)}.";
            if (result.Success && (result.Errors == null || result.Errors.Count == 0))
            {
                Debug.Log(head);
                return;
            }

            if (result.Errors != null)
            {
                for (int i = 0; i < result.Errors.Count; i++)
                {
                    Debug.LogError($"[ConfigImport] {result.Errors[i]}");
                }
            }

            if (!result.Success)
            {
                Debug.LogError(head);
            }
            else
            {
                Debug.LogWarning(head);
            }
        }
    }
}
