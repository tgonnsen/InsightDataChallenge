using System;
using System.Collections.Concurrent;
using System.IO;

namespace TweetAnalyzer
{
    /// <summary>
    /// Reads lines from a specified input file and stores them in a queue
    /// The queue may be read from concurrently while this class continues writing into it
    /// The ReadTweets method can be invoked as an asyncrhonous task, but only 1 task of it can exist
    /// </summary>
    public static class TweetReader
    {
        // Whether all the lines in the text file have been placed in the Tweets queue
        public static bool IsTweetReadingComplete { get; private set; }

        // A queue of all the lines from the text file
        public static ConcurrentQueue<string> Tweets { get; private set; }

        // A list of valid separators to split a text file line into separate words
        public static readonly char[] WordSeparator = { ' ' }; 
        
        // The file path for the text file to be read
        private static string FilePath; 

        // Initializes variables before first use and sets the input file that should be read from
        public static void Initialize(string filePath)
        {
            FilePath = filePath;
            IsTweetReadingComplete = false;
            Tweets = new ConcurrentQueue<string>();
        }

        // Reads lines out of the input file and stores them in the queue
        public static void ReadTweets()
        {
            DateTime startTime = DateTime.Now;
            
            long tweetNumber = 0;
            using (StreamReader reader = File.OpenText(FilePath))
            {
                while (!reader.EndOfStream)
                {
                    /* The tweet number is stored along with the tweet so that multiple processes can read from the queue 
                     * while maintaining an awareness of the initial order, which is necessary for calculating the running median */
                    Tweets.Enqueue(tweetNumber.ToString() + WordSeparator[0] + reader.ReadLine());
                    tweetNumber++;
                }
            }
            IsTweetReadingComplete = true; //The processors need to know when no additional items will be added to the queue
            
            Console.Out.WriteLine("Tweets Read In : " + (DateTime.Now - startTime));
        }
    }
}