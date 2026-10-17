namespace MultiBookReaderExample
{
    public class ReaderService
    {
        public async Task<IEnumerable<string>> DownloadBookWordsAsync(string uri, HttpClient client)
        {
            Console.WriteLine("Downloading '{0}'...", uri);

            var textReader = await client.GetStreamAsync(uri);
            var reader = new StreamReader(textReader);
            return reader.ReadAllWords();
        }
    }
}
