using System;
using System.Collections.Generic;
using System.Text;
using xParLib;

namespace xpar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            
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
            
            // Обрабатываем все строки сразу
            var results = transformer.Transform(lines.ToArray());
            foreach (var result in results)
            {
                Console.WriteLine(result);
            }
        }
    }

}