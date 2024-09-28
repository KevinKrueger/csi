using csi.Interpreter;
using csi.Interpreter.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [Test]
        public void ParseProgram_ShouldReturnFunctionNode_ForFunctionDefinition()
        {
            // Arrange
            var code = "function greet(name) { print(\"Hello, \" + name); }";
            var lexer = new Lexer();
            var tokens = lexer.Tokenize(code);
            var parser = new Parser(tokens);

            // Act
            var programNode = parser.ParseProgram();

            // Assert
            Assert.IsNotNull(programNode);
            Assert.AreEqual(1, programNode.Statements.Count);
            Assert.IsInstanceOf<FunctionNode>(programNode.Statements[0]);

            var functionNode = (FunctionNode)programNode.Statements[0];
            Assert.AreEqual("greet", functionNode.Name);
            Assert.AreEqual(1, functionNode.Parameters.Count);
            Assert.AreEqual("name", functionNode.Parameters[0]);
        }

        // Weitere Tests für den Parser...
    }
}
