using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TweetAnalyzer
{
    /// <summary>
    /// The driver routine for reading in lines from a text file and outputting two files
    /// The first file is an alphabetical list of every word that appears in the text and a count for the number of occurrences
    /// The second file is a running median for the number of unique words per line in the text
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;

            // Initialize the stages of the pipeline
            TweetReader.Initialize(InputFilePath);
            TweetProcessor.Initialize(AsciiMaximumValue - AsciiMinimumValue);
            WordWriter.Initialize(OutputDirectory, OutputTempFilePrefix);
            MedianCalculator.Initialize(OutputDirectory + OutputMediansFileName);

            // In parallel, we can read from the file, split the lines into words, and store the words separately by initial character
            List<Task> stage1Tasks = new List<Task>
            {
                Task.Run(() => TweetReader.ReadTweets()),
                Task.Run(() => TweetProcessor.ProcessTweets())
            };
            Task.WaitAll(stage1Tasks.ToArray());

            // After the tweets have been read and parsed, we can calculate the medians, sort the words, and write it all to files
            List<Task> stage2Tasks = new List<Task>();
            stage2Tasks.Add(Task.Run(() => MedianCalculator.CalculateRunningMedianAndWriteToFile()));
            for (int i = 0; i < AsciiMaximumValue - AsciiMinimumValue; i++)
            {
                int tempIndex = i; //Necessary due to how closures work in this version of C#
                stage2Tasks.Add(Task.Run(() => WordWriter.WriteWordsToFile(tempIndex)));
            }
            Task.WaitAll(stage2Tasks.ToArray());

            if (ConfigurationManager.AppSettings["IsWindows"].ToLower() == "true")
            {
                // A file for each possible initial character has been created - now consolidate them into the final output file
                MergeFiles();
            }

            Console.Out.WriteLine("Total Duration: " + (DateTime.Now - startTime));

            if (ConfigurationManager.AppSettings["WaitOnExit"].ToLower() == "true")
            {
                Console.In.ReadLine();
            }
        }

        private static void MergeFiles()
        {
            DateTime mergeStartTime = DateTime.Now;

            // Use the operating system to efficiently concatenate the files in ASCII order
            Process mergeProcess = GetProcess();
            mergeProcess.StartInfo.Arguments = "/C copy /B " + OutputTempFilePrefix + "*.txt " + OutputWordsFileName;
            mergeProcess.Start();
            mergeProcess.WaitForExit();

            // Delete the intermediate temporary files
            Process cleanupProcess = GetProcess();
            cleanupProcess.StartInfo.Arguments = "/C del " + OutputTempFilePrefix + "*.txt";
            cleanupProcess.Start();

            Console.Out.WriteLine("Files Merged In : " + (DateTime.Now - mergeStartTime));
        }

        private static Process GetProcess()
        {
            return new Process { StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                WorkingDirectory = OutputDirectory,
                WindowStyle = ProcessWindowStyle.Hidden
            } };
        }

        /* Application Settings from App.config, cached so they are only read once */
        private static string inputFilePath = null;
        public static string InputFilePath { get { return inputFilePath ?? (inputFilePath = ConfigurationManager.AppSettings["InputFilePath"]); } }

        private static string outputDirectory = null;
        public static string OutputDirectory { get { return outputDirectory ?? (outputDirectory = ConfigurationManager.AppSettings["OutputDirectory"]); } }

        private static string outputTempFilePrefix = null;
        public static string OutputTempFilePrefix { get { return outputTempFilePrefix ?? (outputTempFilePrefix = ConfigurationManager.AppSettings["OutputTempFilePrefix"]); } }

        private static string outputMediansFileName = null;
        public static string OutputMediansFileName { get { return outputMediansFileName ?? (outputMediansFileName = ConfigurationManager.AppSettings["OutputMediansFileName"]); } }

        private static string outputWordsFileName = null;
        public static string OutputWordsFileName { get { return outputWordsFileName ?? (outputWordsFileName = ConfigurationManager.AppSettings["OutputWordsFileName"]); } }

        private static int? asciiMinimumValue = null;
        public static int AsciiMinimumValue
        {
            get
            {
                if (!asciiMinimumValue.HasValue)
                    asciiMinimumValue = int.Parse(ConfigurationManager.AppSettings["AsciiMinimumValue"]);
                return asciiMinimumValue.Value;
            }
        }

        private static int? asciiMaximumValue = null;
        public static int AsciiMaximumValue
        {
            get
            {
                if (!asciiMaximumValue.HasValue)
                    asciiMaximumValue = int.Parse(ConfigurationManager.AppSettings["AsciiMaximumValue"]);
                return asciiMaximumValue.Value;
            }
        }

        private static int? maximumLineLength = null;
        public static int MaximumLineLength
        {
            get
            {
                if (!maximumLineLength.HasValue)
                    maximumLineLength = int.Parse(ConfigurationManager.AppSettings["MaximumLineLength"]);
                return maximumLineLength.Value;
            }
        }
    }
}