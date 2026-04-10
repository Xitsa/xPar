using System;
using System.Text;

namespace xParLib
{
    /// <summary>
    /// Параметры для переформатирования абзацев, соответствующие оригинальному par (Par 1.53.0).
    /// </summary>
    /// <remarks>
    /// Зависимости параметров (из par.doc):
    /// - <c>Hang</c> и <c>Quote</c> должны быть определены перед вычислением <c>Prefix</c>.
    /// - <c>Fit</c> и <c>Last</c> должны быть определены перед вычислением <c>Touch</c>.
    ///
    /// <c>Prefix</c> и <c>Suffix</c> вычисляются на основе входных строк (comprelen/comsuflen),
    /// поэтому их значение по умолчанию определяется в методе <c>Transform</c>, а не здесь.
    /// <c>Touch</c> вычисляется в <c>Parse</c> как <c>Fit || Last</c>.
    /// </remarks>
    public class ParOptions
    {
        // Целочисленные параметры
        public int Hang { get; set; } = 0;
        public int? Prefix { get; set; }
        public int Repeat { get; set; } = 0;
        public int? Suffix { get; set; }
        public int Tab { get; set; } = 1;
        public int Width { get; set; } = 72;

        // Булевы параметры
        public bool Body { get; set; } = false;
        public bool Cap { get; set; } = false;
        public bool Div { get; set; } = false;
        public bool Err { get; set; } = false;
        public bool Expel { get; set; } = false;
        public bool Fit { get; set; } = false;
        public bool Guess { get; set; } = false;
        public bool Invis { get; set; } = false;
        public bool Just { get; set; } = false;
        public bool Last { get; set; } = false;
        public bool Quote { get; set; } = false;
        public bool Report { get; set; } = false;
        public bool? Touch { get; set; }

        // Параметры справки
        public bool Help { get; set; } = false;
        public bool Version { get; set; } = false;

        /// <summary>
        /// Разбирает аргументы командной строки и возвращает экземпляр ParOptions.
        /// </summary>
        /// <param name="args">Аргументы командной строки</param>
        /// <returns>Экземпляр ParOptions с заполненными параметрами</returns>
        /// <exception cref="ArgumentException">Если аргумент некорректен</exception>
        public static ParOptions Parse(string[] args)
        {
            var options = new ParOptions();

            foreach (var arg in args)
            {
                ParseSingleArg(arg, options);
            }

            // Вычисляем Touch по умолчанию, если не задан
            if (!options.Touch.HasValue)
            {
                options.Touch = options.Fit || options.Last;
            }

            return options;
        }

        private static void ParseSingleArg(string arg, ParOptions options)
        {
            if (arg == null) throw new ArgumentNullException(nameof(arg));

            // Убираем ведущий минус, если есть
            var s = arg.StartsWith("-") ? arg.Substring(1) : arg;

            // Специальные команды
            if (s == "help")
            {
                options.Help = true;
                return;
            }
            if (s == "version")
            {
                options.Version = true;
                return;
            }

            // Обработка B, P, Q, W, Z (наборы символов) — пока пропускаем
            if (s.StartsWith("B") || s.StartsWith("P") || s.StartsWith("Q") ||
                s.StartsWith("W") || s.StartsWith("Z"))
            {
                // TODO: реализовать парсинг наборов символов
                return;
            }

            // Проверка: аргумент начинается с цифры → число без флага
            int pos = 0;
            if (s.Length > 0 && char.IsDigit(s[0]))
            {
                int val = ReadAllDigits(s, 0);
                if (val <= 8)
                    options.Prefix = val;
                else
                    options.Width = val;
                pos = GetDigitsLength(s, 0);
            }

            // Парсинг параметров
            while (pos < s.Length)
            {
                char flag = s[pos];
                pos++;

                // Читаем число, если есть
                int? num = ReadNumber(s, ref pos);

                switch (flag)
                {
                    // Целочисленные параметры
                    case 'h':
                        options.Hang = num ?? 1;
                        break;
                    case 'p':
                        options.Prefix = num; // null = unset
                        break;
                    case 'r':
                        options.Repeat = num ?? 3;
                        break;
                    case 's':
                        options.Suffix = num; // null = unset
                        break;
                    case 'T':
                        options.Tab = num ?? 8;
                        break;
                    case 'w':
                        options.Width = num ?? 79;
                        break;

                    // Булевы параметры
                    case 'b':
                        options.Body = num != 0;
                        break;
                    case 'c':
                        options.Cap = num != 0;
                        break;
                    case 'd':
                        options.Div = num != 0;
                        break;
                    case 'E':
                        options.Err = num != 0;
                        break;
                    case 'e':
                        options.Expel = num != 0;
                        break;
                    case 'f':
                        options.Fit = num != 0;
                        break;
                    case 'g':
                        options.Guess = num != 0;
                        break;
                    case 'i':
                        options.Invis = num != 0;
                        break;
                    case 'j':
                        options.Just = num != 0;
                        break;
                    case 'l':
                        options.Last = num != 0;
                        break;
                    case 'q':
                        options.Quote = num != 0;
                        break;
                    case 'R':
                        options.Report = num != 0;
                        break;
                    case 't':
                        options.Touch = num != 0;
                        break;

                    default:
                        throw new ArgumentException($"Bad argument: {arg}");
                }
            }
        }

        private static int GetDigitsLength(string s, int start)
        {
            int pos = start;
            while (pos < s.Length && char.IsDigit(s[pos]))
            {
                pos++;
            }
            return pos - start;
        }

        private static int ReadAllDigits(string s, int start)
        {
            int pos = start;
            while (pos < s.Length && char.IsDigit(s[pos]))
            {
                pos++;
            }

            var numStr = s.Substring(start, pos - start);
            if (int.TryParse(numStr, out int result) && result >= 0 && result < 10000)
                return result;

            throw new ArgumentException($"Invalid number: {numStr}");
        }

        private static int? ReadNumber(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && char.IsDigit(s[pos]))
            {
                pos++;
            }

            if (pos == start)
                return null;

            var numStr = s.Substring(start, pos - start);
            if (int.TryParse(numStr, out int result) && result >= 0 && result < 10000)
                return result;

            throw new ArgumentException($"Invalid number: {numStr}");
        }
    }
}
