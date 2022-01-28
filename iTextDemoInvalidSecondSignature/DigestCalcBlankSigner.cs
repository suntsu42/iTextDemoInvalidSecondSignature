using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTextDemoInvalidSecondSignature
{

    public class DigestCalcBlankSigner : IExternalSignatureContainer
    {
        private readonly PdfName _filter;
        private readonly PdfName _subFilter;
        private byte[] _docBytesHash;

        public DigestCalcBlankSigner(PdfName filter, PdfName subFilter)
        {
            _filter = filter;
            _subFilter = subFilter;
        }

        internal virtual byte[] GetDocBytesHash()
        {
            return _docBytesHash;
        }

        public virtual byte[] Sign(Stream data)
        {
            _docBytesHash = CalcDocBytesHash(data);
            //We don't return the signature since it is applied via GetDocBytesHash() later
            return new byte[0];
        }

        public virtual void ModifySigningDictionary(PdfDictionary signDic)
        {
            signDic.Put(PdfName.Filter, _filter);
            signDic.Put(PdfName.SubFilter, _subFilter);
        }

        internal static byte[] CalcDocBytesHash(Stream docBytes)
        {
            byte[] docBytesHash = null;
            docBytesHash = DigestAlgorithms.Digest(docBytes, DigestUtilities.GetDigest(DigestAlgorithms.SHA256));
            return docBytesHash;
        }
    }
}
