using csi.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Tests
{
    [TestFixture]
    public class InterpreterTests
    {
        [Test]
        public void Interpreter_ShouldPrintVariableValue()
        {
            // Arrange
            var code = "a = 42; print(a);";
            var output = CaptureOutput(() =>
            {
                var lexer = new Lexer();
                var tokens = lexer.Tokenize(code);
                var parser = new Parser(tokens);
                var programNode = parser.ParseProgram();
                var interpreter = new Interpreter.Interpreter();
                interpreter.Execute(programNode);
            });

            // Assert
            Assert.AreEqual("42", output.Trim());
        }

        [Test]
        public void Interpreter_ShouldExecuteFunction()
        {
            // Arrange
            var code = @"
                function greet() {
                    print(""Hello"");
                }
                greet();
            ";
            var output = CaptureOutput(() =>
            {
                var lexer = new Lexer();
                var tokens = lexer.Tokenize(code);
                var parser = new Parser(tokens);
                var programNode = parser.ParseProgram();
                var interpreter = new Interpreter.Interpreter();
                interpreter.Execute(programNode);
            });

            // Assert
            Assert.AreEqual("Hello", output.Trim());
        }

        // Weitere Tests für den Interpreter...

        private string CaptureOutput(TestDelegate action)
        {
            using (var sw = new StringWriter())
            {
                var originalOut = Console.Out;
                Console.SetOut(sw);

                action();

                Console.SetOut(originalOut);
                return sw.ToString();
            }
        }
    }
}
