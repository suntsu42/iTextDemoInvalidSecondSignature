using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iTextDemoInvalidSecondSignature
{
    public class MyPdfDocument : PdfDocument
    {
        public MyPdfDocument(PdfReader reader, PdfWriter writer, StampingProperties properties) : base(reader, writer, properties)
        {

        }

        override protected void TryInitTagStructure(PdfDictionary str)
        {
            structTreeRoot = null;
            structParentIndex = -1;
        }
    }
}
