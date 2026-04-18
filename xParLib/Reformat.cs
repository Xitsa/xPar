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
        List<string> Prefixes,
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
            var prefixes = new List<string>(numIn);
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
                        Prefixes: new List<string>(),
                        Suffixes: new List<string>(),
                        L: 0,
                        ErrorMessage: $"Line {i - startIndex + 1} shorter than <prefix> + <suffix> = {prefix} + {suffix} = {affix}");
                }

                prefixes.Add(GetPrefix(line, prefix));
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
                            Prefixes: new List<string>(),
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
                Prefixes: prefixes,
                Suffixes: suffixes,
                L: L,
                ErrorMessage: null);
        }

        //
        // SimpleBreaks — выбор разрывов строк (максимизация кратчайшей строки)
        //

        /// <summary>
        /// Выбирает разрывы строк, максимизируя длину кратчайшей строки.
        /// </summary>
        /// <remarks>
        /// Аналог simplebreaks() из reformat.c (строки 98–136).
        /// Использует динамическое программирование: идёт с конца к началу,
        /// для каждого слова находит оптимальный разрыв.
        /// </remarks>
        /// <param name="words">Список слов (изменяется: поля Score и NextLine).</param>
        /// <param name="L">Максимальная длина строки.</param>
        /// <param name="last">Учитывать ли последнюю строку при расчёте.</param>
        /// <returns>
        /// Длина кратчайшей строки; -1 если слово шире L; L если слов нет.
        /// </returns>
        public static int SimpleBreaks(List<Word> words, int L, bool last)
        {
            if (words.Count == 0) return L;

            int n = words.Count;

            // Фаза 1: инициализация хвоста (с конца к началу)
            // Все слова, которые помещаются на одну строку с конца
            int idx = n - 1;
            int linelen = words[idx].Width;

            while (idx >= 0 && linelen <= L)
            {
                words[idx].Score = last ? linelen : L;
                words[idx].NextLine = null;

                // Переход к предыдущему слову
                idx--;
                if (idx < 0) break;

                // Добавляем: shifted-пробел + обычный пробел + ширина предыдущего слова
                linelen += (words[idx + 1].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0);
                linelen += 1 + words[idx].Width;
            }

            // Фаза 2: основной DP для оставшихся слов
            while (idx >= 0)
            {
                words[idx].Score = -1;

                linelen = words[idx].Width;
                int j = idx + 1;

                while (j < n && linelen <= L)
                {
                    int score = words[j].Score;
                    if (linelen < score) score = linelen;

                    if (score >= words[idx].Score)
                    {
                        words[idx].NextLine = j;
                        words[idx].Score = score;
                    }

                    linelen += 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                    j++;
                }

                idx--;
            }

            return words[0].Score;
        }

        //
        // NormalBreaks — выбор разрывов строк (без justification)
        //

        /// <summary>
        /// Выбирает разрывы строк согласно политике для just=0.
        /// </summary>
        /// <remarks>
        /// Аналог normalbreaks() из reformat.c (строки 138–196).
        /// Три этапа: оптимизация ширины (fit), определение shortest, минимизация суммы квадратов.
        /// </remarks>
        /// <param name="words">Список слов (изменяется: поля Score и NextLine).</param>
        /// <param name="L">Максимальная длина строки.</param>
        /// <param name="fit">Минимизировать разницу между строками.</param>
        /// <param name="last">Учитывать ли последнюю строку.</param>
        /// <returns>Сообщение об ошибке или null при успехе.</returns>
        public static string? NormalBreaks(List<Word> words, int L, bool fit, bool last)
        {
            if (words.Count == 0) return null;

            int target = L;

            // === Этап A: Оптимизация ширины (только при fit=1) ===
            if (fit)
            {
                int bestScore = L + 1;
                for (int tryL = L; ; tryL--)
                {
                    int shortest = SimpleBreaks(words, tryL, last);
                    if (shortest < 0) break;
                    if (tryL - shortest < bestScore)
                    {
                        target = tryL;
                        bestScore = target - shortest;
                    }
                }
            }

            // === Этап B: Определение минимальной длины строки ===
            int shortestLen = SimpleBreaks(words, target, last);
            if (shortestLen < 0)
                return "Cannot format paragraph (impossibility 1)";

            // === Этап C: Минимизация суммы квадратов отклонений ===
            int n = words.Count;
            for (int idx = n - 1; idx >= 0; idx--)
            {
                words[idx].Score = -1;

                int linelen = words[idx].Width;
                int j = idx + 1;

                while (j < n && linelen <= target)
                {
                    int extra = target - linelen;
                    int minlen = shortestLen;
                    int score = words[j].Score;

                    if (linelen >= minlen && score >= 0)
                    {
                        score += extra * extra;
                        if (words[idx].Score < 0 || score <= words[idx].Score)
                        {
                            words[idx].NextLine = j;
                            words[idx].Score = score;
                        }
                    }

                    linelen += 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                    j++;
                }

                // Случай: все оставшиеся слова помещаются на одну строку
                // После цикла j >= n, linelen = полная длина от idx до конца
                if (j >= n && linelen <= target)
                {
                    int extra = target - linelen;
                    int minlen = shortestLen;
                    int score = 0;
                    if (!last)
                        extra = minlen = 0;

                    if (linelen >= minlen && score >= 0)
                    {
                        score += extra * extra;
                        if (words[idx].Score < 0 || score <= words[idx].Score)
                        {
                            words[idx].NextLine = null;
                            words[idx].Score = score;
                        }
                    }
                }
            }

            if (words[0].Score < 0)
                return "Cannot format paragraph (impossibility 2)";

            return null;
        }

        //
        // JustBreaks — выбор разрывов строк (justification)
        //

        /// <summary>
        /// Выбирает разрывы строк согласно политике для just=1 (выравнивание по обоим краям).
        /// </summary>
        /// <remarks>
        /// Аналог justbreaks() из reformat.c (строки 211–289).
        /// Две фазы: минимизация максимального межсловного промежутка,
        /// затем минимизация суммы квадратов дополнительных пробелов.
        /// </remarks>
        /// <param name="words">Список слов (изменяется: поля Score и NextLine).</param>
        /// <param name="L">Максимальная длина строки.</param>
        /// <param name="last">Учитывать ли последнюю строку.</param>
        /// <returns>Сообщение об ошибке или null при успехе.</returns>
        public static string? JustBreaks(List<Word> words, int L, bool last)
        {
            if (words.Count == 0) return null;

            int n = words.Count;

            // === Фаза 1: Минимизация максимального межсловного промежутка ===
            for (int idx = n - 1; idx >= 0; idx--)
            {
                words[idx].Score = L;

                int extra = L - words[idx].Width;
                int numgaps = 0;
                int j = idx + 1;

                while (j <= n && extra >= 0)
                {
                    int gap = numgaps > 0 ? (extra + numgaps - 1) / numgaps : L;
                    int score = (j < n) ? words[j].Score : 0;

                    if (j >= n && !last)
                        gap = 0;

                    if (gap > score) score = gap;

                    if (score < words[idx].Score)
                    {
                        words[idx].NextLine = (j < n) ? j : null;
                        words[idx].Score = score;
                    }

                    if (j >= n) break;

                    extra -= 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                    numgaps++;
                    j++;
                }
            }

            int maxgap = words[0].Score;
            if (maxgap >= L)
                return "Cannot justify.";

            // === Фаза 2: Минимизация суммы квадратов дополнительных пробелов ===
            for (int idx = n - 1; idx >= 0; idx--)
            {
                words[idx].Score = -1;

                int extra = L - words[idx].Width;
                int numgaps = 0;
                int j = idx + 1;

                while (j <= n && extra >= 0)
                {
                    int gap = numgaps > 0 ? (extra + numgaps - 1) / numgaps : L;

                    if (j >= n)
                    {
                        if (!last)
                        {
                            words[idx].NextLine = null;
                            words[idx].Score = 0;
                            break;
                        }
                        // last=true: score = 0
                    }

                    int score = (j < n) ? words[j].Score : 0;

                    if (gap <= maxgap && score >= 0)
                    {
                        int numbiggaps = (numgaps > 0) ? extra % numgaps : 0;
                        int baseGap = extra / numgaps;
                        score += baseGap * (extra + numbiggaps) + numbiggaps;

                        if (words[idx].Score < 0 || score <= words[idx].Score)
                        {
                            words[idx].NextLine = (j < n) ? j : null;
                            words[idx].Score = score;
                        }
                    }

                    if (j >= n) break;

                    extra -= 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                    numgaps++;
                    j++;
                }
            }

            if (words[0].Score < 0)
                return "Cannot format paragraph (impossibility 3)";

            return null;
        }

        //
        // ConstructLines — построение выходных строк
        //

        /// <summary>
        /// Строит выходные строки абзаца из слов с расставленными разрывами.
        /// </summary>
        /// <remarks>
        /// Аналог блока "Construct the lines" из reformat.c (строки 459–522).
        /// Включает фазу touch и цикл построения строк с префиксами, телом и суффиксами.
        /// </remarks>
        /// <param name="words">Список слов с расставленными NextLine.</param>
        /// <param name="prefixes">Префиксы исходных строк.</param>
        /// <param name="suffixes">Суффиксы исходных строк.</param>
        /// <param name="L">Длина тела строки (может измениться при touch).</param>
        /// <param name="affixes">Результат вычисления аффиксов IP.</param>
        /// <param name="hang">Минимальное количество строк.</param>
        /// <param name="just">Режим justification.</param>
        /// <param name="last">Учитывать ли последнюю строку.</param>
        /// <param name="touch">Изменить L на фактическую длину.</param>
        /// <returns>Список выходных строк.</returns>
        public static IReadOnlyList<string> ConstructLines(
            List<Word> words,
            List<string> prefixes,
            List<string> suffixes,
            int L,
            SetAffixesResult affixes,
            int hang,
            bool just,
            bool last,
            bool touch)
        {
            int numIn = prefixes.Count;
            int prefix = affixes.Prefix;
            int suffix = affixes.Suffix;
            int affix = prefix + suffix;
            var result = new List<string>();

            // Фаза touch: изменить L на фактическую длину самой длинной строки
            if (!just && touch && words.Count > 0)
            {
                int maxLen = 0;
                int? wordIdx = 0;
                while (wordIdx.HasValue)
                {
                    int lineLen = words[wordIdx.Value].Width;
                    int j = wordIdx.Value + 1;
                    while (j < words.Count && j != words[wordIdx.Value].NextLine)
                    {
                        lineLen += 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                        j++;
                    }
                    if (lineLen > maxLen) maxLen = lineLen;
                    wordIdx = words[wordIdx.Value].NextLine;
                }
                L = maxLen;
            }

            int numout = 0;
            int? wordIdx2 = words.Count > 0 ? 0 : null;

            while (numout < hang || wordIdx2.HasValue)
            {
                // Шаг 1: Вычисление numgaps и extra
                int numgaps = 0;
                int extra = L;
                if (wordIdx2.HasValue)
                {
                    extra = L - words[wordIdx2.Value].Width;
                    int j = wordIdx2.Value + 1;
                    while (j < words.Count && j != words[wordIdx2.Value].NextLine)
                    {
                        numgaps++;
                        extra -= 1 + (words[j].Flags.HasFlag(WordFlags.Shifted) ? 1 : 0) + words[j].Width;
                        j++;
                    }
                }

                // Шаг 2: Вычисление linelen
                bool hasNextLine = wordIdx2.HasValue && words[wordIdx2.Value].NextLine.HasValue;
                int linelen;
                if (suffix > 0 || (just && (hasNextLine || last)))
                    linelen = L + affix;
                else if (wordIdx2.HasValue)
                    linelen = prefix + L - extra;
                else
                    linelen = prefix;

                var sb = new StringBuilder(linelen);

                // Шаг 3: Копирование префикса
                string prefixText;
                if (numout < numIn)
                    prefixText = prefixes[numout];
                else if (numIn > hang)
                    prefixText = prefixes[numIn - 1];
                else
                    prefixText = prefixes[numIn - 1];

                var prefixGraphemes = LineReader.GetGraphemes(prefixText);
                int prefixGraphemeCount = prefixGraphemes.Count;

                if (numout < numIn || numIn > hang)
                {
                    // Копируем первые prefix графем
                    int take = Math.Min(prefix, prefixGraphemeCount);
                    sb.Append(string.Concat(prefixGraphemes.Take(take)));
                    // Дополняем пробелами если нужно
                    for (int i = take; i < prefix; i++)
                        sb.Append(' ');
                }
                else
                {
                    // numout >= numIn && numIn <= hang: fallback
                    int take = Math.Min(affixes.AugmentedFallbackPre, prefixGraphemeCount);
                    take = Math.Min(take, prefix);
                    sb.Append(string.Concat(prefixGraphemes.Take(take)));
                    // Дополняем пробелами до prefix
                    for (int i = take; i < prefix; i++)
                        sb.Append(' ');
                }

                // Шаг 4: Копирование тела (слов)
                if (wordIdx2.HasValue)
                {
                    int phase = numgaps / 2;
                    int j = wordIdx2.Value;
                    while (true)
                    {
                        sb.Append(words[j].Text);

                        int nextJ = j + 1;
                        if (nextJ >= words.Count || nextJ == words[wordIdx2.Value].NextLine)
                            break;

                        sb.Append(' ');

                        // Justification: распределить дополнительные пробелы
                        if (just && (hasNextLine || last) && numgaps > 0)
                        {
                            phase += extra;
                            while (phase >= numgaps)
                            {
                                sb.Append(' ');
                                phase -= numgaps;
                            }
                        }

                        // Shifted: дополнительный пробел
                        if (words[nextJ].Flags.HasFlag(WordFlags.Shifted))
                            sb.Append(' ');

                        j = nextJ;
                    }
                }

                // Шаг 5: Заполнение пробелами до linelen - affix
                int bodyEndLen = linelen - affix;
                while (sb.Length < bodyEndLen)
                    sb.Append(' ');

                // Шаг 6: Копирование суффикса
                string suffixText;
                if (numout < numIn)
                    suffixText = suffixes[numout];
                else if (numIn > hang)
                    suffixText = suffixes[numIn - 1];
                else
                    suffixText = suffixes[numIn - 1];

                var suffixGraphemes = LineReader.GetGraphemes(suffixText);
                int suffixGraphemeCount = suffixGraphemes.Count;

                if (numout < numIn || numIn > hang)
                {
                    // Копируем последние suffix графем
                    int take = Math.Min(suffix, suffixGraphemeCount);
                    int start = suffixGraphemeCount - take;
                    sb.Append(string.Concat(suffixGraphemes.Skip(start)));
                    // Дополняем пробелами если нужно
                    for (int i = take; i < suffix; i++)
                        sb.Append(' ');
                }
                else
                {
                    // numout >= numIn && numIn <= hang: fallback
                    int take = Math.Min(affixes.FallbackSuf, suffixGraphemeCount);
                    take = Math.Min(take, suffix);
                    int start = Math.Max(0, suffixGraphemeCount - take);
                    sb.Append(string.Concat(suffixGraphemes.Skip(start)));
                    // Дополняем пробелами до suffix
                    for (int i = take; i < suffix; i++)
                        sb.Append(' ');
                }

                result.Add(sb.ToString());
                numout++;

                if (wordIdx2.HasValue)
                    wordIdx2 = words[wordIdx2.Value].NextLine;
            }

            return result;
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
        /// Извлекает префикс строки — первые <paramref name="prefixCount"/> графем.
        /// </summary>
        private static string GetPrefix(string line, int prefixCount)
        {
            if (prefixCount == 0) return string.Empty;

            var graphemes = LineReader.GetGraphemes(line);
            int count = Math.Min(prefixCount, graphemes.Count);
            return string.Concat(graphemes.Take(count));
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
