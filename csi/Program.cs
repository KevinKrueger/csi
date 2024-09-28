using csi.Interpreter;
using System;

namespace csi // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Überprüfen, ob eine Datei angegeben wurde
            if (args.Length == 0)
            {
                Console.WriteLine("Bitte geben Sie den Pfad zu einer .mycs-Datei an.");
                return;
            }

            var filePath = args[0];

            // Überprüfen, ob die Datei existiert und die richtige Endung hat
            if (!File.Exists(filePath) || Path.GetExtension(filePath) != ".mycs")
            {
                Console.WriteLine("Die angegebene Datei existiert nicht oder hat nicht die Endung .mycs.");
                return;
            }

            var code = File.ReadAllText(filePath);

            try
            {
                // Lexer
                var lexer = new Lexer();
                var tokens = lexer.Tokenize(code);

                // Parser
                var parser = new Parser(tokens);
                var ast = parser.ParseProgram();

                // Interpreter
                var interpreter = new Interpreter.Interpreter();
                interpreter.Execute(ast);
            }
            catch (InterpreterException ex)
            {
                Console.WriteLine("Interpreter-Fehler: " + ex.ExceptionValue);
                Console.WriteLine("Stack-Trace:");
                foreach (var frame in ex.StackTrace)
                {
                    Console.WriteLine("  bei " + frame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

        }
    }
}