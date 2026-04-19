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
    /// Результат вычисления comprelen и comsuflen (в графемах).
    /// </summary>
    public readonly record struct CompresuflenResult(int Prefix, int Suffix);

    /// <summary>
    /// Результат вычисления аффиксов IP.
    /// </summary>
    public readonly record struct SetAffixesResult(
        int Prefix,
        int Suffix,
        int AugmentedFallbackPre,
        int FallbackSuf);

    /// <summary>
    /// Класс для чтения и аннотирования строк (аналог readlines() из par.c).
    /// </summary>
    public class LineReader
    {
        /// <summary>
        /// Читает строки из входного массива до EOF, protected line или blank line.
        /// </summary>
        /// <remarks>Аналог readlines() из par.c (строки 87–93).</remarks>
        public static ReadLinesResult ReadLines(
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

        //
        // Compresuflen — аналог compresuflen() из par.c
        //

        /// <summary>
        /// Получить список графем-кластеров строки.
        /// </summary>
        /// <param name="line">Входная строка.</param>
        /// <returns>Список графем-кластеров.</returns>
        public static IReadOnlyList<string> GetGraphemes(string line)
        {
            if (string.IsNullOrEmpty(line))
                return Array.Empty<string>();

            var list = new List<string>();
            var enumerator = StringInfo.GetTextElementEnumerator(line);
            while (enumerator.MoveNext())
                list.Add(enumerator.GetTextElement());
            return list.AsReadOnly();
        }

        /// <summary>
        /// Подсчитать количество графем в строке.
        /// </summary>
        private static int CountGraphemes(string line)
        {
            if (string.IsNullOrEmpty(line))
                return 0;

            int count = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(line);
            while (enumerator.MoveNext())
                count++;
            return count;
        }

        /// <summary>
        /// Вычисляет comprelen и comsuflen для набора строк.
        /// </summary>
        /// <remarks>Аналог compresuflen() из par.c (строки 481–540).</remarks>
        /// <param name="segments">Массив сегментов строк (LineSegment)</param>
        /// <param name="startIndex">Индекс первого сегмента для обработки (включительно)</param>
        /// <param name="endIndex">Индекс последнего сегмента для обработки (включительно)</param>
        /// <param name="bodyChars">Набор body-символов</param>
        /// <param name="body">Флаг режима body (false = 0, true = 1), соответствует ParOptions.Body</param>
        /// <param name="minPrefix">Минимальная известная длина префикса (в графемах)</param>
        /// <param name="minSuffix">Минимальная известная длина суффикса (в графемах)</param>
        /// <returns>CompresuflenResult с полями Prefix (comprelen) и Suffix (comsuflen) в графемах</returns>
        public static CompresuflenResult Compresuflen(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex,
            Charset bodyChars,
            bool body,
            int minPrefix,
            int minSuffix)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            if (startIndex < 0 || startIndex > endIndex || endIndex >= segments.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            // Базовая строка — segments[startIndex].Line
            var baseGraphemes = GetGraphemes(segments[startIndex].Line);
            int totalBase = baseGraphemes.Count;

            // ===== Часть A: вычисление префикса (comprelen) =====
            // par.c 492-493: start = *lines; knownstart = start + pre; end = knownstart;
            int knownStart = minPrefix;
            int end = knownStart;

            // par.c 494-497: найти конец «тела» базовой строки
            if (body)
            {
                // body=1: идти до конца строки
                end = totalBase;
            }
            else
            {
                // body=0: идти до первого body-символа
                while (end < totalBase && !bodyChars.IsMember(baseGraphemes[end]))
                    end++;
            }

            // par.c 498-502: сузить end по всем строкам
            for (int lineIdx = startIndex + 1; lineIdx <= endIndex; lineIdx++)
            {
                var g = GetGraphemes(segments[lineIdx].Line);
                int totalG = g.Count;

                int p1 = knownStart; // индекс в baseGraphemes
                int p2 = minPrefix;   // индекс в g

                while (p1 < end && p2 < totalG && baseGraphemes[p1] == g[p2])
                {
                    p1++;
                    p2++;
                }
                end = p1;
            }

            // par.c 503-510: корректировка для body=1
            if (body)
            {
                // Отступить от end до knownStart, ища последний non-space non-body символ
                for (int p1 = end; p1 > knownStart;)
                {
                    p1--;
                    string gm = baseGraphemes[p1];
                    if (gm != " ")
                    {
                        if (bodyChars.IsMember(gm))
                            end = p1; // par: end = p1 (НЕ включает body-символ)
                        else
                            break; // non-body — выходим, end уже за ним
                    }
                }
            }

            int prefix = end; // par.c 511: *ppre = end - start; (start = 0)

            // ===== Часть B: вычисление суффикса (comsuflen) =====
            // par.c 513-515
            int bKnownStart = prefix; // индекс графема после префикса
            int bEnd = totalBase;     // конец базовой строки (количество графем)
            int bKnownEnd = bEnd - minSuffix;

            int bStart;
            if (body)
            {
                // body=1: start = knownStart
                bStart = bKnownStart;
            }
            else
            {
                // body=0: идти назад от knownEnd, пропуская non-body символы
                bStart = bKnownEnd;
                while (bStart > bKnownStart)
                {
                    // Проверяем графем перед bStart
                    string gm = baseGraphemes[bStart - 1];
                    if (!bodyChars.IsMember(gm))
                        bStart--;
                    else
                        break;
                }
            }

            // par.c 521-527: сузить start по всем строкам
            for (int lineIdx = startIndex + 1; lineIdx <= endIndex; lineIdx++)
            {
                var g = GetGraphemes(segments[lineIdx].Line);
                int totalG = g.Count;

                int knownstart2 = prefix; // индекс после префикса в текущей строке
                // p1 = bKnownEnd, p2 = конец строки - minSuffix
                int p1 = bKnownEnd;
                int p2 = totalG - minSuffix;

                while (p1 > bStart && p2 > knownstart2 && g[p2 - 1] == baseGraphemes[p1 - 1])
                {
                    p1--;
                    p2--;
                }
                bStart = p1;
            }

            // par.c 528-535: корректировка
            if (body)
            {
                // Идти вперёд от bStart, пока space или body-символ
                int p1 = bStart;
                while (bStart < bKnownEnd &&
                       (baseGraphemes[bStart] == " " || bodyChars.IsMember(baseGraphemes[bStart])))
                {
                    bStart++;
                }
                // Если продвинулись и последний был пробел — отступить на один
                if (bStart > p1 && baseGraphemes[bStart - 1] == " ")
                    bStart--;
            }
            else
            {
                // Пропустить двойные пробелы в начале суффикса
                while (bEnd - bStart >= 2 &&
                       baseGraphemes[bStart] == " " && baseGraphemes[bStart + 1] == " ")
                {
                    bStart++;
                }
            }

            int suffix = bEnd - bStart; // par.c 536: *psuf = end - start;

            return new CompresuflenResult(prefix, suffix);
        }

        //
        // Delimit — аналог delimit() из par.c
        //

        /// <summary>
        /// Определяет бестелесные строки и размечает свойства сегментов.
        /// </summary>
        /// <remarks>Аналог delimit() из par.c (строки 544–628).</remarks>
        /// <param name="segments">Массив сегментов строк (LineSegment)</param>
        /// <param name="startIndex">Индекс первого сегмента для обработки (включительно)</param>
        /// <param name="endIndex">Индекс последнего сегмента для обработки (включительно)</param>
        /// <param name="bodyChars">Набор body-символов</param>
        /// <param name="repeat">Минимальное кол-во повторений для bodiless</param>
        /// <param name="body">Флаг режима body (false = 0, true = 1)</param>
        /// <param name="div">Флаг деления по отступам (false = 0, true = 1)</param>
        /// <param name="minPrefix">Минимальная известная длина префикса (в графемах)</param>
        /// <param name="minSuffix">Минимальная известная длина суффикса (в графемах)</param>
        public static void Delimit(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex,
            Charset bodyChars,
            int repeat,
            bool body,
            bool div,
            int minPrefix,
            int minSuffix)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            if (startIndex < 0 || startIndex > endIndex + 1 || endIndex >= segments.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            // par.c 562: if (endline == lines) return;
            if (startIndex > endIndex) return;

            // par.c 564-568: if (endline == lines + 1)
            if (startIndex == endIndex)
            {
                var prop = segments[startIndex].Prop;
                prop.Flags |= LineFlags.First;
                prop.P = minPrefix;
                prop.S = minSuffix;
                segments[startIndex].Prop = prop;
                return;
            }

            // par.c 570: compresuflen(...)
            var csResult = Compresuflen(segments, startIndex, endIndex, bodyChars, body, minPrefix, minSuffix);
            minPrefix = csResult.Prefix;
            minSuffix = csResult.Suffix;

            // par.c 572-594: определение bodiless строк
            bool hasAnyBodiless = false;

            for (int lineIdx = startIndex; lineIdx <= endIndex; lineIdx++)
            {
                var prop = segments[lineIdx].Prop;
                prop.Flags |= LineFlags.Bodiless;
                prop.P = minPrefix;
                prop.S = minSuffix;

                var graphemes = GetGraphemes(segments[lineIdx].Line);
                int totalG = graphemes.Count;
                int bodyEnd = totalG - minSuffix;
                int bodyStart = minPrefix;

                string repeatGrapheme;
                if (bodyStart < bodyEnd)
                    repeatGrapheme = graphemes[bodyStart];
                else
                    repeatGrapheme = " ";

                // par.c 582-583: проверка на НЕ bodiless
                if (repeatGrapheme != " " &&
                    (IsInserted(prop) || repeat == 0 || (bodyEnd - bodyStart) < repeat))
                {
                    prop.Flags &= ~LineFlags.Bodiless;
                }
                else
                {
                    // par.c 584-590: проверка повторяемости
                    for (int g = bodyStart + 1; g < bodyEnd; g++)
                    {
                        if (graphemes[g] != repeatGrapheme)
                        {
                            prop.Flags &= ~LineFlags.Bodiless;
                            break;
                        }
                    }
                }

                if (IsBodiless(prop))
                {
                    hasAnyBodiless = true;
                    prop.Rc = repeatGrapheme;
                }

                segments[lineIdx].Prop = prop;
            }

            // par.c 596-613: рекурсивная обработка
            if (hasAnyBodiless)
            {
                int lineIdx = startIndex;
                while (lineIdx <= endIndex)
                {
                    if (IsBodiless(segments[lineIdx].Prop))
                    {
                        lineIdx++;
                        continue;
                    }

                    // Найти следующий bodiless
                    int nextLineIdx = lineIdx + 1;
                    while (nextLineIdx <= endIndex && !IsBodiless(segments[nextLineIdx].Prop))
                        nextLineIdx++;

                    // Рекурсивный вызов для подблока [lineIdx .. nextLineIdx-1]
                    if (lineIdx <= nextLineIdx - 1)
                    {
                        Delimit(segments, lineIdx, nextLineIdx - 1, bodyChars, repeat, body, div, minPrefix, minSuffix);
                    }

                    lineIdx = nextLineIdx;
                }

                return;
            }

            // par.c 615-618: нет div → First на первую строку
            if (!div)
            {
                var prop = segments[startIndex].Prop;
                prop.Flags |= LineFlags.First;
                segments[startIndex].Prop = prop;
                return;
            }

            // par.c 620-628: div=true — разметка по статусу пробела
            var firstGraphemes = GetGraphemes(segments[startIndex].Line);
            bool firstStatus = minPrefix < firstGraphemes.Count && firstGraphemes[minPrefix] == " ";

            for (int lineIdx = startIndex; lineIdx <= endIndex; lineIdx++)
            {
                var g = GetGraphemes(segments[lineIdx].Line);
                bool currentStatus = minPrefix < g.Count && g[minPrefix] == " ";
                if (currentStatus == firstStatus)
                {
                    var prop = segments[lineIdx].Prop;
                    prop.Flags |= LineFlags.First;
                    segments[lineIdx].Prop = prop;
                }
            }
        }

        /// <summary>
        /// Проверить: сегмент помечен как бестелесный.
        /// </summary>
        private static bool IsBodiless(LineProp prop)
        {
            return (prop.Flags & LineFlags.Bodiless) != 0;
        }

        /// <summary>
        /// Проверить: сегмент помечен как вставленный (Insert flag).
        /// </summary>
        private static bool IsInserted(LineProp prop)
        {
            return (prop.Flags & LineFlags.Inserted) != 0;
        }

        /// <summary>
        /// Проверить: сегмент помечен как vacant (бестелесный + rc = " ").
        /// </summary>
        private static bool IsVacant(LineProp prop)
        {
            return IsBodiless(prop) && prop.Rc == " ";
        }

        //
        // MarkSuperf — аналог marksuperf() из par.c
        //

        /// <summary>
        /// Размечает избыточные (superfluous) строки в сегменте.
        /// Требует, чтобы L_BODILESS был уже установлен (после вызова Delimit).
        /// </summary>
        /// <remarks>Аналог marksuperf() из par.c (строки 631–660).</remarks>
        /// <param name="segments">Массив сегментов строк (LineSegment)</param>
        /// <param name="startIndex">Индекс первого сегмента для обработки (включительно)</param>
        /// <param name="endIndex">Индекс последнего сегмента для обработки (включительно)</param>
        public static void MarkSuperf(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            if (startIndex < 0 || startIndex > endIndex + 1 || endIndex >= segments.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            // par.c 643-645: пометить все vacant как SUPERF
            for (int lineIdx = startIndex; lineIdx <= endIndex; lineIdx++)
            {
                if (IsVacant(segments[lineIdx].Prop))
                {
                    var prop = segments[lineIdx].Prop;
                    prop.Flags |= LineFlags.Superf;
                    segments[lineIdx].Prop = prop;
                }
            }

            // par.c 647-658: определение лучшей vacant в каждой группе
            bool inBody = false;
            int minNonSpace = 0;
            int bestIdx = -1; // вместо mprop = &dummy

            for (int lineIdx = startIndex; lineIdx <= endIndex; lineIdx++)
            {
                if (IsVacant(segments[lineIdx].Prop))
                {
                    // par.c 651-652: подсчёт non-space символов
                    int nonSpaceCount = CountNonSpaceGraphemes(segments[lineIdx].Line);

                    // par.c 653-655: если в теле или меньше минимума — новый лучший
                    if (inBody || nonSpaceCount < minNonSpace)
                    {
                        minNonSpace = nonSpaceCount;
                        bestIdx = lineIdx;
                    }
                    inBody = false;
                }
                else
                {
                    // par.c 657: снятие SUPERF с лучшей vacant из предыдущей группы
                    if (!inBody && bestIdx >= 0)
                    {
                        var prop = segments[bestIdx].Prop;
                        prop.Flags &= ~LineFlags.Superf;
                        segments[bestIdx].Prop = prop;
                    }
                    inBody = true;
                }
            }
        }

        /// <summary>
        /// Подсчитать количество не-пробельных графем в строке.
        /// </summary>
        /// <remarks>Аналог цикла "for (p = *line; *p; ++p) if (*p != ' ') ++num;" из par.c 651.</remarks>
        private static int CountNonSpaceGraphemes(string line)
        {
            if (string.IsNullOrEmpty(line))
                return 0;

            int count = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(line);
            while (enumerator.MoveNext())
            {
                string grapheme = enumerator.GetTextElement();
                if (grapheme != " ")
                    count++;
            }
            return count;
        }

        //
        // SetAffixes — аналог setaffixes() из par.c
        //

        /// <summary>
        /// Вычисляет итоговые значения префикса и суффикса для IP.
        /// </summary>
        /// <remarks>Аналог setaffixes() из par.c (строки 664–696).</remarks>
        /// <param name="segments">Массив сегментов строк (LineSegment)</param>
        /// <param name="startIndex">Индекс первого сегмента IP (включительно)</param>
        /// <param name="endIndex">Индекс последнего сегмента IP (включительно)</param>
        /// <param name="bodyChars">Набор body-символов</param>
        /// <param name="quoteChars">Набор quote-символов</param>
        /// <param name="hang">Пропуск первых hang строк при вычислении</param>
        /// <param name="body">Флаг режима body (false = 0, true = 1)</param>
        /// <param name="quote">Флаг quote (false = 0, true = 1)</param>
        /// <param name="prefix">Входной префикс (если HasValue, используется как есть)</param>
        /// <param name="suffix">Входной суффикс (если HasValue, используется как есть)</param>
        /// <returns>SetAffixesResult с итоговыми значениями</returns>
        public static SetAffixesResult SetAffixes(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex,
            Charset bodyChars,
            Charset quoteChars,
            int hang,
            bool body,
            bool quote,
            int? prefix,
            int? suffix)
        {
            if (segments == null) throw new ArgumentNullException(nameof(segments));
            if (startIndex < 0 || startIndex > endIndex || endIndex >= segments.Count)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            // par.c 678: numin = endline - inlines;
            int numin = endIndex - startIndex + 1;

            // par.c 680-681: compresuflen если prefix или suffix не заданы
            int computedPrefix = 0, computedSuffix = 0;
            if ((!prefix.HasValue || !suffix.HasValue) && numin > hang + 1)
            {
                var csResult = Compresuflen(segments, startIndex + hang, endIndex, bodyChars, body, 0, 0);
                computedPrefix = csResult.Prefix;
                computedSuffix = csResult.Suffix;
            }

            // par.c 683-684: augmented fallback prelen
            int fallbackPre = segments[startIndex].Prop.P;
            int augmentedFallbackPre;

            if (numin == 1 && quote)
            {
                // Добавить quote-символы после fallbackPre
                var graphemes = GetGraphemes(segments[startIndex].Line);
                int pos = fallbackPre;
                while (pos < graphemes.Count && quoteChars.IsMember(graphemes[pos]))
                    pos++;
                augmentedFallbackPre = pos;
            }
            else
            {
                augmentedFallbackPre = fallbackPre;
            }

            // par.c 688: fallback suflen
            int fallbackSuf = segments[startIndex].Prop.S;

            // par.c 690-696: финальные значения
            int finalPrefix = prefix.HasValue
                ? prefix.Value
                : (numin > hang + 1 ? computedPrefix : augmentedFallbackPre);

            int finalSuffix = suffix.HasValue
                ? suffix.Value
                : (numin > hang + 1 ? computedSuffix : fallbackSuf);

            return new SetAffixesResult(finalPrefix, finalSuffix, augmentedFallbackPre, fallbackSuf);
        }
    }
}
