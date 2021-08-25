using System;
using System.IO;
using iTextSharp.text;


namespace PQ
{
    class Config
    {
        private string configFilename = null;
        private string inputDirectory = null;
        private string outputDirectory = null;
        private string logFileDirectory = null;
        private string tempDirectory = null;
        private string archiveDirectory = null;
        private string archiveSubdirectory = null;
        private string qaFailDirectory = null;
        private string logFileName = null;
        Rectangle maxBoundary;
        Rectangle maxPage;

        public Config(string inFilename)
        {
            configFilename = inFilename;
        }

        public Config()
        {

        }

        public void ReadConfiguration()
        {
            String[] lines = null;
            String line;
            string[] dimensions;

            if (File.Exists(configFilename))
                 lines = System.IO.File.ReadAllLines(configFilename);
            else
            {
                System.Console.WriteLine(" EXCEPTION: CONFIGURATION FILE not found: " + configFilename);
                System.Environment.Exit(97);
            }


            foreach (string lne in lines)
            {
                line = lne.ToUpper().Trim();

                if (line.Trim().Length > 0 && line.IndexOf("=") > -1)
                {
                    if (line.Substring(0, line.IndexOf("=")).Equals("INPUTDIR"))
                        InputDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("OUTPUTDIR"))
                        OutputDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("LOGFILEDIR"))
                        LogFileDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("LOGFILENAME"))
                        LogFileName = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("TEMPDIR"))
                        TempDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("QAFAILDIR"))
                        QAFailDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("ARCHIVEDIR"))
                        ArchiveDirectory = line.Substring(line.IndexOf("=") + 1);
                    else if (line.Substring(0, line.IndexOf("=")).Equals("MAXPAGE"))
                    {
                        dimensions = line.Substring(line.IndexOf("=") + 1).Split('|');
                        if (dimensions.Length == 4)
                            MaxPage = new Rectangle(int.Parse(dimensions[0]), int.Parse(dimensions[1]), int.Parse(dimensions[2]), int.Parse(dimensions[3]));
                        else Console.WriteLine("** INVALID MAXPAGE Dimensions" + line + "\n\n");
                    }
                    else if (line.Substring(0, line.IndexOf("=")).Equals("MAXBOUNDARY"))
                    {
                        dimensions = line.Substring(line.IndexOf("=") + 1).Split('|');
                        if (dimensions.Length == 4)
                            MaxBoundary = new Rectangle(int.Parse(dimensions[0]), int.Parse(dimensions[1]), int.Parse(dimensions[2]), int.Parse(dimensions[3]));
                        else Console.WriteLine("** INVALID MAXBOUNDARY Dimensions" + line + "\n\n");
                    }
                    else
                    {
                        Console.WriteLine("**UNKNOWN LINE IN CONFIG FILE: " + line + "\n\n");
                    }
                }
                else if (line.Length > 0)
                {
                    Console.WriteLine("**UNKNOWN LINE IN CONFIG FILE: " + line + "\n\n");
                }

            } // foreach (string line ....

            if (inputDirectory != null &&
                 outputDirectory != null &&
                 logFileDirectory != null &&
                 logFileName != null &&
                 qaFailDirectory != null &&
                 tempDirectory != null &&
                 archiveDirectory != null &&
                 maxBoundary != null &&
                 maxPage != null)
                return;
            else // *** raise an error here because the config file was incomplete.....
            {
                System.Console.WriteLine(" **ERROR: Configuration file invalid, missing an item, or invalid formatting.");
                System.Environment.Exit(101);
            }

        } //ReadConfiguration

        public string ConfigFilename
        {
            get { return configFilename; }
            set { configFilename = value; }
        } // ConfigFilename

        public string InputDirectory
        {
            get { return inputDirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: Input Path not found: " + value);
                    System.Environment.Exit(99);
                }
                else
                {
                    inputDirectory = value;
                    Console.WriteLine(" Input Path: " + inputDirectory);
                }
            }
        } // InputDirectory

        public string OutputDirectory
        {
            get { return outputDirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: Output Path not found: " + value);
                    System.Environment.Exit(99);
                }
                else
                {
                    outputDirectory = value;
                    Console.WriteLine(" Output Path: " + outputDirectory);
                }
            }
        } // OutputDirectory

        public string LogFileDirectory
        {
            get { return logFileDirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: Logfile Path not found: " + value);
                    System.Environment.Exit(99);
                }
                else
                {
                    logFileDirectory = value;
                    Console.WriteLine(" Log File Path: " + logFileDirectory);
                }
            }
        } //LogFileDirectory

        public string LogFileName
        {
            get { return logFileName; }
            set
            {
                string tempName = value;
                int startPos = tempName.IndexOf("%D");
                int endPos = -1;
                if (startPos > -1)
                {
                    endPos = tempName.Substring(startPos + 1).IndexOf("%D");
                    if (endPos > -1)
                        logFileName = tempName.Substring(0, startPos) + DateTime.Now.ToString(tempName.Substring(startPos + 2, endPos - 1).Replace('Y', 'y').Replace('D', 'd')) + ".logs";
                    else
                    {
                        logFileName = tempName.Substring(0, startPos - 1) + ".logs";
                        Console.WriteLine("CONFIG FILE Log File Name Date Parameter not value: " + value);
                    }
                }
                else
                {
                    logFileName = value;
                    Console.WriteLine(" Log Filename: " + logFileName);
                }
            }
        } // LogFileName

        public string TempDirectory
        {
            get { return tempDirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: Temporary Path not found: " + value);
                    System.Environment.Exit(99);
                }
                else
                {
                    tempDirectory = value;
                    System.Console.WriteLine(" Temporary File Path: " + tempDirectory);
                }
            }
        } // TempDirectory

        public string ArchiveDirectory
        {
            get { return ArchiveSubdirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: Archive Path not found: " + value);
                    System.Environment.Exit(99);
                }
                archiveDirectory = value;
                archiveSubdirectory = DateTime.Now.ToString("yyyyMMddHHmm");
                if (!Directory.Exists(ArchiveSubdirectory))
                {
                    Directory.CreateDirectory(ArchiveSubdirectory);
                    if (!Directory.Exists(ArchiveSubdirectory))
                    {
                        System.Console.WriteLine("ERROR: Unable to create the archive subdirectory: " + ArchiveSubdirectory);
                        System.Environment.Exit(99);
                    }
                    System.Console.WriteLine(" Archive Path: " + ArchiveSubdirectory);
                }
                else archiveDirectory = value;
            }
        } // TempDirectory

        public string ArchiveSubdirectory
        {
            get { return archiveDirectory + "\\" + archiveSubdirectory; }
        } // TempDirectory

        public string QAFailDirectory
        {
            get { return qaFailDirectory; }
            set
            {
                if (value != null && !Directory.Exists(value))
                {
                    System.Console.WriteLine("ERROR: QA Failures Path not found: " + value);
                    System.Environment.Exit(99);
                }
                else
                {
                    qaFailDirectory = value;
                    System.Console.WriteLine(" QA Failure Path: " + qaFailDirectory);
                }
            }
        } // ArchiveDirectory

        public Rectangle MaxPage
        {
            get { return maxPage; }
            set { maxPage = value; }
        }

        public Rectangle MaxBoundary
        {
            get { return maxBoundary; }
            set { maxBoundary = value; }
        }

    } // class config
} // namespace PQ
