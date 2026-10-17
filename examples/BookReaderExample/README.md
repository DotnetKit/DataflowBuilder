
# Book Reversed Words Reader

Analyze a book's text and discover all reversed word pairs (e.g., `evil`/`live`, `wolf`/`flow`) using a fluent single-source dataflow pipeline.

This sample is an idiomatic `DotnetKit.DataflowBuilder` implementation of the classic [Microsoft TPL Dataflow Walkthrough](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline?source=recommendations), demonstrating asynchronous ingestion, one-to-many transformations, and parallel processing.

---

## Pipeline Workflow

The pipeline orchestrates text downloading, tokenization, filtering, parallel analysis, and display:

1. **Source Ingestion (`FromSource<string>`)**:
   - Accepts a single book URI string via `SendAsync(bookUri)`.

2. **Asynchronous Download (`ProcessAsync`)**:
   - Downloads the full book text over HTTP via `ReaderService.DownloadBookAsync` using an uncompressed/gzip-enabled `HttpClient`.

3. **Word Tokenization (`Process`)**:
   - Strips non-letter characters and splits the raw book text into an array of words (`CreateWords`).

4. **Filtering & Deduplication (`Process`)**:
   - Retains only unique words longer than three characters (`FilterWords`) to eliminate short noise words and duplicate evaluations.

5. **Parallel Reversed Word Matching (`ProcessMany`)**:
   - Evaluates the word collection with PLINQ (`AsParallel()`), discovering words whose reversed counterpart also exists in the vocabulary (`FindReversedWords`).
   - Flattens matching results into downstream single-word emissions.

6. **Target Output (`ToTarget`)**:
   - Logs each discovered palindrome/reversed word pair to the console (`PrintReversedWords`).

---

## Source Data

The example streams a public-domain book from [Project Gutenberg](https://www.gutenberg.org/) as uncompressed plain text:

| Parameter | Value |
| :--- | :--- |
| **Source URI** | `https://www.gutenberg.org/cache/epub/16452/pg16452.txt` |
| **Format** | UTF-8 Plain Text (`.txt`) |
| **Content** | Full text of an English literature classic with Gutenberg header and footer |

---

## Output Example

When executed, the pipeline logs progress at each stage and streams the discovered reversed pairs:

```text
Downloading 'https://www.gutenberg.org/cache/epub/16452/pg16452.txt'...
Creating word list...
Filtering word list...
Finding reversed words...
Found reversed words wolf/flow
Found reversed words swam/maws
Found reversed words flow/wolf
Found reversed words pool/loop
Found reversed words deer/reed
Found reversed words loop/pool
Found reversed words strap/parts
Found reversed words speed/deeps
Found reversed words ward/draw
Found reversed words moor/room
Found reversed words deeps/speed
Found reversed words meed/deem
Found reversed words deem/meed
Found reversed words tops/spot
Found reversed words teem/meet
Found reversed words maws/swam
Found reversed words room/moor
Found reversed words evil/live
Found reversed words meet/teem
Found reversed words live/evil
Found reversed words reed/deer
Found reversed words spot/tops
Found reversed words parts/strap
Found reversed words draw/ward
```

---

## Running the Example

Run the console application directly with the .NET CLI:

```bash
dotnet run --project examples/BookReaderExample/BookReaderExample.csproj
```

