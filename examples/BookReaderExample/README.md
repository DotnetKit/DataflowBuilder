
# Explanation

Original example with low level implementation of TPL dataflow : [Microsoft walkthrough-creating-a-dataflow-pipeline](https://learn.microsoft.com/en-us/dotnet/standard/parallel-programming/walkthrough-creating-a-dataflow-pipeline?source=recommendations)

The example program demonstrates the following steps using Dataflow pipeline builder

1. **Download the Book Text:**
   - The `DownloadBookAsync` method downloads the book text from the specified URI.

2. **Create a Word List:**
   - The `CreateWordList` method splits the book text into an array of words.

3. **Filter the Word List:**
   - The `FilterWordList` method removes short words and duplicates from the word array.

4. **Find Reversed Words:**
   - The `FindReversedWords` method finds all words in the filtered word array whose reverse also occurs in the word array.

5. **Print Reversed Words:**
   - The `PrintReversedWords` method displays the words and their corresponding reverse words to the console.
