
namespace BookReaderExample
{
    public class ReaderService
    {

        public Task<string> DownloadBookAsync(string uri)
        {
            Console.WriteLine("Downloading '{0}'...", uri);

            return new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip }).GetStringAsync(uri);
        }
        // Separates the specified text into an array of words. 
        public string[] CreateWords(string text)
        {
            Console.WriteLine("Creating word list...");

            // Remove common punctuation by replacing all non-letter characters
            // with a space character.
            char[] tokens = text.Select(c => char.IsLetter(c) ? c : ' ').ToArray();
            text = new string(tokens);

            // Separate the text into an array of words.
            return text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        // Removes short words and duplicates.
        public string[] FilterWords(string[] words)
        {
            {
                Console.WriteLine("Filtering word list...");

                return words
                   .Where(word => word.Length > 3)
                   .Distinct()
                   .ToArray();
            }
            ;
        }


        // Finds all words in the specified collection whose reverse also
        // exists in the collection.
        public ParallelQuery<string> FindReversedWords(string[] words)
        {
            Console.WriteLine("Finding reversed words...");

            var wordsSet = new HashSet<string>(words);

            return from word in words.AsParallel()
                   let reverse = new string(word.Reverse().ToArray())
                   where word != reverse && wordsSet.Contains(reverse)
                   select word;
        }

        // Prints the provided reversed words to the console.
        public void PrintReversedWords(string reversedWord)
        {

            Console.WriteLine("Found reversed words {0}/{1}",
               reversedWord, new string(reversedWord.Reverse().ToArray()));

        }
    }
}
