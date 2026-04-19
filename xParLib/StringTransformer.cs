using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xParLib
{
    /// <summary>
    /// Главный класс преобразования строк. Содержит публичный метод <see cref="Transform"/>
    /// для переформатирования входных строк абзаца.
    /// </summary>
    /// <remarks>Аналог главного цикла main() из par.c (строки 770–930).</remarks>
    public class StringTransformer
    {
        /// <summary>
        /// Преобразует входные строки согласно логике форматирования par.
        /// </summary>
        /// <param name="lines">Входные строки для преобразования.</param>
        /// <param name="options">Параметры форматирования.</param>
        /// <returns>Результат преобразования в виде списка строк.</returns>
        public IReadOnlyList<string> Transform(IReadOnlyList<string> lines, ParOptions options)
        {
            var result = new List<string>();
            int index = 0;

            var protectChars = options.ProtectChars ?? Charset.Parse("");
            var quoteChars = options.QuoteChars ?? Charset.Parse("> ");
            var whiteChars = options.WhiteChars ?? Charset.Parse(" \f\n\r\t\v");

            bool sawNonEmpty = false;
            bool shouldOutputEmpty = false;

            while (index < lines.Count)
            {
                string line = lines[index];
                if (IsProtectedLine(line, protectChars))
                {
                    sawNonEmpty = true;
                    result.Add(line);
                    index++;
                    continue;
                }
                else if (IsBlankLine(line))
                {
                    if (options.Expel)
                    {
                        shouldOutputEmpty = sawNonEmpty;
                        index++;
                        continue;
                    }
                    result.Add(string.Empty);
                    index++;
                    continue;
                }

                sawNonEmpty = true;
                if (shouldOutputEmpty)
                {
                    result.Add(string.Empty);
                    shouldOutputEmpty = false;
                }

                // ReadLines читает абзац до blank/protected/EOF
                var readResult = LineReader.ReadLines(
                    lines, index,
                    protectChars, quoteChars, whiteChars,
                    options.Tab, options.Invis, options.Quote);

                // Обработка абзаца
                ProcessParagraph(readResult.Segments, options, result);
                if (readResult.IsEof)
                {
                    // EOF — выходим
                    break;
                }

                index = readResult.NextIndex;
            }

            return result;
        }

        /// <summary>
        /// Обрабатывает один абзац (результат ReadLines).
        /// </summary>
        private static void ProcessParagraph(
            LineSegment[] segments,
            ParOptions options,
            List<string> result)
        {
            int count = segments.Length;

            // Delimit — определение bodiless строк
            LineReader.Delimit(
                segments, 0, count - 1,
                options.BodyChars ?? Charset.Parse(""),
                options.Repeat, options.Body, options.Div,
                minPrefix: 0, minSuffix: 0);

            // MarkSuperf — разметка superfluous строк
            if (options.Expel)
            {
                LineReader.MarkSuperf(segments, 0, count - 1);
            }

            // Цикл по блокам
            int blockStart = 0;
            while (blockStart < count)
            {
                if (segments[blockStart].Prop.Flags.HasFlag(LineFlags.Bodiless))
                {
                    // Bodiless строка
                    ProcessBodilessLine(segments[blockStart], options, result);
                    blockStart++;
                }
                else
                {
                    // Найти конец блока
                    int blockEnd = blockStart + 1;
                    while (blockEnd < count
                           && !segments[blockEnd].Prop.Flags.HasFlag(LineFlags.Bodiless)
                           && !segments[blockEnd].Prop.Flags.HasFlag(LineFlags.First))
                    {
                        blockEnd++;
                    }

                    // Обычный блок: segments[blockStart .. blockEnd-1]
                    ProcessBlock(segments, blockStart, blockEnd - 1, options, result);
                    blockStart = blockEnd;
                }
            }
        }

        /// <summary>
        /// Обрабатывает bodiless строку.
        /// </summary>
        private static void ProcessBodilessLine(
            LineSegment segment,
            ParOptions options,
            List<string> result)
        {
            bool isInvis = options.Invis;
            bool isExpel = options.Expel;
            bool isInserted = segment.Prop.Flags.HasFlag(LineFlags.Inserted);
            bool isSuperf = segment.Prop.Flags.HasFlag(LineFlags.Superf);

            // Пропускаем, если invis+inserted или expel+superf
            if ((isInvis && isInserted) || (isExpel && isSuperf))
                return;

            int p = segment.Prop.P;
            int s = segment.Prop.S;
            string rc = segment.Prop.Rc ?? " ";
            string line = segment.Line;

            if (options.Repeat == 0 || (rc == " " && s == 0))
            {
                // Обрезать trailing пробелы
                int end = line.Length;
                while (end > 0 && line[end - 1] == ' ')
                    end--;
                result.Add(line.Substring(0, end));
            }
            else
            {
                // Префикс + repeat-символы × n + суффикс
                int n = options.Width - p - s;
                if (n < 0) n = 0;

                var sb = new StringBuilder(options.Width);

                // Префикс
                var prefixGraphemes = LineReader.GetGraphemes(line);
                int prefixTake = Math.Min(p, prefixGraphemes.Count);
                sb.Append(string.Concat(prefixGraphemes.Take(prefixTake)));

                // Repeat-символы
                for (int i = 0; i < n; i++)
                    sb.Append(rc);

                // Суффикс
                int totalGraphemes = prefixGraphemes.Count;
                int suffixStart = totalGraphemes - s;
                if (suffixStart < 0) suffixStart = 0;
                if (s > 0 && suffixStart < totalGraphemes)
                {
                    sb.Append(string.Concat(prefixGraphemes.Skip(suffixStart)));
                }

                result.Add(sb.ToString());
            }
        }

        /// <summary>
        /// Обрабатывает обычный блок строк.
        /// </summary>
        private static void ProcessBlock(
            LineSegment[] segments,
            int blockStart,
            int blockEnd,
            ParOptions options,
            List<string> result)
        {
            int blockLength = blockEnd - blockStart + 1;
            if (blockLength <= 0) return;

            // Вычислить аффиксы
            var affixes = LineReader.SetAffixes(
                segments, blockStart, blockEnd,
                options.BodyChars ?? Charset.Parse(""),
                options.QuoteChars ?? Charset.Parse("> "),
                options.Hang, options.Body, options.Quote,
                options.Prefix, options.Suffix);

            // Проверка: width > prefix + suffix
            if (options.Width <= affixes.Prefix + affixes.Suffix)
            {
                // Ошибка — вернуть строки как есть
                for (int i = blockStart; i <= blockEnd; i++)
                    result.Add(segments[i].Line);
                return;
            }

            // Форматирование
            var formattedLines = ReformatModule.Reformat(
                segments, blockStart, blockEnd, options, affixes);

            result.AddRange(formattedLines);
        }

        /// <summary>
        /// Проверить: строка является protected (первый графем в protectChars).
        /// </summary>
        private static bool IsProtectedLine(string line, Charset protectChars)
        {
            if (string.IsNullOrEmpty(line)) return false;

            var graphemes = LineReader.GetGraphemes(line);
            if (graphemes.Count == 0) return false;

            return protectChars.IsMember(graphemes[0]);
        }

        /// <summary>
        /// Проверить: строка является blank (пустая или только пробелы).
        /// </summary>
        private static bool IsBlankLine(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }
    }
}
