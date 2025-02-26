// Compare two books and find the common words between them by using a multisource pipeline (join blocks).
using MultiBookReaderExample;
using DotnetKit.DataflowBuilder;
using System.Diagnostics;

var bookService = new ReaderService();

const string bookUri = "https://www.gutenberg.org/cache/epub/16452/pg16452.txt";
const string bookUri2 = "https://www.gutenberg.org/cache/epub/16453/pg16453.txt";
var target = string.Empty;

var watcher = new Stopwatch();

var pipeline = DataFlowPipelineBuilder.FromSources<string, string>()
    .ProcessAsync(async (books) => (await bookService.DownloadBookWordsAsync(books.Item1), await bookService.DownloadBookWordsAsync(books.Item2)))
    .Process(books => (new HashSet<string>(books.Item1), new HashSet<string>(books.Item2)))
    .Process(words => words.Item1.Intersect(words.Item2).ToArray())
    .Process(words => words.OrderBy(word => word).ToArray())
    .ToTarget(words => Console.WriteLine(string.Join(", ", words)))
    .Build();

watcher.Start();
await pipeline.Send1Async(bookUri);
await pipeline.Send2Async(bookUri2);

await pipeline.CompleteAsync();
watcher.Stop();
Console.WriteLine($"Elapsed time: {watcher.ElapsedMilliseconds} ms");
