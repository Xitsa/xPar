using System.Globalization;
using System.Text;

namespace xParLib
{
    public class StringTransformer
    {
        public string[] Transform(string[] lines)
        {
            return lines.Select(TransformLine).ToArray();
        }

        private string TransformLine(string line)
        {
            var words = line
                .Normalize(NormalizationForm.FormKD)
                .Split(' ') ?? Array.Empty<string>();
            var stat = string.Join(", ", words.Select(CountGraphemes));
            return line + "[" + stat + "]";
        }

        private int CountGraphemes(string word)
        {
            var length = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(word);
            while (enumerator.MoveNext())
                length += Wcwidth.UnicodeCalculator.GetWidth(enumerator.GetTextElement()[0]);
            return length;
        }
    }
}