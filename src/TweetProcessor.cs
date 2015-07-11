using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TweetAnalyzer
{
    /// <summary>
    /// Processes lines from the reader queue by splitting them into words and storing them in the appropriate WordDictionary
    /// Also passes the number of unique words in the line to the MedianCalculator for analysis
    /// The ProcessTweets method can be invoked as an asychronous task and internally manages many tasks processing tweets in parallel
    /// </summary>
    public static class TweetProcessor
    {
        // An array (indexed by initial character of the word) of WordDictionary objects to store all the words in separate collections
        public static WordDictionary[] WordDictionaries;

        // How long to wait after creating a new processing task before re-evaluating the situation; best guess based on experimentation
        private static int DispatchDelay = 100;

        // How many Tweets each processor task should be responsible for; best guess based on experimentation
        private static int ProcessorTaskThroughput = 1000;

        // Initializes variables before first use and sets the range of ASCII values for the initial character of words
        public static void Initialize(int capacity)
        {
            WordDictionaries = new WordDictionary[capacity];
            for (int i = 0; i < capacity; i++)
                WordDictionaries[i] = new WordDictionary();
        }

        // Whether all the lines in the text file have been processed and the words placed in separate WordDictionary objects
        public static bool IsTweetProcessingComplete
        {
            get { return TweetReader.IsTweetReadingComplete && TweetReader.Tweets.Count == 0; }
        }

        // A driver function that processes the Tweets queue efficiently through parallelization in balance with the rate the reader adds to the queue
        public static void ProcessTweets()
        {
            DateTime startTime = DateTime.Now;
            IList<Task> processorTasks = new List<Task>();

            // Keep going until all the internal tasks have been completed and all tweets have been read and processed
            while (!IsTweetProcessingComplete || processorTasks.Count > 0)
            {
                /* We should have at least 1 processor running if there are any tweets to process
                 * Or if there are significantly more tweets to process than existing processor tasks, create a new task to run in parallel */
                if ((processorTasks.Count == 0 && TweetReader.Tweets.Count > 0) || TweetReader.Tweets.Count > (ProcessorTaskThroughput * processorTasks.Count))
                    processorTasks.Add(Task.Run(() => ProcessTweetsInternal()));

                // Wait a few moments before re-evaluating the situation
                Task.WaitAll(new []{Task.Delay(DispatchDelay)});
                processorTasks = processorTasks.Where(t => !t.IsCompleted).ToList();
            }

            Console.Out.WriteLine("Tweets Processed In : " + (DateTime.Now - startTime));
        }

        /* The internal function that performs the processing, splitting the tweet into words and storing the results
         * It's written so that many instances of this method can be run in parallel to process the Tweets queue efficiently */
        private static void ProcessTweetsInternal()
        {
            // If we catch up with the reader adding tweets to queue, this process can stop
            while (TweetReader.Tweets.Count > 0)
            {
                //Grab a tweet out of the queue
                string tempTweet = null;
                if (TweetReader.Tweets.TryDequeue(out tempTweet) && tempTweet != null)
                {
                    // We embedded the order into the line, so remove that first before splitting into words
                    string[] tweetParts = tempTweet.Split(TweetReader.WordSeparator, 2);
                    int tweetNumber = int.Parse(tweetParts[0]);
                    string[] words = tweetParts[1].Split();

                    // Pass the unique number of words in the tweet to the MedianCalculator while preserving its initial order
                    MedianCalculator.AddNumberToList(tweetNumber, words.Distinct().Count());

                    // Add the words to the appropriate WordDictionary based on its intial character
                    foreach (string word in words)
                        WordDictionaries[(int)word[0] - Program.AsciiMinimumValue].AddWord(word);
                }
            }
        }
    }
}