namespace DataChunker
{
    using System.Text;
    using UglyToad.PdfPig;
    using UglyToad.PdfPig.Content;

    public static class PdfTextExtractor
    {
        public static Document GetText(string pdfFileName)
        {
            var document = new Document();
            if (!File.Exists(pdfFileName))
            {
                Console.WriteLine($"File {pdfFileName} does not exist");
                return document;
            }
            using (PdfDocument pdfDocument = PdfDocument.Open(pdfFileName,new ParsingOptions() { SkipMissingFonts = true}))
            {
                var result = new StringBuilder();
                int noOfPages = pdfDocument.NumberOfPages;
                for (int pageNo = 0; pageNo < noOfPages; ++pageNo)
                {
                    Page page = pdfDocument.GetPage(pageNo+1);
                    document.Add(page.Text, pageNo.ToString());
                }
            }

            return document;
        }

    }
}
