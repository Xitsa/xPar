using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace xParLib
{
    /// <summary>
    /// Главный класс преобразования строк. Содержит публичный метод <see cref="Transform"/>
    /// для переформатирования входных строк абзаца.
    /// </summary>
    public class StringTransformer
    {
        /// <summary>
        /// Преобразует входные строки согласно логике форматирования.
        /// </summary>
        /// <param name="lines">Входные строки для преобразования.</param>
        /// <returns>Результат преобразования в виде списка строк.</returns>
        public IReadOnlyList<string> Transform(IReadOnlyList<string> lines)
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