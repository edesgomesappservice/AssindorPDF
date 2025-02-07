//using Chilkat;
using Newtonsoft.Json;
using SDK.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CloudDocs.AssinadorDigital
{
    public static class Functions
    {


        public static bool GravaLog(string Valor)
        {

            string path = AppDomain.CurrentDomain.BaseDirectory + "Log";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            string Arquivo = path + "/Log_" + DateTime.Now.Year + "." + DateTime.Now.Month + "." + DateTime.Now.Day + ".txt";

            try
            {

                if (!System.IO.File.Exists(Arquivo))
                {
                    using (System.IO.StreamWriter sw = System.IO.File.CreateText(Arquivo))
                    {
                        sw.WriteLine(Valor);
                    }
                    return true;
                }
                else
                {
                    using (System.IO.StreamWriter sw = System.IO.File.AppendText(Arquivo))
                    {
                        sw.WriteLine(Valor);
                    }
                    return true;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
                return false;
            }
        }

        public static string[] GetCrlDistributionPoints(this X509Certificate2 certificate)
        {
            System.Security.Cryptography.X509Certificates.X509Extension ext = certificate.Extensions.Cast<System.Security.Cryptography.X509Certificates.X509Extension>().FirstOrDefault(
                e => e.Oid.Value == "2.5.29.31");

            if (ext == null || ext.RawData == null || ext.RawData.Length < 11)
                return EmptyStrings;

            int prev = -2;
            List<string> items = new List<string>();
            while (prev != -1 && ext.RawData.Length > prev + 1)
            {
                int next = IndexOf(ext.RawData, 0x86, prev == -2 ? 8 : prev + 1);
                if (next == -1)
                {
                    if (prev >= 0)
                    {
                        string item = Encoding.UTF8.GetString(ext.RawData, prev + 2, ext.RawData.Length - (prev + 2));
                        items.Add(item);
                    }

                    break;
                }

                if (prev >= 0 && next > prev)
                {
                    string item = Encoding.UTF8.GetString(ext.RawData, prev + 2, next - (prev + 2));
                    items.Add(item);
                }

                prev = next;
            }

            return items.ToArray();
        }

        private static int IndexOf(byte[] instance, byte item, int start)
        {
            for (int i = start, l = instance.Length; i < l; i++)
                if (instance[i] == item)
                    return i;

            return -1;
        }

        private static string[] EmptyStrings = new string[0];

        public static List<X509Certificate2> GetCurrentUserCertificates()
        {
            List<X509Certificate2> certificates = new List<X509Certificate2>();
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly);
            foreach (X509Certificate2 cert in store.Certificates)
            {
                if (cert.Subject.ToUpper().Contains("ICP-BRASIL"))
                {
                    //if (IsFromSmartCard(cert))
                        certificates.Add(cert);
                }
            }
            return certificates;
        }

        public static string PegarCPFCertificado(X509Certificate2 Certificado)
        {
            try
            {
                var retorno = GetSubjectAlternativeNames(Certificado);
                return retorno;
                /*Chilkat.Global glob = new Chilkat.Global();
                bool success = glob.UnlockBundle("APPSVC.CB1102020_CMvTGxJzpD1l");

                Chilkat.Cert certStore = new Cert();

                certStore.LoadFromBinary(Certificado.RawData);

                string subjectAltNameXml = certStore.Rfc822Name;

                GravaLog(subjectAltNameXml);

                if (!string.IsNullOrEmpty(subjectAltNameXml) & subjectAltNameXml != "localhost")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(subjectAltNameXml);

                    string json = JsonConvert.SerializeXmlNode(doc);

                    dynamic DadosCertificados = JsonConvert.DeserializeObject<dynamic>(json);

                    string DadosPessoa = DadosCertificados.sequence.contextSpecific[0].contextSpecific.octets.Value;
                    string DadosEmpresa = DadosCertificados.sequence.contextSpecific[2].contextSpecific.octets.Value;

                    DadosPessoa = Base64StringDecode(DadosPessoa);
                    DadosEmpresa = Base64StringDecode(DadosEmpresa);
                    if (Conversion.ToDecimal(DadosEmpresa) > 0)
                        return DadosEmpresa; //eCNPJ

                    string CPF = DadosPessoa.Substring(8, 11);

                    return CPF;
                }
                return "";*/
            }
            catch (Exception ex)
            {
                GravaLog(ex.Message);
            }
            return "";
        }

        public static string GetSubjectAlternativeNames(X509Certificate2 cert)
        {
            var subjectAlternativeNames = cert.Extensions.Cast<X509Extension>()
                                                .Where(n => n.Oid.Value == "2.5.29.17") //Subject Alternative Name
                                                .Select(n => new AsnEncodedData(n.Oid, n.RawData))
                                                .Select(n => n.Format(true))
                                                .FirstOrDefault();

            var delimiters = new char[] { ':' };
            var pairs = subjectAlternativeNames.Split(new[] { ",", "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var ChaveCPF = "2.16.76.1.3.1";
            var ChaveCNPJ = "2.16.76.1.3.3";

            foreach (var pair in pairs)
            {
                if (pair.Contains(ChaveCPF))
                {
                    var cpf = pair.Replace("=", "").Replace(ChaveCPF, "").Trim();
                    string[] words = cpf.Split(' ');
                    IList<byte> data = new List<byte>();
                    string stringValue = "";
                    foreach (string word in words)
                    {
                        int value = Convert.ToInt32(word, 16);
                        if (value >= 30)
                            stringValue += Char.ConvertFromUtf32(value);

                    }

                    if (stringValue.Length > 0)
                    {
                        return stringValue.Substring(9, 11);
                    }
                }
                else if (pair.Contains(ChaveCNPJ))
                {
                    var cpf = pair.Replace("=", "").Replace(ChaveCNPJ, "").Trim();
                    string[] words = cpf.Split(' ');
                    IList<byte> data = new List<byte>();
                    string stringValue = "";
                    foreach (string word in words)
                    {
                        int value = Convert.ToInt32(word, 16);
                        if (value >= 30)
                            stringValue += Char.ConvertFromUtf32(value);
                    }

                    if (stringValue.Length > 0)
                    {
                        return stringValue;
                    }
                }
            }
            return "";
        }

        public static string Base64StringDecode(string encodedString)
        {
            var bytes = Convert.FromBase64String(encodedString);

            var decodedString = Encoding.UTF8.GetString(bytes);

            return decodedString;
        }


        public static bool IsFromSmartCard(X509Certificate2 certificate)
        {
            try
            {
                bool result = (certificate.HasPrivateKey);
                if (result)
                {
                    RSACryptoServiceProvider rsa = certificate.PrivateKey as RSACryptoServiceProvider;
                    if (rsa != null && rsa.CspKeyContainerInfo.HardwareDevice)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                return result;
            }
            catch
            {
                return false;
            }
        }

        public static string EncryptParametroURL(string Valor, string ChaveAcesso)
        {
            return Functions.ConvertStringToHex(EncryptDecryptQueryString.Encrypt(Valor, ChaveAcesso));
        }
        public static string DecryptParametroURL(string Valor, string ChaveAcesso)
        {
            return EncryptDecryptQueryString.Decrypt(Functions.ConvertHexToString(Valor), ChaveAcesso);
        }

        public static IList<string> ListUF = new List<string>(){"AC",
                                                    "AL",
                                                    "AM",
                                                    "AP",
                                                    "BA",
                                                    "CE",
                                                    "DF",
                                                    "ES",
                                                    "GO",
                                                    "MA",
                                                    "MG",
                                                    "MS",
                                                    "MT",
                                                    "PA",
                                                    "PB",
                                                    "PE",
                                                    "PI",
                                                    "PR",
                                                    "RJ",
                                                    "RN",
                                                    "RO",
                                                    "RR",
                                                    "RS",
                                                    "SC",
                                                    "SE",
                                                    "pf",
                                                    "TO"};

        public static string getKey(string Valor, string Chave)
        {
            string retorno = "";
            if (Chave == "CN")
            {
                try
                {
                    retorno = Valor.Substring(Valor.IndexOf("CN") + 3, Valor.IndexOf(":") - 3);
                }
                catch (Exception)
                {
                    retorno = "";
                }
            }
            if (Chave == "CNPJ")
            {
                try
                {
                    retorno = Valor.Substring(Valor.IndexOf(":") + 1, 14);

                    decimal cnpj = Conversion.ToDecimal(retorno);

                    if (cnpj == 0)
                    {
                        retorno = Valor.Substring(Valor.IndexOf(":") + 1);
                        retorno = retorno.Substring(0, retorno.IndexOf(","));
                        cnpj = Conversion.ToDecimal(retorno);
                    }
                    if (cnpj == 0)
                        retorno = "";
                }
                catch (Exception)
                {
                    retorno = "";
                }
                retorno = FormatarCpfCnpj(retorno);

            }
            else if (Chave == "UF")
            {
                try
                {
                    retorno = Valor.Substring(Valor.IndexOf("S=") + 2, 2);

                    if (!ListUF.Contains(retorno))
                        retorno = "";
                }
                catch (Exception)
                {
                    retorno = "";
                }
            }

            return retorno;
        }

        public static string FormatarCpfCnpj(string strCpfCnpj)
        {
            if (strCpfCnpj.Length <= 11)
            {
                MaskedTextProvider mtpCpf = new MaskedTextProvider(@"000\.000\.000-00");
                mtpCpf.Set(ZerosEsquerda(strCpfCnpj, 11));
                return mtpCpf.ToString();
            }
            else
            {
                MaskedTextProvider mtpCnpj = new MaskedTextProvider(@"00\.000\.000/0000-00");
                mtpCnpj.Set(ZerosEsquerda(strCpfCnpj, 14));
                return mtpCnpj.ToString();
            }
        }

        public static string ZerosEsquerda(string strString, int intTamanho)

        {

            string strResult = "";

            for (int intCont = 1; intCont <= (intTamanho - strString.Length); intCont++)

            {

                strResult += "0";

            }

            return strResult + strString;

        }

        public static string ConvertStringToHex(string asciiString)
        {
            string hex = "";
            foreach (char c in asciiString)
            {
                int tmp = c;
                hex += String.Format("{0:x2}", (uint)System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }

        public static string ConvertHexToString(string HexValue)
        {
            string StrValue = "";
            while (HexValue.Length > 0)
            {
                StrValue += System.Convert.ToChar(System.Convert.ToUInt32(HexValue.Substring(0, 2), 16)).ToString();
                HexValue = HexValue.Substring(2, HexValue.Length - 2);
            }
            return StrValue;
        }

        public static WebProxy PegarProxy()
        {
            string Key = System.Configuration.ConfigurationManager.AppSettings["Key"];

            WebProxy proxy = new WebProxy();
            string Proxy = System.Configuration.ConfigurationManager.AppSettings["Proxy"];
            string Endereco = System.Configuration.ConfigurationManager.AppSettings["Endereco"];
            string Login = System.Configuration.ConfigurationManager.AppSettings["LoginProxy"];
            string SenhaProxy = SDK.Util.EncryptDecryptQueryString.Decrypt(System.Configuration.ConfigurationManager.AppSettings["SenhaProxy"], Key.Substring(0, 8));
            string Dominio = System.Configuration.ConfigurationManager.AppSettings["Dominio"];

            proxy = new WebProxy();
            proxy.Credentials = CredentialCache.DefaultCredentials;

            if (Endereco != "")
            {
                proxy = new WebProxy(Endereco, true);
                proxy.Credentials = new NetworkCredential(Login, SenhaProxy, Dominio);
            }

            return proxy;
        }
    }
}
