using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ContrastDvnr
{
    public class TeamServerConnectivityChecker
    {
        const string evalTSUrl = "https://eval.contrastsecurity.com";
        const string prodTSUrl = "https://app.contrastsecurity.com";
        //const string prodTSUrl = "https://teamserver-dotnet.internal.contsec.com";

        public static List<string> CheckForConnectionProblems(bool ignoreCertificateErrors)
        {
            List<string> errors = new List<string>();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            if(ignoreCertificateErrors)
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }

            using (var httpClient = new HttpClient(new HttpClientHandler() { UseCookies = false, AllowAutoRedirect = true }))
            {

                string evalError = TryConnection(httpClient, evalTSUrl);
                if (!string.IsNullOrEmpty(evalError))
                    errors.Add(evalError);

                string prodError = TryConnection(httpClient, prodTSUrl);
                if (!string.IsNullOrEmpty(prodError))
                    errors.Add(prodError);

            }

            return errors;
        }

        private static string TryConnection(HttpClient httpClient, string url)
        {
            try
            {
                var connCheck = httpClient.GetAsync(url).Result;

                if (!connCheck.IsSuccessStatusCode)
                {
                    return $"Could not verify connection to {url}.  Response code: {connCheck.StatusCode}";
                }
            }
            catch (Exception e)
            {
                return $"Could not verify connection to {url}. Error: {e}";
            }

            return null;
        }
    }
}
