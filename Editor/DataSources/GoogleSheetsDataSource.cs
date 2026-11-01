using System.Net;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Data source for Google spreadsheets shared as "Anyone with the link - Viewer". Uses the public
    /// <c>gviz/csv</c> endpoint to download a single sheet without authentication. Private sheets
    /// require <see cref="GoogleSheetsOAuthDataSource"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Data Source/Google Sheets (public)", fileName = "GoogleSheetsDataSource", order = 1)]
    public sealed class GoogleSheetsDataSource : ScriptableObject, IDataSource
    {
        private const string DOWNLOAD_URL_FORMAT = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}";
        private const string OPEN_URL_FORMAT = "https://docs.google.com/spreadsheets/d/{0}";

        [SerializeField]
        [Tooltip("Google Spreadsheet ID. Pulled from the URL between /d/ and /edit (e.g. 1AbCdEf...).")]
        private string _spreadsheetId;

        /// <summary>Spreadsheet id configured in the inspector; used by the importer and the Open button.</summary>
        public string SpreadsheetId => _spreadsheetId;

        /// <inheritdoc />
        public string Describe()
        {
            return string.IsNullOrEmpty(_spreadsheetId)
                ? "GoogleSheetsDataSource(<unassigned>)"
                : $"GoogleSheetsDataSource({_spreadsheetId})";
        }

        /// <inheritdoc />
        public string GetBrowseUrl()
        {
            return string.IsNullOrEmpty(_spreadsheetId) ? null : string.Format(OPEN_URL_FORMAT, _spreadsheetId);
        }

        /// <inheritdoc />
        public string Fetch(string sheetName, out string error)
        {
            if (string.IsNullOrEmpty(_spreadsheetId))
            {
                error = "GoogleSheetsDataSource has no spreadsheet id assigned.";
                return null;
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                error = "GoogleSheetsDataSource requires a non-empty sheet name for the entry.";
                return null;
            }

            string url = string.Format(DOWNLOAD_URL_FORMAT, _spreadsheetId, UnityWebRequestEscape(sheetName));

            using (WebClientWithCookies client = new WebClientWithCookies())
            {
                try
                {
                    string body = client.DownloadString(url);
                    if (string.IsNullOrEmpty(body))
                    {
                        error = $"Empty response from {url}.";
                        return null;
                    }

                    if (body.Contains("signin/identifier") || body.Contains("ServiceLogin"))
                    {
                        error = $"Google returned a sign-in page for sheet '{sheetName}'. Make sure the spreadsheet is shared as 'Anyone with the link - Viewer', or use GoogleSheetsOAuthDataSource for private sheets.";
                        return null;
                    }

                    error = null;
                    return body;
                }
                catch (WebException ex)
                {
                    error = $"HTTP error fetching {url}: {ex.Message}";
                    return null;
                }
            }
        }

        private static string UnityWebRequestEscape(string value)
        {
            // Minimal URL escape sufficient for sheet names (handles spaces and basic punctuation).
            return UnityEngine.Networking.UnityWebRequest.EscapeURL(value).Replace("+", "%20");
        }
    }
}
