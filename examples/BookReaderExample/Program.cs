// See https://aka.ms/new-console-template for more information
using BookReaderExample;
using DotnetKit.DataflowBuilder;

var bookService = new ReaderService();

const string bookUri = "https://www.gutenberg.org/cache/epub/16452/pg16452.txt";
var target = string.Empty;

var pipeline = DataFlowPipelineBuilder.FromSource<string>()
    .ProcessAsync(bookService.DownloadBookAsync)
    .Process(bookService.CreateWords)
    .Process(bookService.FilterWords)
    .ProcessMany(bookService.FindReversedWords)
    .ToTarget(bookService.PrintReversedWords)
    .Build();

await pipeline.SendAsync(bookUri);

await pipeline.CompleteAsync();

