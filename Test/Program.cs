using System;
using System.IO;
using System.Linq;
using System.Reflection;
using PL0Compiler;
namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var sourceCode = args.FirstOrDefault(i =>
                !string.IsNullOrWhiteSpace(i) && File.Exists(i) &&
                Path.GetExtension(i).Equals(".pl0", StringComparison.CurrentCultureIgnoreCase));

            if (sourceCode == null)
            {
                var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (Directory.Exists(folderPath))
                {
                    sourceCode = Directory.EnumerateFiles(folderPath, "*.pl0").FirstOrDefault();
                }
            }

            if (sourceCode == null)
            {
                Console.WriteLine("No source code provided! Press any key to exit");
                Console.ReadLine();
                return;
            }

            new Compiler(config =>
            {
                config.Execute = true;
                config.SourceCodeFilePath = sourceCode;
                config.PrintAssemblyCode = args.Any(i => i.Equals("-a", StringComparison.CurrentCultureIgnoreCase));
                config.PrintLexemesOnScreen = args.Any(i => i.Equals("-l", StringComparison.CurrentCultureIgnoreCase));
                config.VmConfiguration.PrintExecutionTraceOnScreen =
                    args.Any(i => i.Equals("-v", StringComparison.CurrentCultureIgnoreCase));
            }).Start();
        }
    }
}