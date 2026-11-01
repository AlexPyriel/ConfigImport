using System.Collections.Generic;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Structured outcome of a single importer run. Returned by every reload entry point so callers
    /// (inspector, menu item, batch-mode CLI) can distinguish between a clean run, partial import and
    /// a hard failure.
    /// </summary>
    public struct ImportResult
    {
        /// <summary>Whether every requested sheet was fetched and parsed without a hard error.</summary>
        public bool Success;

        /// <summary>Number of rows successfully written into target repositories across all sheets.</summary>
        public int RowsImported;

        /// <summary>Number of rows skipped (missing key column, unknown column, converter failure).</summary>
        public int RowsSkipped;

        /// <summary>Human-readable error messages collected during the run. Empty on a fully successful import.</summary>
        public List<string> Errors;

        /// <summary>Returns an empty success result with an initialized error list.</summary>
        public static ImportResult Empty()
        {
            return new ImportResult
            {
                Success = true,
                RowsImported = 0,
                RowsSkipped = 0,
                Errors = new List<string>(),
            };
        }
    }
}
