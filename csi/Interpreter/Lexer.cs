using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace csi.Interpreter
{
    public class Lexer
    {
        private static readonly List<TokenDefinition> tokenDefinitions = new List<TokenDefinition>
        {

            new TokenDefinition(TokenType.Whitespace, @"\s+"),
            new TokenDefinition(TokenType.Comment, @"//.*?$"),
            new TokenDefinition(TokenType.Keyword, @"\b(class|function|if|else|while|for|return|import|new|try|catch|throw|finally|public|private|protected|virtual|override|extends|this|super|interface|implements|break|continue)\b"),
            new TokenDefinition(TokenType.Identifier, @"\b[_a-zA-Z][_a-zA-Z0-9]*\b"),
            new TokenDefinition(TokenType.Number, @"\b\d+(\.\d+)?\b"),
            new TokenDefinition(TokenType.StringLiteral, "\"(\\\\.|[^\"])*\""),
            new TokenDefinition(TokenType.Operator, @"[+\-*/%]"),
            new TokenDefinition(TokenType.AssignmentOperator, @"="),
            new TokenDefinition(TokenType.ComparisonOperator, @"==|!=|<=|>=|<|>"),
            new TokenDefinition(TokenType.LogicalOperator, @"&&|\|\|"),
            new TokenDefinition(TokenType.Separator, @"[;,]"),
            new TokenDefinition(TokenType.LeftParenthesis, @"\("),
            new TokenDefinition(TokenType.RightParenthesis, @"\)"),
            new TokenDefinition(TokenType.LeftBrace, @"\{"),
            new TokenDefinition(TokenType.RightBrace, @"\}"),
            new TokenDefinition(TokenType.Dot, @"\."),
            new TokenDefinition(TokenType.Comma, @","),
            new TokenDefinition(TokenType.LessThan, @"<"),
            new TokenDefinition(TokenType.GreaterThan, @">")
        };

        public List<Token> Tokenize(string code)
        {
            var tokens = new List<Token>();
            int index = 0;
            int lineNumber = 1;

            while (index < code.Length)
            {
                bool matchFound = false;

                foreach (var definition in tokenDefinitions)
                {
                    var regex = new Regex($"^{definition.Regex}", RegexOptions.Multiline);
                    var match = regex.Match(code.Substring(index));

                    if (match.Success)
                    {
                        if (definition.Type != TokenType.Whitespace && definition.Type != TokenType.Comment)
                        {
                            var token = new Token
                            {
                                Type = definition.Type,
                                Value = match.Value,
                                LineNumber = lineNumber
                            };
                            tokens.Add(token);
                            Console.WriteLine($"Tokenized: {token.Type} '{token.Value}'");
                        }

                        lineNumber += match.Value.Count(c => c == '\n');
                        index += match.Length;
                        matchFound = true;
                        break;
                    }
                }

                if (!matchFound)
                {
                    throw new Exception($"Unbekanntes Symbol bei Zeile {lineNumber}, Index {index}: '{code[index]}'");
                }
            }

            return tokens;
        }

    }
}
