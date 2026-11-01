# Changelog

## [1.0.0] - 2026-11-01

Initial public release.

- `ConfigImporter` ScriptableObject: holds a data source + a list of `(sheetName, target, keyColumnName)` entries. One asset = one spreadsheet / data set, N targets.
- Writes directly into any `ConfigRepository<TKey, TConfig>` through the `IConfigImportTarget` bridge (requires `com.gdf.config-repository >= 1.1.0`).
- Pluggable `IDataSource`: `GoogleSheetsDataSource` (public `gviz/csv` endpoint), `GoogleSheetsOAuthDataSource` (OAuth2 Installed-App flow with token persistence in `EditorPrefs`), `LocalCsvDataSource` (`TextAsset` in the project).
- RFC 4180 CSV parser (quoted fields, embedded commas, multi-line cells, escaped quotes).
- Reflection-based column -> field mapping; column header must match the field name.
- Pluggable `ConfigValueConverter` ScriptableObjects for complex types. Ships with: `ColorConverter` (HEX / `R,G,B` / `R,G,B,A`), `SpriteConverter` (`AssetDatabase.FindAssets`), `AssetReferenceConverter` (Addressables by address), `ListConverter` (`;`-separated primitives).
- Inspector buttons: `Reload` (one spreadsheet) and `Open in browser` (opens the data source URL in the default browser).
- `Tools/GDF/Reload All Configs` menu runs every `ConfigImporter` asset in the project.
- Static CLI entry point `GDF.ConfigImport.Editor.Cli.ReloadAll` for `unity -batchmode -executeMethod`.
- Structured `ImportResult` (`Success`, `RowsImported`, `Skipped`, `Errors`) returned by every reload call.
- EditMode tests: CSV parser (RFC 4180 edge cases), field mapper, each default converter, and an end-to-end `LocalCsvDataSource` fixture.
