using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace xParLib
{
    /// <summary>
    /// Флаги свойств слова (аналог wflag_t из reformat.c).
    /// </summary>
    /// <remarks>Аналог W_SHIFTED, W_CURIOUS, W_CAPITAL из reformat.c (строки 46–52).</remarks>
    [Flags]
    public enum WordFlags
    {
        /// <summary>Нет флагов.</summary>
        None = 0,

        /// <summary>Дополнительный пробел перед словом (если не первое в строке).</summary>
        Shifted = 1,

        /// <summary>«Любопытное» слово — содержит terminal-символ с alphanumeric до него.</summary>
        Curious = 2,

        /// <summary>Капитализированное слово — первый alphanumeric не lowercase.</summary>
        Capital = 4,
    }

    /// <summary>
    /// Представление слова в абзаце (аналог struct word из reformat.c).
    /// </summary>
    /// <remarks>
    /// Аналог struct word из reformat.c (строки 31–44).
    /// В отличие от оригинала, слова хранятся в List&lt;Word&gt;, а не в связном списке.
    /// Поля Prev/Next заменены на индексацию в списке.
    /// </remarks>
    public class Word
    {
        /// <summary>Текст слова.</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>Индекс первого слова следующей строки (для word wrapping). null — если нет следующей строки.</summary>
        public int? NextLine { get; set; }

        /// <summary>Значение целевой функции (для алгоритма разбиения на строки).</summary>
        public int Score { get; set; }

        /// <summary>Визуальная ширина слова в графемах (не количество символов!).</summary>
        public int Width { get; set; }

        /// <summary>Флаги свойств слова.</summary>
        public WordFlags Flags { get; set; }

        /// <summary>Индекс строки в segments (для логики guess — проверка смежности слов).</summary>
        public int SourceLineIndex { get; set; }

        /// <summary>Разделитель после слова: " " или "".</summary>
        public string SeparatorAfterWord { get; set; } = string.Empty;
    }

    /// <summary>
    /// Результат работы метода <see cref="ReformatModule.ExtractWords"/>.
    /// </summary>
    public readonly record struct ExtractWordsResult(
        List<Word> Words,
        List<string> Suffixes,
        int L,
        string? ErrorMessage);

    /// <summary>
    /// Реализация переноса логики форматирования.
    /// </summary>
    /// <remarks>Аналог файла reformat.c оригинального par (Par 1.53.0).</remarks>
    public static class ReformatModule
    {
        /// <summary>
        /// Переформатирует входные сегменты абзаца в отформатированный набор строк согласно
        /// параметрам форматирования и вычисленным аффиксам IP.
        /// </summary>
        /// <remarks>Аналог функции reformat() из reformat.c оригинального par (строка 297).</remarks>
        /// <param name="segments">Массив сегментов абзаца (LineSegment). Включает строки абзаца и их свойства.</param>
        /// <param name="startIndex">Начальная позиция диапазона сегментов для обработки (включительно).</param>
        /// <param name="endIndex">Конечная позиция диапазона сегментов для обработки (включительно).</param>
        /// <param name="options">Параметры форматирования (Width, Just, Fit и др.).</param>
        /// <param name="affixes">Результат вычисления префикса и суффикса IP (SetAffixesResult).</param>
        /// <returns>Массив строк с отформатированным текстом абзаца.</returns>
        public static string[] Reformat(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex,
            ParOptions options,
            SetAffixesResult affixes)
        {
            // TODO: реализовать логику форматирования на следующем этапе
            return Array.Empty<string>();
        }

        //
        // ExtractWords — выделение слов, guess, проверка/разбиение длины
        //

        /// <summary>
        /// Выделяет список слов из сегментов абзаца, обрабатывает guess и проверяет длину.
        /// </summary>
        /// <remarks>
        /// Аналог блоков выделения слов (reformat.c строки 331–368),
        /// обработки guess (reformat.c строки 371–397) и проверки/разбиения длинных слов
        /// (reformat.c строки 400–430).
        /// </remarks>
        /// <param name="segments">Массив сегментов строк.</param>
        /// <param name="startIndex">Индекс первого сегмента (включительно).</param>
        /// <param name="endIndex">Индекс последнего сегмента (включительно).</param>
        /// <param name="prefix">Длина префикса.</param>
        /// <param name="suffix">Длина суффикса.</param>
        /// <param name="width">Общая ширина строки.</param>
        /// <param name="terminalChars">Набор terminal-символов (для curious word).</param>
        /// <param name="cap">Флаг: все слова считаются капитализированными.</param>
        /// <param name="guess">Флаг: обработка curious/capital слов.</param>
        /// <param name="report">Флаг: ошибка при слове &gt; L вместо разбиения.</param>
        /// <returns>ExtractWordsResult со списком слов, суффиксами, L и сообщением об ошибке.</returns>
        public static ExtractWordsResult ExtractWords(
            IReadOnlyList<LineSegment> segments,
            int startIndex,
            int endIndex,
            int prefix,
            int suffix,
            int width,
            Charset terminalChars,
            bool cap,
            bool guess,
            bool report)
        {
            int L = width - prefix - suffix;
            int numIn = endIndex - startIndex + 1;
            var suffixes = new List<string>(numIn);

            var words = new List<Word>();
            bool onFirstWord = true;

            // === Этап 1: Выделение слов (reformat.c 331–368) ===
            for (int i = startIndex; i <= endIndex; i++)
            {
                string line = segments[i].Line;
                int lineVisualWidth = CalcVisualWidth(line);
                int affix = prefix + suffix;

                if (lineVisualWidth < affix)
                {
                    return new ExtractWordsResult(
                        Words: new List<Word>(),
                        Suffixes: new List<string>(),
                        L: 0,
                        ErrorMessage: $"Line {i - startIndex + 1} shorter than <prefix> + <suffix> = {prefix} + {suffix} = {affix}");
                }

                suffixes.Add(GetSuffix(line, suffix));

                var graphemes = LineReader.GetGraphemes(line);
                int totalGraphemes = graphemes.Count;
                int bodyStart = prefix;
                int bodyEnd = totalGraphemes - suffix;

                int p1 = bodyStart;
                while (p1 < bodyEnd)
                {
                    // Пропустить пробелы (уже унифицированы в LineReader.ProcessLine)
                    while (p1 < bodyEnd && graphemes[p1] == " ")
                        p1++;
                    if (p1 >= bodyEnd) break;

                    // p2 = p1 ДО возможного сброса onFirstWord (как в оригинале: p2 = p1; if (onfirstword) p1 = ...)
                    int p2 = p1;

                    if (onFirstWord)
                    {
                        p1 = bodyStart;
                        onFirstWord = false;
                    }

                    // Найти конец слова (до следующего пробела)
                    while (p2 < bodyEnd && graphemes[p2] != " ")
                        p2++;

                    string wordText = string.Concat(graphemes.Skip(p1).Take(p2 - p1));
                    int wordWidth = CalcGraphemeWidthRange(graphemes, p1, p2);

                    // Определить разделитель после слова (все пробелы до следующего слова)
                    int sepStart = p2;
                    int sepEnd = p2;
                    while (sepEnd < bodyEnd && graphemes[sepEnd] == " ")
                        sepEnd++;
                    string separator = string.Concat(graphemes.Skip(sepStart).Take(sepEnd - sepStart));

                    words.Add(new Word
                    {
                        Text = wordText,
                        Width = wordWidth,
                        Flags = WordFlags.None,
                        SourceLineIndex = i - startIndex,
                        SeparatorAfterWord = separator,
                    });

                    p1 = p2;
                }
            }

            // === Этап 2: Обработка guess (reformat.c 371–397) ===
            if (guess)
            {
                for (int idx = 0; idx < words.Count; idx++)
                {
                    var w2 = words[idx];
                    if (IsCurious(w2, terminalChars))
                        w2.Flags |= WordFlags.Curious;

                    if (cap || IsCapitalized(w2))
                    {
                        w2.Flags |= WordFlags.Capital;

                        if (idx > 0)
                        {
                            var w1 = words[idx - 1];
                            if (w1.Flags.HasFlag(WordFlags.Curious))
                            {
                                // Проверка: разделены ли одним пробелом
                                if (w1.SourceLineIndex == w2.SourceLineIndex &&
                                    w1.SeparatorAfterWord == " ")
                                {
                                    // Слияние: w2 поглощает w1
                                    w2.Text = w1.Text + " " + w2.Text;
                                    w2.Width = w1.Width + 1 + w2.Width;
                                    // Флаги: capital — если оба capital, иначе снять
                                    if (!w1.Flags.HasFlag(WordFlags.Capital))
                                        w2.Flags &= ~WordFlags.Capital;
                                    if (w1.Flags.HasFlag(WordFlags.Shifted))
                                        w2.Flags |= WordFlags.Shifted;
                                    // Удалить w1 из списка
                                    words.RemoveAt(idx - 1);
                                    idx--; // скорректировать индекс
                                }
                                else
                                {
                                    w2.Flags |= WordFlags.Shifted;
                                }
                            }
                        }
                    }
                    words[idx] = w2;
                }
            }

            // === Этап 3: Проверка/разбиение длинных слов (reformat.c 400–430) ===
            if (report)
            {
                foreach (var w in words)
                {
                    if (w.Width > L)
                    {
                        return new ExtractWordsResult(
                            Words: new List<Word>(),
                            Suffixes: new List<string>(),
                            L: 0,
                            ErrorMessage: $"Word too long: {w.Text}");
                    }
                }
            }
            else
            {
                int idx = 0;
                while (idx < words.Count)
                {
                    var w = words[idx];
                    if (w.Width <= L)
                    {
                        idx++;
                        continue;
                    }

                    // Разбить слово на части
                    var (firstPart, secondPart) = SplitWord(w, L);

                    // firstPart вставляется на текущую позицию, secondPart — на следующую
                    words[idx] = firstPart;
                    words.Insert(idx + 1, secondPart);

                    idx++; // перейти к secondPart (проверить, нужно ли ещё разбить)
                }
            }

            return new ExtractWordsResult(
                Words: words,
                Suffixes: suffixes,
                L: L,
                ErrorMessage: null);
        }

        //
        // Вспомогательные методы
        //

        /// <summary>
        /// Вычисляет визуальную ширину одной графемы.
        /// Корректно обрабатывает surrogate pairs (эмодзи и другие символы за пределами BMP).
        /// </summary>
        private static int GetGraphemeWidth(string grapheme)
        {
            if (string.IsNullOrEmpty(grapheme)) return 0;

            // BMP символ — один char
            if (grapheme.Length == 1)
                return Wcwidth.UnicodeCalculator.GetWidth(grapheme[0]);

            // Surrogate pair — получаем code point
            int codePoint = char.ConvertToUtf32(grapheme, 0);
            return Wcwidth.UnicodeCalculator.GetWidth(codePoint);
        }

        /// <summary>
        /// Вычисляет визуальную ширину строки (сумма ширин всех графем).
        /// </summary>
        private static int CalcVisualWidth(string line)
        {
            if (string.IsNullOrEmpty(line))
                return 0;

            int width = 0;
            var enumerator = StringInfo.GetTextElementEnumerator(line);
            while (enumerator.MoveNext())
                width += GetGraphemeWidth(enumerator.GetTextElement());
            return width;
        }

        /// <summary>
        /// Извлекает суффикс строки — последние <paramref name="suffixCount"/> графем.
        /// </summary>
        private static string GetSuffix(string line, int suffixCount)
        {
            if (suffixCount == 0) return string.Empty;

            var graphemes = LineReader.GetGraphemes(line);
            int start = graphemes.Count - suffixCount;
            if (start < 0) start = 0;
            return string.Concat(graphemes.Skip(start));
        }

        /// <summary>
        /// Вычисляет суммарную визуальную ширину графем в диапазоне [from, to).
        /// </summary>
        private static int CalcGraphemeWidthRange(IReadOnlyList<string> graphemes, int from, int to)
        {
            int width = 0;
            for (int i = from; i < to; i++)
                width += GetGraphemeWidth(graphemes[i]);
            return width;
        }

        /// <summary>
        /// Проверяет, является ли графема alphanumeric (буква или цифра).
        /// </summary>
        private static bool IsAlphanumeric(string grapheme)
        {
            if (string.IsNullOrEmpty(grapheme)) return false;
            char ch = grapheme[0];
            return char.IsLetterOrDigit(ch);
        }

        /// <summary>
        /// Проверяет, является ли слово «любопытным» (curious).
        /// Curious word — слово, содержащее terminal-символ, после которого нет alphanumeric,
        /// но хотя бы один alphanumeric есть до него.
        /// </summary>
        /// <remarks>Аналог checkcurious() из reformat.c (строки 73–92).</remarks>
        private static bool IsCurious(Word word, Charset terminalChars)
        {
            var graphemes = LineReader.GetGraphemes(word.Text);
            bool foundTerminal = false;

            // Идём с конца: ищем terminal, после которого нет alphanumeric
            for (int i = graphemes.Count - 1; i >= 0; i--)
            {
                string g = graphemes[i];
                if (IsAlphanumeric(g))
                {
                    if (foundTerminal)
                        return true; // alphanumeric до terminal
                }
                else if (terminalChars.IsMember(g))
                {
                    foundTerminal = true;
                }
            }

            return false;
        }

        /// <summary>
        /// Проверяет, является ли слово капитализированным.
        /// Capitalized word — слово с хотя бы одним alphanumeric, первый alphanumeric — не lowercase.
        /// </summary>
        /// <remarks>Аналог checkcapital() из reformat.c (строки 58–68).</remarks>
        private static bool IsCapitalized(Word word)
        {
            var graphemes = LineReader.GetGraphemes(word.Text);
            foreach (var g in graphemes)
            {
                if (IsAlphanumeric(g))
                {
                    // Первый alphanumeric — проверяем, не lowercase ли он
                    char ch = g[0];
                    return !char.IsLower(ch);
                }
            }
            return false; // нет alphanumeric
        }

        /// <summary>
        /// Разбивает слово на две части так, что первая часть имеет визуальную ширину ≤ maxLength.
        /// </summary>
        /// <param name="word">Слово для разбиения.</param>
        /// <param name="maxLength">Максимальная визуальная ширина первой части.</param>
        /// <returns>Кортеж (first, second) — два новых слова.</returns>
        private static (Word first, Word second) SplitWord(Word word, int maxLength)
        {
            var graphemes = LineReader.GetGraphemes(word.Text);
            int width = 0;
            int splitAt = 0;

            for (int i = 0; i < graphemes.Count; i++)
            {
                int w = GetGraphemeWidth(graphemes[i]);
                if (width + w > maxLength)
                    break;
                width += w;
                splitAt = i + 1;
            }

            // Если не удалось разбить (первая графема уже шире maxLength), берём хотя бы одну графему
            if (splitAt == 0 && graphemes.Count > 0)
            {
                splitAt = 1;
                width = GetGraphemeWidth(graphemes[0]);
            }

            string firstText = string.Concat(graphemes.Take(splitAt));
            string secondText = string.Concat(graphemes.Skip(splitAt));

            var first = new Word
            {
                Text = firstText,
                Width = width,
                Flags = WordFlags.None,
                SourceLineIndex = word.SourceLineIndex,
                SeparatorAfterWord = string.Empty,
            };

            var second = new Word
            {
                Text = secondText,
                Width = CalcGraphemeWidthRange(graphemes, splitAt, graphemes.Count),
                Flags = word.Flags,
                SourceLineIndex = word.SourceLineIndex,
                SeparatorAfterWord = word.SeparatorAfterWord,
            };

            // Capital и Shifted переносятся на first, second — без этих флагов
            if (word.Flags.HasFlag(WordFlags.Capital))
            {
                first.Flags |= WordFlags.Capital;
                second.Flags &= ~WordFlags.Capital;
            }
            if (word.Flags.HasFlag(WordFlags.Shifted))
            {
                first.Flags |= WordFlags.Shifted;
                second.Flags &= ~WordFlags.Shifted;
            }

            return (first, second);
        }
    }
}
