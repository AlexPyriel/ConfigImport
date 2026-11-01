using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Project-wide reload: scans every <see cref="ConfigImporter"/> asset through
    /// <see cref="AssetDatabase.FindAssets"/> and runs <c>Reload</c> on each one in order. Useful
    /// as the "one button to rebuild the whole balance" for designers and from CI.
    /// </summary>
    public static class ReloadAllConfigsMenu
    {
        private const string MENU_PATH = "Tools/GDF/Reload All Configs";

        [MenuItem(MENU_PATH)]
        private static void ReloadAll()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ConfigImporter)}");
            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning("[ConfigImport] No ConfigImporter assets found in the project.");
                return;
            }

            int totalImported = 0;
            int totalSkipped = 0;
            int totalErrors = 0;
            int failedImporters = 0;

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
                    failedImporters++;
                }

                if (result.Errors != null)
                {
                    for (int e = 0; e < result.Errors.Count; e++)
                    {
                        Debug.LogError($"[ConfigImport] '{importer.name}': {result.Errors[e]}");
                    }
                }
            }

            string summary = $"[ConfigImport] Reload All — importers={guids.Length}, failed={failedImporters}, imported={totalImported}, skipped={totalSkipped}, errors={totalErrors}.";
            if (failedImporters > 0)
            {
                Debug.LogError(summary);
            }
            else
            {
                Debug.Log(summary);
            }
        }
    }
}
