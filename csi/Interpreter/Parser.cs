using csi.Interpreter.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Interpreter
{
    public class Parser
    {
        private List<Token> tokens;
        private int position = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token CurrentToken => position < tokens.Count ? tokens[position] : null;

        private void Advance() => position++;

        public ProgramNode ParseProgram()
        {
            var program = new ProgramNode();

            while (CurrentToken != null)
            {
                var statement = ParseStatement();
                if (statement != null)
                {
                    program.Statements.Add(statement);
                }
                else
                {
                    break;
                }
            }

            return program;
        }

        private ASTNode ParseStatement()
        {
            if (CurrentToken == null)
                return null;

            if (CurrentToken.Type == TokenType.Keyword)
            {
                switch (CurrentToken.Value)
                {
                    case "class":
                        return ParseClassDeclaration();
                    case "function":
                        return ParseFunctionDeclaration();
                    case "if":
                        return ParseIfStatement();
                    case "while":
                        return ParseWhileStatement();
                    case "for":
                        return ParseForStatement();
                    case "return":
                        return ParseReturnStatement();
                    case "try":
                        return ParseTryCatchFinally();
                    case "throw":
                        return ParseThrowStatement();
                    case "import":
                        return ParseImportStatement();
                    case "break":
                        Advance(); // 'break'
                        return new BreakNode();
                    case "continue":
                        Advance(); // 'continue'
                        return new ContinueNode();
                    default:
                        throw new Exception("Unbekanntes Schlüsselwort: " + CurrentToken.Value);
                }
            }
            else if (CurrentToken.Type == TokenType.Identifier)
            {
                if (PeekNextToken()?.Type == TokenType.AssignmentOperator)
                {
                    return ParseAssignment();
                }
                else
                {
                    var expr = ParseExpression();
                    return expr;
                }
            }
            else
            {
                return null;
            }
        }

        private ASTNode ParseAssignment()
        {
            var variableName = CurrentToken.Value;
            Advance(); // Identifier
            Advance(); // '='
            var expression = ParseExpression();
            return new AssignmentNode { VariableName = variableName, Expression = expression, LineNumber = CurrentToken.LineNumber };
        }

        private ExpressionNode ParseExpression()
        {
            return ParseLogicalOrExpression();
        }

        private ExpressionNode ParseLogicalOrExpression()
        {
            var left = ParseLogicalAndExpression();

            while (CurrentToken != null && CurrentToken.Type == TokenType.LogicalOperator && CurrentToken.Value == "||")
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseLogicalAndExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseLogicalAndExpression()
        {
            var left = ParseEqualityExpression();

            while (CurrentToken != null && CurrentToken.Type == TokenType.LogicalOperator && CurrentToken.Value == "&&")
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseEqualityExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseEqualityExpression()
        {
            var left = ParseRelationalExpression();

            while (CurrentToken != null && CurrentToken.Type == TokenType.ComparisonOperator)
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseRelationalExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseRelationalExpression()
        {
            var left = ParseAdditiveExpression();

            while (CurrentToken != null && (CurrentToken.Value == "<" || CurrentToken.Value == ">" || CurrentToken.Value == "<=" || CurrentToken.Value == ">="))
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseAdditiveExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseAdditiveExpression()
        {
            var left = ParseMultiplicativeExpression();

            while (CurrentToken != null && (CurrentToken.Value == "+" || CurrentToken.Value == "-"))
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseMultiplicativeExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseMultiplicativeExpression()
        {
            var left = ParseUnaryExpression();

            while (CurrentToken != null && (CurrentToken.Value == "*" || CurrentToken.Value == "/" || CurrentToken.Value == "%"))
            {
                var op = CurrentToken.Value;
                Advance();
                var right = ParseUnaryExpression();
                left = new BinaryOperationNode { Left = left, Operator = op, Right = right };
            }

            return left;
        }

        private ExpressionNode ParseUnaryExpression()
        {
            if (CurrentToken != null && (CurrentToken.Value == "+" || CurrentToken.Value == "-"))
            {
                var op = CurrentToken.Value;
                Advance();
                var expr = ParsePrimaryExpression();
                return new UnaryOperationNode { Operator = op, Operand = expr };
            }
            else
            {
                return ParsePrimaryExpression();
            }
        }

        private ExpressionNode ParsePrimaryExpression()
        {
            if (CurrentToken == null)
                throw new Exception("Unerwartetes Ende der Eingabe");

            if (CurrentToken.Value == "new")
            {
                Advance(); // 'new'
                var className = CurrentToken.Value;
                Advance(); // Klassenname
                return new NewExpressionNode { ClassName = className };
            }
            else if (CurrentToken.Value == "super")
            {
                Advance(); // 'super'
                ExpectTokenValue(".");
                Advance(); // '.'
                var methodName = CurrentToken.Value;
                Advance(); // Methodenname
                var methodCall = ParseMethodCall(new SuperExpressionNode { });
                return new SuperCallNode
                {
                    MethodName = methodName,
                    Arguments = ((MethodCallNode)methodCall).Arguments
                };
            }
            else if (CurrentToken.Type == TokenType.Number)
            {
                var numberNode = new NumberNode { Value = double.Parse(CurrentToken.Value) };
                Advance();
                return numberNode;
            }
            else if (CurrentToken.Type == TokenType.StringLiteral)
            {
                var stringValue = CurrentToken.Value.Substring(1, CurrentToken.Value.Length - 2); // Entferne die Anführungszeichen
                var stringNode = new StringNode { Value = stringValue };
                Advance();
                return stringNode;
            }
            else if (CurrentToken.Type == TokenType.Identifier)
            {
                // Überprüfen, ob es sich um einen Funktionsaufruf handelt
                if (PeekNextToken()?.Value == "(")
                {
                    return ParseFunctionCall();
                }
                else
                {
                    var expr = ParseAtom();
                    while (CurrentToken != null && CurrentToken.Value == ".")
                    {
                        Advance(); // '.'
                        var memberName = CurrentToken.Value;
                        Advance(); // Membername
                        expr = new MemberAccessNode { Object = expr, MemberName = memberName };

                        // Überprüfen auf Methodenaufruf nach Memberzugriff
                        if (CurrentToken != null && CurrentToken.Value == "(")
                        {
                            expr = ParseMethodCall(expr);
                        }
                    }
                    return expr;
                }
            }
            else if (CurrentToken.Value == "(")
            {
                Advance(); // '('
                var expr = ParseExpression();
                ExpectTokenValue(")");
                Advance(); // ')'
                return expr;
            }
            else
            {
                throw new Exception("Unerwartetes Token: " + CurrentToken.Value);
            }
        }


        private ExpressionNode ParseAtom()
        {
            var token = CurrentToken;
            Advance(); // Identifier
            return new VariableNode { Name = token.Value };
        }

        private ExpressionNode ParseMethodCall(ExpressionNode target)
        {
            Advance(); // '('
            var arguments = new List<ExpressionNode>();

            while (CurrentToken != null && CurrentToken.Value != ")")
            {
                var arg = ParseExpression();
                arguments.Add(arg);

                if (CurrentToken.Value == ",")
                {
                    Advance(); // ','
                }
            }

            ExpectTokenValue(")");
            Advance(); // ')'

            return new MethodCallNode { Target = target, Arguments = arguments };
        }

        private FunctionCallNode ParseFunctionCall()
        {
            var functionName = CurrentToken.Value;
            Advance(); // Funktionsname
            ExpectTokenValue("(");
            Advance(); // '('

            var arguments = new List<ExpressionNode>();
            while (CurrentToken != null && CurrentToken.Value != ")")
            {
                var arg = ParseExpression();
                arguments.Add(arg);

                if (CurrentToken.Value == ",")
                {
                    Advance(); // ','
                }
            }

            ExpectTokenValue(")");
            Advance(); // ')'

            return new FunctionCallNode { FunctionName = functionName, Arguments = arguments };
        }


        private ASTNode ParseIfStatement()
        {
            Advance(); // 'if'
            ExpectTokenValue("(");
            Advance(); // '('
            var condition = ParseExpression();
            ExpectTokenValue(")");
            Advance(); // ')'
            var thenBranch = ParseBlock();
            ASTNode elseBranch = null;

            if (CurrentToken != null && CurrentToken.Value == "else")
            {
                Advance(); // 'else'
                elseBranch = ParseBlock();
            }

            return new IfNode { Condition = condition, ThenBranch = thenBranch, ElseBranch = elseBranch };
        }

        private ASTNode ParseWhileStatement()
        {
            Advance(); // 'while'
            ExpectTokenValue("(");
            Advance(); // '('
            var condition = ParseExpression();
            ExpectTokenValue(")");
            Advance(); // ')'
            var body = ParseBlock();
            return new WhileNode { Condition = condition, Body = body };
        }

        private ASTNode ParseForStatement()
        {
            Advance(); // 'for'
            ExpectTokenValue("(");
            Advance(); // '('
            var initializer = ParseStatement();
            ExpectTokenValue(";");
            Advance(); // ';'
            var condition = ParseExpression();
            ExpectTokenValue(";");
            Advance(); // ';'
            var iterator = ParseExpression();
            ExpectTokenValue(")");
            Advance(); // ')'
            var body = ParseBlock();
            return new ForNode { Initializer = initializer, Condition = condition, Iterator = iterator, Body = body };
        }

        private ASTNode ParseBlock()
        {
            if (CurrentToken.Value == "{")
            {
                Advance(); // '{'
                var statements = new List<ASTNode>();

                while (CurrentToken != null && CurrentToken.Value != "}")
                {
                    var stmt = ParseStatement();
                    statements.Add(stmt);
                }

                ExpectTokenValue("}");
                Advance(); // '}'
                return new BlockNode { Statements = statements };
            }
            else
            {
                // Einzelne Anweisung ohne geschweifte Klammern
                var stmt = ParseStatement();
                return new BlockNode { Statements = new List<ASTNode> { stmt } };
            }
        }

        private void ExpectTokenValue(string value)
        {
            if (CurrentToken == null || CurrentToken.Value != value)
            {
                throw new Exception($"Erwartet '{value}', aber gefunden '{CurrentToken?.Value}'");
            }
        }

        private Token PeekNextToken()
        {
            return position + 1 < tokens.Count ? tokens[position + 1] : null;
        }

        private ASTNode ParseFunctionDeclaration()
        {
            Advance(); // 'function'
            var functionName = CurrentToken.Value;
            Advance(); // Funktionsname
            ExpectTokenValue("(");
            Advance(); // '('

            var parameters = new List<string>();
            while (CurrentToken != null && CurrentToken.Value != ")")
            {
                if (CurrentToken.Type != TokenType.Identifier)
                    throw new Exception("Parametername erwartet");

                parameters.Add(CurrentToken.Value);
                Advance();

                if (CurrentToken.Value == ",")
                {
                    Advance(); // ','
                }
            }

            ExpectTokenValue(")");
            Advance(); // ')'

            var body = ParseBlock();

            return new FunctionNode { Name = functionName, Parameters = parameters, Body = body };
        }

        private ASTNode ParseReturnStatement()
        {
            Advance(); // 'return'
            var expression = ParseExpression();
            return new ReturnNode { Expression = expression };
        }

        private ASTNode ParseTryCatchFinally()
        {
            Advance(); // 'try'
            var tryBlock = ParseBlock();

            ASTNode catchBlock = null;
            string exceptionVariable = null;

            if (CurrentToken != null && CurrentToken.Value == "catch")
            {
                Advance(); // 'catch'
                ExpectTokenValue("(");
                Advance(); // '('
                exceptionVariable = CurrentToken.Value;
                Advance(); // Variablenname
                ExpectTokenValue(")");
                Advance(); // ')'
                catchBlock = ParseBlock();
            }

            ASTNode finallyBlock = null;
            if (CurrentToken != null && CurrentToken.Value == "finally")
            {
                Advance(); // 'finally'
                finallyBlock = ParseBlock();
            }

            return new TryCatchFinallyNode
            {
                TryBlock = tryBlock,
                ExceptionVariable = exceptionVariable,
                CatchBlock = catchBlock,
                FinallyBlock = finallyBlock
            };
        }

        private ASTNode ParseThrowStatement()
        {
            Advance(); // 'throw'
            var expression = ParseExpression();
            return new ThrowNode { Expression = expression };
        }

        private ASTNode ParseClassDeclaration()
        {
            Advance(); // 'class'
            var className = CurrentToken.Value;
            Advance(); // Klassenname

            string baseClassName = null;
            if (CurrentToken.Value == "extends")
            {
                Advance(); // 'extends'
                baseClassName = CurrentToken.Value;
                Advance(); // Basis-Klassenname
            }

            ExpectTokenValue("{");
            Advance(); // '{'

            var members = new List<ClassMemberNode>();

            while (CurrentToken != null && CurrentToken.Value != "}")
            {
                var member = ParseClassMember();
                members.Add(member);
            }

            ExpectTokenValue("}");
            Advance(); // '}'

            return new ClassNode { Name = className, BaseClassName = baseClassName, Members = members };
        }

        private ClassMemberNode ParseClassMember()
        {
            string accessModifier = "private";
            if (CurrentToken.Value == "public" || CurrentToken.Value == "private" || CurrentToken.Value == "protected")
            {
                accessModifier = CurrentToken.Value;
                Advance(); // Zugriffsmodifizierer
            }

            bool isVirtual = false;
            bool isOverride = false;

            if (CurrentToken.Value == "virtual")
            {
                isVirtual = true;
                Advance(); // 'virtual'
            }
            else if (CurrentToken.Value == "override")
            {
                isOverride = true;
                Advance(); // 'override'
            }

            if (CurrentToken.Value == "function")
            {
                return ParseMethodDeclaration(accessModifier, isVirtual, isOverride);
            }
            else
            {
                return ParseFieldDeclaration(accessModifier);
            }
        }

        private FieldNode ParseFieldDeclaration(string accessModifier)
        {
            var fieldName = CurrentToken.Value;
            Advance(); // Feldname

            ExpressionNode initialValue = null;
            if (CurrentToken.Value == "=")
            {
                Advance(); // '='
                initialValue = ParseExpression();
            }

            ExpectTokenValue(";");
            Advance(); // ';'

            return new FieldNode { AccessModifier = accessModifier, Name = fieldName, InitialValue = initialValue };
        }

        private MethodNode ParseMethodDeclaration(string accessModifier, bool isVirtual, bool isOverride)
        {
            Advance(); // 'function'
            var methodName = CurrentToken.Value;
            Advance(); // Methodenname

            ExpectTokenValue("(");
            Advance(); // '('

            var parameters = new List<string>();
            while (CurrentToken != null && CurrentToken.Value != ")")
            {
                if (CurrentToken.Type != TokenType.Identifier)
                    throw new Exception("Parametername erwartet");

                parameters.Add(CurrentToken.Value);
                Advance();

                if (CurrentToken.Value == ",")
                {
                    Advance(); // ','
                }
            }

            ExpectTokenValue(")");
            Advance(); // ')'

            var body = ParseBlock();

            return new MethodNode
            {
                AccessModifier = accessModifier,
                IsVirtual = isVirtual,
                IsOverride = isOverride,
                Name = methodName,
                Parameters = parameters,
                Body = body
            };
        }

        private ASTNode ParseImportStatement()
        {
            Advance(); // 'import'
            var moduleName = CurrentToken.Value;
            Advance(); // Modulname

            ExpectTokenValue(";");
            Advance(); // ';'

            return new ImportNode { ModuleName = moduleName };
        }
    }
}

