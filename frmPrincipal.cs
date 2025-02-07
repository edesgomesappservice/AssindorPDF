using Spire.Pdf;
using Spire.Pdf.Security;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using CloudDocs.AssinadorDigital.WsDocs2;
using MessagingToolkit.QRCode.Codec;
using Spire.Pdf.Widget;
using Spire.Pdf.Graphics;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf.security;
using iTextSharp.text.pdf;
using System.Diagnostics;
using SDK.Util;
using System.Configuration;
using iTextSharp.text.pdf.parser;
using System.Reflection;
using Syncfusion.Pdf.Security;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography;
using WebSocketSharp;
using WebSocketSharp.Server;
using Newtonsoft.Json;

namespace CloudDocs.AssinadorDigital
{
    public partial class frmPrincipal : Form
    {
        string _DocId = "";
        string _ClienteId = "";
        string _UsuarioId = "";
        string _OrigemUser = "";
        string URLRetorno = "";
        string cpfCnpjCertificado = "";
        private bool Sair = false;

        public System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();

        public static string DadosRecebido = "";

        X509Certificate2 CertificadoSelecionado = null;

        public frmPrincipal()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
            InitializeComponent();
        }

        static HttpListener server;

        private void CarregarCertificados()
        {
            List<X509Certificate2> fcollection = Functions.GetCurrentUserCertificates();

            foreach (X509Certificate2 cert in fcollection)
            {
                if (cert.Subject.ToUpper().Contains("ICP-BRASIL"))
                {
                    string Nome = cert.Subject.Substring(cert.Subject.IndexOf("CN=") + 3);
                    if (Nome.Contains(","))
                        Nome = Nome.Substring(0, Nome.IndexOf(","));

                    if (Conversion.ToDateTime(cert.GetExpirationDateString()) > DateTime.Now)
                    {
                        CertificadoCombo certificado = new CertificadoCombo();
                        certificado.Nome = Nome;
                        certificado.cert = cert;

                        cboCertificados.Items.Add(certificado);
                        cboCertificados.DisplayMember = "Nome";
                    }
                }
            }
        }

        public static string ListarCertificados()
        {
            List<X509Certificate2> fcollection = Functions.GetCurrentUserCertificates();

            List<CertificadoWS> certs = new List<CertificadoWS>();

            foreach (X509Certificate2 cert in fcollection)
            {
                if (cert.Subject.ToUpper().Contains("ICP-BRASIL"))
                {
                    string Nome = cert.Subject;
                    if (Nome.Contains(","))
                        Nome = Nome.Substring(0, Nome.IndexOf(","));

                    if (Conversion.ToDateTime(cert.GetExpirationDateString()) > DateTime.Now)
                    {
                        certs.Add(new CertificadoWS { Certificado = cert.Subject });
                    }
                }
            }

            return JsonConvert.SerializeObject(certs);

        }

        public static X509Certificate2 PegarCertificados(string Certificado)
        {
            List<X509Certificate2> fcollection = Functions.GetCurrentUserCertificates();

            List<CertificadoWS> certs = new List<CertificadoWS>();

            foreach (X509Certificate2 cert in fcollection)
            {
                if (cert.Subject.ToUpper().Contains("ICP-BRASIL"))
                {
                    if(cert.Subject== Certificado)
                    {
                        return cert;
                    }
                }
            }
            return null;
        }


        public class CertificadoWS
        {
            public string Certificado { get; set; }
        }

        public class CertificadoCombo
        {
            public string Nome { get; set; }
            public X509Certificate2 cert { get; set; }
        }

        //public class wsListarCertificados : WebSocketBehavior
        //{
        //    protected override void OnMessage(MessageEventArgs e)
        //    {
        //        Send(frmPrincipal.ListarCertificados());
        //    }
        //}

        //public class ReceberDocumentos : WebSocketBehavior
        //{
        //    protected override void OnMessage(MessageEventArgs e)
        //    {
        //        frmPrincipal.DadosRecebido = e.Data;

        //        string[]Dados = DadosRecebido.Split('|');

        //        //AssinarDocumento(Dados[0], Dados[1], Dados[2], Dados[3]);

        //    }
        //    public void AssinarDocumento(string DocumentoId, string ClienteId, string UsuarioId, string Certificado)
        //    {
        //        string LinkSite = "http://localhost:53396/";
        //        if (string.IsNullOrEmpty(LinkSite))
        //            LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

        //        string EnderecoCloudDocs = LinkSite + "/ConverterDocumento.aspx?docId=" + DocumentoId + "&clienteid=" + ClienteId;

        //        string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
        //        System.IO.Directory.CreateDirectory(DiretorioTemp);

        //        string dest = "";

        //        string NomeArquivo = Guid.NewGuid().ToString().ToUpper() + ".pdf";

        //        dest = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);

        //        WebClient webClient = new WebClient();
        //        webClient.UseDefaultCredentials = true;
        //        webClient.DownloadFile(EnderecoCloudDocs, dest);

        //        WSDocs2 ws = new WSDocs2();
        //        AuthHeader auth = new AuthHeader();
        //        auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
        //        ws.AuthHeaderValue = auth;
        //        ws.Url = LinkSite + "/WsDocs2.asmx";
        //        ws.Proxy = Functions.PegarProxy();

        //        USUARIO usuario = ws.BuscarUsuario(Conversion.ToDecimal(UsuarioId));
        //        //nomeCompleto = usuario.USUNOMECOMPLETO;

        //        X509Certificate2 Cert = PegarCertificados(Certificado);

        //        string cpfCnpjCertificado = Functions.PegarCPFCertificado(Cert);

        //    }


        //}




        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            //timer.Tick += Timer_Tick;
            //timer.Interval = 500;
            //timer.Start();

            var wssv = new WebSocketServer("ws://127.0.0.1:8000");
            wssv.AddWebSocketService<WSAssinador.ReceberDocumentos>("/ReceberDocumentos");
            wssv.AddWebSocketService<WSAssinador.wsListarCertificados>("/ListarCertificados");
            wssv.Start();

            //Chilkat.Global glob = new Chilkat.Global();
            //bool success = glob.UnlockBundle("APPSVC.CB1102020_CMvTGxJzpD1l");
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("NDczNjI5QDMxMzkyZTMyMmUzMGRDVElvOVFCNWN6ZXBvR2FuTzVLMVNtL2g3LzdOS2VXc1dTMHg1UWZXTVk9;NDczNjMwQDMxMzkyZTMyMmUzMEErd3VMalNpVm5UMmhzdVY0SW5sM0FGT2lNYlR3ZlZjdEllbStWY3BHajA9;NDczNjMxQDMxMzkyZTMyMmUzME9UU1pnck5ZcFU3N3o1YnUrWDdEekZaMTFHS2J3SkVpbHE3bHlhUVZhcFk9;NDczNjMyQDMxMzkyZTMyMmUzMEZuTW1QQkFKVlJjbFlJa0pnRFZnV0tFS1c3NnNCcE12SllDdUZVRENBUG89;NDczNjMzQDMxMzkyZTMyMmUzMFZITUk2TlhFTDM2d2dQY0o0MUdYY3V6OUVmUW9zNlBKaGF4QkRTYTJDN289;NDczNjM0QDMxMzkyZTMyMmUzMGpLWWp4NjBHR0tkVmZuR3g5OERxNWtNc3R2ZDlmRk40V1RSc09OeFVaaWc9;NDczNjM1QDMxMzkyZTMyMmUzMGdVWWNpTnhua2l0SE4vNGZqQ1ZsSkxoTDVYUG5mRUlxUnlYZUtsQ1Racnc9;NDczNjM2QDMxMzkyZTMyMmUzMExETFc4aHVRUUdjajMzaFREQy85bklhZ2hxK2pTRTN3aUxrVUxPclhxWkE9;NDczNjM3QDMxMzkyZTMyMmUzMGYzYU5Bc2l4a1pNQ1pDUnI2b2RQaU9pUTJtZWdJQjRBS2RUS1lReTljL3c9;NDczNjM4QDMxMzkyZTMyMmUzMFpOKzJ4dnRYcEVzdHJlbE5Wb0hzUFpabjhEakcwb0tzUnZCOHRWd09GVXc9");
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mjk2NzUyOUAzMjMwMmUzNDJlMzBnMXZxU0JZN0lYTDJyVytJeUlHVmFQa1p2Y1JWb2JzQU1sU1JaRVdqOTVBPQ==;Mjk2NzUzMEAzMjMwMmUzNDJlMzBjSENNOUlHdDdHdVhQQVZDd3ZkaVROcEUrWVVxbVNlRnV2emhSTWxXRmNBPQ==;Mgo+DSMBaFt/QHRqVVhlWFpFdEBBXHxAd1p/VWJYdVt5flBPcDwsT3RfQF5iS35Udk1jW35ccXxURA==;Mgo+DSMBPh8sVXJ0S0J+XE9BclRDX3xKf0x/TGpQb19xflBPallYVBYiSV9jS31SdEVkWXdecnVXR2laWQ==;ORg4AjUWIQA/Gnt2VVhkQlFac1tJXGFWfVJpTGpQdk5xdV9DaVZUTWY/P1ZhSXxQd0djXn9WcHZURGJdU0A=;NRAiBiAaIQQuGjN/V0Z+WE9EaFpGVmJLYVB3WmpQdldgdVRMZVVbQX9PIiBoS35RdURhWXheeHRXR2JeU0x3;Mjk2NzUzNUAzMjMwMmUzNDJlMzBWVVhEbE8zQmpYc1JXaCtybjkvMXdxRy83VG1zTWUrcmZjOHAzMXNiUnI4PQ==;Mjk2NzUzNkAzMjMwMmUzNDJlMzBPQWlQRU5LSkphWk80a0lKeFhyRWRWdHNyU2hYUXZ3cXBsdjNLV21kaXUwPQ==;Mgo+DSMBMAY9C3t2VVhkQlFac1tJXGFWfVJpTGpQdk5xdV9DaVZUTWY/P1ZhSXxQd0djXn9WcHZURGJVUEI=;Mjk2NzUzOEAzMjMwMmUzNDJlMzBYUTJJOUI1bnBnbzhFeDI2VzVzZ2tFWStFQkVSSFFBck5HUndrdjhKbG13PQ==;Mjk2NzUzOUAzMjMwMmUzNDJlMzBud1QyQmJzK3N5NXhHaThMd3NUL3c5N215WlJ1eWp4eUdqNDQ4MmhNRjVRPQ==;Mjk2NzU0MEAzMjMwMmUzNDJlMzBWVVhEbE8zQmpYc1JXaCtybjkvMXdxRy83VG1zTWUrcmZjOHAzMXNiUnI4PQ==");


            URLRetorno = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];
            lblURL.Text = URLRetorno;

            Spire.License.LicenseProvider.SetLicenseKey("esoBmitCAQDNUVqmlbVZSAXmKkZUrr+/M+2qvrCOaZyvDkX8S134PJjv1KQlQ6mwo97tH8fSKssaP8GhOc6zqLwPnGJqmfGTzH6IPPRslYriUtojVc2ywa+6ONwvenS1gP/VqWfSIPb4LAsUxLEng2uMWCIQvq8eVkEnTAZX2AY/ak1Juqif1zcFvldXCmtWUUcGQpdS2JU349HAllDeR2EAuSxzWs/VLwB8biNSs6Zfb1bj9Mh1A20rvUq4ArPISHghk4EM+Ecs8BUFUEN9ueHPBCdRjCehgWxE5Hmti4gmgYuY2jUzvdurYCgcON5rkdndIzGNYiJWcbJtzlM2er+lyS6FgcRDLGWEGKy4eQiFtcXzZImYfdfpPjpHmztAfewJVSJFP+tvnWddVVyb//rub/hDQg2AvRHJwHfdmUSW9GzyOCKSWoO/pEvcEntsjHF0Kc51K0EPerDGckqiquuFonoONabdmERXt3w7/KkQNQTCh6K4j0VoVRnkWkCGxl7+I6GqrkjDpflSCudq9+QoD7Z+ZrDLpIc2h5EdtZ/Hc04Z1+kN1g9/9q7Nkmwfw2ij0I/qIsz3Ij1Ggrzm1Zd9Qb9URK3GW9V1yeAd724ElAvwUuPe4edAIYuNxmLu7pHAP6A6t4VEJhjWe80wR/5O+Uj0xPIejDbLN9kL2YsHcFfzSzljCm6EzhR2/Xsnu/DgFgOxJtCYFKjBQntjDzNQr8qiejdDQO2G0tCiNMLt96SwhqhVbqQ6tnKc5ElD/Y6qhf1IX0jho13YewKqzHkETDrwVGnKOYYYD1ApXAWig0vLOa/zeu0IuXmCXYyDxd4XKC254KFquAlXtYRz2FLyP+rriOzoX4wxLFDkSu5K+tvAwExDye+ZAqFdvsCSN/lYqiRM95M+QcZIfKjDOdVXUOelmze34Jg35K74KcL6057wlZrBiXj3KJ3Vwghauh69SPm4MIyVjnqTDb97SYpLtn5hNMDXek2Km07dJjyPCfVPI4aSFrAnrhvZYfqn1yFPFb1EZK91olnF3EBPUjYNvnmGmnRcZ9pC14+glkiH2f+M5/iSMJhYkcHPqCgH5StnJXcDE0+gjY3YNpAcJ5mHju7UjWhziEO421um99ey0lzTA16DKyTsJZua0Sycf6jf6nIUKSwx2mfLb9IshWatN3v8hKqUTmK+ZOl8GoQhygoPWXEowT4+qog9VZ+nt1ulsMOB/myVO1+bgZPOdyxR+EVVlPgGy1cmKmh0vQvdNPVki2/hxlwwwDzldfscDiDBmeDwjlUwvCAYUGj2/PZlMWDAacfQE1vOCUPD9i1vA6I+H7tq/J/FN1pKaLUBqHofHqc5cBWs+45K2KrVTM/nfavFn3o2PSEUlSXWxU36t1MaO1UsmcHbtf/wVswx8b7a7x3+jiZVXuXhawOSBzqnLdNixES/ChJhfgbUUE2We0aOX3rpL94KKxpybPFTCmzAVxQiBjaULeZEMNpo0FoxRBsrlVuE81CUcby9q+vz3WYdPfWS2/LSr8VRssWw/6cocxltar/Kuk/NM50v/nEozSrbUYR7bvXQQVHE8OkVnNuARiaTGw==", true);

            string NomeSistema = "AppService - Assinador de Documentos " + Application.ProductVersion;
            this.Invoke(new Action(() => this.Text = NomeSistema));

            CarregarCertificados();

            //server = new HttpListener();
            //server.Prefixes.Add("http://127.0.0.1:8000/");
            //server.Prefixes.Add("http://localhost:8000/");
            //server.Start();

            try
            {
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"TempImagens"))
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"TempImagens", true);

                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"file.json"))
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"file.json");

                //{ "Mensagem": "OK"}
            }
            finally { }


            //Thread thread = new Thread(() => Processo(this));
            //thread.Start();


            //webViewerPDF.Navigate(@"file://C:\Projetos\CloudDocs\CloudDocs.Web\CloudDocs.Web\CloudDocs.AssinadorDigital\bin\Debug\TempFile\3D389B93-0618-4E80-A213-68C972FDAA4E.pdf");
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if(DadosRecebido!="")
            {
                timer.Stop();

                this.Invoke(new Action(() => this.WindowState = FormWindowState.Normal));
                this.Invoke(new Action(() => this.Focus()));
                this.Invoke(new Action(() => this.BringToFront()));
                this.Invoke(new Action(() => this.Activate()));
                this.Invoke(new Action(() => this.btnAssinar_Click(null,null)));
            }
        }

        string responseString = "<HTML><BODY>Programa Executado!</BODY></HTML>";

        public void Processo(frmPrincipal frm)
        {
            while (true)
            {
                HttpListenerContext context = server.GetContext();
                HttpListenerResponse response = context.Response;
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET");

                var queryString = HttpUtility.ParseQueryString(context.Request.Url.Query);
                string paran = queryString["paran"];
                string clienteid = queryString["clienteid"];
                string usuarioid = queryString["usuid"];
                string origemUser = queryString["origemuser"];
                
                string NomeSistema = "Assinador de Documentos";

                if (paran != null && paran != "")
                {
                    this.Invoke(new Action(() => this.Show()));
                    this.Invoke(new Action(() => this.WindowState = FormWindowState.Normal));
                    this.Invoke(new Action(() => this.Focus()));

                    if (queryString["URLRetorno"]!=null)
                    {
                        URLRetorno = queryString["URLRetorno"];
                        if (URLRetorno.LastIndexOf("/")>-1)
                        {
                            URLRetorno = URLRetorno.Substring(0, URLRetorno.Length - 1);//Retirar a ultima Barra "/"
                        }

                        System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        config.AppSettings.Settings["LinkSite"].Value = URLRetorno;
                        config.Save(ConfigurationSaveMode.Modified);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                    else
                    {
                        URLRetorno = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];
                    }

                    if(!string.IsNullOrEmpty(NomeSistema))
                    {
                        NomeSistema  = NomeSistema + " - Assinador Digital " + Application.ProductVersion;
                        this.Invoke(new Action(() => this.Text = NomeSistema));
                    }

                    this.Invoke(new Action(() => lblURL.Text = URLRetorno));

                    MostrarDocumento(paran, clienteid, usuarioid, origemUser);


                }
                else if (paran != null && paran.ToUpper().Equals("F"))
                {
                    this.Invoke(new Action(() => this.Show()));
                    this.Invoke(new Action(() => this.WindowState = FormWindowState.Minimized));
                    this.Invoke(new Action(() => this.Focus()));
                }


                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                output.Close();
            }
        }
        string NomeArquivo = "";

        private void MostrarDocumento(string DocId, string clienteid, string usuarioid, string origemUser)
        {
            responseString = "<HTML><BODY>Programa Executado!</BODY></HTML>";

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"file.json"))
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"file.json");

            //this.Invoke(new Action(() => webViewerPDF.Navigate("")));

            _DocId = DocId;
            _ClienteId = clienteid;
            _UsuarioId = usuarioid;

            if (String.IsNullOrEmpty(origemUser) || origemUser == "1")
            {
                _OrigemUser = "1";
            }
            else
            {
                _OrigemUser = "2";
            }

            LimparTemporario();

            string LinkSite = URLRetorno;
            if(string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

            string EnderecoCloudDocs = LinkSite + "/ConverterDocumento.aspx?docId=" + DocId + "&clienteid=" + clienteid;

            string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
            System.IO.Directory.CreateDirectory(DiretorioTemp);

            string dest = "";

            NomeArquivo = Guid.NewGuid().ToString().ToUpper() + ".pdf";

            dest = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);

            WebClient webClient = new WebClient();
            webClient.UseDefaultCredentials = true;
            webClient.DownloadFile(EnderecoCloudDocs, dest);

            this.Invoke(new Action(() => this.WindowState = FormWindowState.Normal));
            this.Invoke(new Action(() => this.Focus()));
            this.Invoke(new Action(() => this.BringToFront())); 
            this.Invoke(new Action(() => this.Activate())); 

            //this.Invoke(new Action(() => webViewerPDF.Navigate(string.Format(@"file://{0}\TempFile\" + NomeArquivo + "", Application.StartupPath))));


            /*PdfViewer.PdfViewer pdf = new PdfViewer.PdfViewer();
            pdf.Dock = System.Windows.Forms.DockStyle.Fill;
            pdf.Location = new System.Drawing.Point(0, 0);
            pdf.Name = "pdfViewer1";
            pdf.Size = new System.Drawing.Size(585, 595);
            pdf.TabIndex = 0;

            pdf.Document = PdfViewer.PdfDocument.Load(dest, PdfViewer.PdfEngine.Chrome);

            this.Invoke(new Action(() => this.grdDocumento.Controls.Add(pdf)));*/
            //webBrowser1.Navigate();

            //this.Invoke(new Action(() => webViewerPDF.Navigate(string.Format(@"file://{0}\TempFile\" + NomeArquivo + "", Application.StartupPath))));

        }

        private void btnAssinar_Click(object sender, EventArgs e)
        {
            string[] Dados = DadosRecebido.Split('|');
            _DocId = Dados[0];
            _ClienteId = Dados[1];
            _UsuarioId = Dados[2];


            for(int i = 0; i < cboCertificados.Items.Count; i++)
            {
                CertificadoCombo Cert = cboCertificados.Items[i] as CertificadoCombo;
                if (Cert.cert.Subject==Dados[3])
                {
                    cboCertificados.SelectedIndex = i;
                    break;
                }
            }


            MostrarDocumento(_DocId, _ClienteId, _UsuarioId, "");

            string LinkSite = URLRetorno;
            if (string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];
            try
            {
                if (Conversion.ToDecimal(_UsuarioId)==0 || Convert.ToDecimal(_DocId)==0)
                {
                    MessageBox.Show("É obrigatório selecionar o documento para ser certificado",
                                    "Atenção!",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1);
                    return;
                }

                int IndexCertificado = cboCertificados.SelectedIndex;

                if (IndexCertificado == -1)
                {
                    MessageBox.Show("É obrigatório selecionar o certificado A3",
                                    "Atenção!",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1);
                    return;
                }

                WSDocs2 ws = new WSDocs2();
                AuthHeader auth = new AuthHeader();
                auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
                ws.AuthHeaderValue = auth;
                ws.Url = LinkSite + "/WsDocs2.asmx";
                ws.Proxy = Functions.PegarProxy();

                string nomeCompleto = "";
                string profissao = "";

                if (_OrigemUser == "2")
                {
                    DOC_USUARIOEXTERNO usuario = ws.BuscarUsuarioExterno(Conversion.ToDecimal(_UsuarioId));
                    nomeCompleto = usuario.USENOME;
                }
                else
                {
                    USUARIO usuario = ws.BuscarUsuario(Conversion.ToDecimal(_UsuarioId));
                    nomeCompleto = usuario.USUNOMECOMPLETO;
                }


                string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
                string PDFSaida = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);


                CertificadoCombo certificado = (CertificadoCombo)cboCertificados.SelectedItem;

                X509Certificate2 Certificado = certificado.cert;// fcollection[IndexCertificado];

                cpfCnpjCertificado = Functions.PegarCPFCertificado(Certificado);

                if (string.IsNullOrEmpty(_DocId))
                {
                    MessageBox.Show("Nenhum documento selecionado para assinar.",
                                    "Atenção!",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1);
                    return;
                }

                if (ValidaAssinatura(PDFSaida, Certificado.Subject))
                {
                    MessageBox.Show("Esse documento já foi assinado por esse certificado! " + Certificado.Subject,
                                    "Atenção!",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1);
                    return;
                }


                string SolicitacaoDocumento = ws.ConsultarSolicitacaoDocumentoAberta(Convert.ToDecimal(_DocId));
                System.Data.DataTable dtSolicitacao = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(SolicitacaoDocumento, (typeof(System.Data.DataTable)));
                if (dtSolicitacao != null && dtSolicitacao.Rows.Count > 0)
                {
                    MessageBox.Show("O documento não pode ser assinado digitalmente porque existem solicitações de assinatura eletrônica para ele", "Atenção!",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Exclamation,
                                    MessageBoxDefaultButton.Button1);
                    return;
                }

                int qtdAssinaturaDigital = VerificarQuantidadeAssinaturas(PDFSaida);
                string assinaturasEletronicas = ws.ConsultarAssinaturas(Convert.ToDecimal(_DocId));
                System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(assinaturasEletronicas, (typeof(System.Data.DataTable)));

                int totalAssinaturasEletronicas = 0;
                if (dt != null && dt.Rows.Count > 0)
                {
                    totalAssinaturasEletronicas = dt.Rows.Count;
                }
                int totalAssinaturas = 0;
                totalAssinaturas = totalAssinaturas + totalAssinaturasEletronicas + qtdAssinaturaDigital;

                string pdfDestino = DiretorioTemp + Guid.NewGuid().ToString() + ".pdf";

                string tpdid = ws.ConsultarDocumento(Convert.ToDecimal(_DocId));

                //string consultarAreas = ws.ConsultarAreasAssinaturas(Convert.ToDecimal(tpdid));

                string TipoDocumento = ws.ConsultarTipoDocumento(Convert.ToInt32(tpdid));

                //Syncfusion.Pdf.Parsing.PdfLoadedDocument loadedDocument = new Syncfusion.Pdf.Parsing.PdfLoadedDocument(dest);

                //foreach(var page in loadedDocument.Pages)
                //{
                //    Syncfusion.Pdf.PdfLoadedPage p = page as Syncfusion.Pdf.PdfLoadedPage;
                //    Syncfusion.Pdf.Graphics.PdfGraphics graphics = p.Graphics;
                //    Syncfusion.Pdf.Graphics.PdfFont font = new Syncfusion.Pdf.Graphics.PdfStandardFont(Syncfusion.Pdf.Graphics.PdfFontFamily.Helvetica, 1);
                //    graphics.DrawString("", font, Syncfusion.Pdf.Graphics.PdfBrushes.Black, new PointF(0, 0));
                //}

                //loadedDocument.Save(pdfDestino);

                //loadedDocument.Close();
                //loadedDocument.Dispose();

                if(qtdAssinaturaDigital==0)
                {
                    ConverterPDF(PDFSaida);
                }

                System.Data.DataTable dtTipoDocumento = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(TipoDocumento, (typeof(System.Data.DataTable)));

                if (dtTipoDocumento != null && dtTipoDocumento.Rows.Count > 0 && Conversion.ToBoolean(dtTipoDocumento.Rows[0]["tpdFotoRosto"].ToString()))
                {
                    //pdfDestino = AssinarDocumentosRodaPeSync(PDFSaida, Conversion.ToInteger(_ClienteId), Convert.ToDecimal(_DocId), Convert.ToDecimal(_UsuarioId), "", cpfCnpjCertificado, "0", Certificado);
                    pdfDestino = AssinarDocumentosRodaPe(PDFSaida, Conversion.ToInteger(_ClienteId), Convert.ToDecimal(_DocId), Convert.ToDecimal(_UsuarioId), "", cpfCnpjCertificado, "0", Certificado);
                }
                else
                {

                    string Temp = "";

                    string hash = Functions.EncryptParametroURL(_DocId, ConfigurationManager.AppSettings["Key"]).ToUpper();


                    bool multiplasAssinaturas = ws.MultiplasAssinaturas(Convert.ToDecimal(_DocId));


                    int alturaTopo = 170;
                    alturaTopo = alturaTopo + (40 * (totalAssinaturas));
                    int alturaTopoTitulo = 0;

                    int espacoLado = -400;

                    if (multiplasAssinaturas)
                    {
                        if (totalAssinaturas == 0)
                        {
                            //System.IO.File.Copy(dest, pdfDestino, true);
                            Temp = AdicionarPagina(PDFSaida, Convert.ToDecimal(_DocId), "S", 0);
                        }
                        else
                        {
                            Temp = PDFSaida;
                        }
                    }
                    else
                    {
                        if (totalAssinaturas > 0)
                        {
                            MessageBox.Show("Esse documento só permite uma assinatura e já foi assinado",
                                            "Atenção!",
                                            MessageBoxButtons.OK,
                                            MessageBoxIcon.Exclamation,
                                            MessageBoxDefaultButton.Button1);
                            return;
                        }

                        Temp = PDFSaida;

                        float llx = 0;
                        float lly = 0;
                        float urx = 0;
                        //float ury = 0;

                        var pdfReaderCount = new PdfReader(new RandomAccessFileOrArray(Temp), null);
                        var contentParser = new PdfReaderContentParser(pdfReaderCount);

                        string pdfDestino2 = DiretorioTemp + Guid.NewGuid().ToString() + ".pdf";

                        using (var pdfStamperCount = new PdfStamper(pdfReaderCount, new FileStream(pdfDestino2, FileMode.Create, FileAccess.Write)))
                        {
                            var contentByte = pdfStamperCount.GetOverContent(1);
                            var rectangle = new iTextSharp.text.Rectangle(0, 0, 0, 0);
                            for (int i = 1; i <= pdfReaderCount.NumberOfPages; i++)
                            {
                                var marginFinder = new TextMarginFinder();
                                var pageSize = pdfReaderCount.GetPageSize(i);
                                var regionText = new iTextSharp.text.Rectangle(pageSize.Left, pageSize.Bottom + 80, pageSize.Right, pageSize.Top - 80);
                                var filtered = new FilteredRenderListener(marginFinder, new RegionTextRenderFilter(regionText), new SpaceFilter());
                                contentParser.ProcessContent(i, new TextRenderInfoSplitter(filtered));
                                contentByte = pdfStamperCount.GetOverContent(i);
                                rectangle = new iTextSharp.text.Rectangle(marginFinder.GetLlx(), marginFinder.GetLly(), marginFinder.GetWidth() + 90, 0);
                                contentByte.Stroke();

                                llx = marginFinder.GetLlx();
                                lly = marginFinder.GetLly();
                                urx = marginFinder.GetWidth() + 90;
                                //ury = 0;
                            }
                        }

                        Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument(Temp);
                        PdfPageBase ultimaPagina = document.Pages[document.Pages.Count - 1]; // PEGAR A ÚLTIMA PAGINA

                        float espacamento = (ultimaPagina.Size.Height - lly) + 20;
                        float espacamentoAssinatura = ultimaPagina.Size.Height - espacamento;

                        if (espacamentoAssinatura < 150)
                        {
                            Temp = AdicionarPagina(PDFSaida, Convert.ToDecimal(_DocId), "S", 0);

                        }
                        else
                        {
                            alturaTopo = Conversion.ToInteger(ultimaPagina.Size.Height - lly) + 105;

                            alturaTopoTitulo = Conversion.ToInteger(ultimaPagina.Size.Height - lly);

                            Temp = AdicionarPagina(PDFSaida, Convert.ToDecimal(_DocId), "N", alturaTopoTitulo);

                            espacoLado = -435;
                        }

                    }

                    Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
                    Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { cp.ReadCertificate(Certificado.RawData) };
                    IExternalSignature externalSignature = new X509Certificate2Signature(Certificado, "SHA-1");
                    PdfReader pdfReader = new PdfReader(Temp);
                    iTextSharp.text.Rectangle psize = pdfReader.GetPageSize(pdfReader.NumberOfPages);

                    FileStream signedPdf = new FileStream(pdfDestino, FileMode.Create);  //the output pdf file
                    PdfStamper pdfStamper = PdfStamper.CreateSignature(pdfReader, signedPdf, '\0', null, true);
                    PdfSignatureAppearance signatureAppearance = pdfStamper.SignatureAppearance;

                    string NomeUsuario = nomeCompleto;
                    if (!string.IsNullOrEmpty(profissao))
                    {
                        NomeUsuario += ", " + profissao;
                    }

                    signatureAppearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.GRAPHIC_AND_DESCRIPTION;
                    signatureAppearance.SignatureGraphic = iTextSharp.text.Image.GetInstance(AppDomain.CurrentDomain.BaseDirectory + "digitalsign.jpg");
                    signatureAppearance.Layer2Font = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.COURIER, 8, 0, iTextSharp.text.BaseColor.BLACK);
                    signatureAppearance.Layer2Text = "Assinado digitalmente por: " + NomeUsuario + ", Certificado Digital: " + Certificado.Subject + " Data da Assinatura: " + DateTime.Now;
                    signatureAppearance.RunDirection = PdfWriter.RUN_DIRECTION_DEFAULT;
                    signatureAppearance.SignatureGraphic.Alignment = iTextSharp.text.Image.ALIGN_LEFT;
                    signatureAppearance.SetVisibleSignature(new iTextSharp.text.Rectangle(espacoLado, psize.Height - alturaTopo, 575, psize.Height - (alturaTopo - 40)), pdfReader.NumberOfPages, Guid.NewGuid().ToString().ToUpper());
                    signatureAppearance.SignDate = System.DateTime.Now;
                    signatureAppearance.Acro6Layers = true;
                    MakeSignature.SignDetached(signatureAppearance, externalSignature, chain, null, null, null, 0, CryptoStandard.CMS);
                    //MakeSignature.SignDetached(signatureAppearance, externalSignature, chain, null, null, null, 0, CryptoStandard.CADES);
                }


                FileInfo fsi = new FileInfo(pdfDestino);
                int Offset = 0; // starting offset.
                int ChunkSize = 65536; // 64 * 1024 kb
                byte[] Buffer = new byte[ChunkSize];
                string nomeArquivoCry = Guid.NewGuid().ToString().ToUpper() + ".pdf.cry";
                string ChaveAcesso = "916FBB14";
                SDK.Util.Encrypt.EncryptFile(fsi.FullName, fsi.DirectoryName + "/" + nomeArquivoCry, ChaveAcesso);
                FileInfo fsiCry = new FileInfo(fsi.DirectoryName + "/" + nomeArquivoCry);
                FileStream fileStream = new FileStream(fsi.DirectoryName + "/" + nomeArquivoCry, FileMode.Open, FileAccess.Read);

                int iTotalPaginas = 0;
                long FileSize = 0;
                try
                {
                    PdfReader pdfReader2 = new iTextSharp.text.pdf.PdfReader(pdfDestino);
                    iTotalPaginas = pdfReader2.NumberOfPages;
                }
                catch (BadPasswordException)
                {
                    iTotalPaginas = 1;
                }
                catch (Exception)
                {
                    iTotalPaginas = 1;
                }

                string Estrutura = _ClienteId.ToString().PadLeft(6, '0') + "/" + DateTime.Now.ToString("yyyyMMdd");

                try
                {
                    FileSize = fsiCry.Length; // File size of file being uploaded.
                    fileStream.Position = Offset;
                    int BytesRead = 0;
                    int iTotalLoop = Convert.ToInt32(FileSize / ChunkSize);

                    while (Offset != FileSize) // continue uploading the file chunks until offset = file size.
                    {
                        BytesRead = fileStream.Read(Buffer, 0, ChunkSize); // read the next chunk

                        if (BytesRead != Buffer.Length)
                        {
                            ChunkSize = BytesRead;
                            byte[] TrimmedBuffer = new byte[BytesRead];
                            Array.Copy(Buffer, TrimmedBuffer, BytesRead);
                            Buffer = TrimmedBuffer; // the trimmed buffer should become the new 'buffer'
                        }

                        bool ChunkAppened = ws.EnviarArquivos(nomeArquivoCry, Estrutura, Buffer, Offset);

                        if (!ChunkAppened)
                        {
                            break;
                        }

                        Offset += BytesRead; // save the offset position for resume
                    }
                }
                catch (Exception)
                {
                    fileStream.Close();
                }
                finally
                {
                    fileStream.Close();
                }

                if (!ws.VerificaArquivo(Estrutura, nomeArquivoCry, fsiCry.Length))
                {
                    MessageBox.Show("Não foi possivel enviar o arquivo para o storage.",
                                   "Atenção!",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Exclamation,
                                   MessageBoxDefaultButton.Button1);
                }
                else
                {
                    try
                    {
                        File.Delete(fsi.DirectoryName + "/" + nomeArquivoCry);
                    }
                    catch { }

                    string TrasmitirArquivo = ws.CriarNovaVersaoAssinatura(Estrutura + "/" + nomeArquivoCry, Convert.ToDecimal(_DocId), Convert.ToInt32(_ClienteId), nomeArquivoCry, FileSize, iTotalPaginas, Convert.ToDecimal(_UsuarioId), _OrigemUser);

                    if (TrasmitirArquivo.Contains("Sucesso"))
                    {
                        string docId = TrasmitirArquivo.Split('|')[1];

                        //MostrarDocumento(docId, _ClienteId, _UsuarioId, _OrigemUser);

                        responseString = "Atualizar";

                        MessageBox.Show("Documento Assinado com sucesso!",
                                  "Sucesso!",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information,
                                  MessageBoxDefaultButton.Button1);
                        
                        this.WindowState = FormWindowState.Minimized;
                    }
                    else
                    {
                        MessageBox.Show(TrasmitirArquivo);
                    }
                }

            }
            catch (Exception) 
            {
                MessageBox.Show("Não foi possivel assinar o documento com o certificado selecionado.",
                                   "Atenção!",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Error,
                                   MessageBoxDefaultButton.Button1);
            }
            finally
            {
                DadosRecebido = "";
                timer.Start();
            }

            //iTextSharpSign.SmartCard.SignHashed(Temp, pdfDestino, "", "", Certificado, Convert.ToDecimal(_DocId), totalAssinaturas);
        }


        public void ConverterPDF(string ArquivoEntrada)
        {
            
            Syncfusion.Pdf.Parsing.PdfLoadedDocument loadedDocument = new Syncfusion.Pdf.Parsing.PdfLoadedDocument(ArquivoEntrada);

            foreach (var page in loadedDocument.Pages)
            {
                Syncfusion.Pdf.PdfLoadedPage p = page as Syncfusion.Pdf.PdfLoadedPage;
                Syncfusion.Pdf.Graphics.PdfGraphics graphics = p.Graphics;
                Syncfusion.Pdf.Graphics.PdfFont font = new Syncfusion.Pdf.Graphics.PdfStandardFont(Syncfusion.Pdf.Graphics.PdfFontFamily.Helvetica, 1);
                graphics.DrawString("", font, Syncfusion.Pdf.Graphics.PdfBrushes.Black, new PointF(0, 0));
            }

            loadedDocument.Save();

            loadedDocument.Close();
            loadedDocument.Dispose();
        }
        public string AssinarDocumentosRodaPe(string Arquivo, int ClienteId, decimal DocumentoId, decimal UsuarioId, string IpAcesso, string CertificadoCNPJ, string Rotacao, X509Certificate2 Certificado = null)
        {
            //INICO:

             string LinkSite = URLRetorno;
            if (string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

            WSDocs2 ws = new WSDocs2();
            AuthHeader auth = new AuthHeader();
            auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
            ws.AuthHeaderValue = auth;
            ws.Url = LinkSite + "/WsDocs2.asmx";
            ws.Proxy = Functions.PegarProxy();


            string HashValidacao = Functions.EncryptParametroURL(DocumentoId.ToString(), "916FBB14").ToUpper();

            //int QtdeTentativa = 0;
            string NovoArquivo = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";
            string ArquivoConvertido = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";
            string DocAssinado = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0')))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0'));

            string Extensao = System.IO.Path.GetExtension(Arquivo);

            Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument(Arquivo);
            
            try
            {

                string cliente = ws.ConsultarCliente(Convert.ToDecimal(_ClienteId));
                System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(cliente, (typeof(System.Data.DataTable)));
                DateTime DataHora = DateTime.Now;

                string SiteValidacao = "";

                if (!string.IsNullOrEmpty(dt.Rows[0]["CLISITEVALIDACAODOCS"].ToString()))
                {
                    SiteValidacao = dt.Rows[0]["CLISITEVALIDACAODOCS"] + "/ValidarDocumento.aspx";
                }
                else
                {
                    SiteValidacao = LinkSite + "/ValidarDocumento.aspx";
                }

                string Assinante = "";
                //string CNPJ = "";
                string TipoAssinatura = "";

                if (Certificado != null)
                {
                    string DiretorioBaseCertificado = AppDomain.CurrentDomain.BaseDirectory;

                    DiretorioBaseCertificado = DiretorioBaseCertificado.Substring(0, DiretorioBaseCertificado.Length - 1);
                    Assinante = Functions.getKey(Certificado.Subject, "CN");
                    TipoAssinatura = "Digital";

                }
                else
                {
                    Assinante = dt.Rows[0]["CLINOME"].ToString();
                    CertificadoCNPJ = dt.Rows[0]["CLICNPJ"].ToString();
                    TipoAssinatura = "Eletronicamente";
                }

                try
                {
                    DataHora = DateTime.Now;
                }
                catch
                {
                    DataHora = DateTime.Now;
                }

                PdfPageBase page = document.Pages[document.Pages.Count - 1];

                ChancelarQRCode(page, SiteValidacao, HashValidacao, Rotacao, TipoAssinatura, DataHora, Assinante, CertificadoCNPJ);

                string Rodape = "";
                if (TipoAssinatura == "Digital")
                {
                    Rodape = "Documento Assinado digitalmente. Hash de Validação";
                }
                else
                {
                    Rodape = "Documento assinado eletronicamente. Hash de Validação";
                }

                int contador = 1;
                foreach (PdfPageBase pageChancela in document.Pages)
                {
                    EscreverRodape(HashValidacao, pageChancela, Rodape, document.Pages.Count, contador);
                }

                document.SaveToFile(NovoArquivo, FileFormat.PDF);
                document.Close();
                document.Dispose();

                if (Certificado != null)
                {
                    FileStream signedPdf = new FileStream(DocAssinado, FileMode.Create);  //the output pdf file

                    PdfReader pdfReader = new PdfReader(NovoArquivo);
                    PdfStamper pdfStamper = PdfStamper.CreateSignature(pdfReader, signedPdf, '\0', null, true);

                    Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
                    Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { cp.ReadCertificate(Certificado.RawData) };
                    IExternalSignature externalSignature = new X509Certificate2Signature(Certificado, "SHA-1");

                    iTextSharp.text.Rectangle psize = pdfReader.GetPageSize(pdfReader.NumberOfPages);

                    PdfSignatureAppearance signatureAppearance = pdfStamper.SignatureAppearance;

                    ITSAClient tsc = new TSAClientBouncyCastle("http://ts.cartaodecidadao.pt/tsa/server", "", "");

                    signatureAppearance.Reason = "Assinatura Digital - ICP-Brasil";
                    signatureAppearance.Location = "Brasília-DF";
                    signatureAppearance.SignDate = DataHora;
                    signatureAppearance.Acro6Layers = true;

                    OcspVerifier ocspVerifier = new OcspVerifier(null, null);
                    IOcspClient ocspClient = new OcspClientBouncyCastle(ocspVerifier);

                    ICrlClient crlClient = new CrlClientOnline();
                    List<ICrlClient> crlList = new List<ICrlClient>();

                    string[] crls = Functions.GetCrlDistributionPoints(Certificado);

                    crlClient = new CrlClientOnline(crls);
                    crlList.Add(crlClient);

                    MakeSignature.SignDetached(signatureAppearance, externalSignature, chain, null, ocspClient, null, 0, CryptoStandard.CADES);

                    pdfStamper.Close();
                    pdfStamper.Dispose();
                    pdfReader.Close();
                    pdfReader.Dispose();
                    signedPdf.Close();
                    signedPdf.Dispose();

                    return DocAssinado;
                }
                else
                {
                    return NovoArquivo;
                }

            }
            catch (Exception)
            {
                return "";
            }

        }

        public void ChancelarQRCode(PdfPageBase page, string SiteValidacao, string HashValidacao, string Rotacao, string TipoAssinatura, DateTime DataHora, string Assinante, string CNPJ)
        {
            try
            {

                Spire.Pdf.Graphics.PdfTrueTypeFont font = new Spire.Pdf.Graphics.PdfTrueTypeFont(new Font("Arial", 8.25f, FontStyle.Regular), true);

                QRCodeEncoder qrCodecEncoder = new QRCodeEncoder();
                qrCodecEncoder.QRCodeBackgroundColor = Color.White;
                qrCodecEncoder.QRCodeForegroundColor = Color.Black;
                qrCodecEncoder.CharacterSet = "UTF-8";
                qrCodecEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
                qrCodecEncoder.QRCodeScale = 6;
                qrCodecEncoder.QRCodeVersion = 0;
                qrCodecEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.Q;

                String data = SiteValidacao + "?hash=" + HashValidacao;
                System.Drawing.Image imageQRCode = qrCodecEncoder.Encode(data);
                Spire.Pdf.Graphics.PdfImage image = Spire.Pdf.Graphics.PdfImage.FromImage(imageQRCode);

                if (Rotacao == "1")
                {
                    float x = page.Canvas.ClientSize.Width - 10;
                    float y = page.Size.Height - 10;
                    page.Canvas.TranslateTransform(x, y);
                    page.Canvas.RotateTransform(270);

                    page.Canvas.DrawImage(image, 0, -40, 40, 40);
                }
                else
                {
                    page.Canvas.DrawImage(image, 20, page.Canvas.ClientSize.Height - 60, 40, 40);
                }

                StringBuilder st = new StringBuilder();
                st.Append("Documento Gerado e Assinado " + TipoAssinatura + " em " + DataHora.ToString("dd/MM/yyyy") + " às " + DataHora.ToString("HH:mm:ss") + " (data e hora de Brasília)." + Environment.NewLine);
                //st.Append("Dados do Assinante: AppService Tecnologia da Informação LTDA - CNPJ: 18.775.558/0001-04" + Environment.NewLine);
                st.Append("Dados do Assinante: " + Assinante + " - CPF/CNPJ: " + CNPJ + Environment.NewLine);
                st.Append("Código de Verificação: " + HashValidacao + Environment.NewLine);
                st.Append("Valide esse documento em: " + SiteValidacao + " Informando o código de verificação.");

                Rectangle labelBounds = new Rectangle(65, Convert.ToInt16(page.Canvas.ClientSize.Height) - 58, 600, 400);

                if (Rotacao == "1")
                {
                    PdfStringFormat centerAlignment = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);

                    page.Canvas.DrawString(st.ToString(), font, PdfBrushes.Black, 45, -20, centerAlignment);
                }
                else
                {
                    page.Canvas.DrawString(st.ToString(), font, PdfBrushes.Black, labelBounds, new PdfStringFormat() { Alignment = PdfTextAlignment.Left });
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public string AssinarDocumentosRodaPeSync(string Arquivo, int ClienteId, decimal DocumentoId, decimal UsuarioId, string IpAcesso, string CertificadoCNPJ, string Rotacao, X509Certificate2 Certificado = null)
        {
            //INICO:

            string LinkSite = URLRetorno;
            if (string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

            WSDocs2 ws = new WSDocs2();
            AuthHeader auth = new AuthHeader();
            auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
            ws.AuthHeaderValue = auth;
            ws.Url = LinkSite + "/WsDocs2.asmx";
            ws.Proxy = Functions.PegarProxy();


            string HashValidacao = Functions.EncryptParametroURL(DocumentoId.ToString(), "916FBB14").ToUpper();

            //int QtdeTentativa = 0;
            string NovoArquivo = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";
            string ArquivoConvertido = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";
            string DocAssinado = AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0') + "/" + Guid.NewGuid().ToString() + ".pdf";

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0')))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "/TempImagens/" + ClienteId.ToString().PadLeft(6, '0'));

            string Extensao = System.IO.Path.GetExtension(Arquivo);

            Syncfusion.Pdf.Parsing.PdfLoadedDocument document = new Syncfusion.Pdf.Parsing.PdfLoadedDocument(Arquivo);

            //foreach (var page in loadedDocument.Pages)
            //{
            //    Syncfusion.Pdf.PdfLoadedPage p = page as Syncfusion.Pdf.PdfLoadedPage;
            //    Syncfusion.Pdf.Graphics.PdfGraphics graphics = p.Graphics;
            //    Syncfusion.Pdf.Graphics.PdfFont font = new Syncfusion.Pdf.Graphics.PdfStandardFont(Syncfusion.Pdf.Graphics.PdfFontFamily.Helvetica, 1);
            //    graphics.DrawString("", font, Syncfusion.Pdf.Graphics.PdfBrushes.Black, new PointF(0, 0));
            //}

            //loadedDocument.Save();

            //loadedDocument.Close();
            //loadedDocument.Dispose();

            try
            {

                string cliente = ws.ConsultarCliente(Convert.ToDecimal(_ClienteId));
                System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(cliente, (typeof(System.Data.DataTable)));
                DateTime DataHora = DateTime.Now;

                string SiteValidacao = "";

                if (!string.IsNullOrEmpty(dt.Rows[0]["CLISITEVALIDACAODOCS"].ToString()))
                {
                    SiteValidacao = dt.Rows[0]["CLISITEVALIDACAODOCS"] + "/ValidarDocumento.aspx";
                }
                else
                {
                    SiteValidacao = LinkSite + "/ValidarDocumento.aspx";
                }

                string Assinante = "";
                //string CNPJ = "";
                string TipoAssinatura = "";

                if (Certificado != null)
                {
                    string DiretorioBaseCertificado = AppDomain.CurrentDomain.BaseDirectory;

                    DiretorioBaseCertificado = DiretorioBaseCertificado.Substring(0, DiretorioBaseCertificado.Length - 1);
                    Assinante = Functions.getKey(Certificado.Subject, "CN");
                    TipoAssinatura = "Digital";

                }
                else
                {
                    Assinante = dt.Rows[0]["CLINOME"].ToString();
                    CertificadoCNPJ = dt.Rows[0]["CLICNPJ"].ToString();
                    TipoAssinatura = "Eletronicamente";
                }

                //try
                //{
                //    DataHora = DateTime.Now;
                //}
                //catch
                //{
                //    DataHora = DateTime.Now;
                //}


                Syncfusion.Pdf.PdfLoadedPage page = document.Pages[document.Pages.Count-1] as Syncfusion.Pdf.PdfLoadedPage;

                //PdfPageBase page = document.Pages[document.Pages.Count - 1];

                ChancelarQRCodeSync(page, SiteValidacao, HashValidacao, Rotacao, TipoAssinatura, DataHora, Assinante, CertificadoCNPJ);

                string Rodape = "";
                if (TipoAssinatura == "Digital")
                {
                    Rodape = "Documento Assinado digitalmente. Hash de Validação";
                }
                else
                {
                    Rodape = "Documento assinado eletronicamente. Hash de Validação";
                }

                int contador = 1;
                foreach (Syncfusion.Pdf.PdfLoadedPage pageCancela in document.Pages)
                {
                    EscreverRodapeSyn(pageCancela, HashValidacao, Rodape, document.Pages.Count, contador);
                }

                //document.Save();

                //document.Close();
                //document.Dispose();


                if (Certificado != null)
                {
                    CertificadoSelecionado = Certificado;
                    Syncfusion.Pdf.Security.PdfCertificate pdfCertificate = new Syncfusion.Pdf.Security.PdfCertificate(Certificado);

                    Syncfusion.Pdf.Security.PdfSignature signature = new Syncfusion.Pdf.Security.PdfSignature(document, page, null, "Signature");

                    //signature.TimeStampServer = new TimeStampServer(new Uri("https://act.bry.com.br"), "55930744025", "9o9Eo3pa");

                    signature.ContactInfo = "Assinatura Digital - ICP-Brasil";
                    signature.LocationInfo = "Brasília-DF";
                    signature.Reason = "Assinatura Digital - ICP-Brasil";

                    signature.EnableLtv = true;
                    //signature.ComputeHash += Signature_ComputeHash;
                    //signature.Visible = false;
                    PdfSignatureSettings settings = signature.Settings;
                    //signature.Settings.DigestAlgorithm = DigestAlgorithm.RIPEMD160;
                    signature.Settings.CryptographicStandard = CryptographicStandard.CADES;

                    signature.CreateLongTermValidity(new List<X509Certificate2> { Certificado });

                }

                document.Save();
                document.Close(true);
                document.Dispose();

                return Arquivo;
                

            }
            catch (Exception)
            {
                return "";
            }

        }

        //void Signature_ComputeHash(object sender, PdfSignatureEventArgs arguments)

        //{
        //    if (CertificadoSelecionado != null)
        //    {
        //        //Get the document bytes

        //        byte[] documentBytes = arguments.Data;

        //        SignedCms signedCms = new SignedCms(new ContentInfo(documentBytes), detached: true);

        //        //Compute the signature using the specified digital ID file and the password

        //        var cmsSigner = new CmsSigner(CertificadoSelecionado);

        //        //Set the digest algorithm SHA256

        //        cmsSigner.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1");


        //        signedCms.ComputeSignature(cmsSigner);

        //        //Embed the encoded digital signature to the PDF document

        //        arguments.SignedData = signedCms.Encode();
        //    }

        //}

        public void ChancelarQRCodeSync(Syncfusion.Pdf.PdfLoadedPage page, string SiteValidacao, string HashValidacao, string Rotacao, string TipoAssinatura, DateTime DataHora, string Assinante, string CNPJ)
        {
            try
            {

                Syncfusion.Pdf.Graphics.PdfTrueTypeFont font = new Syncfusion.Pdf.Graphics.PdfTrueTypeFont(new Font("Arial", 8f, FontStyle.Regular), true);

                QRCodeEncoder qrCodecEncoder = new QRCodeEncoder();
                qrCodecEncoder.QRCodeBackgroundColor = Color.White;
                qrCodecEncoder.QRCodeForegroundColor = Color.Black;
                qrCodecEncoder.CharacterSet = "UTF-8";
                qrCodecEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
                qrCodecEncoder.QRCodeScale = 6;
                qrCodecEncoder.QRCodeVersion = 0;
                qrCodecEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.Q;

                String data = SiteValidacao + "?hash=" + HashValidacao;
                System.Drawing.Image imageQRCode = qrCodecEncoder.Encode(data);

                Syncfusion.Pdf.Graphics.PdfGraphics graphics = page.Graphics;

                Syncfusion.Pdf.Graphics.PdfBitmap image = new Syncfusion.Pdf.Graphics.PdfBitmap(imageQRCode);
                
                graphics.DrawImage(image, 20, graphics.Size.Height - 57, 40, 40);

                StringBuilder st = new StringBuilder();
                st.Append("Documento Gerado e Assinado " + TipoAssinatura + " em " + DataHora.ToString("dd/MM/yyyy") + " às " + DataHora.ToString("HH:mm:ss") + " (data e hora de Brasília)." + Environment.NewLine);
                
                if(CNPJ.Length>=14)
                    st.Append("Dados do Assinante: " + Assinante + " - CNPJ: " + Functions.FormatarCpfCnpj(CNPJ) + Environment.NewLine);
                else
                    st.Append("Dados do Assinante: " + Assinante + " - CPF: " + Functions.FormatarCpfCnpj(CNPJ) + Environment.NewLine);

                st.Append("Código de Verificação: " + HashValidacao + Environment.NewLine);
                st.Append("Valide esse documento em: " + SiteValidacao + " Informando o código de verificação.");

                Syncfusion.Pdf.Graphics.PdfFont fontSync = font;
                graphics.DrawString(st.ToString(), fontSync, Syncfusion.Pdf.Graphics.PdfBrushes.Black, new PointF(65, graphics.Size.Height - 55));

            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private void EscreverRodapeSyn(Syncfusion.Pdf.PdfLoadedPage page, string hash, string Texto, int totalPagina, int paginaAtual)
        {
            string rodape = Texto + ": " + hash + " / Página " + paginaAtual + " de " + totalPagina;

            Syncfusion.Pdf.Graphics.PdfGraphics graphics = page.Graphics;
            Syncfusion.Pdf.Graphics.PdfFont fontSync = new Syncfusion.Pdf.Graphics.PdfStandardFont(Syncfusion.Pdf.Graphics.PdfFontFamily.Courier, 8);

            Syncfusion.Pdf.Graphics.PdfGraphicsState state = page.Graphics.Save();

            float x = graphics.ClientSize.Width - 15;
            float y = graphics.ClientSize.Height - 30;

            graphics.TranslateTransform(x, y);
            graphics.RotateTransform(270);

            RectangleF rectangleF = new RectangleF(0, 0, graphics.ClientSize.Width - 20, 30);

            graphics.DrawString(rodape, fontSync, Syncfusion.Pdf.Graphics.PdfBrushes.Black, rectangleF);
            page.Graphics.Restore(state);

            //            Pagina.Canvas.TranslateTransform(x, y);

            //          Pagina.Canvas.RotateTransform(270);
            //        Pagina.Canvas.DrawString(rodape, fontTexto, brush, 20, 0, centerAlignment);
        }
        public bool ValidaAssinatura(string Arquivo, string Subject)
        {
            bool existe = false;

            try
            {

                Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument();
                doc.LoadFromFile(Arquivo);
                List<Spire.Pdf.Security.PdfSignature> signatures = new List<Spire.Pdf.Security.PdfSignature>();

                if (doc.Form != null)
                {
                    var form = (PdfFormWidget)doc.Form;
                    for (int i = 0; i < form.FieldsWidget.Count; ++i)
                    {
                        var field = form.FieldsWidget[i] as PdfSignatureFieldWidget;

                        if (field != null && field.Signature != null)
                        {
                            Spire.Pdf.Security.PdfSignature signature = field.Signature;
                            signatures.Add(signature);
                        }
                    }

                    if (signatures.Count > 0)
                    {
                        foreach (Spire.Pdf.Security.PdfSignature cert in signatures)
                        {
                            if(cert.Certificate.Subject== Subject)
                            {
                                return true;
                            }
                            //string Dados = cert.Certificate.Subject;
                            //string CpfCnpjCertificado = Functions.getKey(Dados, "CNPJ");

                            //if (CPFCNPJ != "")
                            //{
                            //    if (CPFCNPJ == CpfCnpjCertificado)
                            //        existe = true;
                            //}
                            //else
                            //{
                            //    existe = true;
                            //}
                        }
                    }
                }

                return existe;
            }
            catch (Exception)
            {
                return existe;
            }
        }

        private string AdicionarPagina(string NomePDF, decimal DocumentoId, string addPage, int alturaTopo)
        {
            Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument(NomePDF);
            PdfPageBase ultimaPagina = document.Pages[document.Pages.Count - 1]; // PEGAR A ÚLTIMA PAGINA
            PdfPageBase novaPagina = null;
            string key = System.Configuration.ConfigurationManager.AppSettings["key"];
            string hash = Functions.EncryptParametroURL(DocumentoId.ToString(), "916FBB14").ToUpper();

            if (addPage == "S")
            {
                novaPagina = document.Pages.Insert(document.Pages.Count, ultimaPagina.Size); // ADICIONA UMA PÁGINA NO FINAL DO DOCUMENTO COM O MESMO TAMANHO DA ÚLTIMA PÁGINA

            }
            else
            {
                novaPagina = document.Pages[document.Pages.Count - 1];
            }


            string Titulo = "ASSINATURA(S) ELETRÔNICA(S)";
            string Rodape = "Documento assinado digitalmente conforme anexo. Hash de Validação";

            EscreverTitulo(novaPagina, hash, Titulo, addPage, alturaTopo, document.Pages.Count);
            //EscreverRodape(hash, ultimaPagina, Rodape);

            if (addPage == "S")
            {
                int contador = 1;
                foreach (PdfPageBase page in document.Pages)
                {
                    if (contador < document.Pages.Count)
                    {
                        EscreverRodape(hash, page, Rodape, document.Pages.Count, contador);
                    }
                    contador++;
                }
            }
            else
            {
                int contador = 1;
                foreach (PdfPageBase page in document.Pages)
                {
                    if (contador <= document.Pages.Count)
                    {
                        EscreverRodape(hash, page, Rodape, document.Pages.Count, contador);
                    }
                    contador++;
                }
            }


            string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
            string pdfTemp = DiretorioTemp + Guid.NewGuid().ToString() + ".pdf";

            document.SaveToFile(pdfTemp);
            document.Close();
            document.Dispose();

            return pdfTemp;
        }

        private void EscreverTitulo(Spire.Pdf.PdfPageBase page, string hash, string Titulo, string addPage, int alturaTopo, int totalPaginas)
        {
            string LinkSite = URLRetorno;
            if (string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

            //Draw the text - alignment
            Spire.Pdf.Graphics.PdfFont fontTitulo = new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.Helvetica, 15f);
            Spire.Pdf.Graphics.PdfFont fontTexto = new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.Helvetica, 9f);
            PdfSolidBrush brush = new PdfSolidBrush(Color.Black);

            int tamanhoY = 10;

            int tamanhoYQR = 25;

            int espacoLado = 0;

            PdfStringFormat centerAlignment = new PdfStringFormat(PdfTextAlignment.Center, PdfVerticalAlignment.Middle);
            PdfStringFormat leftAlignment = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
            if (addPage == "N")
            {
                tamanhoY = alturaTopo;
                tamanhoYQR = alturaTopo + 10;
                espacoLado = 20;


                //page.Canvas.DrawString(Titulo, fontTitulo, brush, page.Canvas.ClientSize.Width / 2, tamanhoY, centerAlignment);
                page.Canvas.DrawString("A autenticidade do documento pode ser conferida no site:", fontTexto, brush, 80, tamanhoY + 30, leftAlignment);
            }
            else
            {
                page.Canvas.DrawString(Titulo, fontTitulo, brush, page.Canvas.ClientSize.Width / 2, tamanhoY, centerAlignment);
                page.Canvas.DrawString("A autenticidade do documento pode ser conferida no site:", fontTexto, brush, 70, tamanhoY + 30, leftAlignment);
            }

            WSDocs2 ws = new WSDocs2();
            AuthHeader auth = new AuthHeader();
            auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
            ws.AuthHeaderValue = auth;
            ws.Url = LinkSite + "/WsDocs2.asmx";
            ws.Proxy = Functions.PegarProxy();

            string cliente = ws.ConsultarCliente(Convert.ToDecimal(_ClienteId));
            System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(cliente, (typeof(System.Data.DataTable)));

            

            if (!string.IsNullOrEmpty(dt.Rows[0]["CLISITEVALIDACAODOCS"].ToString()))
            {
                LinkSite = dt.Rows[0]["CLISITEVALIDACAODOCS"] + "/ValidarDocumento.aspx";
            }
            else
            {
                LinkSite = LinkSite + "/ValidarDocumento.aspx";
            }

            if (addPage == "N")
            {
                page.Canvas.DrawString(LinkSite, fontTexto, brush, 80, tamanhoY + 40, leftAlignment);
                page.Canvas.DrawString("informando o código CRC: " + hash, fontTexto, brush, 80, tamanhoY + 50, leftAlignment);
            }
            else
            {
                page.Canvas.DrawString(LinkSite, fontTexto, brush, 70, tamanhoY + 40, leftAlignment);
                page.Canvas.DrawString("informando o código CRC: " + hash + "  / Página " + totalPaginas + " de " + totalPaginas, fontTexto, brush, 70, tamanhoY + 50, leftAlignment);
            }



            QRCodeEncoder qrCodecEncoder = new QRCodeEncoder();
            qrCodecEncoder.QRCodeBackgroundColor = System.Drawing.Color.White;
            qrCodecEncoder.QRCodeForegroundColor = System.Drawing.Color.Black;
            qrCodecEncoder.CharacterSet = "UTF-8";
            qrCodecEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            qrCodecEncoder.QRCodeScale = 6;
            qrCodecEncoder.QRCodeVersion = 0;
            qrCodecEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.Q;

            System.Drawing.Image imageQRCode;
            //string a ser gerada

            string LinkValida = "";

            if (!string.IsNullOrEmpty(dt.Rows[0]["CLISITEVALIDACAODOCS"].ToString()))
            {
                LinkValida = dt.Rows[0]["CLISITEVALIDACAODOCS"] + "/ValidarDocumento.aspx";
            }
            else
            {
                LinkValida = LinkSite + "/ValidarDocumento.aspx";
            }


            String data = LinkValida + "/ValidarDocumento.aspx?hash=" + hash;
            imageQRCode = qrCodecEncoder.Encode(data);

            Spire.Pdf.Graphics.PdfImage image = Spire.Pdf.Graphics.PdfImage.FromImage(imageQRCode);

            page.Canvas.DrawImage(image, espacoLado, tamanhoYQR, 50, 50);

        }

        private void EscreverRodape(string hash, Spire.Pdf.PdfPageBase Pagina, string Texto, int totalPagina, int paginaAtual)
        {
            Spire.Pdf.Graphics.PdfFont fontTexto = new Spire.Pdf.Graphics.PdfFont(PdfFontFamily.Courier, 8f);
            PdfSolidBrush brush = new PdfSolidBrush(Color.Black);
            string rodape = Texto + ": " + hash + " / Página " + paginaAtual + " de " + totalPagina;

            PdfStringFormat centerAlignment = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);

            float x = Pagina.Canvas.ClientSize.Width - 10;
            float y = Pagina.Size.Height - 30;
            Pagina.Canvas.TranslateTransform(x, y);

            Pagina.Canvas.RotateTransform(270);
            Pagina.Canvas.DrawString(rodape, fontTexto, brush, 20, 0, centerAlignment);
        }

        

        public int VerificarQuantidadeAssinaturas(string Arquivo)
        {
            try
            {
                Spire.Pdf.PdfDocument doc = new Spire.Pdf.PdfDocument();
                doc.LoadFromFile(Arquivo);
                List<Spire.Pdf.Security.PdfSignature> signatures = new List<Spire.Pdf.Security.PdfSignature>();

                if (doc.Form != null)
                {
                    var form = (Spire.Pdf.Widget.PdfFormWidget)doc.Form;
                    for (int i = 0; i < form.FieldsWidget.Count; ++i)
                    {
                        var field = form.FieldsWidget[i] as Spire.Pdf.Widget.PdfSignatureFieldWidget;

                        if (field != null && field.Signature != null)
                        {
                            Spire.Pdf.Security.PdfSignature signature = field.Signature;
                            signatures.Add(signature);
                        }
                    }
                }

                return signatures.Count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void frmPrincipal_Resize(object sender, EventArgs e)
        {
            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(500);
                this.ShowInTaskbar = true;
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void frmPrincipal_FormClosing(object sender, FormClosingEventArgs e)
        {
            //this.Invoke(new Action(() => this.grdDocumento.Controls.Clear()));
            //webViewerPDF.Navigate("");
            this.WindowState = FormWindowState.Minimized;
            //to hide from taskbar
            this.Hide();
            LimparTemporario();
            e.Cancel = !Sair;
        }

        public void LimparTemporario()
        {
            try
            {
                if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"TempImagens"))
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"TempImagens", true);
            }
            catch
            {

            }
        }
        private void btnFechar_Click(object sender, EventArgs e)
        {
            //webViewerPDF.Navigate("");
            //this.Invoke(new Action(() => this.grdDocumento.Controls.Clear()));
            frmPrincipal.DadosRecebido = "";
            timer.Start();
            this.WindowState = FormWindowState.Minimized;
            //to hide from taskbar

            LimparTemporario();

            this.Hide();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Sair = true;
            this.Close();
            Environment.Exit(0);
            Application.Exit();
        }

        private void btnConfiguracoes_Click(object sender, EventArgs e)
        {
            new frmConfig().ShowDialog();

            string LinkSite = URLRetorno;
            if (string.IsNullOrEmpty(LinkSite))
                LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

            lblURL.Text = LinkSite;
        }

        private void btnRecarregar_Click(object sender, EventArgs e)
        {
            cboCertificados.Items.Clear();
            CarregarCertificados();
        }
    }
}
