using csi.Interpreter.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace csi.Interpreter
{
    public class InterpreterException : Exception
    {
        public object ExceptionValue;
        public List<string> StackTrace { get; }

        public InterpreterException(object value, List<string> stackTrace)
        {
            ExceptionValue = value;
            StackTrace = stackTrace;
        }
    }

    public class Interpreter
    {
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        private Dictionary<string, FunctionNode> functions = new Dictionary<string, FunctionNode>();
        private static Dictionary<string, ClassDefinition> classes = new Dictionary<string, ClassDefinition>();
        private Dictionary<string, InterfaceNode> interfaces = new Dictionary<string, InterfaceNode>();
        private List<string> callStack = new List<string>();
        private Stack<Dictionary<string, object>> variableScopes = new Stack<Dictionary<string, object>>();
        private ObjectInstance currentInstance;
        private bool shouldBreak = false;
        private bool shouldContinue = false;

        // Eingebaute Funktionen
        private Dictionary<string, Func<List<object>, object>> builtInFunctions = new Dictionary<string, Func<List<object>, object>>
        {
            { "print", args => { Console.WriteLine(args[0]); return null; } },
            { "input", args => { Console.Write(args[0]); return Console.ReadLine(); } },
            { "sqrt", args => { return Math.Sqrt(Convert.ToDouble(args[0])); } },
            { "abs", args => { return Math.Abs(Convert.ToDouble(args[0])); } },
            { "typeof", args => {
                var obj = args[0];
                if (obj is ObjectInstance instance)
                {
                    return instance.ClassDef.Name;
                }
                else
                {
                    return obj.GetType().Name;
                }
            }},
            { "getMethods", args => {
                var className = args[0].ToString();
                if (classes.TryGetValue(className, out var classDef))
                {
                    return classDef.Members.Values
                        .Where(m => m is MethodDefinition)
                        .Select(m => m.Name)
                        .ToList();
                }
                else
                {
                    throw new Exception("Klasse nicht gefunden: " + className);
                }
            }},
            { "getFields", args => {
                var className = args[0].ToString();
                if (classes.TryGetValue(className, out var classDef))
                {
                    return classDef.Members.Values
                        .Where(m => m is FieldDefinition)
                        .Select(m => m.Name)
                        .ToList();
                }
                else
                {
                    throw new Exception("Klasse nicht gefunden: " + className);
                }
            }}
        };

        public void Execute(ASTNode node)
        {
            switch (node)
            {
                case ProgramNode program:
                    foreach (var statement in program.Statements)
                    {
                        Execute(statement);
                        if (shouldBreak || shouldContinue)
                            break;
                    }
                    break;
                case BlockNode block:
                    foreach (var statement in block.Statements)
                    {
                        Execute(statement);
                        if (shouldBreak || shouldContinue)
                            break;
                    }
                    break;
                case AssignmentNode assignment:
                    var value = EvaluateExpression(assignment.Expression);
                    AssignVariable(assignment.VariableName, value);
                    break;
                case ExpressionNode expression:
                    EvaluateExpression(expression);
                    break;
                case IfNode ifNode:
                    var conditionValue = EvaluateExpression(ifNode.Condition);
                    if (IsTrue(conditionValue))
                    {
                        Execute(ifNode.ThenBranch);
                    }
                    else if (ifNode.ElseBranch != null)
                    {
                        Execute(ifNode.ElseBranch);
                    }
                    break;
                case WhileNode whileNode:
                    while (IsTrue(EvaluateExpression(whileNode.Condition)))
                    {
                        Execute(whileNode.Body);
                        if (shouldBreak)
                        {
                            shouldBreak = false;
                            break;
                        }
                        if (shouldContinue)
                        {
                            shouldContinue = false;
                            continue;
                        }
                    }
                    break;
                case ForNode forNode:
                    Execute(forNode.Initializer);
                    while (IsTrue(EvaluateExpression(forNode.Condition)))
                    {
                        Execute(forNode.Body);
                        if (shouldBreak)
                        {
                            shouldBreak = false;
                            break;
                        }
                        if (shouldContinue)
                        {
                            shouldContinue = false;
                            continue;
                        }
                        EvaluateExpression(forNode.Iterator);
                    }
                    break;
                case BreakNode _:
                    shouldBreak = true;
                    break;
                case ContinueNode _:
                    shouldContinue = true;
                    break;
                case FunctionNode functionNode:
                    functions[functionNode.Name] = functionNode;
                    break;
                case ReturnNode returnNode:
                    var returnValue = EvaluateExpression(returnNode.Expression);
                    throw new ReturnException(returnValue);
                case PrintNode printNode:
                    var printValue = EvaluateExpression(printNode.Expression);
                    Console.WriteLine(printValue);
                    break;
                case ClassNode classNode:
                    DefineClass(classNode);
                    break;
                case TryCatchFinallyNode tryCatchFinallyNode:
                    try
                    {
                        Execute(tryCatchFinallyNode.TryBlock);
                    }
                    catch (InterpreterException ex)
                    {
                        if (tryCatchFinallyNode.CatchBlock != null)
                        {
                            variables[tryCatchFinallyNode.ExceptionVariable] = ex.ExceptionValue;
                            Execute(tryCatchFinallyNode.CatchBlock);
                        }
                        else
                        {
                            throw; // Ausnahme weiterwerfen, wenn kein Catch-Block vorhanden ist
                        }
                    }
                    finally
                    {
                        if (tryCatchFinallyNode.FinallyBlock != null)
                        {
                            Execute(tryCatchFinallyNode.FinallyBlock);
                        }
                    }
                    break;
                case ThrowNode throwNode:
                    var exceptionValue = EvaluateExpression(throwNode.Expression);
                    throw new InterpreterException(exceptionValue, new List<string>(callStack));
                case ImportNode importNode:
                    ExecuteImport(importNode);
                    break;
                default:
                    throw new Exception("Unbekannter Anweisungstyp: " + node.GetType().Name);
            }
        }

        private void AssignVariable(string name, object value)
        {
            if (variableScopes.Count > 0)
            {
                Console.WriteLine($"Zuweisung von lokaler Variable: {name}");
                variableScopes.Peek()[name] = value;
            }
            else
            {
                Console.WriteLine($"Zuweisung von globaler Variable: {name}");
                variables[name] = value;
            }
        }


        private object EvaluateExpression(ExpressionNode node)
        {
            if (node is NumberNode numberNode)
            {
                return numberNode.Value;
            }
            else if (node is StringNode stringNode)
            {
                return stringNode.Value;
            }
            else if (node is VariableNode variableNode)
            {
                var value = GetVariableValue(variableNode.Name);
                return value;
            }

            else if (node is UnaryOperationNode unaryNode)
            {
                var operandValue = EvaluateExpression(unaryNode.Operand);

                switch (unaryNode.Operator)
                {
                    case "-":
                        if (operandValue is double num)
                            return -num;
                        else
                            throw new Exception("Unärer '-' Operator erfordert eine numerische Operanden.");
                    case "+":
                        if (operandValue is double num2)
                            return +num2;
                        else
                            throw new Exception("Unärer '+' Operator erfordert eine numerische Operanden.");
                    case "!":
                        if (operandValue is bool b)
                            return !b;
                        else if (operandValue is double num3)
                            return num3 == 0;
                        else
                            throw new Exception("Unärer '!' Operator erfordert einen booleschen oder numerischen Operanden.");
                    default:
                        throw new Exception($"Unbekannter unärer Operator '{unaryNode.Operator}'.");
                }
            }
            else if (node is BinaryOperationNode binOpNode)
            {
                var leftVal = EvaluateExpression(binOpNode.Left);
                var rightVal = EvaluateExpression(binOpNode.Right);

                // Unterstützung für String-Konkatenation
                if (leftVal is string || rightVal is string)
                {
                    if (binOpNode.Operator == "+")
                    {
                        return leftVal.ToString() + rightVal.ToString();
                    }
                    else
                    {
                        throw new Exception("Operator '" + binOpNode.Operator + "' nicht für Strings unterstützt");
                    }
                }
                else
                {
                    var leftNum = Convert.ToDouble(leftVal);
                    var rightNum = Convert.ToDouble(rightVal);

                    switch (binOpNode.Operator)
                    {
                        case "+":
                            return leftNum + rightNum;
                        case "-":
                            return leftNum - rightNum;
                        case "*":
                            return leftNum * rightNum;
                        case "/":
                            return leftNum / rightNum;
                        case "%":
                            return leftNum % rightNum;
                        case "==":
                            return leftNum == rightNum;
                        case "!=":
                            return leftNum != rightNum;
                        case "<":
                            return leftNum < rightNum;
                        case ">":
                            return leftNum > rightNum;
                        case "<=":
                            return leftNum <= rightNum;
                        case ">=":
                            return leftNum >= rightNum;
                        default:
                            throw new Exception("Unbekannter Operator: " + binOpNode.Operator);
                    }
                }
            }
            else if (node is FunctionCallNode funcCall)
            {
                return ExecuteFunctionCall(funcCall);
            }
            else if (node is NewExpressionNode newExpr)
            {
                return InstantiateObject(newExpr.ClassName);
            }
            else if (node is MemberAccessNode memberAccess)
            {
                var instance = EvaluateExpression(memberAccess.Object) as ObjectInstance;
                if (instance == null)
                    throw new Exception("Mitgliedszugriff auf ein Nicht-Objekt");

                return GetMember(instance, memberAccess.MemberName);
            }
            else if (node is MethodCallNode methodCall)
            {
                return CallMethod(methodCall);
            }
            else if (node is SuperCallNode superCall)
            {
                return CallSuperMethod(superCall);
            }
            else
            {
                throw new Exception("Unbekannter Ausdruckstyp: " + node.GetType().Name);
            }
        }

        private object GetVariableValue(string name)
        {
            if (variableScopes.Count > 0 && variableScopes.Peek().TryGetValue(name, out var value))
            {
                return value;
            }
            else if (variables.TryGetValue(name, out value))
            {
                return value;
            }
            else
            {
                throw new Exception("Variable nicht definiert: " + name);
            }
        }

        private bool IsTrue(object value)
        {
            if (value is bool boolVal)
            {
                return boolVal;
            }
            else if (value is double numVal)
            {
                return numVal != 0;
            }
            else if (value != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private object ExecuteFunctionCall(FunctionCallNode funcCall)
        {
            if (builtInFunctions.TryGetValue(funcCall.FunctionName, out var builtInFunc))
            {
                var argValues = funcCall.Arguments.Select(arg => EvaluateExpression(arg)).ToList();
                return builtInFunc(argValues);
            }
            else if (functions.TryGetValue(funcCall.FunctionName, out var function))
            {
                var argValues = new List<object>();
                foreach (var arg in funcCall.Arguments)
                {
                    argValues.Add(EvaluateExpression(arg));
                }

                if (function.Parameters.Count != argValues.Count)
                    throw new Exception("Falsche Anzahl von Argumenten für Funktion " + funcCall.FunctionName);

                callStack.Add(funcCall.FunctionName);
                variableScopes.Push(new Dictionary<string, object>());

                for (int i = 0; i < function.Parameters.Count; i++)
                {
                    variableScopes.Peek()[function.Parameters[i]] = argValues[i];
                }

                try
                {
                    Execute(function.Body);
                }
                catch (ReturnException ex)
                {
                    return ex.Value;
                }
                finally
                {
                    variableScopes.Pop();
                    callStack.RemoveAt(callStack.Count - 1);
                }

                return null;
            }
            else
            {
                throw new Exception("Funktion nicht definiert: " + funcCall.FunctionName);
            }
        }

        private void DefineClass(ClassNode classNode)
        {
            ClassDefinition baseClass = null;
            if (classNode.BaseClassName != null)
            {
                if (!classes.TryGetValue(classNode.BaseClassName, out baseClass))
                    throw new Exception("Basis-Klasse nicht gefunden: " + classNode.BaseClassName);
            }

            var classDef = new ClassDefinition
            {
                Name = classNode.Name,
                BaseClass = baseClass,
                Members = new Dictionary<string, ClassMemberDefinition>()
            };

            foreach (var member in classNode.Members)
            {
                if (member is FieldNode fieldNode)
                {
                    var fieldDef = new FieldDefinition
                    {
                        Name = fieldNode.Name,
                        AccessModifier = fieldNode.AccessModifier,
                        InitialValue = fieldNode.InitialValue != null ? EvaluateExpression(fieldNode.InitialValue) : null
                    };
                    classDef.Members[fieldDef.Name] = fieldDef;
                }
                else if (member is MethodNode methodNode)
                {
                    var methodDef = new MethodDefinition
                    {
                        Name = methodNode.Name,
                        AccessModifier = methodNode.AccessModifier,
                        IsVirtual = methodNode.IsVirtual,
                        IsOverride = methodNode.IsOverride,
                        Parameters = methodNode.Parameters,
                        Body = methodNode.Body
                    };
                    classDef.Members[methodDef.Name] = methodDef;
                }
            }

            classes[classDef.Name] = classDef;
        }

        private ObjectInstance InstantiateObject(string className)
        {
            if (!classes.TryGetValue(className, out var classDef))
                throw new Exception("Klasse nicht definiert: " + className);

            var obj = new ObjectInstance { ClassDef = classDef };
            Console.WriteLine($"Erstelle neues Objekt der Klasse '{className}'.");

            // Initialisieren der Felder, inklusive der Basisklasse
            InitializeFields(obj, classDef);

            return obj;
        }

        private void InitializeFields(ObjectInstance obj, ClassDefinition classDef)
        {
            if (classDef.BaseClass != null)
            {
                InitializeFields(obj, classDef.BaseClass);
            }

            foreach (var member in classDef.Members.Values)
            {
                if (member is FieldDefinition fieldDef)
                {
                    obj.Fields[fieldDef.Name] = fieldDef.InitialValue;
                }
            }
        }

        private object GetMember(ObjectInstance instance, string memberName)
        {
            if (instance.Fields.TryGetValue(memberName, out var value))
            {
                return value;
            }
            else if (FindMethodDefinition(instance.ClassDef, memberName) != null)
            {
                return new BoundMethod { Instance = instance, MethodName = memberName };
            }
            else
            {
                throw new Exception("Mitglied nicht gefunden: " + memberName);
            }
        }

        private MethodDefinition FindMethodDefinition(ClassDefinition classDef, string methodName)
        {
            if (classDef.Members.TryGetValue(methodName, out var member) && member is MethodDefinition methodDef)
            {
                return methodDef;
            }
            else if (classDef.BaseClass != null)
            {
                return FindMethodDefinition(classDef.BaseClass, methodName);
            }
            else
            {
                return null;
            }
        }

        private object CallMethod(MethodCallNode methodCall)
        {
            var target = EvaluateExpression(methodCall.Target);
            if (target is BoundMethod boundMethod)
            {
                Console.WriteLine($"Aufruf von Methode '{boundMethod.MethodName}' auf Objekt der Klasse '{boundMethod.Instance.ClassDef.Name}'");

                var methodDef = FindMethodDefinition(boundMethod.Instance.ClassDef, boundMethod.MethodName);
                if (methodDef == null)
                    throw new Exception("Methode nicht gefunden: " + boundMethod.MethodName);

                var argValues = methodCall.Arguments.Select(arg => EvaluateExpression(arg)).ToList();

                Console.WriteLine($"Übergebe {argValues.Count} Argumente.");

                if (methodDef.Parameters.Count != argValues.Count)
                    throw new Exception("Falsche Anzahl von Argumenten für Methode " + boundMethod.MethodName);

                var previousInstance = currentInstance;
                currentInstance = boundMethod.Instance;

                variableScopes.Push(new Dictionary<string, object>());
                for (int i = 0; i < methodDef.Parameters.Count; i++)
                {
                    variableScopes.Peek()[methodDef.Parameters[i]] = argValues[i];
                }
                variableScopes.Peek()["this"] = currentInstance;

                callStack.Add(methodDef.Name);

                try
                {
                    Execute(methodDef.Body);
                }
                catch (ReturnException ex)
                {
                    return ex.Value;
                }
                finally
                {
                    variableScopes.Pop();
                    currentInstance = previousInstance;
                    callStack.RemoveAt(callStack.Count - 1);
                }

                return null;
            }
            else
            {
                throw new Exception("Ungültiger Methodenaufruf");
            }
        }



        private object CallSuperMethod(SuperCallNode superCall)
        {
            if (currentInstance == null)
                throw new Exception("super kann nur innerhalb einer Methode verwendet werden");

            var methodDef = FindMethodDefinition(currentInstance.ClassDef.BaseClass, superCall.MethodName);
            if (methodDef == null)
                throw new Exception("Basismethode nicht gefunden: " + superCall.MethodName);

            var argValues = superCall.Arguments.Select(arg => EvaluateExpression(arg)).ToList();

            if (methodDef.Parameters.Count != argValues.Count)
                throw new Exception("Falsche Anzahl von Argumenten für Methode " + superCall.MethodName);

            variableScopes.Push(new Dictionary<string, object>());
            for (int i = 0; i < methodDef.Parameters.Count; i++)
            {
                variableScopes.Peek()[methodDef.Parameters[i]] = argValues[i];
            }
            variableScopes.Peek()["this"] = currentInstance;

            callStack.Add("super." + methodDef.Name);

            try
            {
                Execute(methodDef.Body);
            }
            catch (ReturnException ex)
            {
                return ex.Value;
            }
            finally
            {
                variableScopes.Pop();
                callStack.RemoveAt(callStack.Count - 1);
            }

            return null;
        }

        private void ExecuteImport(ImportNode importNode)
        {
            var modulePath = importNode.ModuleName + ".mycs";
            if (!File.Exists(modulePath))
            {
                throw new Exception("Modul nicht gefunden: " + modulePath);
            }

            var code = File.ReadAllText(modulePath);

            var lexer = new Lexer();
            var tokens = lexer.Tokenize(code);

            var parser = new Parser(tokens);
            var ast = parser.ParseProgram();

            Execute(ast);
        }

        private class ReturnException : Exception
        {
            public object Value;

            public ReturnException(object value)
            {
                Value = value;
            }
        }
    }

    // Hilfsklassen für Klassen und Objekte
    public class ClassDefinition
    {
        public string Name;
        public ClassDefinition BaseClass;
        public Dictionary<string, ClassMemberDefinition> Members;
        public List<string> ImplementedInterfaces = new List<string>();
    }

    public abstract class ClassMemberDefinition
    {
        public string Name;
    }

    public class FieldDefinition : ClassMemberDefinition
    {
        public string AccessModifier;
        public object InitialValue;
    }

    public class MethodDefinition : ClassMemberDefinition
    {
        public string AccessModifier;
        public bool IsVirtual;
        public bool IsOverride;
        public List<string> Parameters;
        public ASTNode Body;
    }

    public class ObjectInstance
    {
        public ClassDefinition ClassDef;
        public Dictionary<string, object> Fields = new Dictionary<string, object>();
    }

    public class BoundMethod
    {
        public ObjectInstance Instance;
        public string MethodName;
    }

}
