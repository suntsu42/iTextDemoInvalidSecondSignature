using iText.Kernel.Pdf;
using iText.Signatures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTextDemoInvalidSecondSignature
{
    /// <summary>
    /// Implementation for pdf deferred signing support using iText
    /// </summary>
    internal class ReadySignatureSigner : IExternalSignatureContainer
    {
        private readonly byte[] cmsSignatureContents;

        internal ReadySignatureSigner(byte[] cmsSignatureContents)
        {
            this.cmsSignatureContents = cmsSignatureContents;
        }

        public virtual byte[] Sign(Stream data)
        {
            return cmsSignatureContents;
        }

        public virtual void ModifySigningDictionary(PdfDictionary signDic)
        {
        }
    }
}
