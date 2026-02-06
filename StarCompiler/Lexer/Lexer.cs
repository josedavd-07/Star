using System;
using System.Collections.Generic;
using System.Text;

namespace StarCompiler.Lexer;

/// <summary>
/// Convierte el texto fuente de Star en una lista de tokens.
/// Converts Star source code into a list of tokens.
/// </summary>
public class Lexer
{
    private readonly string _text;
    private int _position;

    public Lexer(string text)
    {
        _text = text;
        _position = 0;
    }

    /// <summary>
    /// Analiza todo el texto y devuelve la lista de tokens.
    /// Tokenizes the entire source code and returns a list of tokens.
    /// </summary>
    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (_position < _text.Length)
        {
            char current = _text[_position];

            // ------------------------------------
            // 1️⃣ Comentarios
            // ------------------------------------

            // Comentario de línea: ** o // hasta el final de línea
            if ((current == '*' && _position + 1 < _text.Length && _text[_position + 1] == '*') ||
                (current == '/' && _position + 1 < _text.Length && _text[_position + 1] == '/'))
            {
                _position += 2; // saltar los marcadores de comentario (** o //)
                while (_position < _text.Length && _text[_position] != '\n')
                    _position++;
                continue;
            }

            // Comentario multilínea: /* ... */
            if (current == '/' && _position + 1 < _text.Length && _text[_position + 1] == '*')
            {
                _position += 2; // saltar /*
                while (_position + 1 < _text.Length && !(_text[_position] == '*' && _text[_position + 1] == '/'))
                    _position++;

                _position += 2; // saltar */
                continue;
            }

            // ------------------------------------
            // 2️⃣ Tokens normales
            // ------------------------------------

            if (char.IsWhiteSpace(current))
            {
                _position++;
            }
            else if (current == '"')
            {
                tokens.Add(ReadString());
            }
            else if (current == '\'')
            {
                tokens.Add(ReadChar());
            }
            else if (char.IsDigit(current))
            {
                tokens.Add(ReadNumber());
            }
            else if (char.IsLetter(current))
            {
                tokens.Add(ReadWord());
            }
            else if (current == '(')
            {
                tokens.Add(new Token(TokenType.LParen, "("));
                _position++;
            }
            else if (current == ')')
            {
                tokens.Add(new Token(TokenType.RParen, ")"));
                _position++;
            }
            else if (current == '=')
            {
                if (_position + 1 < _text.Length && _text[_position + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.EqualsEquals, "=="));
                    _position += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Equals, "="));
                    _position++;
                }
            }
            else if (current == '+')
            {
                tokens.Add(new Token(TokenType.Plus, "+"));
                _position++;
            }
            else if (current == '-')
            {
                // Check for arrow ->
                if (_position + 1 < _text.Length && _text[_position + 1] == '>')
                {
                    tokens.Add(new Token(TokenType.Arrow, "->"));
                    _position += 2;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Minus, "-"));
                    _position++;
                }
            }
            else if (current == '*')
            {
                tokens.Add(new Token(TokenType.Multiply, "*"));
                _position++;
            }
            else if (current == '/')
            {
                // Check if it's a comment start "/*"
                if (_position + 1 < _text.Length && _text[_position + 1] == '*')
                {
                    // It is a comment start, let the comment logic handle it
                    // However, note that the comment logic is at the TOP of the loop.
                    // So if we are here, we already passed the comment check...
                    // WAIT. The comment check was:
                    // if (current == '/' && _position + 1 < _text.Length && _text[_position + 1] == '*')
                    // So if we reached here, it means it is NOT '/*', so it must be just '/'
                    tokens.Add(new Token(TokenType.Divide, "/"));
                    _position++;
                }
                else
                {
                    tokens.Add(new Token(TokenType.Divide, "/"));
                    _position++;
                }
            }
            else if (current == '!')
            {
                if (_position + 1 < _text.Length && _text[_position + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.NotEquals, "!="));
                    _position += 2;
                }
                else
                {
                    throw new Exception("Unexpected character '!'");
                }
            }
            else if (current == '<')
            {
                tokens.Add(new Token(TokenType.LessThan, "<"));
                _position++;
            }
            else if (current == '>')
            {
                tokens.Add(new Token(TokenType.GreaterThan, ">"));
                _position++;
            }
            else if (current == '&')
            {
                if (_position + 1 < _text.Length && _text[_position + 1] == '&')
                {
                    tokens.Add(new Token(TokenType.And, "&&"));
                    _position += 2;
                }
                else
                {
                    throw new Exception("Unexpected character '&'");
                }
            }
            else if (current == '|')
            {
                if (_position + 1 < _text.Length && _text[_position + 1] == '|')
                {
                    tokens.Add(new Token(TokenType.Or, "||"));
                    _position += 2;
                }
                else
                {
                    throw new Exception("Unexpected character '|'");
                }
            }
            else if (current == '{')
            {
                tokens.Add(new Token(TokenType.LBrace, "{"));
                _position++;
            }
            else if (current == '}')
            {
                tokens.Add(new Token(TokenType.RBrace, "}"));
                _position++;
            }
            else if (current == ';')
            {
                tokens.Add(new Token(TokenType.Semicolon, ";"));
                _position++;
            }
            else if (current == '[')
            {
                tokens.Add(new Token(TokenType.LBracket, "["));
                _position++;
            }
            else if (current == ']')
            {
                tokens.Add(new Token(TokenType.RBracket, "]"));
                _position++;
            }
            else if (current == ',')
            {
                tokens.Add(new Token(TokenType.Comma, ","));
                _position++;
            }
            else if (current == '.')
            {
                tokens.Add(new Token(TokenType.Dot, "."));
                _position++;
            }
            else
            {
                throw new Exception($"Unexpected character: {current}");
            }
        }

        tokens.Add(new Token(TokenType.EOF, ""));
        return tokens;
    }

    private Token ReadString()
    {
        _position++; // saltar "
        var sb = new StringBuilder();

        while (_position < _text.Length && _text[_position] != '"')
        {
            sb.Append(_text[_position]);
            _position++;
        }

        _position++; // saltar cierre "
        return new Token(TokenType.String, sb.ToString());
    }

    private Token ReadChar()
    {
        _position++; // saltar '
        if (_position >= _text.Length)
            throw new Exception("Unexpected end of file in Char literal");

        char value = _text[_position];
        _position++;

        if (_text[_position] != '\'')
            throw new Exception("Expected closing ' for Char literal");

        _position++; // saltar cierre '
        return new Token(TokenType.Char, value.ToString());
    }

    private Token ReadWord()
    {
        var sb = new StringBuilder();

        while (_position < _text.Length && (char.IsLetterOrDigit(_text[_position]) || _text[_position] == '_'))
        {
            sb.Append(_text[_position]);
            _position++;
        }

        string word = sb.ToString();

        // Keywords: Emit, EmitLn, true, false, When, ElseWhen, Otherwise
        // Loops: Orbit, While, SpinWhile, Explore
        // Collections: Galaxy, in
        // Functions: StarFunction, return
        if (word == "Emit" || word == "EmitLn" || word == "true" || word == "false" ||
            word == "When" || word == "ElseWhen" || word == "Otherwise" ||
            word == "Orbit" || word == "While" || word == "SpinWhile" || word == "Explore" ||
            word == "in" || word == "StarFunction" || word == "return" ||
            word == "Constellation" || word == "new" || word == "this" ||
            word == "StarName" || word == "Public" || word == "Protected" ||
            word == "Private" || word == "Static" || word == "Read")
            return new Token(TokenType.Keyword, word);

        // Tipos de datos
        // Galaxy can be treated as a type too? 
        // Logic: If "Galaxy" is used in variable declaration "Galaxy g = ...", it acts as type.
        // If we put it in Keyword, ParserVariableDeclaration might fail if it expects TokenType.Type.
        // "Galaxy" IS the type name for list.
        if (word == "Int" || word == "String" || word == "Double" || word == "Float" ||
            word == "Char" || word == "Bool" || word == "Nova" || word == "Object" || word == "Galaxy")
            return new Token(TokenType.Type, word);

        // Identificador normal
        return new Token(TokenType.Identifier, word);
    }

    private Token ReadNumber()
    {
        var sb = new StringBuilder();

        while (_position < _text.Length && (char.IsDigit(_text[_position]) || _text[_position] == '.'))
        {
            sb.Append(_text[_position]);
            _position++;
        }

        return new Token(TokenType.Number, sb.ToString());
    }
}
