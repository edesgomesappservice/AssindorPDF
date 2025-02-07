using System;
using System.Collections.Generic;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;
using System.Collections;
using Org.BouncyCastle.Pkcs;
using iTextSharp.text.pdf;
using System.IO;
using iTextSharp.text.xml.xmp;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography;
using iTextSharp.text;
//using iTextSharp.text.pdf.security;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Asn1;
using CloudDocs.AssinadorDigital.WsDocs2;
using System.Data;
using MessagingToolkit.QRCode.Codec;

///
/// <summary>
/// This Library allows you to sign a PDF document using iTextSharp
/// </summary>
/// <author>Alaa-eddine KADDOURI</author>
///
///

namespace iTextSharpSign
{
    /// <summary>
    /// This class hold the certificate and extract private key needed for e-signature 
    /// </summary>
    class Cert
    {
        #region Attributes

        private string path = "";
        private string password = "";
        private AsymmetricKeyParameter akp;
        private Org.BouncyCastle.X509.X509Certificate[] chain;

        #endregion

        #region Accessors
        public Org.BouncyCastle.X509.X509Certificate[] Chain
        {
            get { return chain; }
        }
        public AsymmetricKeyParameter Akp
        {
            get { return akp; }
        }

        public string Path
        {
            get { return path; }
        }

        public string Password
        {
            get { return password; }
            set { password = value; }
        }
        #endregion

        #region Helpers

        private void processCert()
        {
            string alias = null;
            Pkcs12Store pk12;

            //First we'll read the certificate file
            pk12 = new Pkcs12Store(new FileStream(this.Path, FileMode.Open, FileAccess.Read), this.password.ToCharArray());

            //then Iterate throught certificate entries to find the private key entry
            IEnumerator i = pk12.Aliases.GetEnumerator();
            while (i.MoveNext())
            {
                alias = ((string)i.Current);
                if (pk12.IsKeyEntry(alias))
                    break;
            }

            this.akp = pk12.GetKey(alias).Key;
            X509CertificateEntry[] ce = pk12.GetCertificateChain(alias);
            this.chain = new Org.BouncyCastle.X509.X509Certificate[ce.Length];
            for (int k = 0; k < ce.Length; ++k)
                chain[k] = ce[k].Certificate;

        }
        #endregion

        #region Constructors
        public Cert()
        { }
        public Cert(string cpath)
        {
            this.path = cpath;
            this.processCert();
        }
        public Cert(string cpath, string cpassword)
        {
            this.path = cpath;
            this.Password = cpassword;
            this.processCert();
        }
        #endregion

    }

    /// <summary>
    /// This is a holder class for PDF metadata
    /// </summary>
    class MetaData
    {
        private Hashtable info = new Hashtable();

        public Hashtable Info
        {
            get { return info; }
            set { info = value; }
        }

        public string Author
        {
            get { return (string)info["Author"]; }
            set { info.Add("Author", value); }
        }
        public string Title
        {
            get { return (string)info["Title"]; }
            set { info.Add("Title", value); }
        }
        public string Subject
        {
            get { return (string)info["Subject"]; }
            set { info.Add("Subject", value); }
        }
        public string Keywords
        {
            get { return (string)info["Keywords"]; }
            set { info.Add("Keywords", value); }
        }
        public string Producer
        {
            get { return (string)info["Producer"]; }
            set { info.Add("Producer", value); }
        }

        public string Creator
        {
            get { return (string)info["Creator"]; }
            set { info.Add("Creator", value); }
        }

        public Hashtable getMetaData()
        {
            return this.info;
        }
        public byte[] getStreamedMetaData()
        {
            MemoryStream os = new System.IO.MemoryStream();
            XmpWriter xmp = new XmpWriter(os);
            xmp.Close();
            return os.ToArray();
        }

    }

    /// <summary>
    /// this is the most important class
    /// it uses iTextSharp library to sign a PDF document
    /// </summary>
    class PDFSigner
    {
        private string inputPDF = "";
        private string outputPDF = "";
        private Cert myCert;
        private MetaData metadata;

        public PDFSigner(string input, string output)
        {
            this.inputPDF = input;
            this.outputPDF = output;
        }

        public PDFSigner(string input, string output, Cert cert)
        {
            this.inputPDF = input;
            this.outputPDF = output;
            this.myCert = cert;
        }
        public PDFSigner(string input, string output, MetaData md)
        {
            this.inputPDF = input;
            this.outputPDF = output;
            this.metadata = md;
        }
        public PDFSigner(string input, string output, Cert cert, MetaData md)
        {
            this.inputPDF = input;
            this.outputPDF = output;
            this.myCert = cert;
            this.metadata = md;
        }

        public void Verify()
        {
        }


        public void Sign(string SigReason, string SigContact, string SigLocation, bool visible)
        {
            PdfReader reader = new PdfReader(this.inputPDF);
            //Activate MultiSignatures
            PdfStamper st = PdfStamper.CreateSignature(reader, new FileStream(this.outputPDF, FileMode.Create, FileAccess.Write), '\0', null, true);
            //To disable Multi signatures uncomment this line : every new signature will invalidate older ones !
            //PdfStamper st = PdfStamper.CreateSignature(reader, new FileStream(this.outputPDF, FileMode.Create, FileAccess.Write), '\0'); 

            //st.MoreInfo = this.metadata.getMetaData();
            st.XmpMetadata = this.metadata.getStreamedMetaData();
            PdfSignatureAppearance sap = st.SignatureAppearance;

            sap.SetCrypto(this.myCert.Akp, this.myCert.Chain, null, PdfSignatureAppearance.WINCER_SIGNED);
            sap.Reason = SigReason;
            sap.Contact = SigContact;
            sap.Location = SigLocation;
            if (visible)
                sap.SetVisibleSignature(new iTextSharp.text.Rectangle(5, 5, 100, 50), 1, null);

            st.Close();
        }

    }


    public class SmartCard
    {
        public static void SignHashed(String PDFOrigem, String PDFDestino, String Reason, String Location, decimal docid, int totalAssinaturas)
        {
            SignHashed(PDFOrigem, PDFDestino, Reason, Location, null, docid, totalAssinaturas);
        }

        public static void SignHashed(String PDFOrigem, String PDFDestino, String Reason, String Location, X509Certificate2 Cert, decimal docid, int totalAssinaturas)
        {
            X509Certificate2 card;

            if (Cert == null)
            {
                card = GetCertificate();
            }
            else
                card = Cert;

            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { cp.ReadCertificate(card.RawData) };

            PdfReader reader = new PdfReader(PDFOrigem);
            PdfStamper stp = PdfStamper.CreateSignature(reader, new FileStream(PDFDestino, FileMode.Create), '\0', null, true);
            PdfSignatureAppearance sap = stp.SignatureAppearance;

            iTextSharp.text.Rectangle psize = reader.GetPageSize(reader.NumberOfPages);

            Spire.Pdf.Graphics.PdfImage image = Spire.Pdf.Graphics.PdfImage.FromFile(AppDomain.CurrentDomain.BaseDirectory + @"eletronica.jpg");


            int alturaTopo = 170;
            int alturaTopoAjuste = 130;

            alturaTopo = alturaTopo + (40 * totalAssinaturas);
            alturaTopoAjuste = alturaTopoAjuste + (40 * totalAssinaturas);

            //sap.SetVisibleSignature(new iTextSharp.text.Rectangle(5, 5, 500, 30), 1, null);
            sap.SetVisibleSignature(new iTextSharp.text.Rectangle(98, psize.Height - alturaTopo, 480, psize.Height - alturaTopoAjuste), reader.NumberOfPages, null);
            sap.SignDate = DateTime.Now;
            sap.SetCrypto(null, chain, null, null);
            //sap.Reason = Reason;
            //sap.Location = Location;
            sap.Acro6Layers = true;
            //iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(AppDomain.CurrentDomain.BaseDirectory + "eletronica.jpg");
            //sap.Render = PdfSignatureAppearance.SignatureRender.GraphicAndDescription;
            //sap.SignatureGraphic = img;

            PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKMS, PdfName.ADBE_PKCS7_SHA1);
            dic.Date = new PdfDate(sap.SignDate);
            dic.Name = PdfPKCS7.GetSubjectFields(chain[0]).GetField("CN");
            if (sap.Reason != null)
                dic.Reason = sap.Reason;
            if (sap.Location != null)
                dic.Location = sap.Location;
            sap.CryptoDictionary = dic;
            int csize = 4000;

            Dictionary<PdfName, int> exc = new Dictionary<PdfName, int>();
            exc[PdfName.CONTENTS] = csize * 2 + 2;
            sap.PreClose(exc);

            HashAlgorithm sha = new SHA1CryptoServiceProvider();

            Stream s = sap.RangeStream;
            int read = 0;
            byte[] buff = new byte[8192];
            while ((read = s.Read(buff, 0, 8192)) > 0)
            {
                sha.TransformBlock(buff, 0, read, buff, 0);
            }
            sha.TransformFinalBlock(buff, 0, 0);
            byte[] pk = SignMsg(sha.Hash, card, false);

            byte[] outc = new byte[csize];

            PdfDictionary dic2 = new PdfDictionary();

            Array.Copy(pk, 0, outc, 0, pk.Length);

            dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
            sap.Close(dic2);


            PdfViewer.PdfViewer pdf = new PdfViewer.PdfViewer();
            pdf.Dock = System.Windows.Forms.DockStyle.Fill;
            pdf.Location = new System.Drawing.Point(0, 0);
            pdf.Name = "pdfViewer1";
            pdf.Size = new System.Drawing.Size(585, 595);
            pdf.TabIndex = 0;

            pdf.Document = PdfViewer.PdfDocument.Load(PDFDestino, PdfViewer.PdfEngine.Chrome);
        }

        

        public static void SignDetached()
        {
            X509Certificate2 card = GetCertificate();
            Org.BouncyCastle.X509.X509CertificateParser cp = new Org.BouncyCastle.X509.X509CertificateParser();
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[] { cp.ReadCertificate(card.RawData) };

            PdfReader reader = new PdfReader("hello.pdf");
            PdfStamper stp = PdfStamper.CreateSignature(reader, new FileStream("hello_detached.pdf", FileMode.Create), '\0');
            PdfSignatureAppearance sap = stp.SignatureAppearance;
            sap.SetVisibleSignature(new Rectangle(100, 100, 300, 200), 1, null);
            sap.SignDate = DateTime.Now;
            sap.SetCrypto(null, chain, null, null);
            sap.Reason = "I like to sign";
            sap.Location = "Universe";
            sap.Acro6Layers = true;
            sap.Render = PdfSignatureAppearance.SignatureRender.NameAndDescription;
            PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED);
            dic.Date = new PdfDate(sap.SignDate);
            dic.Name = PdfPKCS7.GetSubjectFields(chain[0]).GetField("CN");
            if (sap.Reason != null)
                dic.Reason = sap.Reason;
            if (sap.Location != null)
                dic.Location = sap.Location;
            sap.CryptoDictionary = dic;
            int csize = 10000;
            Dictionary<PdfName, int> exc = new Dictionary<PdfName, int>();
            exc[PdfName.CONTENTS] = csize * 2 + 2;
            sap.PreClose(exc);

            Stream s = sap.RangeStream;
            MemoryStream ss = new MemoryStream();
            int read = 0;
            byte[] buff = new byte[8192];
            while ((read = s.Read(buff, 0, 8192)) > 0)
            {
                ss.Write(buff, 0, read);
            }
            byte[] pk = SignMsg(ss.ToArray(), card, true);

            byte[] outc = new byte[csize];

            PdfDictionary dic2 = new PdfDictionary();

            Array.Copy(pk, 0, outc, 0, pk.Length);

            dic2.Put(PdfName.CONTENTS, new PdfString(outc).SetHexWriting(true));
            sap.Close(dic2);
        }

        //  Sign the message with the private key of the signer.
        static public byte[] SignMsg(Byte[] msg, X509Certificate2 signerCert, bool detached)
        {
            //  Place message in a ContentInfo object.
            //  This is required to build a SignedCms object.
            ContentInfo contentInfo = new ContentInfo(msg);

            //  Instantiate SignedCms object with the ContentInfo above.
            //  Has default SubjectIdentifierType IssuerAndSerialNumber.
            SignedCms signedCms = new SignedCms(contentInfo, detached);

            //  Formulate a CmsSigner object for the signer.
            CmsSigner cmsSigner = new CmsSigner(signerCert);

            // Include the following line if the top certificate in the
            // smartcard is not in the trusted list.
            cmsSigner.IncludeOption = X509IncludeOption.EndCertOnly;

            //  Sign the CMS/PKCS #7 message. The second argument is
            //  needed to ask for the pin.
            signedCms.ComputeSignature(cmsSigner, false);

            //  Encode the CMS/PKCS #7 message.
            byte[] bb = signedCms.Encode();
            //return bb here if no timestamp is to be applied
            CmsSignedData sd = new CmsSignedData(bb);
            SignerInformationStore signers = sd.GetSignerInfos();
            byte[] signature = null;
            SignerInformation signer = null;
            foreach (SignerInformation signer_ in signers.GetSigners())
            {
                signer = signer_;
                break;
            }
            signature = signer.GetSignature();

            Org.BouncyCastle.Asn1.Cms.AttributeTable at = new Org.BouncyCastle.Asn1.Cms.AttributeTable(GetTimestamp(signature));

            signer = SignerInformation.ReplaceUnsignedAttributes(signer, null);
            IList signerInfos = new ArrayList();
            signerInfos.Add(signer);
            sd = CmsSignedData.ReplaceSigners(sd, new SignerInformationStore(signerInfos));
            bb = sd.GetEncoded();
            return bb;
        }

        public static Asn1EncodableVector GetTimestamp(byte[] signature)
        {
            byte[] tsImprint = PdfEncryption.DigestComputeHash("SHA1", signature);
            ITSAClient tsc = new TSAClientBouncyCastle("http://ca.signfiles.com/TSAServer.aspx", null, null);
            //return tsc.GetTimeStampToken(null, tsImprint);
            String ID_TIME_STAMP_TOKEN = "1.2.840.113549.1.9.16.2.14"; // RFC 3161 id-aa-timeStampToken

            Asn1InputStream tempstream = new Asn1InputStream(new MemoryStream(tsc.GetTimeStampToken(tsImprint)));

            Asn1EncodableVector unauthAttributes = new Asn1EncodableVector();

            Asn1EncodableVector v = new Asn1EncodableVector();
            v.Add(new DerObjectIdentifier(ID_TIME_STAMP_TOKEN)); // id-aa-timeStampToken
            Asn1Sequence seq = (Asn1Sequence)tempstream.ReadObject();
            v.Add(new DerSet(seq));

            unauthAttributes.Add(new DerSequence(v));
            return unauthAttributes;

        }

        public static X509Certificate2 GetCertificate()
        {
            X509Store st = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            st.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection col = st.Certificates;
            X509Certificate2 card = null;
            X509Certificate2Collection sel = X509Certificate2UI.SelectFromCollection(col, "Certificates", "Select one to sign", X509SelectionFlag.SingleSelection);
            if (sel.Count > 0)
            {
                X509Certificate2Enumerator en = sel.GetEnumerator();
                en.MoveNext();
                card = en.Current;
            }
            st.Close();
            return card;
        }
    }

}






