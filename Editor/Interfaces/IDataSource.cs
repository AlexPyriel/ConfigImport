namespace GDF.ConfigImport
{
    /// <summary>
    /// Abstract source of CSV data for the importer. A data source resolves a logical sheet name
    /// (identifier) into a CSV string; the importer parses it and writes rows into the target repository.
    /// Implementations may hit the network (Google Sheets), the file system (local CSV) or any other store.
    /// </summary>
    public interface IDataSource
    {
        /// <summary>
        /// Human-readable identifier of the backing document shown in logs and error messages.
        /// For a Google spreadsheet this is the URL or the spreadsheet id; for a local asset — its path.
        /// </summary>
        string Describe();

        /// <summary>
        /// Returns a URL that opens the data source in the default system browser, or <see langword="null"/>
        /// when the data source has no browseable representation (e.g. a local TextAsset). The inspector
        /// uses this to render an "Open in browser" button.
        /// </summary>
        string GetBrowseUrl();

        /// <summary>
        /// Downloads or reads the CSV content for a specific sheet.
        /// </summary>
        /// <param name="sheetName">Sheet identifier passed from <c>ConfigImporter.SheetEntry.SheetName</c>; may be <see langword="null"/> or empty when the source has a single stream.</param>
        /// <param name="error">Human-readable error on failure; <see langword="null"/> on success.</param>
        /// <returns>CSV payload on success; <see langword="null"/> on failure (in which case <paramref name="error"/> is set).</returns>
        string Fetch(string sheetName, out string error);
    }
}
