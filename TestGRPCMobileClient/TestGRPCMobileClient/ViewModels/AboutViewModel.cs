using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Communication;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Info;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;

namespace TestGRPCMobileClient.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        //private const string Address = "https://alexpc.loc:5001";
        private const string Address = "https://192.168.7.7:5001";
        private static CancellationTokenSource cancellationToken;
        public Informer.InformerClient client;
        private string token;

        public ICommand OpenWebCommand { get; }
        public ICommand OnAuth { get; }
        public ICommand OnPing { get; }
        public ICommand OnGetInfo { get; }



        public AboutViewModel()
        {
            Console.WriteLine("Start client...");
            string s = GetRootCertificates();

           // ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            IPAddress addr = IPAddress.Parse("192.168.7.7");
            IPHostEntry entry = Dns.GetHostEntry(addr);
            Console.WriteLine(entry.HostName);
            Console.WriteLine("Hello World!");
            var channel = new Channel("192.168.7.7", 5000, new SslCredentials(s));

            client = new Informer.InformerClient(channel);
            Title = "About";
            OpenWebCommand = new Command(async () => await Browser.OpenAsync("https://xamarin.com"));
            OnAuth = new Command(OnAuthCommand);
            OnPing  = new Command(OnPingCommand);
            OnGetInfo = new Command(OnGetInfoCommand);
        }

        private async void OnGetServerConfigCommand(object obj)
        {
            //await GetServerConfig(client, token);
        }

        private async void OnGetInfoCommand(object obj)
        {
            await GetServerVersion(client, null);
        }

        private async void OnPingCommand(object obj)
        {
            await GetPing(client);
        }

        private async void OnAuthCommand(object obj)
        {
            await Authenticate();
        }

        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            //return true; // Это если мы хотим вообще наплевать на сертификаты но использовать SSL
            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                    {
                        if ((certificate.Subject == certificate.Issuer) &&
                            ((status.Status == X509ChainStatusFlags.UntrustedRoot) ||
                             // эта строка возможно не нужна при боевом использовании с настоящим сертификатом, у которого есть доверенный корневой сертификат
                             (status.Status == X509ChainStatusFlags.PartialChain))) 
                        {
                            // Self-signed certificates with an untrusted root are valid. 
                            continue;
                        }
                        else
                        {
                            if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags
                                    .NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }

                // When processing reaches this line, the only errors in the certificate chain are 
                // untrusted root errors for self-signed certificates. These certificates are valid
                // for default Exchange server installations, so return true.
                return true;
            }
            else
            {
                // In all other cases, return false.
                return false;
            }
        }

        private async Task GetPing(Informer.InformerClient client)
        {
            Console.WriteLine("Getting ping...");
            try
            {
                Metadata headers = null;

                //var response = client.GetServerPing(new Empty(), headers);
                
                var response = await client.GetServerPingAsync(new Empty(), headers);
                string result = "Nan";
                if (response.PingResponse_ == 1)
                    result = "Ok!";
                Console.WriteLine($"Ping say: {result }");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error get server ping." + Environment.NewLine + ex.ToString());
            }
        }
        private async Task<string> Authenticate()
        {
            token  = String.Empty;
            try
            {
                Console.WriteLine($"Authenticating as {Environment.UserName}...");
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;
                handler.ClientCertificates.Add(LoadCertificate());
                using (var httpClient = new HttpClient(handler))
                {
                    var request = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(
                            $"{Address}/generateJwtToken?name=systemUser&password=FSedfwie34rh9q2?n0-4rmyq2867rtfnw43mdp!"),
                        Version = new Version(1, 1)
                    };
                
                    var tokenResponse = await httpClient.SendAsync(request);
                    tokenResponse.EnsureSuccessStatusCode();
                    token = await tokenResponse.Content.ReadAsStringAsync();
                }
                Console.WriteLine("Successfully authenticated.");
            }
            catch (Exception e)
            {
                Console.WriteLine("==========================ERROR==========================");
                Console.WriteLine(e.Message);
                Console.WriteLine(e);
                Console.WriteLine("=========================================================");
            }

            return token;
        }

        private async Task GetServerVersion(Informer.InformerClient client, string token)
        {
            Console.WriteLine("Getting information...");
            try
            {
                Metadata headers = null;
                var _token = await Authenticate();
                if (_token  != null)
                {
                    headers = new Metadata();
                    headers.Add("Authorization", $"Bearer {token}");
                }

                var response = await client.GetServerInformationAsync(new Empty(), headers);
                Console.WriteLine($"Server version: {response.Version}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error get server version." + Environment.NewLine + ex.ToString());
            }
        }

        public string GetRootCertificates()
        {
            StringBuilder builder = new StringBuilder();
            X509Certificate2 mCert = LoadCertificate();

            builder.AppendLine(
                "# Issuer: " + mCert.Issuer.ToString() + "\n" +
                "# Subject: " + mCert.Subject.ToString() + "\n" +
                "# Label: " + mCert.FriendlyName.ToString() + "\n" +
                "# Serial: " + mCert.SerialNumber.ToString() + "\n" +
                "# SHA1 Fingerprint: " + mCert.GetCertHashString().ToString() + "\n" +
                ExportToPEM(mCert) + "\n");
            
            return builder.ToString();
        }

        private X509Certificate2 LoadCertificate()
        {
            string fileName = $"/mnt/sdcard/certs/alexpc_loc_cert.pfx";
            var bCert = File.ReadAllBytes(fileName);
            var x509 = new X509Certificate2(bCert, @"Cegthctrhtn1");
            return x509;
        }

        /// <summary>
        /// Export a certificate to a PEM format string
        /// </summary>
        /// <param name="cert">The certificate to export</param>
        /// <returns>A PEM encoded string</returns>
        public string ExportToPEM(X509Certificate cert)
        {
            StringBuilder builder = new StringBuilder();            

            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

            return builder.ToString();
        }
    }
}