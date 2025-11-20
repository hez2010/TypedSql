using System;
using System.Globalization;
using System.Text;

namespace TypedSql.Runtime;

internal enum ComparisonOperator
{
    Equals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    NotEqual
}

internal sealed record SelectionClause(bool SelectAll, IReadOnlyList<string> ColumnIdentifiers);

internal enum LiteralKind
{
    Integer,
    String,
    Float,
    Boolean
}

internal readonly record struct LiteralValue(LiteralKind Kind, int IntValue, string? StringValue, float FloatValue, bool BoolValue)
{
    public static LiteralValue FromInt(int value) => new(LiteralKind.Integer, value, null, 0f, false);

    public static LiteralValue FromString(string? value) => new(LiteralKind.String, 0, value, 0f, false);

    public static LiteralValue FromFloat(float value) => new(LiteralKind.Float, 0, null, value, false);

    public static LiteralValue FromBool(bool value) => new(LiteralKind.Boolean, 0, null, 0f, value);
}

internal abstract record WhereExpression;

internal sealed record ComparisonExpression(string ColumnIdentifier, ComparisonOperator Operator, LiteralValue Literal) : WhereExpression;

internal sealed record AndExpression(WhereExpression Left, WhereExpression Right) : WhereExpression;

internal sealed record OrExpression(WhereExpression Left, WhereExpression Right) : WhereExpression;

internal sealed record NotExpression(WhereExpression Expression) : WhereExpression;

internal sealed record ParsedQuery(SelectionClause Selection, WhereExpression? Where);

internal static class SqlParser
{
    public static ParsedQuery Parse(string sql)
    {
        var tokens = Tokenize(sql);
        var index = 0;

        Expect(tokens, ref index, "SELECT");
        if (index >= tokens.Count)
        {
            throw new InvalidOperationException("Expected selection list after SELECT.");
        }

        SelectionClause selection;
        if (tokens[index] == "*")
        {
            index++;
            selection = new SelectionClause(true, Array.Empty<string>());
        }
        else
        {
            var columns = new List<string>();
            while (index < tokens.Count && !tokens[index].Equals("FROM", StringComparison.OrdinalIgnoreCase))
            {
                columns.Add(tokens[index++]);
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException("Expected at least one column in SELECT clause.");
            }

            selection = new SelectionClause(false, columns);
        }

        Expect(tokens, ref index, "FROM");
        if (index >= tokens.Count)
        {
            throw new InvalidOperationException("Expected table name after FROM.");
        }

        var source = tokens[index++];
        if (!source.Equals("$"))
        {
            throw new InvalidOperationException("Queries must select FROM $ to reference the provided rows.");
        }
        WhereExpression? where = null;

        if (index < tokens.Count && tokens[index].Equals("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            index++;
            where = ParseWhereExpression(tokens, ref index);
        }

        if (index < tokens.Count)
        {
            throw new InvalidOperationException($"Unexpected token '{tokens[index]}'.");
        }

        return new ParsedQuery(selection, where);
    }

    private static LiteralValue ParseLiteral(string token)
    {
        if (token.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return LiteralValue.FromBool(true);
        }

        if (token.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return LiteralValue.FromBool(false);
        }

        if (token.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return LiteralValue.FromString(null);
        }

        if (token.Length >= 2 && token[0] == '\'' && token[^1] == '\'')
        {
            var sb = new StringBuilder(token.Length - 2);
            for (var i = 1; i < token.Length - 1; i++)
            {
                var ch = token[i];
                if (ch == '\'' && i + 1 < token.Length - 1 && token[i + 1] == '\'')
                {
                    sb.Append('\'');
                    i++;
                }
                else
                {
                    sb.Append(ch);
                }
            }
            return LiteralValue.FromString(sb.ToString());
        }

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return LiteralValue.FromInt(number);
        }

        if (float.TryParse(token, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var floatValue))
        {
            return LiteralValue.FromFloat(floatValue);
        }

        throw new InvalidOperationException($"Literal '{token}' is not a supported type.");
    }

    private static ComparisonOperator ParseOperator(string token)
    {
        return token switch
        {
            "=" => ComparisonOperator.Equals,
            ">" => ComparisonOperator.GreaterThan,
            "<" => ComparisonOperator.LessThan,
            ">=" => ComparisonOperator.GreaterOrEqual,
            "<=" => ComparisonOperator.LessOrEqual,
            "!=" => ComparisonOperator.NotEqual,
            _ => throw new InvalidOperationException($"Unsupported comparison operator '{token}'.")
        };
    }

    private static WhereExpression ParseWhereExpression(IReadOnlyList<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
        {
            throw new InvalidOperationException("Expected predicate after WHERE.");
        }

        return ParseOrExpression(tokens, ref index);
    }

    private static WhereExpression ParseOrExpression(IReadOnlyList<string> tokens, ref int index)
    {
        var left = ParseAndExpression(tokens, ref index);
        while (index < tokens.Count && tokens[index].Equals("OR", StringComparison.OrdinalIgnoreCase))
        {
            index++;
            var right = ParseAndExpression(tokens, ref index);
            left = new OrExpression(left, right);
        }

        return left;
    }

    private static WhereExpression ParseAndExpression(IReadOnlyList<string> tokens, ref int index)
    {
        var left = ParseUnaryExpression(tokens, ref index);
        while (index < tokens.Count && tokens[index].Equals("AND", StringComparison.OrdinalIgnoreCase))
        {
            index++;
            var right = ParseUnaryExpression(tokens, ref index);
            left = new AndExpression(left, right);
        }

        return left;
    }

    private static WhereExpression ParseUnaryExpression(IReadOnlyList<string> tokens, ref int index)
    {
        if (index >= tokens.Count)
        {
            throw new InvalidOperationException("Unexpected end of WHERE clause.");
        }

        if (tokens[index].Equals("NOT", StringComparison.OrdinalIgnoreCase))
        {
            index++;
            var expression = ParseUnaryExpression(tokens, ref index);
            return new NotExpression(expression);
        }

        if (tokens[index] == "(")
        {
            index++;
            var expression = ParseOrExpression(tokens, ref index);
            if (index >= tokens.Count || tokens[index] != ")")
            {
                throw new InvalidOperationException("Unclosed parenthesis in WHERE clause.");
            }

            index++;
            return expression;
        }

        return ParseComparisonExpression(tokens, ref index);
    }

    private static WhereExpression ParseComparisonExpression(IReadOnlyList<string> tokens, ref int index)
    {
        if (index + 2 >= tokens.Count)
        {
            throw new InvalidOperationException("Malformed predicate in WHERE clause.");
        }

        var column = tokens[index++];
        var opToken = tokens[index++];
        var literalToken = tokens[index++];
        var op = ParseOperator(opToken);
        var literal = ParseLiteral(literalToken);
        return new ComparisonExpression(column, op, literal);
    }

    private static void Expect(IReadOnlyList<string> tokens, ref int index, string keyword)
    {
        if (index >= tokens.Count || !tokens[index].Equals(keyword, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Expected keyword '{keyword}'.");
        }

        index++;
    }

    private static List<string> Tokenize(string sql)
    {
        var tokens = new List<string>();
        var builder = new StringBuilder();

        for (var i = 0; i < sql.Length; i++)
        {
            var ch = sql[i];
            if (char.IsWhiteSpace(ch) || ch == ',' || ch == ';')
            {
                Flush();
                continue;
            }

            if (ch == '(' || ch == ')')
            {
                Flush();
                tokens.Add(ch.ToString());
                continue;
            }

            if (ch == '\'')
            {
                Flush();
                var literal = new StringBuilder();
                literal.Append(ch);
                i++;
                var closed = false;
                for (; i < sql.Length; i++)
                {
                    var current = sql[i];
                    literal.Append(current);
                    if (current == '\'')
                    {
                        if (i + 1 < sql.Length && sql[i + 1] == '\'')
                        {
                            literal.Append('\'');
                            i++;
                            continue;
                        }

                        closed = true;
                        break;
                    }
                }

                if (!closed)
                {
                    throw new InvalidOperationException("Unterminated string literal.");
                }

                tokens.Add(literal.ToString());
                continue;
            }

            if (ch is '!' or '>' or '<' or '=')
            {
                Flush();
                if ((ch == '!' || ch == '>' || ch == '<') && i + 1 < sql.Length && sql[i + 1] == '=')
                {
                    tokens.Add(new string([ch, '=']));
                    i++;
                }
                else
                {
                    tokens.Add(ch.ToString());
                }
                continue;
            }

            builder.Append(ch);
        }

        Flush();
        return tokens;

        void Flush()
        {
            if (builder.Length == 0)
            {
                return;
            }

            tokens.Add(builder.ToString());
            builder.Clear();
        }
    }
}
