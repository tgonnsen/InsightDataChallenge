using System.Collections.Generic;

namespace TweetAnalyzer
{
    /// <summary>
    /// A WordDictionary benefits from the efficiency of the Dictionary structure while locking writes for thread-safety
    /// </summary>
    public class WordDictionary
    {
        // The words stored in the dictionary along with the number of occurrences
        public Dictionary<string, int> Words { get; private set; }

        private object WriteLock = new object();

        public WordDictionary()
        {
            this.Words = new Dictionary<string, int>();
        }

        // Adds new words to the dictionary or increases the number of occurrences if they are already present
        public void AddWord(string word)
        {
            lock (WriteLock)
            {
                if (Words.ContainsKey(word))
                    Words[word]++;
                else
                    Words.Add(word, 1);
            }
        }
    }
}