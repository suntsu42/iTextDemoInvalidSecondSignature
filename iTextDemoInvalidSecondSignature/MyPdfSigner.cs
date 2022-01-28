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
    internal class MyPdfSigner : PdfSigner
    {
        public MyPdfSigner(PdfReader reader, Stream outputStream, StampingProperties properties) : base(reader, outputStream, properties)
        {
        }

        override protected PdfDocument InitDocument(PdfReader reader, PdfWriter writer, StampingProperties properties)
        {
            return new MyPdfDocument(reader, writer, properties);
        }


    }
}
