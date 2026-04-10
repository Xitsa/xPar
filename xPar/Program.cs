using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using xParLib;

namespace xpar
{
    class Program
    {
        private const string Version = "xpar 1.0.0";

        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            // 1. Инициализация charset'ов из переменных окружения (как в par.c)
            var bodyChars = Charset.Parse(Environment.GetEnvironmentVariable("PARBODY") ?? "");
            var protectChars = Charset.Parse(Environment.GetEnvironmentVariable("PARPROTECT") ?? "");
            var quoteChars = Charset.Parse(Environment.GetEnvironmentVariable("PARQUOTE") ?? "> ");
            var whiteChars = Charset.Parse(" \f\n\r\t\v");
            var terminalChars = Charset.Parse(".?!:");

            // 2. Обработка PARINIT (если задан — аргументы из него идут перед командной строкой)
            string[] allArgs;
            var parinit = Environment.GetEnvironmentVariable("PARINIT");
            if (!string.IsNullOrEmpty(parinit))
            {
                // Разделяем по начальным whitespace (до обработки опций W)
                var initArgs = parinit.Split(new[] { ' ', '\f', '\n', '\r', '\t', '\v' },
                    StringSplitOptions.RemoveEmptyEntries);
                allArgs = initArgs.Concat(args).ToArray();
            }
            else
            {
                allArgs = args;
            }

            // 3. Разбор всех аргументов (PARINIT + командная строка)
            var options = ParOptions.Parse(allArgs, bodyChars, protectChars, quoteChars, whiteChars, terminalChars);

            // 4. Обработка help
            if (options.Help)
            {
                PrintHelp();
                return;
            }

            // 5. Обработка version
            if (options.Version)
            {
                Console.WriteLine(Version);
                return;
            }

            // 6. Чтение stdin и трансформация
            var transformer = new StringTransformer();
            var lines = new List<string>();
            string? curLine;

            do
            {
                curLine = Console.ReadLine();
                if (curLine != null)
                {
                    lines.Add(curLine);
                }
            }
            while (curLine != null);

            var results = transformer.Transform(lines);
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }

        /// <summary>
        /// Выводит краткую справку об опциях.
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Usage: xpar [help] [version] [B<op><set>] [P<op><set>] [Q<op><set>]");
            Console.WriteLine("           [W<op><set>] [Z<op><set>] [h[<hang>]] [p[<prefix>]]");
            Console.WriteLine("           [r[<repeat>]] [s[<suffix>]] [T[<Tab>]] [w[<width>]] [b[<body>]]");
            Console.WriteLine("           [c[<cap>]] [d[<div>]] [E[<Err>]] [e[<expel>]] [f[<fit>]]");
            Console.WriteLine("           [g[<guess>]] [i[<invis>]] [j[<just>]] [l[<last>]] [q[<quote>]]");
            Console.WriteLine("           [R[<Report>]] [t[<touch>]]");
            Console.WriteLine();
            Console.WriteLine("Charset options:");
            Console.WriteLine("  B<op><set>  Body characters");
            Console.WriteLine("  P<op><set>  Protective characters");
            Console.WriteLine("  Q<op><set>  Quote characters");
            Console.WriteLine("  W<op><set>  White characters");
            Console.WriteLine("  Z<op><set>  Terminal characters");
            Console.WriteLine("  <op> is = (set), + (add), or - (remove)");
            Console.WriteLine();
            Console.WriteLine("Environment variables:");
            Console.WriteLine("  PARBODY     Body characters (charset syntax)");
            Console.WriteLine("  PARPROTECT  Protective characters (charset syntax)");
            Console.WriteLine("  PARQUOTE    Quote characters (charset syntax, default: '> ')");
            Console.WriteLine("  PARINIT     Command-line arguments (separated by whitespace)");
            Console.WriteLine();
            Console.WriteLine("Recommended PARINIT: rTbgqR B=.,?'_A_a_@ Q=_s>|");
        }
    }
}
