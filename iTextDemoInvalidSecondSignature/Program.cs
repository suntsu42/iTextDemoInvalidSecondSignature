using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.X509;
using System.Security.Cryptography;
using System.IO;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Security;

namespace iTextDemoInvalidSecondSignature
{
    public class Program
    {
        static string SignatureAttributeName = "DeferredSignature";
        const int EstimateSize = 32000;
        const string TimeServerUrl = "https://freetsa.org/tsr";
        static X509Certificate2 LocalSigningCertificate = null;

        static void Main(string[] args)
        {
            try
            {
                SignDocumentX509Certificate();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("done");
            Console.ReadLine();
        }


        private static void SignDocumentX509Certificate()
        {
            string PDFToSign = @"C:\temp\test.pdf";
            string PDFOutputPath = @"C:\temp\";

            //use the following powershell command to create the certificate: 
            //New-SelfSignedCertificate -CertStoreLocation "Cert:LocalMachine\My\" -Subject "SignCertSHA256" -KeyUsage "KeyEncipherment","DigitalSignature" -NotAfter "1/29/2022 12:00:00 AM" -KeyExportPolicy "ExportableEncrypted" -Type "SSLServerAuthentication" -KeySpec "KeyExchange" -HashAlgorithm "SHA256"

            LocalSigningCertificate = LoadCertificateFromStore(StoreName.My, StoreLocation.LocalMachine, "2734cb0c40a1dfbe57ed6c0f8fc1b898ffa9bd58");

            FileInfo fi = new FileInfo(PDFToSign);
            //Initial signature
            var signedPdf = SignPdf(File.ReadAllBytes(fi.FullName));
            File.WriteAllBytes(System.IO.Path.Combine(PDFOutputPath, $"first_signed_loclacert.pdf"), signedPdf);

            //Second signature
            signedPdf = SignPdf(File.ReadAllBytes(System.IO.Path.Combine(PDFOutputPath, $"first_signed_loclacert.pdf")));
            File.WriteAllBytes(System.IO.Path.Combine(PDFOutputPath, $"second_signed_loclacert.pdf"), signedPdf);

            Console.WriteLine($"Newly signed file can be found here: {PDFOutputPath}");
        }

        /// <summary>
        /// Sign the given pdf document using the certificate configured via ctor
        /// </summary>
        /// <param name="pdfToSign">The pdf to sign as byte array</param>
        /// <param name="visualRepresentation">The visual representation(Image) shown on the pdf</param>
        /// <returns>The signed pdf document as byte array</returns>
        public static byte[] SignPdf(byte[] pdfToSign)
        {

            byte[] hash = null;
            byte[] tmpPdf = null;
            //Step #1 >> prepare pdf for signing (Allocate space for the signature and calculate hash)
            using (MemoryStream input = new MemoryStream(pdfToSign))
            {
                using (var reader = new PdfReader(input))
                {
                    StampingProperties sp = new StampingProperties();
                    sp.UseAppendMode();
                    using (MemoryStream baos = new MemoryStream())
                    {
                        var signer = new PdfSigner(reader, baos, sp);

                        //Has to be NOT_CERTIFIED since otherwiese a pdf cannot be signed multiple times
                        signer.SetCertificationLevel(PdfSigner.NOT_CERTIFIED);

                        //Make the SignatureAttributeName unique
                        SignatureAttributeName = $"SignatureAttributeName_{DateTime.Now:yyyyMMddTHHmmss}";
                        signer.SetFieldName(SignatureAttributeName);
                        DigestCalcBlankSigner external = new DigestCalcBlankSigner(PdfName.Adobe_PPKLite, PdfName.Adbe_pkcs7_detached);

                        signer.SignExternalContainer(external, EstimateSize);
                        hash = external.GetDocBytesHash();
                        tmpPdf = baos.ToArray();
                    }
                }

                //Step #2 >> Create the signature based on the document hash
                byte[] signature = null;
                signature = CreatePKCS7SignatureViaX509Certificate(hash);

                //Step #3 >> Apply the signature to the document
                ReadySignatureSigner extSigContainer = new ReadySignatureSigner(signature);
                using (MemoryStream preparedPdfStream = new MemoryStream(tmpPdf))
                {
                    using (var pdfReader = new PdfReader(preparedPdfStream))
                    {
                        using (PdfDocument docToSign = new PdfDocument(pdfReader))
                        {
                            using (MemoryStream outStream = new MemoryStream())
                            {
                                PdfSigner.SignDeferred(docToSign, SignatureAttributeName, outStream, extSigContainer);
                                return outStream.ToArray();
                            }
                        }
                    }
                }

            }
        }

        private static byte[] CreatePKCS7SignatureViaX509Certificate(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException(nameof(hash));


            ITSAClient tsaClient = null;
            tsaClient = new TSAClientBouncyCastle(TimeServerUrl, "", "", 8192, "SHA256");


            Org.BouncyCastle.X509.X509Certificate cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(LocalSigningCertificate);
            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)LocalSigningCertificate.PrivateKey;
            AsymmetricCipherKeyPair keyPair = DotNetUtilities.GetRsaKeyPair(rsa);

            PrivateKeySignature signature = new PrivateKeySignature(keyPair.Private, "SHA256");
            String hashAlgorithm = signature.GetHashAlgorithm();
            PdfPKCS7 sgn = new PdfPKCS7(null, new[] { cert }, hashAlgorithm, false);

            //new overload in iText 7.2
            byte[] sh = sgn.GetAuthenticatedAttributeBytes(hash, PdfSigner.CryptoStandard.CMS, null, null);

            byte[] extSignature = signature.Sign(sh);
            sgn.SetExternalDigest(extSignature, null, signature.GetEncryptionAlgorithm());

            //new overload in iText 7.2
            return sgn.GetEncodedPKCS7(hash, PdfSigner.CryptoStandard.CMS, tsaClient, null, null);
        }

        public static X509Certificate2 LoadCertificateFromStore(StoreName storeName, StoreLocation storeLocation, string thumbprint)
        {
            List<X509Certificate2> certificates = new List<X509Certificate2>();
            X509Store store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.OpenExistingOnly);
            X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
            if (certCollection[0] == null)
                throw new Exception($"Certificate with thumbprint {thumbprint} doesn't exist");
            var cert = certCollection[0];

            try
            {
                var key = cert.PrivateKey;
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading PrivateKey. Make sure the certificate was created using: -KeySpec KeyExchange");
            }
            return cert;
        }

    }
}
