using System;
using System.IO;
using System.Linq;

namespace TweetAnalyzer
{
    /// <summary>
    /// Sorts the words that begin with a specified character and then writes them to a file
    /// The WriteWordsToFile method can be invoked as an asynchronous task
    /// There can be multiple tasks operating in parallel, but they should all have a different wordIndex (initial character)
    /// </summary>
    public static class WordWriter
    {
        private static string FileDirectory;
        private static string TempFilePrefix;

        public static void Initialize(string fileDirectory, string tempFilePrefix)
        {
            FileDirectory = fileDirectory;
            TempFilePrefix = tempFilePrefix;
        }

        // Creates a temporary file with all the words that start with a specified character, sorted alphabetically and listed with number of occurrences
        public static void WriteWordsToFile(int wordIndex)
        {
            if (TweetProcessor.WordDictionaries[wordIndex].Words.Any())
            {
                DateTime startTime = DateTime.Now;

                // Create a name for the temporary file so that it'll be unique and in the correct alphabetical order to be combined later
                int maxFileCharacters = (Program.AsciiMaximumValue - Program.AsciiMinimumValue).ToString().Length;
                string tempFileName = FileDirectory + TempFilePrefix + wordIndex.ToString().PadLeft(maxFileCharacters, '0') + ".txt";

                // Write the file, sorting the words alphabetically and listing the number of occurrences
                File.WriteAllLines(tempFileName,
                    TweetProcessor.WordDictionaries[wordIndex].Words.OrderBy(kv => kv.Key).Select(word => word.Key.PadRight(Program.MaximumLineLength) + word.Value));

                // Once we're done with the dictionary, we can release it from memory
                TweetProcessor.WordDictionaries[wordIndex] = null;

                Console.Out.WriteLine("'" + (char)(wordIndex + Program.AsciiMinimumValue) + "' Words Written To File In : " + (DateTime.Now - startTime));
            }
        }
    }
}