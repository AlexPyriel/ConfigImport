using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport.Editor
{
    /// <summary>
    /// Static entry points for batchmode. Call with
    /// <c>unity -batchmode -quit -executeMethod GDF.ConfigImport.Editor.Cli.ReloadAll</c>
    /// from a CI pipeline. The process exits with code 1 when any importer reports a failure.
    /// </summary>
    public static class Cli
    {
        /// <summary>
        /// Reloads every <see cref="ConfigImporter"/> asset in the project. Exits the editor process
        /// with a non-zero code on failure so CI can fail the step.
        /// </summary>
        public static void ReloadAll()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ConfigImporter)}");
            int totalImported = 0;
            int totalSkipped = 0;
            int totalErrors = 0;
            int failed = 0;

            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[ConfigImport.Cli] No ConfigImporter assets found.");
                EditorApplication.Exit(0);
                return;
            }

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                ConfigImporter importer = AssetDatabase.LoadAssetAtPath<ConfigImporter>(path);
                if (importer == null)
                {
                    continue;
                }

                ImportResult result = importer.Reload();
                totalImported += result.RowsImported;
                totalSkipped += result.RowsSkipped;
                totalErrors += result.Errors == null ? 0 : result.Errors.Count;

                if (!result.Success)
                {
                    failed++;
                }

                if (result.Errors != null)
                {
                    for (int e = 0; e < result.Errors.Count; e++)
                    {
                        Debug.LogError($"[ConfigImport.Cli] '{importer.name}': {result.Errors[e]}");
                    }
                }
            }

            string summary = $"[ConfigImport.Cli] importers={guids.Length}, failed={failed}, imported={totalImported}, skipped={totalSkipped}, errors={totalErrors}.";
            if (failed > 0)
            {
                Debug.LogError(summary);
                EditorApplication.Exit(1);
            }
            else
            {
                Debug.Log(summary);
                EditorApplication.Exit(0);
            }
        }
    }
}
