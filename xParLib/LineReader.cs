using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace xParLib
{
    /// <summary>
    /// Флаги для пометки свойств строк (аналог lflag_t из par.c).
    /// </summary>
    [Flags]
    public enum LineFlags
    {
        None = 0,
        Bodiless = 1,    // Бестелесная строка (L_BODILESS)
        Inserted = 2,    // Вставлена механизмом quote (L_INSERTED)
        First = 4,       // Первая строка абзаца (L_FIRST)
        Superf = 8,      // Избыточная строка (L_SUPERF)
    }

    /// <summary>
    /// Свойства строки (аналог struct lineprop из par.c).
    /// </summary>
    public struct LineProp
    {
        public int P { get; set; }           // Длина префикса (или fallback prelen)
        public int S { get; set; }           // Длина суффикса (или fallback suflen)
        public LineFlags Flags { get; set; } // Флаги
        public string Rc { get; set; }       // Повторяющийся графем-кластер бестелесной строки

        public LineProp(int p = 0, int s = 0, LineFlags flags = LineFlags.None, string? rc = null)
        {
            P = p;
            S = s;
            Flags = flags;
            Rc = rc ?? "";
        }
    }

    /// <summary>
    /// Пара строка + свойства (для избежания рассогласования массивов).
    /// </summary>
    public class LineSegment
    {
        public string Line { get; set; }
        public LineProp Prop { get; set; }

        public LineSegment(string line, LineProp prop)
        {
            Line = line;
            Prop = prop;
        }
    }

    /// <summary>
    /// Результат работы ReadLines — массив пар + позиция следующего чтения.
    /// </summary>
    public class ReadLinesResult
    {
        public LineSegment[] Segments { get; }
        public int NextIndex { get; }
        public bool IsEof { get; }

        public ReadLinesResult(LineSegment[] segments, int nextIndex, bool isEof)
        {
            Segments = segments;
            NextIndex = nextIndex;
            IsEof = isEof;
        }
    }

    /// <summary>
    /// Класс для чтения и аннотирования строк (аналог readlines() из par.c).
    /// </summary>
    public class LineReader
    {
        /// <summary>
        /// Читает строки из входного массива до EOF, protected line или blank line.
        /// </summary>
        public ReadLinesResult ReadLines(
            IReadOnlyList<string> inputLines,
            int startIndex,
            Charset protectChars,
            Charset quoteChars,
            Charset whiteChars,
            int tab,
            bool invis,
            bool quote)
        {
            if (inputLines == null) throw new ArgumentNullException(nameof(inputLines));
            if (startIndex < 0 || startIndex > inputLines.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            var segments = new List<LineSegment>();
            var vprop = new LineProp(0, 0, LineFlags.None, ""); // Обычная строка — все нули

            bool isEmpty = true;
            bool isFirstLine = true;

            // Для quote-логики
            string prevLine = "";
            int prevQuotePrefixEnd = 0;
            bool prevQuoteOnly = false;

            for (int i = startIndex; i < inputLines.Count; i++)
            {
                string rawLine = inputLines[i];

                // Проверка protected: первый графем-кластер в protectChars
                string firstGrapheme = GetFirstGrapheme(rawLine);
                if (firstGrapheme != "" && protectChars.IsMember(firstGrapheme))
                {
                    // Protected line — стоп, не включаем
                    return new ReadLinesResult(segments.ToArray(), i, isEof: false);
                }

                // Обработка строки
                string processedLine = ProcessLine(rawLine, whiteChars, tab);

                // Обновляем isBlank: строка пустая или только пробелы
                bool lineIsBlank = string.IsNullOrWhiteSpace(processedLine);

                // Blank line проверка (par.c строки 363–365)
                // Если blank и это не первая пустая строка (т.е. уже были символы), стоп
                if (lineIsBlank && !isEmpty)
                {
                    // Blank line — стоп, не включаем
                    return new ReadLinesResult(segments.ToArray(), i, isEof: false);
                }

                // Quote-логика
                if (quote)
                {
                    ProcessQuote(segments, ref processedLine, ref prevLine,
                        ref prevQuotePrefixEnd, ref prevQuoteOnly,
                        quoteChars, invis, isFirstLine);
                }

                segments.Add(new LineSegment(processedLine, vprop));
                isEmpty = false;
                isFirstLine = false;
            }

            // EOF — все строки прочитаны
            return new ReadLinesResult(segments.ToArray(), inputLines.Count, isEof: true);
        }

        /// <summary>
        /// Обрабатывает одну строку: tab expansion, whitespace → пробел, NUL пропуск.
        /// </summary>
        private static string ProcessLine(string line, Charset whiteChars, int tab)
        {
            if (string.IsNullOrEmpty(line)) return line;

            var result = new StringBuilder(line.Length * 2);
            int currentWidth = 0;

            var enumerator = StringInfo.GetTextElementEnumerator(line);
            while (enumerator.MoveNext())
            {
                string grapheme = enumerator.GetTextElement();

                // NUL — пропуск
                if (grapheme == "\0") continue;

                // Tab expansion
                if (grapheme == "\t")
                {
                    int spacesToAdd = tab - (currentWidth % tab);
                    if (spacesToAdd == 0) spacesToAdd = tab;
                    result.Append(' ', spacesToAdd);
                    currentWidth += spacesToAdd;
                    continue;
                }

                // Whitespace → пробел
                if (whiteChars.IsMember(grapheme))
                {
                    grapheme = " ";
                }

                result.Append(grapheme);
                currentWidth += Wcwidth.UnicodeCalculator.GetWidth(grapheme[0]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Получить первый графем-кластер строки.
        /// </summary>
        private static string GetFirstGrapheme(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";

            var enumerator = StringInfo.GetTextElementEnumerator(line);
            if (enumerator.MoveNext())
                return enumerator.GetTextElement();

            return "";
        }

        /// <summary>
        /// Quote-логика: вставка vacant lines между разными уровнями цитирования.
        /// </summary>
        private static void ProcessQuote(
            List<LineSegment> segments,
            ref string processedLine,
            ref string prevLine,
            ref int prevQuotePrefixEnd,
            ref bool prevQuoteOnly,
            Charset quoteChars,
            bool invis,
            bool isFirstLine)
        {
            var iprop = new LineProp(flags: LineFlags.Inserted);

            int quotePrefixEnd = FindQuotePrefixEnd(processedLine, quoteChars);
            bool isQuoteOnly = IsQuoteOnly(processedLine, quoteChars, quotePrefixEnd);

            if (!isFirstLine)
            {
                string currentQuotePrefix = processedLine.Substring(0, quotePrefixEnd);
                string prevQuotePrefix = prevLine.Substring(0, prevQuotePrefixEnd);

                // Общий префикс двух quote-префиксов
                int commonLen = CommonPrefixLength(currentQuotePrefix, prevQuotePrefix);
                string commonPrefix = currentQuotePrefix.Substring(0, commonLen);

                // Если quote-уровни разные
                if (commonPrefix.Length != currentQuotePrefix.Length ||
                    commonPrefix.Length != prevQuotePrefix.Length)
                {
                    if (!invis && (prevQuoteOnly || isQuoteOnly))
                    {
                        // Укоротить строку, состоящую только из quote
                        if (prevQuoteOnly)
                        {
                            // Предыдущая строка — укоротить (она уже в segments)
                            int lastIdx = segments.Count - 1;
                            segments[lastIdx] = new LineSegment(commonPrefix, segments[lastIdx].Prop);
                            prevLine = commonPrefix;
                            prevQuotePrefixEnd = commonPrefix.Length;
                        }
                        if (isQuoteOnly)
                        {
                            processedLine = commonPrefix;
                            quotePrefixEnd = commonPrefix.Length;
                        }
                    }
                    else
                    {
                        // Вставить vacant line
                        segments.Add(new LineSegment(commonPrefix, iprop));
                    }
                }
            }

            prevLine = processedLine;
            prevQuotePrefixEnd = quotePrefixEnd;
            prevQuoteOnly = isQuoteOnly;
        }

        /// <summary>
        /// Найти конец quote-префикса (индекс после последнего quote-символа, без trailing пробелов).
        /// </summary>
        private static int FindQuotePrefixEnd(string line, Charset quoteChars)
        {
            int i = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(line);
            int charIndex = 0;

            // Считаем quote-символы
            while (enumerator.MoveNext())
            {
                string grapheme = enumerator.GetTextElement();
                if (quoteChars.IsMember(grapheme))
                {
                    charIndex += grapheme.Length;
                    i = charIndex;
                }
                else
                {
                    break;
                }
            }

            // Убираем trailing пробелы
            while (i > 0 && line[i - 1] == ' ')
                i--;

            return i;
        }

        /// <summary>
        /// Проверить: строка состоит только из quote-символов и пробелов.
        /// </summary>
        private static bool IsQuoteOnly(string line, Charset quoteChars, int quotePrefixEnd)
        {
            string remainder = line.Substring(quotePrefixEnd);
            var enumerator = StringInfo.GetTextElementEnumerator(remainder);
            while (enumerator.MoveNext())
            {
                string grapheme = enumerator.GetTextElement();
                if (grapheme != " " && !quoteChars.IsMember(grapheme))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Длина общего префикса двух строк.
        /// </summary>
        private static int CommonPrefixLength(string a, string b)
        {
            int len = Math.Min(a.Length, b.Length);
            int i = 0;
            while (i < len && a[i] == b[i])
                i++;
            return i;
        }
    }
}
