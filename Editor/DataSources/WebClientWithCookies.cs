using System;
using System.Net;

namespace GDF.ConfigImport
{
    /// <summary>
    /// Thin <see cref="WebClient"/> wrapper that persists cookies across redirects. Google's
    /// <c>gviz/csv</c> endpoint issues a short-lived cookie on the first request and expects it back
    /// on the follow-up — vanilla <see cref="WebClient"/> drops cookies and the response ends up as
    /// a sign-in page.
    /// </summary>
    internal sealed class WebClientWithCookies : WebClient
    {
        private readonly CookieContainer _cookies = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);
            if (request is HttpWebRequest http)
            {
                http.CookieContainer = _cookies;
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            WebResponse response = base.GetWebResponse(request, result);
            CaptureCookies(response);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            WebResponse response = base.GetWebResponse(request);
            CaptureCookies(response);
            return response;
        }

        private void CaptureCookies(WebResponse response)
        {
            if (response is HttpWebResponse http && http.Cookies != null)
            {
                _cookies.Add(http.Cookies);
            }
        }
    }
}
