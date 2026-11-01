using System;
using System.Collections.Generic;
using System.Reflection;
using ConfigRepository;
using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Single-asset entry point for a CSV / spreadsheet import. Pairs a <see cref="IDataSource"/>
    /// (where to pull data) with a list of <see cref="SheetEntry"/> values (which sheet goes into
    /// which repository). One asset = one spreadsheet / data set; a project can hold any number of
    /// importers. Converters registered on the asset handle non-primitive field types.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Importer", fileName = "ConfigImporter", order = 0)]
    public sealed class ConfigImporter : ScriptableObject
    {
        [SerializeReference]
        [Tooltip("Pluggable data source (Google Sheets, local TextAsset, …). Assign a ScriptableObject implementing IDataSource.")]
        private ScriptableObject _dataSourceAsset;

        [SerializeField]
        [Tooltip("Sheets to import. Each entry pairs a sheet name with a target ConfigRepository.")]
        private List<SheetEntry> _sheets = new List<SheetEntry>();

        [SerializeField]
        [Tooltip("Optional value converters for non-primitive field types (Color, Sprite, AssetReference, List<T>). " +
                 "The importer picks the first converter whose TargetType matches a field.")]
        private List<ConfigValueConverter> _converters = new List<ConfigValueConverter>();

        /// <summary>Data source cast to the interface; <see langword="null"/> when unassigned or of a wrong type.</summary>
        public IDataSource DataSource => _dataSourceAsset as IDataSource;

        /// <summary>Sheet entries configured in the inspector.</summary>
        public IReadOnlyList<SheetEntry> Sheets => _sheets;

        /// <summary>Converters configured in the inspector.</summary>
        public IReadOnlyList<ConfigValueConverter> Converters => _converters;

        /// <summary>
        /// Reloads every sheet entry on this importer. Clears each target repository, parses the CSV,
        /// writes rows, and persists the resulting ScriptableObjects to disk. Returns a structured
        /// result the caller can log or forward to a CLI exit code.
        /// </summary>
        public ImportResult Reload()
        {
            ImportResult result = ImportResult.Empty();

            IDataSource source = DataSource;
            if (source == null)
            {
                result.Success = false;
                result.Errors.Add($"ConfigImporter '{name}' has no data source assigned.");
                return result;
            }

            List<IConfigValueConverter> converters = BuildConverterList();

            for (int s = 0; s < _sheets.Count; s++)
            {
                SheetEntry entry = _sheets[s];
                string label = string.IsNullOrEmpty(entry.SheetName) ? "<default>" : entry.SheetName;

                if (entry.Target == null)
                {
                    result.Errors.Add($"Sheet '{label}' has no target repository assigned.");
                    result.Success = false;
                    continue;
                }

                if (!(entry.Target is IConfigImportTarget target))
                {
                    result.Errors.Add($"Sheet '{label}' target {entry.Target.name} does not implement IConfigImportTarget. Upgrade com.gdf.config-repository to v1.1.0+.");
                    result.Success = false;
                    continue;
                }

                string csv = source.Fetch(entry.SheetName, out string fetchError);
                if (csv == null)
                {
                    result.Errors.Add($"Sheet '{label}' fetch failed: {fetchError}");
                    result.Success = false;
                    continue;
                }

                ReloadSheet(entry, target, csv, converters, ref result);
                MarkDirty((UnityEngine.Object)target);
            }

            AssetDatabase.SaveAssets();
            return result;
        }

        private void ReloadSheet(
            SheetEntry entry,
            IConfigImportTarget target,
            string csv,
            List<IConfigValueConverter> converters,
            ref ImportResult result)
        {
            List<List<string>> rows = CsvParser.Parse(csv);
            if (rows.Count == 0)
            {
                result.Errors.Add($"Sheet '{entry.SheetName}' parsed to zero rows.");
                return;
            }

            List<string> header = rows[0];
            string keyColumn = string.IsNullOrEmpty(entry.KeyColumnName) ? "Id" : entry.KeyColumnName;
            int keyColumnIndex = header.IndexOf(keyColumn);
            if (keyColumnIndex < 0)
            {
                result.Errors.Add($"Sheet '{entry.SheetName}' is missing the key column '{keyColumn}'. Headers: {string.Join(",", header)}.");
                return;
            }

            Type configType = target.ConfigType;
            List<(int ColumnIndex, FieldInfo Field)> fieldMap = ReflectionFieldMapper.BuildFieldMap(configType, header, keyColumn);

            target.Clear();

            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                string rawKey = row[keyColumnIndex];

                if (!ReflectionFieldMapper.TryParseKey(target.KeyType, rawKey, out object key, out string keyError))
                {
                    result.RowsSkipped++;
                    result.Errors.Add($"Sheet '{entry.SheetName}' row {r}: key parse failed. {keyError}");
                    continue;
                }

                object config = Activator.CreateInstance(configType);
                List<string> rowErrors = new List<string>();
                ReflectionFieldMapper.PopulateFields(config, row, fieldMap, converters, rowErrors);
                if (rowErrors.Count > 0)
                {
                    foreach (string e in rowErrors)
                    {
                        result.Errors.Add($"Sheet '{entry.SheetName}' row {r}: {e}");
                    }
                    result.Success = false;
                }

                if (target.TrySet(key, config))
                {
                    result.RowsImported++;
                }
                else
                {
                    result.RowsSkipped++;
                    result.Errors.Add($"Sheet '{entry.SheetName}' row {r}: target rejected key '{key}'.");
                }
            }
        }

        private List<IConfigValueConverter> BuildConverterList()
        {
            List<IConfigValueConverter> list = new List<IConfigValueConverter>();
            for (int i = 0; i < _converters.Count; i++)
            {
                if (_converters[i] != null)
                {
                    list.Add(_converters[i]);
                }
            }
            return list;
        }

        private static void MarkDirty(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                EditorUtility.SetDirty(obj);
            }
        }

        /// <summary>
        /// Single sheet-to-repository binding. Serialized inline on <see cref="ConfigImporter"/>.
        /// </summary>
        [Serializable]
        public struct SheetEntry
        {
            [Tooltip("Sheet identifier passed to the data source. For Google Sheets — tab name. For a local TextAsset source — informational only.")]
            public string SheetName;

            [Tooltip("Target ConfigRepository<TKey, TConfig> asset. Must implement IConfigImportTarget (available in com.gdf.config-repository >= 1.1.0).")]
            public ScriptableObject Target;

            [Tooltip("CSV column that supplies the repository key (TKey). Defaults to 'Id' when left empty.")]
            public string KeyColumnName;
        }
    }
}
