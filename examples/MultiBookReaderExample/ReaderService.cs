namespace MultiBookReaderExample
{

    public class ReaderService
    {
        public async Task<IEnumerable<string>> DownloadBookWordsAsync(string uri)
        {
            Console.WriteLine("Downloading '{0}'...", uri);

            var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.GZip });
            var textReader = await client.GetStreamAsync(uri);
            var reader = new StreamReader(textReader);
            return reader.ReadAllWords();
        }

    }

}
