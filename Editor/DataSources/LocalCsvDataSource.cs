using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Data source backed by a <see cref="TextAsset"/> stored in the project. Useful for CSV files
    /// baked into the repository, for tests, and as a fallback when a Google Sheet is unavailable.
    /// A single asset is reused for every sheet entry; the sheet name is informational only.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Data Source/Local CSV", fileName = "LocalCsvDataSource", order = 0)]
    public sealed class LocalCsvDataSource : ScriptableObject, IDataSource
    {
        [SerializeField]
        [Tooltip("TextAsset holding the CSV payload. Imported for every sheet entry of this importer.")]
        private TextAsset _asset;

        /// <inheritdoc />
        public string Describe()
        {
            if (_asset == null)
            {
                return "LocalCsvDataSource(<unassigned>)";
            }

#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(_asset);
            return $"LocalCsvDataSource({path})";
#else
            return $"LocalCsvDataSource({_asset.name})";
#endif
        }

        /// <inheritdoc />
        public string GetBrowseUrl()
        {
            return null;
        }

        /// <inheritdoc />
        public string Fetch(string sheetName, out string error)
        {
            if (_asset == null)
            {
                error = "LocalCsvDataSource has no asset assigned.";
                return null;
            }

            error = null;
            return _asset.text ?? string.Empty;
        }
    }
}
