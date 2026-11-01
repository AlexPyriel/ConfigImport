# GDF Config Import

Editor-time pipeline that imports CSV or Google Sheets data into `com.gdf.config-repository` targets.

Package name: `com.gdf.config-import`
Assembly name: `GDF.ConfigImport.Editor`

## Features

- `ConfigImporter` ScriptableObject with a pluggable data source and a list of `(sheetName -> target repository)` entries. One asset covers one spreadsheet.
- Writes into any `ConfigRepository<TKey, TConfig>` through the non-generic `IConfigImportTarget` bridge; no reflection into the base type's generic arguments.
- Pluggable data sources: `GoogleSheetsDataSource` (public, `gviz/csv`), `GoogleSheetsOAuthDataSource` (OAuth2, private spreadsheets), `LocalCsvDataSource` (`TextAsset`).
- Pluggable `ConfigValueConverter` ScriptableObjects for non-primitive types. Ships with `ColorConverter`, `SpriteConverter`, `AssetReferenceConverter`, `ListConverter`.
- RFC 4180 CSV parser with quoted fields, embedded commas and multi-line cells.
- Inspector buttons: `Reload` and `Open in browser`.
- Menu item `Tools/GDF/Reload All Configs` and a static CLI entry point for `unity -batchmode -executeMethod`.
- Structured `ImportResult` with per-row error reporting.

## Requirements

- Unity `6000.0+`
- `com.gdf.config-repository >= 1.1.0`
- `com.gdf.addressable-asset-loader >= 1.0.0` (only for `AssetReferenceConverter`)

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.config-import": "https://github.com/AlexPyriel/ConfigImport.git#v1.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/ConfigImport.git#v1.0.0`

## Usage

### 1. Create a `ConfigImporter` asset

`Create -> GDF -> Config Importer`. Assign a `Data Source` and add one `Sheet Entry` per repository you want to populate:

- `Sheet Name` — a specific tab inside a spreadsheet, or a key the data source uses to locate the stream.
- `Target` — any `ConfigRepository<TKey, TConfig>` asset in your project.
- `Key Column Name` — the CSV column that becomes `TKey`. Defaults to `Id`.

Click `Reload` in the inspector to pull data; click `Open in browser` to inspect the source document.

### 2. Choose a data source

- `GoogleSheetsDataSource` — public spreadsheets (shared with `Anyone with the link - Viewer`). Set the spreadsheet ID.
- `GoogleSheetsOAuthDataSource` — private spreadsheets. Set the OAuth2 client credentials; the editor opens a browser-based consent screen the first time, then persists tokens in `EditorPrefs`.
- `LocalCsvDataSource` — any `TextAsset` in the project (e.g. a `.csv` under `Assets/`). Use the sheet name to pick between multiple assets, or leave one entry blank.

### 3. Add value converters for complex types

If a column holds a `Color`, `Sprite`, `AssetReference<T>` or a `List<T>`, add a corresponding `ConfigValueConverter` asset (via `Create -> GDF -> Config Converter -> ...`) and reference it from the importer. Converters are matched to field types automatically.

### 4. CI / headless rebuild

Call the static entry point from a build pipeline:

```bash
unity -batchmode -quit -executeMethod GDF.ConfigImport.Editor.Cli.ReloadAll
```

The call exits with a non-zero code if any importer fails.

## Assembly Definition Notes

`GDF.ConfigImport.Editor` is an editor-only assembly. There is no runtime assembly — importers write ScriptableObject data on disk; the game reads it through `com.gdf.config-repository` at runtime as usual.
