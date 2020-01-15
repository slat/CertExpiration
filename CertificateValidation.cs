using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace CertExpiration
{
    public class CertificateValidation
    {
        private const int MaxMilliseconds = 5_000;

        public static CertificateValidationResult Validate(string url)
        {
            var task = Task.Factory.StartNew(() => GetCertificateExpiry(url));
            var ms = 0;
            var timeout = false;
            while (!task.IsCompleted)
            {
                ms += 100;
                task.Wait(100);

                if (ms >= MaxMilliseconds)
                {
                    timeout = true;
                    break;
                }
            }

            return new CertificateValidationResult
            {
                Url = url,
                ExpiresAt = task.IsCompleted ? task.Result : null,
                Timeout = timeout,
            };
        }

        private static DateTime? GetCertificateExpiry(string url)
        {
            try
            {
                using (var client = new TcpClient(url, 443))
                using (var sslStream = new SslStream(client.GetStream(), false, CertCallBack, null))
                {
                    sslStream.AuthenticateAsClient(url);
                    var expiresAtText = sslStream.RemoteCertificate.GetExpirationDateString();
                    if (DateTime.TryParse(expiresAtText, out DateTime val))
                        return val;
                }
            }
            catch
            { }

            return null;
        }

        private static RemoteCertificateValidationCallback CertCallBack =
            delegate (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslError)
        {
            return sslError == SslPolicyErrors.None;
        };
    }

    public class CertificateValidationResult
    {
        public string Url { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? ExpireDays => ExpiresAt.HasValue ? (int?) (ExpiresAt.Value - DateTime.Now).TotalDays : null;
        public bool Expired => ExpiresAt.HasValue ? ExpiresAt.Value <= DateTime.Now : true;
        public bool Timeout { get; set; }
    }
}
