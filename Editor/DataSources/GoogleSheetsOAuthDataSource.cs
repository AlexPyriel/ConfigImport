using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Data source for private Google spreadsheets. Uses OAuth 2.0 Installed-App flow with the loopback
    /// redirect strategy recommended by Google for desktop apps. Tokens are stored in
    /// <see cref="EditorPrefs"/> keyed by the configured OAuth client id, so a studio can share a single
    /// client across the team while each developer signs in once per machine.
    /// <para>
    /// First use opens the system browser with a Google consent screen. The flow listens on
    /// <c>http://localhost:{port}</c> for the one-shot redirect, exchanges the authorization code for
    /// an access + refresh token, and persists both. Subsequent fetches refresh the access token when
    /// needed without reopening the browser.
    /// </para>
    /// </summary>
    [CreateAssetMenu(menuName = "GDF/Config Import/Data Source/Google Sheets (OAuth2)", fileName = "GoogleSheetsOAuthDataSource", order = 2)]
    public sealed class GoogleSheetsOAuthDataSource : ScriptableObject, IDataSource
    {
        private const string DOWNLOAD_URL_FORMAT = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&sheet={1}";
        private const string OPEN_URL_FORMAT = "https://docs.google.com/spreadsheets/d/{0}";
        private const string AUTH_URL = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TOKEN_URL = "https://oauth2.googleapis.com/token";
        private const string SCOPE = "https://www.googleapis.com/auth/spreadsheets.readonly";
        private const string TOKEN_PREFS_PREFIX = "GDF.ConfigImport.OAuth.";

        [SerializeField]
        [Tooltip("Google Spreadsheet ID. Pulled from the URL between /d/ and /edit.")]
        private string _spreadsheetId;

        [SerializeField]
        [Tooltip("OAuth 2.0 Client ID (Desktop Application type) from Google Cloud Console.")]
        private string _clientId;

        [SerializeField]
        [Tooltip("OAuth 2.0 Client Secret matching the client id above.")]
        private string _clientSecret;

        [SerializeField]
        [Tooltip("Localhost TCP port used to receive the OAuth redirect. Any free port works; pick one not already in use on dev machines.")]
        private int _loopbackPort = 8765;

        /// <summary>Spreadsheet id configured in the inspector.</summary>
        public string SpreadsheetId => _spreadsheetId;

        /// <inheritdoc />
        public string Describe()
        {
            return string.IsNullOrEmpty(_spreadsheetId)
                ? "GoogleSheetsOAuthDataSource(<unassigned>)"
                : $"GoogleSheetsOAuthDataSource({_spreadsheetId})";
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
                error = "GoogleSheetsOAuthDataSource has no spreadsheet id assigned.";
                return null;
            }

            if (string.IsNullOrEmpty(_clientId) || string.IsNullOrEmpty(_clientSecret))
            {
                error = "GoogleSheetsOAuthDataSource requires a client id and client secret from Google Cloud Console (Desktop Application).";
                return null;
            }

            if (string.IsNullOrEmpty(sheetName))
            {
                error = "GoogleSheetsOAuthDataSource requires a non-empty sheet name for the entry.";
                return null;
            }

            string accessToken = EnsureAccessToken(out error);
            if (accessToken == null)
            {
                return null;
            }

            string url = string.Format(
                DOWNLOAD_URL_FORMAT,
                _spreadsheetId,
                UnityEngine.Networking.UnityWebRequest.EscapeURL(sheetName).Replace("+", "%20"));

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Headers.Add("Authorization", "Bearer " + accessToken);

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException("Response stream was null.")))
                {
                    error = null;
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                error = $"HTTP error fetching {url}: {ex.Message}";
                return null;
            }
        }

        private string EnsureAccessToken(out string error)
        {
            string refreshToken = EditorPrefs.GetString(RefreshTokenKey(), string.Empty);
            if (!string.IsNullOrEmpty(refreshToken))
            {
                string refreshed = RefreshAccessToken(refreshToken, out error);
                if (refreshed != null)
                {
                    return refreshed;
                }

                // Refresh failed (user revoked, expired): fall through to a fresh consent.
                EditorPrefs.DeleteKey(RefreshTokenKey());
            }

            return StartConsentFlow(out error);
        }

        private string RefreshAccessToken(string refreshToken, out string error)
        {
            string body = string.Join("&", new[]
            {
                "client_id=" + Uri.EscapeDataString(_clientId),
                "client_secret=" + Uri.EscapeDataString(_clientSecret),
                "refresh_token=" + Uri.EscapeDataString(refreshToken),
                "grant_type=refresh_token",
            });

            string response = PostForm(TOKEN_URL, body, out error);
            if (response == null)
            {
                return null;
            }

            return ExtractJsonString(response, "access_token");
        }

        private string StartConsentFlow(out string error)
        {
            string redirectUri = $"http://localhost:{_loopbackPort}";
            string state = Guid.NewGuid().ToString("N");

            string authUrl = AUTH_URL + "?" + string.Join("&", new[]
            {
                "client_id=" + Uri.EscapeDataString(_clientId),
                "redirect_uri=" + Uri.EscapeDataString(redirectUri),
                "response_type=code",
                "scope=" + Uri.EscapeDataString(SCOPE),
                "access_type=offline",
                "prompt=consent",
                "state=" + state,
            });

            Application.OpenURL(authUrl);

            string code = WaitForLoopbackCode(_loopbackPort, state, out error);
            if (code == null)
            {
                return null;
            }

            string body = string.Join("&", new[]
            {
                "client_id=" + Uri.EscapeDataString(_clientId),
                "client_secret=" + Uri.EscapeDataString(_clientSecret),
                "code=" + Uri.EscapeDataString(code),
                "redirect_uri=" + Uri.EscapeDataString(redirectUri),
                "grant_type=authorization_code",
            });

            string response = PostForm(TOKEN_URL, body, out error);
            if (response == null)
            {
                return null;
            }

            string refreshToken = ExtractJsonString(response, "refresh_token");
            if (!string.IsNullOrEmpty(refreshToken))
            {
                EditorPrefs.SetString(RefreshTokenKey(), refreshToken);
            }

            return ExtractJsonString(response, "access_token");
        }

        private static string PostForm(string url, string body, out string error)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                byte[] payload = Encoding.UTF8.GetBytes(body);
                request.ContentLength = payload.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(payload, 0, payload.Length);
                }

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream() ?? throw new InvalidOperationException("Response stream was null.")))
                {
                    error = null;
                    return reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                string details = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream() ?? Stream.Null))
                    {
                        details = reader.ReadToEnd();
                    }
                }

                error = $"OAuth token endpoint failed: {ex.Message}. Body: {details}";
                return null;
            }
        }

        private static string WaitForLoopbackCode(int port, string expectedState, out string error)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, port);
            try
            {
                listener.Start();
            }
            catch (SocketException ex)
            {
                error = $"Could not bind loopback port {port} for the OAuth redirect: {ex.Message}";
                return null;
            }

            // Wait up to 2 minutes for the user to approve.
            IAsyncResult acceptResult = listener.BeginAcceptSocket(null, null);
            if (!acceptResult.AsyncWaitHandle.WaitOne(TimeSpan.FromMinutes(2)))
            {
                listener.Stop();
                error = "Timed out waiting for the OAuth consent redirect.";
                return null;
            }

            using (Socket socket = listener.EndAcceptSocket(acceptResult))
            {
                listener.Stop();

                byte[] buffer = new byte[4096];
                int read = socket.Receive(buffer);
                string request = Encoding.UTF8.GetString(buffer, 0, read);

                string firstLine = request.Split('\n')[0];
                int qmark = firstLine.IndexOf('?');
                int space = firstLine.IndexOf(' ', qmark + 1);
                string queryString = (qmark >= 0 && space > qmark) ? firstLine.Substring(qmark + 1, space - qmark - 1) : string.Empty;

                Dictionary<string, string> query = ParseQueryString(queryString);
                string response = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nConnection: close\r\n\r\n" +
                                  "<html><body><h2>Authorization received.</h2><p>You can close this tab and return to Unity.</p></body></html>";
                byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                socket.Send(responseBytes);

                if (query.TryGetValue("error", out string err))
                {
                    error = $"OAuth consent returned error: {err}.";
                    return null;
                }

                if (!query.TryGetValue("state", out string state) || state != expectedState)
                {
                    error = "OAuth consent redirect carried a mismatched state parameter.";
                    return null;
                }

                if (!query.TryGetValue("code", out string code) || string.IsNullOrEmpty(code))
                {
                    error = "OAuth consent redirect did not include an authorization code.";
                    return null;
                }

                error = null;
                return code;
            }
        }

        private static Dictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(query))
            {
                return result;
            }

            foreach (string pair in query.Split('&'))
            {
                int eq = pair.IndexOf('=');
                if (eq <= 0)
                {
                    continue;
                }

                string key = Uri.UnescapeDataString(pair.Substring(0, eq));
                string value = Uri.UnescapeDataString(pair.Substring(eq + 1));
                result[key] = value;
            }

            return result;
        }

        // Lightweight JSON string extractor sufficient for the OAuth response payload. Avoids a JSON
        // dependency because the editor assembly already pulls in enough transitive deps and the
        // payload shape is stable and small.
        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
            {
                return null;
            }

            string needle = "\"" + key + "\"";
            int start = json.IndexOf(needle, StringComparison.Ordinal);
            if (start < 0)
            {
                return null;
            }

            int colon = json.IndexOf(':', start + needle.Length);
            if (colon < 0)
            {
                return null;
            }

            int firstQuote = json.IndexOf('"', colon + 1);
            if (firstQuote < 0)
            {
                return null;
            }

            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
            {
                return null;
            }

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        private string RefreshTokenKey()
        {
            return TOKEN_PREFS_PREFIX + (_clientId ?? "unassigned") + ".refresh_token";
        }
    }
}
