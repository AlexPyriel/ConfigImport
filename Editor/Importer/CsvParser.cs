using System.Collections.Generic;
using System.Text;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Minimal RFC 4180 CSV parser. Supports quoted fields, embedded commas, embedded line breaks
    /// (CR/LF/CRLF) and escaped quotes (<c>""</c>). Returns an in-memory table where every row is a
    /// list of raw string cells with the exact length of the first (header) row.
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// Parses the input into rows. Empty trailing lines are discarded. Rows shorter than the header
        /// are right-padded with empty strings; rows longer than the header are truncated.
        /// </summary>
        /// <param name="input">CSV payload. May be <see langword="null"/>, which produces an empty result.</param>
        /// <returns>Parsed table. The first entry is the header row. Empty when <paramref name="input"/> is null/empty.</returns>
        public static List<List<string>> Parse(string input)
        {
            List<List<string>> rows = new List<List<string>>();
            if (string.IsNullOrEmpty(input))
            {
                return rows;
            }

            List<string> current = new List<string>();
            StringBuilder cell = new StringBuilder();
            bool inQuotes = false;
            int i = 0;

            while (i < input.Length)
            {
                char c = input[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Escaped quote inside a quoted field.
                        if (i + 1 < input.Length && input[i + 1] == '"')
                        {
                            cell.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = false;
                        i++;
                        continue;
                    }

                    cell.Append(c);
                    i++;
                    continue;
                }

                if (c == '"')
                {
                    inQuotes = true;
                    i++;
                    continue;
                }

                if (c == ',')
                {
                    current.Add(cell.ToString());
                    cell.Clear();
                    i++;
                    continue;
                }

                if (c == '\r' || c == '\n')
                {
                    current.Add(cell.ToString());
                    cell.Clear();
                    rows.Add(current);
                    current = new List<string>();

                    // Swallow CRLF as a single newline.
                    if (c == '\r' && i + 1 < input.Length && input[i + 1] == '\n')
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    continue;
                }

                cell.Append(c);
                i++;
            }

            // Flush trailing cell/row (file without a terminating newline).
            if (cell.Length > 0 || current.Count > 0)
            {
                current.Add(cell.ToString());
                rows.Add(current);
            }

            // Drop fully empty trailing lines (one empty cell "").
            while (rows.Count > 0 && rows[rows.Count - 1].Count == 1 && rows[rows.Count - 1][0].Length == 0)
            {
                rows.RemoveAt(rows.Count - 1);
            }

            if (rows.Count == 0)
            {
                return rows;
            }

            // Normalize row widths against the header.
            int width = rows[0].Count;
            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                while (row.Count < width)
                {
                    row.Add(string.Empty);
                }

                if (row.Count > width)
                {
                    row.RemoveRange(width, row.Count - width);
                }
            }

            return rows;
        }
    }
}
