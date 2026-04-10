using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace xpar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            string? curLine;
            do
            {
                curLine = Console.ReadLine();
                if (curLine != null)
                {
                    var outString = Transform(curLine);
                    Console.WriteLine(outString);
                }
            }
            while (curLine != null);
        }

        static string Transform(string line)
        {
            var words = line
                .Normalize(NormalizationForm.FormKD)
                .Split(' ') ?? Array.Empty<string>();
            var stat = string.Join(", ", words.Select(CountGraphemes));
            return line + "[" + stat + "]";
        }

        static int CountGraphemes(string word)
        {
            var length = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(word);
            while (enumerator.MoveNext())
                length+= Wcwidth.UnicodeCalculator.GetWidth(enumerator.GetTextElement()[0]);
            return length;
        }
    }

}
