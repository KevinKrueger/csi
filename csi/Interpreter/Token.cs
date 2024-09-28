using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Interpreter
{
    public enum TokenType
    {
        Keyword,
        Identifier,
        Number,
        StringLiteral,
        Operator,
        AssignmentOperator,
        ComparisonOperator,
        LogicalOperator,
        Separator,
        LeftParenthesis,
        RightParenthesis,
        LeftBrace,
        RightBrace,
        Whitespace,
        Comment,
        Dot,
        Comma,
        LessThan,
        GreaterThan
    }

    public class Token
    {
        public TokenType Type;
        public string Value;
        public int LineNumber;
    }

    public class TokenDefinition
    {
        public TokenType Type;
        public string Regex;

        public TokenDefinition(TokenType type, string regex)
        {
            Type = type;
            Regex = regex;
        }
    }
}
