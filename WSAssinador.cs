using CloudDocs.AssinadorDigital.WsDocs2;
using iTextSharp.text.exceptions;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using iTextSharp.text.pdf.security;
using MessagingToolkit.QRCode.Codec;
using Newtonsoft.Json;
using SDK.Util;
using Spire.Pdf;
using Spire.Pdf.Graphics;
using Spire.Pdf.Widget;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace CloudDocs.AssinadorDigital
{
    public class WSAssinador
    {
        public static string LinkSite = "";

        public class wsListarCertificados : WebSocketBehavior
        {
            protected override void OnMessage(MessageEventArgs e)
            {
                Send(frmPrincipal.ListarCertificados());
            }
        }

        public class ReceberDocumentos : WebSocketBehavior
        {
            public class Dados
            {
                public string URL { get; set; }
                public string DocId { get; set; }
                public string ClienteId { get; set; }
                public string UsuarioId { get; set; }
                public string Certificado { get; set; }
            }
            protected override void OnMessage(MessageEventArgs e)
            {
                try
                {
                    //string[] Dados = e.Data.Split('|');

                    //AssinarDocumento(Dados[0], Dados[1], Dados[2], Dados[3]);

                    Dados dados = JsonConvert.DeserializeObject<Dados>(e.Data);

                    if (string.IsNullOrEmpty(dados.Certificado))
                    {
                        Functions.GravaLog("É obrigatório selecionar o certificado A3");
                        Send("É obrigatório selecionar o certificado A3");
                        return;
                    }


                    LinkSite = dados.URL;
                    if (string.IsNullOrEmpty(LinkSite))
                        LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

                    System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    config.AppSettings.Settings["LinkSite"].Value = LinkSite;
                    config.Save(ConfigurationSaveMode.Modified);

                    LimparTemporario();

                    ConfigurationManager.RefreshSection("appSettings");

                    foreach (string DocId in dados.DocId.Split(','))
                    {
                        string NomeArquivo = MostrarDocumento(DocId, dados.ClienteId, dados.UsuarioId, "");

                        if (string.IsNullOrEmpty(NomeArquivo))
                            continue;//tentar o próximo;

                        try
                        {
                            WSDocs2 ws = new WSDocs2();
                            AuthHeader auth = new AuthHeader();
                            auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
                            ws.AuthHeaderValue = auth;
                            ws.Url = LinkSite + "/WsDocs2.asmx";
                            ws.Proxy = Functions.PegarProxy();

                            string nomeCompleto = "";
                            string profissao = "";

                            USUARIO usuario = ws.BuscarUsuario(Conversion.ToDecimal(dados.UsuarioId));
                            nomeCompleto = usuario.USUNOMECOMPLETO;

                            string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
                            string PDFSaida = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);


                            X509Certificate2 Certificado = PegarCertificados(dados.Certificado);

                            string cpfCnpjCertificado = Functions.PegarCPFCertificado(Certificado);

                            Functions.GravaLog("CPF: - " + cpfCnpjCertificado);

                            if (ValidaAssinatura(PDFSaida, Certificado.Subject))
                            {
                                Functions.GravaLog("Esse documento já foi assinado por esse certificado");
                                //continue;
                                Send("Esse documento já foi assinado por esse certificado! " + Certificado.Subject);
                                return;
                            }


                            string SolicitacaoDocumento = ws.ConsultarSolicitacaoDocumentoAberta(Convert.ToDecimal(DocId));
                            System.Data.DataTable dtSolicitacao = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(SolicitacaoDocumento, (typeof(System.Data.DataTable)));
                            if (dtSolicitacao != null && dtSolicitacao.Rows.Count > 0)
                            {
                                Functions.GravaLog("O documento não pode ser assinado digitalmente porque existem solicitações de assinatura eletrônica para ele.");
                                Send("O documento não pode ser assinado digitalmente porque existem solicitações de assinatura eletrônica para ele.");
                                
                                //continue;
                                return;
                            }

                            int qtdAssinaturaDigital = VerificarQuantidadeAssinaturas(PDFSaida);
                            string assinaturasEletronicas = ws.ConsultarAssinaturas(Convert.ToDecimal(DocId));
                            System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(assinaturasEletronicas, (typeof(System.Data.DataTable)));

                            int totalAssinaturasEletronicas = 0;
                            if (dt != null && dt.Rows.Count > 0)
                            {
                                totalAssinaturasEletronicas = dt.Rows.Count;
                            }
                            int totalAssinaturas = 0;
                            totalAssinaturas = totalAssinaturas + totalAssinaturasEletronicas + qtdAssinaturaDigital;

                            string pdfDestino = DiretorioTemp + Guid.NewGuid().ToString() + ".pdf";

                            string tpdid = ws.ConsultarDocumento(Convert.ToDecimal(DocId));

                            string TipoDocumento = ws.ConsultarTipoDocumento(Convert.ToInt32(tpdid));

                            if (qtdAssinaturaDigital == 0)
                            {
                                ConverterPDF(PDFSaida);
                            }

                            System.Data.DataTable dtTipoDocumento = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(TipoDocumento, (typeof(System.Data.DataTable)));

                            if (dtTipoDocumento != null && dtTipoDocumento.Rows.Count > 0 && Conversion.ToBoolean(dtTipoDocumento.Rows[0]["tpdFotoRosto"].ToString()))
                            {
                                //pdfDestino = AssinarDocumentosRodaPeSync(PDFSaida, Conversion.ToInteger(_ClienteId), Convert.ToDecimal(_DocId), Convert.ToDecimal(_UsuarioId), "", cpfCnpjCertificado, "0", Certificado);
                                pdfDestino = AssinarDocumentosRodaPe(PDFSaida, Conversion.ToInteger(dados.ClienteId), Convert.ToDecimal(DocId), Convert.ToDecimal(dados.UsuarioId), "", cpfCnpjCertificado, "0", dados.URL, Certificado);
                            }
                            else
                            {

                                string Temp = "";

                                string hash = Functions.EncryptParametroURL(DocId, ConfigurationManager.AppSettings["Key"]).ToUpper();


                                bool multiplasAssinaturas = ws.MultiplasAssinaturas(Convert.ToDecimal(DocId));


                                int alturaTopo = 170;
                                alturaTopo = alturaTopo + (50 * (totalAssinaturas));
                                int alturaTopoTitulo = 0;

                                int espacoLado = -400;

                                if (multiplasAssinaturas)
                                {
                                    if (totalAssinaturas == 0)
                                    {
                                        //System.IO.File.Copy(dest, pdfDestino, true);
                                        Temp = AdicionarPagina(dados.ClienteId, PDFSaida, Convert.ToDecimal(DocId), "S", 0, dados.URL);
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
                                        //Send("Esse documento só permite uma assinatura e já foi assinado");
                                        //return;
                                        continue;
                                    }

                                    Temp = PDFSaida;

                                    float llx = 0;
                                    float lly = 0;
                                    float urx = 0;
                                    //float ury = 0;

                                    var pdfReaderCount = new PdfReader(new RandomAccessFileOrArray(Temp), null);
                                    var contentParser = new PdfReaderContentParser(pdfReaderCount);

                                    string pdfDestino2 = DiretorioTemp + Guid.NewGuid().ToString() + ".pdf";

                                    try
                                    {
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
                                    }
                                    catch(Exception ex)
                                    {
                                        Functions.GravaLog(ex.Message);
                                    }

                                    Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument(Temp);
                                    PdfPageBase ultimaPagina = document.Pages[document.Pages.Count - 1]; // PEGAR A ÚLTIMA PAGINA

                                    float espacamento = (ultimaPagina.Size.Height - lly) + 20;
                                    float espacamentoAssinatura = ultimaPagina.Size.Height - espacamento;

                                    if (espacamentoAssinatura < 150)
                                    {
                                        Temp = AdicionarPagina(dados.ClienteId, PDFSaida, Convert.ToDecimal(DocId), "S", 0, dados.URL);

                                    }
                                    else
                                    {
                                        alturaTopo = Conversion.ToInteger(ultimaPagina.Size.Height - lly) + 105;

                                        alturaTopoTitulo = Conversion.ToInteger(ultimaPagina.Size.Height - lly);

                                        Temp = AdicionarPagina(dados.ClienteId, PDFSaida, Convert.ToDecimal(DocId), "N", alturaTopoTitulo, dados.URL);

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

                            string Estrutura = dados.ClienteId.ToString().PadLeft(6, '0') + "/" + DateTime.Now.ToString("yyyyMMdd");

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
                            catch (Exception ex)
                            {
                                Functions.GravaLog(ex.Message); 
                                fileStream.Close();
                            }
                            finally
                            {
                                fileStream.Close();
                            }

                            if (!ws.VerificaArquivo(Estrutura, nomeArquivoCry, fsiCry.Length))
                            {
                                //Send("Não foi possivel enviar o arquivo para o storage.");
                                continue;
                            }
                            else
                            {
                                try
                                {
                                    File.Delete(fsi.DirectoryName + "/" + nomeArquivoCry);
                                }
                                catch { }

                                string TrasmitirArquivo = ws.CriarNovaVersaoAssinatura(Estrutura + "/" + nomeArquivoCry, Convert.ToDecimal(DocId), Convert.ToInt32(dados.ClienteId), nomeArquivoCry, FileSize, iTotalPaginas, Convert.ToDecimal(dados.UsuarioId), "1");

                                if (TrasmitirArquivo.Contains("Sucesso"))
                                {
                                    string docId = TrasmitirArquivo.Split('|')[1];

                                    //MostrarDocumento(docId, _ClienteId, _UsuarioId, _OrigemUser);

                                    //responseString = "Atualizar";

                                    //Send("Documento Assinado com sucesso!");

                                }
                                else
                                {
                                    //MessageBox.Show(TrasmitirArquivo);
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            Functions.GravaLog(ex.Message);
                            Send("Não foi possivel assinar o documento com o certificado selecionado.");
                        }
                        finally
                        {
                            //DadosRecebido = "";
                            //timer.Start();
                        }
                    }

                    Send("Documento Assinado com sucesso!");
                }
                catch (Exception ex)
                {
                    Functions.GravaLog(ex.Message);
                }

            }

            public static void LimparTemporario()
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

            public string AssinarDocumentosRodaPe(string Arquivo, int ClienteId, decimal DocumentoId, decimal UsuarioId, string IpAcesso, string CertificadoCNPJ, string Rotacao, string URLRetorno, X509Certificate2 Certificado = null)
            {
                //INICO:

                LinkSite = URLRetorno;

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

                    string cliente = ws.ConsultarCliente(Convert.ToDecimal(ClienteId));
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

            private string AdicionarPagina(string ClienteId, string NomePDF, decimal DocumentoId, string addPage, int alturaTopo, string URLRetorno)
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

                EscreverTitulo(ClienteId, novaPagina, hash, Titulo, addPage, alturaTopo, document.Pages.Count, URLRetorno);
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

            private void EscreverTitulo(string ClienteId, Spire.Pdf.PdfPageBase page, string hash, string Titulo, string addPage, int alturaTopo, int totalPaginas, string URLRetorno)
            {
                LinkSite = URLRetorno;
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

                string cliente = ws.ConsultarCliente(Convert.ToDecimal(ClienteId));
                System.Data.DataTable dt = (System.Data.DataTable)Newtonsoft.Json.JsonConvert.DeserializeObject(cliente, (typeof(System.Data.DataTable)));

                string SiteValidacao = "";

                if (!string.IsNullOrEmpty(dt.Rows[0]["CLISITEVALIDACAODOCS"].ToString()))
                {
                    SiteValidacao = dt.Rows[0]["CLISITEVALIDACAODOCS"] + "/ValidarDocumento.aspx";
                }
                else
                {
                    SiteValidacao = LinkSite + "/ValidarDocumento.aspx";
                }

                if (addPage == "N")
                {
                    page.Canvas.DrawString(SiteValidacao, fontTexto, brush, 80, tamanhoY + 40, leftAlignment);
                    page.Canvas.DrawString("informando o código CRC: " + hash, fontTexto, brush, 80, tamanhoY + 50, leftAlignment);
                }
                else
                {
                    page.Canvas.DrawString(SiteValidacao, fontTexto, brush, 70, tamanhoY + 40, leftAlignment);
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
                                if (cert.Certificate.Subject == Subject)
                                {
                                    return true;
                                }
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


            public void AssinarDocumento(string DocumentoId, string ClienteId, string UsuarioId, string Certificado)
            {
                //string LinkSite = "http://localhost:53396/";
                if (string.IsNullOrEmpty(LinkSite))
                    LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

                string EnderecoCloudDocs = LinkSite + "/ConverterDocumento.aspx?docId=" + DocumentoId + "&clienteid=" + ClienteId;

                string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
                System.IO.Directory.CreateDirectory(DiretorioTemp);

                string dest = "";

                string NomeArquivo = Guid.NewGuid().ToString().ToUpper() + ".pdf";

                dest = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);

                WebClient webClient = new WebClient();
                webClient.UseDefaultCredentials = true;
                webClient.DownloadFile(EnderecoCloudDocs, dest);

                WSDocs2 ws = new WSDocs2();
                AuthHeader auth = new AuthHeader();
                auth.Key = System.Configuration.ConfigurationManager.AppSettings["key"];
                ws.AuthHeaderValue = auth;
                ws.Url = LinkSite + "/WsDocs2.asmx";
                ws.Proxy = Functions.PegarProxy();

                USUARIO usuario = ws.BuscarUsuario(Conversion.ToDecimal(UsuarioId));
                //nomeCompleto = usuario.USUNOMECOMPLETO;

                X509Certificate2 Cert = PegarCertificados(Certificado);

                string cpfCnpjCertificado = Functions.PegarCPFCertificado(Cert);

            }

            private string MostrarDocumento(string DocId, string clienteid, string usuarioid, string origemUser)
            {
                try
                {
                    //if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"file.json"))
                    //    File.Delete(AppDomain.CurrentDomain.BaseDirectory + @"file.json");

                    //string _OrigemUser = "1";
                    //string URLRetorno = "";

                    //LimparTemporario();

                    //string LinkSite = URLRetorno;
                    if (string.IsNullOrEmpty(LinkSite))
                        LinkSite = System.Configuration.ConfigurationManager.AppSettings["LinkSite"];

                    string EnderecoCloudDocs = LinkSite + "/ConverterDocumento.aspx?docId=" + DocId + "&clienteid=" + clienteid;

                    string DiretorioTemp = AppDomain.CurrentDomain.BaseDirectory + @"TempImagens\";
                    System.IO.Directory.CreateDirectory(DiretorioTemp);

                    string dest = "";

                    string NomeArquivo = Guid.NewGuid().ToString().ToUpper() + ".pdf";

                    dest = System.IO.Path.Combine(DiretorioTemp, NomeArquivo);

                    WebClient webClient = new WebClient();
                    webClient.UseDefaultCredentials = true;
                    webClient.DownloadFile(EnderecoCloudDocs, dest);
                    webClient.Dispose();

                    //Spire.Pdf.PdfDocument OriginalDoc = new Spire.Pdf.PdfDocument();
                    //OriginalDoc.LoadFromFile(dest);
                    //var conformance = OriginalDoc.Conformance;
                    //if (conformance != null && conformance == PdfConformanceLevel.None)
                    //{
                    //    Spire.Pdf.PdfNewDocument newDOC = new Spire.Pdf.PdfNewDocument();
                    //    newDOC.Conformance = Spire.Pdf.PdfConformanceLevel.Pdf_A1B;

                    //    foreach (Spire.Pdf.PdfPageBase page in OriginalDoc.Pages)
                    //    {
                    //        if (page.Rotation == Spire.Pdf.PdfPageRotateAngle.RotateAngle0)
                    //        {
                    //            System.Drawing.SizeF size = page.Size;
                    //            Spire.Pdf.PdfPageBase p = newDOC.Pages.Add(size, new Spire.Pdf.Graphics.PdfMargins(0));
                    //            page.CreateTemplate().Draw(p, 0, 0);
                    //        }
                    //        else
                    //        {
                    //            System.Drawing.SizeF size = new System.Drawing.SizeF(page.Size.Height + 50, page.Size.Width + 50);
                    //            Spire.Pdf.PdfPageBase p = newDOC.Pages.Add(size, new Spire.Pdf.Graphics.PdfMargins(0));
                    //            page.CreateTemplate().Draw(p, 0, 0);
                    //        }
                    //    }

                    //    if (OriginalDoc.Pages.Count < newDOC.Pages.Count)
                    //        newDOC.Pages.RemoveAt(newDOC.Pages.Count - 1);


                    //    newDOC.Save(dest);
                    //    newDOC.Close();
                    //    newDOC.Dispose();
                    //    OriginalDoc.Close();
                    //    OriginalDoc.Dispose();
                    //}

                    return NomeArquivo;
                }
                catch (Exception ex)
                {
                    string erro = ex.Message;
                    return "";
                }
            }

            public static X509Certificate2 PegarCertificados(string Certificado)
            {
                Functions.GravaLog(Certificado);

                List<X509Certificate2> fcollection = Functions.GetCurrentUserCertificates();

                List<CertificadoWS> certs = new List<CertificadoWS>();

                foreach (X509Certificate2 cert in fcollection)
                {
                    Functions.GravaLog(cert.Subject);

                    if (cert.Subject.ToUpper().Contains("ICP-BRASIL"))
                    {
                        if (cert.Subject == Certificado)
                        {
                            return cert;
                        }
                    }
                }

                Functions.GravaLog("Sem Certificado");

                return null;
            }

            public class CertificadoWS
            {
                public string Certificado { get; set; }
            }


        }
    }
}
