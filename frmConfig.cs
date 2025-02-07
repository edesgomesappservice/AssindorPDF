using SDK.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CloudDocs.AssinadorDigital.WsDocs2;
namespace CloudDocs.AssinadorDigital
{
    public partial class frmConfig : Form
    {
        public frmConfig()
        {
            InitializeComponent();
        }

        private void frmConfig_Load(object sender, EventArgs e)
        {
            txtServidorAcesso.Text = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];
            string Key = System.Configuration.ConfigurationManager.AppSettings["Key"];

            if (System.Configuration.ConfigurationManager.AppSettings["Endereco"] != "")
            {
                rdDefinir.Checked = true;
                rdConfigDefault.Checked = false;
            }
            else
            {
                rdDefinir.Checked = false;
                rdConfigDefault.Checked = true;
            }

            txtEnderecoProxy.Text = System.Configuration.ConfigurationManager.AppSettings["Endereco"];

            txtLogin.Text = System.Configuration.ConfigurationManager.AppSettings["LoginProxy"];

            if (System.Configuration.ConfigurationManager.AppSettings["SenhaProxy"] != "")
                txtSenha.Text = SDK.Util.EncryptDecryptQueryString.Decrypt(System.Configuration.ConfigurationManager.AppSettings["SenhaProxy"], Key.Substring(0, 8));

            txtDominio.Text = System.Configuration.ConfigurationManager.AppSettings["Dominio"];
        }

        private void btnTeste_Click(object sender, EventArgs e)
        {

            string Key = System.Configuration.ConfigurationManager.AppSettings["Key"];

            CloudDocs.AssinadorDigital.WsDocs2.WSDocs2 ws = new CloudDocs.AssinadorDigital.WsDocs2.WSDocs2();
            ws.Url = txtServidorAcesso.Text + "/WsDocs2.asmx";
            CloudDocs.AssinadorDigital.WsDocs2.AuthHeader authentication = new CloudDocs.AssinadorDigital.WsDocs2.AuthHeader();

            authentication.Key = Key;
            ws.AuthHeaderValue = authentication;

            string Proxy = "";
            string Endereco = "";
            string Login = "";
            string SenhaProxy = "";
            string Dominio = "";

            Proxy = rdConfigDefault.Checked.ToString();
            Endereco = txtEnderecoProxy.Text;
            Login = txtLogin.Text;
            SenhaProxy = txtSenha.Text;
            Dominio = txtDominio.Text;

            if (Conversion.ToBoolean(Proxy))
            {
                WebProxy proxy = new WebProxy();
                proxy.Credentials = CredentialCache.DefaultCredentials;

                ws.Proxy = proxy;
            }
            else
            {
                if (Endereco != null)
                {
                    var proxy = new WebProxy(Endereco, true);
                    proxy.Credentials = new NetworkCredential(Login, SenhaProxy, Dominio);
                    WebRequest.DefaultWebProxy = proxy;
                    ws.Proxy = proxy;
                }
            }

            string Configuracao = ws.BuscaConfiguracaoSistema();

            MessageBox.Show("Configurações realizadas com sucesso!");

            System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["LinkSite"].Value = txtServidorAcesso.Text;
            //config.AppSettings.Settings["Proxy"].Value = rdConfigDefault.Checked.ToString();
            config.AppSettings.Settings["Endereco"].Value = txtEnderecoProxy.Text;
            config.AppSettings.Settings["LoginProxy"].Value = txtLogin.Text;
            config.AppSettings.Settings["SenhaProxy"].Value = SDK.Util.EncryptDecryptQueryString.Encrypt(txtSenha.Text, Key.Substring(0, 8));
            config.AppSettings.Settings["Dominio"].Value = txtDominio.Text;

            config.Save(ConfigurationSaveMode.Modified);

            ConfigurationManager.RefreshSection("appSettings");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
