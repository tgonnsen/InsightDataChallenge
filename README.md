# Insight Data Challenge
This is a solution for the coding challenge detailed at https://github.com/InsightDataScience/cc-example

## Solution Approach
My general approach was to write a C# .NET console application to divide the task into stages that would be performed sequentially in a processing pipeline. During development, I experimented with different levels of separation of tasks (from 1 - 5 stages) and levels of parallelization (from completely synchronous to all 5 stages acting concurrently). Using test data that I generated ranging from 10 thousand to 10 million tweets, some approaches were clearly better than others. My final design includes 3 stages that occur in 2 periods of parallel processing. First, a single I/O process reads lines out of the input text file and places each line in a queue. The second stage occurs simultaneously, multiple threads processing items out of this queue by splitting the lines into words and storing them for later analysis. The final stage involves sorting the words (which have been previously separated into distinct groups based on initial character) and writing the sorted results to text files. After all text files have been written, an operating system process is invoked to concatenate the files together into the final result and clean up the temporary files. In addition to analyzing the word content of the tweets, the second and third stages also include a thread for calculating and writing the running median number of unique words per tweet.

## Testing Results
Testing was performed on a 64-bit version of Windows 7 with an Intel Core i5 2.6 GHz CPU and 8 GB of RAM. I ran each test 3 times, averaged the timing results (there was low variance, so they seemed representative), and compared the processing times on a per-thousand-tweet basis. The results indicate a constant amount of application ramp-up with a fairly linear amount of per-tweet processing. This is consistent with my expectations given the technical choices made, detailed further below.
* Test 1: 1000 Tweets ; .34s ; .34s / 1000 tweets
* Test 2: 10,000 Tweets ; .41 ; .041s / 1000 tweets
* Test 3: 100,000 Tweets ; 1.76s ; .018s / 1000 tweets
* Test 4: 1,000,000 Tweets ; 16.65s ; .017s / 1000 tweets

## Notes For Using This Solution
Since I developed and tested this application on a Windows environment, I recommend using a similar system to avoid cross-platform issues. Unfortunately, it wasn't feasible for me to set up a suitable Linux/Mac environment during the course of this challenge, so I was unable to test the deployment scripts and application on other platforms. When using this application on a Windows system, the top-level TweetAnalyzer.exe can be invoked directly. For a Linux environment, I'd suggest running the run.sh script with the latest version of Mono installed and configured to use the .NET Framework 4.5.

## User Options Available in Application Settings
I've extracted a number of common points of variability out into a separate app.config file.
* InputFilePath: The file location to read from
* OutputDirectory: The directory to write to
* OutputTempFilePrefix: Arbitrary prefix for intermediate files
* OutputWordsFileName: Name of the output file of all words that appeared in tweets with occurrence counts
* OutputMediansFileName: Name of the output file of the running median of number of unique words per tweet
* AsciiMinimumValue: Lowest possible ASCII value that we need to handle
* AsciiMaximumValue: Highest possible ASCII value that we need to handle
* MaximumLineLength: Maximum characters that can appear in a Tweet
* WaitOnExit: Whether to wait for the user to hit the enter key before exiting command prompt (which has performance information)
* IsWindows: Whether cmd.exe is available to handle file merging

## Technical Details
### Storing Word Data
I chose to utilize Dictionary objects to store the frequency of words used. In .NET, this data structure operates as a HashTable to achieve constant-time insertions and lookups. The main alternative I considered was using a search tree approach to have the data be sorted by nature. However, it became clear that it would be much more efficient to insert all the data quickly and then perform a single sort algorithm afterwards. In order to minimize the performance hit of concurrent data structure use, I split each the words into many dictionaries based on the initial character. Thus, the sorted outputs of each dictionary could be simply arranged back-to-back without any further sorting required, thereby dividing-and-conqueoring the insertion/sorting burden.

### Effectively Calculating a Running Median
I made two algorithm design decisions that may not be immediately intuitive. First, instead of storing a list of numbers, I leveraged the fact that there was a known small range of potential values and opted for a histogram approach instead. Thus, instead of the list containing 6 '13' values for example, the array at index 13 (the number) would equal 6 (the count). The second was an insight into calculating a running median that would avoid the brute-force approach of re-sorting after each insertion. The details are well documented in the code, but the essence is that I keep track of the previous median and merely nudge it up or down one position in the distribution based on whether the newly inserted number is higher or lower than it. Doing so resulted in significant time savings by removing all of the sorting costs or recalculating the middle of the histogram each time.

### Leveraging Parallel Processing
The two most CPU-intensive aspects of the problem were breaking the tweets down into words to store them and then writing the results to file in sorted order. For the first segment, I implemented a flexible approach that scales the number of processor threads based on the quantity of data remaining to be processed. Thus, small data sets don't get overwhelmed with the overhead of a ton of processor threads, but large data sets can scale up concurrent processing to utilize the computer's available resources. For the second segment, I split each initial character into its own thread for sorting (for example, all the words beginning with 'b' would be sorted separately) and then writing to file. Writing many smaller files reduced the I/O burden since different letters were ready for writing at different times.

## Further Work
Overall, I'm very satisfied with the results that I got with my solution. After having done it, there are a few changes I would make if I were to do it over again. The first would be to implement it in a technology stack that is more easily cross-compatible (such as Java). Not having a ready-to-go development environment for a different language at the start of the challenge, I went with what I felt would enable me to be most productive the quickest so I could spend more time on the solution than setting up a new environment. However, while I feel good about my solution in a Windows system, I'm not satisfied with its ability to easily port to a Linux or Mac environment.

While my code performs very consistently, I offloaded the last step of merging many text files into the final result to the operating system and that process seems highly variable. For instance, while testing my solution with an input of 10 million tweets, I observed results at both 3.5 minutes and 12 minutes - the variability mainly due to the merging process that I had little direct control over. I would likely find a better solution if I absorbed that responsibility back into the application and it would improve it's cross-platform capabilities as well.

Finally, for significant scaling up, I think the general approach would have to be extended to a system distributed amongst multiple machines and likely leveraging a database since keeping everything in-memory limits the size of how much we can scale until it breaks down. However, the goal of this challenge appeared to be more focused on data structure selection and algorithm design and running on a single machine, so I didn't spend much time considering these alternative approaches or enhancements.