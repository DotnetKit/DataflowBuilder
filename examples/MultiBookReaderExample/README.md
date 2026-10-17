
# Multi-Book Common Words Reader

Compare two books and find the common words between them using a multi-source dataflow pipeline (`JoinBlock` under the hood).

Based on the concepts from the [Microsoft TPL Dataflow Walkthrough](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline?source=recommendations), this example demonstrates how `DotnetKit.DataflowBuilder` coordinates multiple inputs into a unified processing graph.

---

## Pipeline Workflow

The multi-source pipeline processes two streams in coordinated stages:

1. **Dual Ingestion (`FromSources<string, string>`)**:
   - Accepts two independent book URIs via `Send1Async(uri1)` and `Send2Async(uri2)`.
   - Uses an underlying `JoinBlock<string, string>` that pairs the inputs into a `Tuple<string, string>`.

2. **Concurrent Download & Word Streaming (`ProcessAsync`)**:
   - Streams both text files concurrently over HTTP using `HttpClient`.
   - Tokenizes each stream into words via `StreamReaderExtensions.ReadAllWords()`.

3. **Distinct Vocabulary Sets (`Process`)**:
   - Converts the words from each book into a `HashSet<string>` to eliminate duplicates and prepare for set operations.

4. **Set Intersection (`Process`)**:
   - Computes `words.Item1.Intersect(words.Item2)` to retain only words present in both books.

5. **Alphabetical Sorting (`Process`)**:
   - Orders common words alphabetically (`OrderBy(word => word)`).

6. **Target Output (`ToTarget`)**:
   - Emits the formatted, comma-separated list of common words to the console.

---

## Source Data

The example ingests public-domain books from [Project Gutenberg](https://www.gutenberg.org/) as plain text:

| Source | Target URI | Description |
| :--- | :--- | :--- |
| **Source 1** | `https://www.gutenberg.org/cache/epub/16452/pg16452.txt` | Book 1 plain text stream |
| **Source 2** | `https://www.gutenberg.org/cache/epub/16453/pg16453.txt` | Book 2 plain text stream |

Each stream contains thousands of lines of uncompressed English text with headers, punctuation, and line breaks.

---

## Output Example

When executed, the program logs the parallel download requests, prints the alphabetized list of overlapping words, and reports elapsed execution time:

```text
Downloading 'https://www.gutenberg.org/cache/epub/16452/pg16452.txt'...
Downloading 'https://www.gutenberg.org/cache/epub/16453/pg16453.txt'...
a, abandon, abide, abides, abiding, ability, able, about, above, abroad, absence, absent, absolute, absolutely, absorbed, absorbing, abstract, absurd, absurdity, abundance, abundant, abundantly, abuse, abused, accents, accept, acceptable, acceptance, accepted, accepting, accepts, access, accessible, accident, accidental, accidentally, accidents, accompanied, accompany, ...
Elapsed time: 1108 ms
```

---

## Running the Example

Run the console application directly with the .NET CLI:

```bash
dotnet run --project examples/MultiBookReaderExample/MultiBookReaderExample.csproj
```

