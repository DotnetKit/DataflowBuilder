// See https://aka.ms/new-console-template for more information
using BookReaderExample;
using DotnetKit.DataflowBuilder;


/*
Original example : https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline?source=recommendations

downloadString TransformBlock<TInput, TOutput>  Downloads the book text from the Web.
createWordList TransformBlock<TInput, TOutput>	Separates the book text into an array of words.
filterWordList TransformBlock<TInput, TOutput>	Removes short words and duplicates from the word array.
findReversedWords TransformManyBlock<TInput, TOutput>	Finds all words in the filtered word array collection whose reverse also occurs in the word array.
printReversedWords ActionBlock<TInput>	Displays words and the corresponding reverse words to the console.
*/

var bookService = new ReaderService();

const string bookUri = "http://www.gutenberg.org/cache/epub/16452/pg16452.txt";
var target = string.Empty;

var pipeline = DataFlowPipelineBuilder.FromSource<string>()
    .ProcessAsync(bookService.DownloadBook)
    .Process(bookService.CreateWordList)
    .Process(bookService.FilterWordList)
    .ProcessMany(bookService.FindReversedWords)
    .ToTarget(bookService.PrintReversedWords)
    .Build();

await pipeline.SendAsync(bookUri);

await pipeline.CompleteAsync();

