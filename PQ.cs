using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.IO;

namespace PQ
{
    public class PQ
    {
        static Config c = new Config();

        static void Main(string[] args)
        {

            bool PageSizeFail = false;
            bool MarginFail = false;
            bool ImagesOutofBounds = false;
            bool ImagePages = false;
            int PageWithError = 0;
            string logDetails;

            Console.WriteLine("PDF Quality Check");
            Console.WriteLine("RLMS");
			Console.WriteLIne("This utility contains and uses iTextSharp. Copyright by iText Group NV. Affero General Public License.\n");

            if (args.Length == 1)
            {
                if (args[0].Substring(0, 6).ToUpper().Equals("CONFIG"))
                {
                    c.ConfigFilename = args[0].Substring(args[0].IndexOf("=") + 1);
                    c.ReadConfiguration();
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("\n USAGE: PQ CONFIG=(config filename)");
                    Environment.Exit(99);
                }
            }
            else if (args.Length != 1)
            {
                Console.WriteLine("\n USAGE: PQ CONFIG=(config filename)");
                Environment.Exit(99);
            }
            else if (args.Length == 1 && !ArgsAreValid(args))
            {
                Environment.Exit(99);
            }

            // loop to read file and write letters
            String[] fileList = Directory.GetFiles(c.InputDirectory);

            // if the log file does not exist, then setup the headers to be added to the file....
            if (!File.Exists(c.LogFileDirectory + "\\" + c.LogFileName))
                logDetails = "timeStamp|filename|result|pagesizefail|marginfail|imagesoutofbounds|ImagePagesAttached|page\n";
            else logDetails = ""; // log file exists. Headers not needed.

            foreach (string f in fileList)
            {
                System.Console.WriteLine(" file:" + f);
                try
                {
                    PdfReader r = new PdfReader(f);
                    PdfReaderContentParser parser = new PdfReaderContentParser(r);
                    PageSizeFail = false;
                    MarginFail = false;
                    ImagesOutofBounds = false;
                    ImagePages = false;
                    PageWithError = 0;

                    for (int pg = 1; pg <= r.NumberOfPages && !PageSizeFail && !MarginFail && !ImagesOutofBounds; pg++)
                    {
                        MyImageRenderListener listener = new MyImageRenderListener(parser);
                        var t = new PQLocationTextExtractionStrategy();
                        parser.ProcessContent(pg, listener);

                        if (!PageMeetsSize(c.MaxPage, r.GetPageSize(pg)))
                        {
                            PageSizeFail = true;
                            PageWithError = pg;
                        }

                        if (!PageSizeFail)  // if a PageSize error not found, then look for text out of bounds
                        {
                            var ex = PdfTextExtractor.GetTextFromPage(r, pg, t);
                            if (!TextWithinBoundaries(c.MaxBoundary, t))
                            {
                                MarginFail = true;
                                PageWithError = pg;
                            }
                        }

                        if (!PageSizeFail && !MarginFail) // if page size is good and text is within boundaries, check images
                        {
                            if ((t.myPoints.Count == 0) && (listener.ImageMatrices.Count > 0))
                                ImagePages = true;
                            else
                            {
                                foreach (Matrix m in listener.ImageMatrices)
                                    if (!ImageInBoundaries(c.MaxBoundary, m))
                                    {
                                        ImagesOutofBounds = true;
                                        PageWithError = pg;
                                        break; // error found - no need to continue
                                    }
                            }
                        }
                    } // for (int pg = 1, ......
                    r.Close();

                    if (!PageSizeFail && !MarginFail && !ImagesOutofBounds && ImagePages) // Page Size is good, Margins are good, but large images....
                    {
                        PDFResizeImages(c, f);
                        logDetails += DateTime.Now.GetDateTimeFormats('G')[0] + "|" + System.IO.Path.GetFileName(f) + "|QC PASS|" + PageSizeFail.ToString() + "|" + MarginFail.ToString() + "|" + ImagesOutofBounds.ToString() + "|" + ImagePages.ToString() + "|" + PageWithError + "\n";
                        System.IO.File.AppendAllText(c.LogFileDirectory + "\\" + c.LogFileName, logDetails);
                        File.Move(f, c.ArchiveSubdirectory + "\\" + System.IO.Path.GetFileName(f)); // QC pass, now resize pages, and log results
                        File.Move(c.TempDirectory + "\\" + System.IO.Path.GetFileName(f), c.OutputDirectory + "\\" + System.IO.Path.GetFileName(f));
                    }
                    else if (!PageSizeFail && !MarginFail && !ImagesOutofBounds && !ImagePages)  // QC pass, NO pages to resize, log results
                    {
                        logDetails += DateTime.Now.GetDateTimeFormats('G')[0] + "|" + System.IO.Path.GetFileName(f) + "|QC PASS|" + PageSizeFail.ToString() + "|" + MarginFail.ToString() + "|" + ImagesOutofBounds.ToString() + "|" + ImagePages.ToString() + "|" + PageWithError + "\n";
                        System.IO.File.AppendAllText(c.LogFileDirectory + "\\" + c.LogFileName, logDetails);
                        File.Copy(f, c.ArchiveSubdirectory + "\\" + System.IO.Path.GetFileName(f)); 
                        File.Move(f, c.OutputDirectory + "\\" + System.IO.Path.GetFileName(f));
                    }
                    else  // else QC failed.... move to failure directory and log result
                    {
                        logDetails += DateTime.Now.GetDateTimeFormats('G')[0] + "|" + System.IO.Path.GetFileName(f) + "|FAIL|" + PageSizeFail.ToString() + "|" + MarginFail.ToString() + "|" + ImagesOutofBounds.ToString() + "|" + ImagePages.ToString() + "|" + PageWithError + "\n";
                        System.IO.File.AppendAllText(c.LogFileDirectory + "\\" + c.LogFileName, logDetails);
                        File.Copy(f, c.ArchiveSubdirectory + "\\" + System.IO.Path.GetFileName(f));
                        File.Move(f, c.QAFailDirectory + "\\" + System.IO.Path.GetFileName(f));
                    }

                } // try ....
                catch (Exception e)
                {
                    Console.WriteLine("\n Exception: " + e.Message);
                    logDetails += DateTime.Now.GetDateTimeFormats('G')[0] + "|" + System.IO.Path.GetFileName(f) + "|EXEPTION: " + e.Message + "\n";
                    System.IO.File.AppendAllText(c.LogFileDirectory + "\\" + c.LogFileName, logDetails);
                }

                logDetails = "";             
            } // foreach file....

        } // static main(....

        //==================================================================================================

        static void PDFResizeImages(Config c, string fileName)
        {
            string outputFile = c.TempDirectory + "\\" + System.IO.Path.GetFileName(fileName);
            string inputFile = fileName;
            PdfImportedPage importedPage;

            try
            {
                FileStream fs = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
                Document doc = new Document();
                PdfWriter w = PdfWriter.GetInstance(doc, fs);
                        
                //Open our PDF for writing
                doc.SetPageSize(new Rectangle(0, 0, 612, 792));
                doc.Open();

                //We need a reader to pull pages from
                PdfReader r = new PdfReader(inputFile);

                //PdfReaderContentParser parser = new PdfReaderContentParser(r);

                for (int pg = 1; pg <= r.NumberOfPages; pg++)
                {
                    //MyImageRenderListener listener = new MyImageRenderListener(parser);
                    //parser.ProcessContent(pg, listener);
                    var t = new PQLocationTextExtractionStrategy();
                    var ex = PdfTextExtractor.GetTextFromPage(r, pg, t);

                    if (t.myPoints.Count > 0)  // text on page, no need to resize images
                    {
                        importedPage = w.GetImportedPage(r, pg);
                        doc.NewPage();
                        iTextSharp.text.Image Img = iTextSharp.text.Image.GetInstance(importedPage);
                        Img.ScaleAbsolute((float)(importedPage.Width * 1.0), (float)(importedPage.Height * 1.0));
                        Img.SetAbsolutePosition(0f, 0f);
                        doc.Add(Img);
                    }
                    else if (t.myPoints.Count == 0) // No text, resize images
                    {
                        importedPage = w.GetImportedPage(r, pg);
                        doc.NewPage();
                        iTextSharp.text.Image Img = iTextSharp.text.Image.GetInstance(importedPage);
                        Img.ScaleAbsolute((float)(importedPage.Width * 0.92), (float)(importedPage.Height * 0.92));
                        doc.Add(Img);
                    }

                } // for loop

                doc.Close();
                r.Close();
            } // try block...
            catch (Exception e)
            {
                System.Console.WriteLine("ERROR RESIZING: " + e.Message + "\n" + e.StackTrace);
            }
        } // ResizeImages

        public static bool PageMeetsSize(Rectangle MaxPage, Rectangle PageSize)
        {
            //Console.WriteLine(PageSize.ToString());
            //Console.WriteLine("M" + MaxPage.ToString());
            if (MaxPage.Width < PageSize.Width)
            {
                Console.WriteLine("PAGE SIZE ERROR: Width exceeds maximum size. (" + PageSize.Width + ")");
                return false;
            }
            if (MaxPage.Height < PageSize.Height)
            {
                Console.WriteLine("PAGE SIZE ERROR: Height exceeds maximum size.(" + PageSize.Height + ")");
                return false;
            }
            return true;
        } // end of PageMeetSize...

        public static bool TextWithinBoundaries(Rectangle maxBoundary, PQLocationTextExtractionStrategy t)
        {
            //Loop through each PDF chunk found
            foreach (var p in t.myPoints)
            {

                if (maxBoundary.Top < p.Rect.Top)
                {
                    Console.WriteLine("TOP MARGIN ERROR: {0} ({1},{2}) to ({3},{4})", p.Text, p.Rect.Left, p.Rect.Bottom, p.Rect.Right, p.Rect.Top);
                    return false;
                }
                if (maxBoundary.Left > p.Rect.Left)
                {
                    Console.WriteLine("LEFT MARGIN ERROR: {0} ({1},{2}) to ({3},{4})", p.Text, p.Rect.Left, p.Rect.Bottom, p.Rect.Right, p.Rect.Top);
                    return false;
                }
                if (maxBoundary.Right < p.Rect.Right)
                {
                    Console.WriteLine("RIGHT MARGIN ERROR: {0} ({1},{2}) to ({3},{4})", p.Text, p.Rect.Left, p.Rect.Bottom, p.Rect.Right, p.Rect.Top);
                    return false;
                }
                if (maxBoundary.Bottom > p.Rect.Bottom)
                {
                    Console.WriteLine("BOTTOM MARGIN ERROR: {0} ({1},{2}) to ({3},{4})", p.Text, p.Rect.Left, p.Rect.Bottom, p.Rect.Right, p.Rect.Top);
                    return false;
                }

            } // foreach (....
            return true;
        } // End of TextWithBoundaries

        public static bool ImageInBoundaries(Rectangle maxBoundary, Matrix m)
        {
            if (maxBoundary.Top < m[Matrix.I32] + m[Matrix.I22])
            {
                Console.WriteLine("IMAGE TOP MARGIN ERROR: {0},{1} - {2}, {3}", m[Matrix.I31], m[Matrix.I32], m[Matrix.I31] + m[Matrix.I11], m[Matrix.I32] + m[Matrix.I22]);
                return false;
            }
            if (maxBoundary.Right < m[Matrix.I31] + m[Matrix.I11])
            {
                Console.WriteLine("IMAGE RIGHT MARGIN ERROR: {0},{1} - {2}, {3}", m[Matrix.I31], m[Matrix.I32], m[Matrix.I31] + m[Matrix.I11], m[Matrix.I32] + m[Matrix.I22]);
                return false;
            }
            if (maxBoundary.Left > m[Matrix.I31])
            {
                Console.WriteLine("IMAGE LEFT MARGIN ERROR: {0},{1} - {2}, {3}", m[Matrix.I31], m[Matrix.I32], m[Matrix.I31] + m[Matrix.I11], m[Matrix.I32] + m[Matrix.I22]);
                return false;
            }
            if (maxBoundary.Bottom > m[Matrix.I32])
            {
                Console.WriteLine("IMAGE BOTTOM MARGIN ERROR: {0},{1} - {2}, {3}", m[Matrix.I31], m[Matrix.I32], m[Matrix.I31] + m[Matrix.I11], m[Matrix.I32] + m[Matrix.I22]);
                return false;
            }
            // m[Matrix.I31], m[Matrix.I32], m[Matrix.I31]+m[Matrix.I11], m[Matrix.I32]+m[Matrix.I22]
            return true;
        } // ImageWithinBoundaries

        public static bool ArgsAreValid(string[] args)
        {
            if (!File.Exists(args[0]))
            {
                System.Console.WriteLine("ERROR: File not found: " + args[0]);
                return false;
            }
            return true;
        } // ArgsAreValid ...

    } // Class PQ

} // NameSpace PQ

