using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace TweetAnalyzer
{
    /// <summary>
    /// Takes in a list of numbers with positions, calculates the running median, and outputs the results to file
    /// </summary>
    public static class MedianCalculator
    {
        private static ConcurrentDictionary<int, int> Numbers;
        private static int[] UniqueWordsPerTweet;
        private static string FilePath;

        //Initializes variables before first use and sets the output file that should be written to
        public static void Initialize(string filePath)
        {
            FilePath = filePath;
            UniqueWordsPerTweet = new int[Program.MaximumLineLength / 2 + 1]; // The maximum number of unique single-letter words possible given the tweet length
            Numbers = new ConcurrentDictionary<int, int>();
        }

        // Records the number at the given position; handles multiple threads calling the method concurrently
        public static void AddNumberToList(int position, int number)
        {
            Numbers.AddOrUpdate(position, number, (k,v) => number);
        }

        // Calculate the running median and write the results to file; should be called only after all the numbers have been added
        public static void CalculateRunningMedianAndWriteToFile()
        {
            DateTime startTime = DateTime.Now;

            bool isFirst = true;    // The first number is a special initial state where the median is the only number
            bool isEven = true;     // The median depends on whether it is even (average of two numbers) or odd (middle number)
            double lastMedian = 0;  // Keeping track of the previous median allows us to make a minor adjustment rather than a full recalculation

            /* The strategy is to keep track of the current median within the histogram. When each new number comes in, we know the median will change
             * by exactly 1 position: lower if the new number is lower than the existing median and higher if it is greater than or equal.
             * 
             * Median1 is the lower median and Median2 is the higher median. When the number of elements is odd, they are the same (single middle value).
             * When the number of elements is even, then they are the two middle values that are averaged.
             * 
             * Position1 and Position2 are the positions within the histogram for Median1 and Median2 respectively (1-based indexing, not 0). 
             * For example, suppose the sorted numbers are 1 1 1 2 2 2. Median1 = 1, Position1 = 3 (the third 1), Median2 = 2, Position2 = 1 (the first 2). 
             */
            int median1 = 0, median2 = 0, position1 = 0, position2 = 0;

            using (StreamWriter writer = new StreamWriter(FilePath))
            {
                foreach (int number in Numbers.OrderBy(kv => kv.Key).Select(kv => kv.Value))
                {
                    isEven = !isEven;

                    /* Recording the number of occurrences of each number is more efficient than inserting into a sorted data structure or sorting after each insertion
                     * To visualize this, the resulting array is similar to a histogram. I'm leveraging the fact that tweets are a relatively small length and
                     * therefore I don't need to handle a list of any possible number here, but instead a fairly modest range (roughly 1 - 70) */
                    UniqueWordsPerTweet[number]++;

                    if (isFirst)
                    {
                        isFirst = false;
                        median1 = median2 = number;
                        position1 = position2 = 1;
                    }
                    else if (number >= lastMedian) // Bigger number inserted, need to move median up one position
                    {
                        if (isEven) // Even case is average of two middle values; upper median needs to go up one position
                        {
                            if (UniqueWordsPerTweet[median2] > position2) // There's another of the same number above the current position; move up within the number 
                                position2++;
                            else // We're already at the top-most position; find the lowest position of the next highest number
                            {
                                do
                                {
                                    median2++;
                                } while (UniqueWordsPerTweet[median2] == 0);

                                position2 = 1;
                            }
                        }
                        else // Odd case is single middle value; move lower median up to match upper median
                        {
                            median1 = median2;
                            position1 = position2;
                        }
                    }
                    else // Smaller number inserted, need to move the median down one position
                    {
                        if (isEven) // Even case is average of two middle values; lower median needs to go down one position
                        {
                            if (position1 > 1) // There's another of the same number below the current position; move down within the number
                                position1--;
                            else // We're already at the bottom-most position; find the highest position of the next lowest number
                            {
                                do
                                {
                                    median1--;
                                } while (UniqueWordsPerTweet[median1] == 0);

                                position1 = UniqueWordsPerTweet[median1];
                            }
                        }
                        else // Odd case is single middle value; move upper median down to match lower median
                        {
                            median2 = median1;
                            position2 = position1;
                        }
                    }

                    // Update the median and write it to file as a number with 2 decimal places
                    lastMedian = (median1 + median2) / 2.0;
                    writer.WriteLine(lastMedian.ToString("F"));
                }
            }

            // Release resources from memory
            Numbers = null;
            UniqueWordsPerTweet = null;

            Console.Out.WriteLine("Medians Calculated and Written In : " + (DateTime.Now - startTime));
        }
    }
}