using System.Text;

namespace MultiBookReaderExample
{
    public static partial class StreamReaderExtensions
    {
        public static IEnumerable<string> ReadAllWords(this StreamReader reader)
        {
            StringBuilder word = new StringBuilder();
            int nextChar;
            while ((nextChar = reader.Read()) != -1)
            {
                char ch = (char)nextChar;

                if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsControl(ch) || char.IsSymbol(ch) || char.IsSeparator(ch) || char.IsNumber(ch))
                {
                    if (word.Length > 0)
                    {
                        yield return word.ToString().ToLowerInvariant();
                        word.Clear();
                    }
                }
                else
                {
                    word.Append(ch);
                }
            }

            // Return the last word if end of stream is reached
            if (word.Length > 0)
            {
                yield return word.ToString();
            }
        }
    }
}