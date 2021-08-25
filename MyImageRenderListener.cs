using System.Collections.Generic;
using iTextSharp.text.pdf.parser;

namespace PQ
{

    public class MyImageRenderListener : IRenderListener
    {
        // ===========================================================================
        /** the byte array of the extracted images */
        private List<byte[]> _myImages;
        public List<byte[]> MyImages
        {
            get { return _myImages; }
        }
        /** the file names of the extracted images */
        private List<string> _imageNames;
        public List<string> ImageNames
        {
            get { return _imageNames; }
        }

        private List<Matrix> _imageMatrices;
        public List<Matrix> ImageMatrices
        {
            get { return _imageMatrices; }
        }
        // ---------------------------------------------------------------------------
        /**
         * Creates a RenderListener that will look for images.
         */
        public MyImageRenderListener(PdfReaderContentParser parser)
        {
            _myImages = new List<byte[]>();
            _imageNames = new List<string>();
            _imageMatrices = new List<Matrix>();
        }
        // ---------------------------------------------------------------------------
        /**
         * @see com.itextpdf.text.pdf.parser.RenderListener#beginTextBlock()
         */
        public void BeginTextBlock() { }
        // ---------------------------------------------------------------------------     
        /**
         * @see com.itextpdf.text.pdf.parser.RenderListener#endTextBlock()
         */
        public void EndTextBlock() { }

        // ---------------------------------------------------------------------------     
        /**
         * @see com.itextpdf.text.pdf.parser.RenderListener#renderImage(
         *     com.itextpdf.text.pdf.parser.ImageRenderInfo)
         */
        public void RenderImage(ImageRenderInfo renderInfo)
        {
            try
            {

                PdfImageObject image = renderInfo.GetImage();
                if (image == null
                  /*
                   * do not attempt to parse => jbig2 decoder not fully implemented.
                   * THE JAVA EXAMPLE INCORRECTLY CREATES A CORRUPT JBIG2 IMAGE
                   * BECAUSE THERE IS NO EXPLICIT CHECK. I POSTED TWICE TO THE MAILING
                   * LIST, SINCE VERSION 5.1.3 BUT THE ERROR HAS NOT BEEN CORRECTED.
                   */
                  || image.GetImageBytesType() == PdfImageObject.ImageBytesType.JBIG2
                )
                    return;

                _imageNames.Add(string.Format("Image{0}.{1}", renderInfo.GetRef().Number, image.GetFileType()
                ));
                _myImages.Add(image.GetImageAsBytes());
                //Matrix mtx = new Matrix();
                //mtx = renderInfo.GetImageCTM();
                _imageMatrices.Add(renderInfo.GetImageCTM());

                //Matrix mtx = renderInfo.GetImageCTM();
                //float[] coordinate = new float[] { mtx[Matrix.I31], mtx[Matrix.I32] };
                //Console.WriteLine("Image at {0}, {1}. ", coordinate[0], coordinate[1]);
            }
            catch
            {
                // pass through any other unsupported image types
            }
        }
        // ---------------------------------------------------------------------------     
        /**
          * @see com.itextpdf.text.pdf.parser.RenderListener#renderText(
          *     com.itextpdf.text.pdf.parser.TextRenderInfo)
          */
        public void RenderText(TextRenderInfo renderInfo) { }
        // ===========================================================================
    }

}
