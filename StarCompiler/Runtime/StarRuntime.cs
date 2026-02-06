using System;
using System.Collections.Generic;

namespace StarCompiler.Runtime;

/// <summary>
/// Ejecuta las instrucciones del lenguaje Star.
/// Executes Star language instructions.
/// </summary>
public class StarRuntime
{
    private readonly Dictionary<string, (string Type, object Value)> _variables = new();

    public void Emit(object valueObj, bool newline = false)
    {
        string output;
        // If it's a valid variable name, resolve it?
        // But ParseExpression already resolves variables to their values.
        // So valueObj is likely the VALUE (int, string, List, etc.)
        // Unless ParseExpression returned a string that happens to be a variable name?
        // In current Parser, ParseFactor resolves variables. So we get the value.
        // Only special case: Strings that are variable names? No, identifiers are resolved.

        if (valueObj is List<object> list)
        {
            output = "[" + string.Join(", ", list) + "]";
        }
        else
        {
            output = valueObj.ToString();
        }

        if (newline)
            Console.WriteLine(output);
        else
            Console.Write(output);
    }

    public object GetVariable(string name)
    {
        if (_variables.ContainsKey(name))
            return _variables[name].Value;

        throw new Exception($"Variable '{name}' not found");
    }


    private object ConvertToType(string type, string value)
    {
        return type switch
        {
            "Int" => int.Parse(value),
            "Int32" => int.Parse(value), // Tolerance
            "Double" => double.Parse(value),
            "Float" => float.Parse(value),
            "Bool" => bool.Parse(value),
            "Boolean" => bool.Parse(value), // Tolerance
            "Char" => value[0],
            "String" => value,
            "Galaxy" => ParseList(value),
            "Object" => value,
            _ => throw new Exception($"Unknown type: {type}")
        };
    }

    private object InferType(string value)
    {
        if (value.StartsWith("[") && value.EndsWith("]"))
            return ParseList(value);

        if (int.TryParse(value, out int intResult)) return intResult;
        if (double.TryParse(value, out double doubleResult)) return doubleResult;
        if (bool.TryParse(value, out bool boolResult)) return boolResult;
        if (value.Length == 1) return value[0]; // Char
        return value; // String por defecto
    }

    // Helper to map .NET type to Star Type
    private string GetStarType(object obj)
    {
        if (obj is int) return "Int";
        if (obj is double) return "Double";
        if (obj is bool) return "Bool";
        if (obj is char) return "Char";
        if (obj is string) return "String";
        if (obj is List<object>) return "Galaxy";
        return "Object";
    }

    // Overload for AST-based execution (accepts object directly)
    public void DeclareVariable(string type, string name, object value)
    {
        object typedValue = value;

        if (type == "Auto")
        {
            if (_variables.ContainsKey(name))
            {
                type = _variables[name].Type;
            }
            else
            {
                type = GetStarType(value);
            }
            typedValue = value;
        }
        else if (type == "Nova")
        {
            type = GetStarType(value);
            typedValue = value;
        }
        else
        {
            // Type is specified, just store it
            typedValue = value;
        }

        _variables[name] = (type, typedValue);
    }

    // Original string-based overload for backward compatibility
    public void DeclareVariable(string type, string name, string value)
    {
        object typedValue = value;

        if (type == "Auto")
        {
            if (_variables.ContainsKey(name))
            {
                type = _variables[name].Type;
                typedValue = ConvertToType(type, value);
            }
            else
            {
                typedValue = InferType(value);
                type = GetStarType(typedValue);
            }
        }
        else if (type != "Nova")
        {
            typedValue = ConvertToType(type, value);
        }
        else
        {
            typedValue = InferType(value);
            type = GetStarType(typedValue);
        }

        _variables[name] = (type, typedValue);
    }

    // State management for function scopes
    public Dictionary<string, (string Type, object Value)> SaveState()
    {
        return new Dictionary<string, (string Type, object Value)>(_variables);
    }

    public void RestoreState(Dictionary<string, (string Type, object Value)> state)
    {
        _variables.Clear();
        foreach (var kvp in state)
        {
            _variables[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Parsea una representación de string de lista "[1, 2, 3]" a List<object>.
    /// Parses a string list representation to List<object>.
    /// </summary>
    private List<object> ParseList(string value)
    {
        var list = new List<object>();
        var content = value.Trim('[', ']');
        if (string.IsNullOrWhiteSpace(content)) return list;

        // Split by comma (naive split)
        var parts = content.Split(',');
        foreach (var part in parts)
        {
            list.Add(InferType(part.Trim()));
        }
        return list;
    }
}