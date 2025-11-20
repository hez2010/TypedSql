using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TypedSql.Runtime;

internal static class SqlCompiler
{
    public static (Type PipelineType, Type RuntimeResultType, Type PublicResultType) Compile<TRow>(ParsedQuery query, bool supportsAot = false)
    {
        var rowType = typeof(TRow);
        Type runtimeResultType;
        Type publicResultType;
        Type pipeline;

        if (query.Selection.SelectAll)
        {
            runtimeResultType = rowType;
            publicResultType = rowType;
            pipeline = typeof(Stop<,>).MakeGenericType(runtimeResultType, rowType);
        }
        else
        {
            var (projectionType, runtimeType, publicType) = BuildSelectionProjection<TRow>(query.Selection.ColumnIdentifiers, supportsAot);
            runtimeResultType = runtimeType;
            publicResultType = publicType;

            var stopNode = typeof(Stop<,>).MakeGenericType(runtimeResultType, rowType);
            pipeline = typeof(Select<,,,,,>).MakeGenericType(
                rowType,
                projectionType,
                stopNode,
                runtimeResultType,
                runtimeResultType,
                rowType);
        }

        if (query.Where is { } whereExpression)
        {
            var predicateType = BuildPredicate<TRow>(whereExpression);
            pipeline = typeof(Where<,,,,>).MakeGenericType(rowType, predicateType, pipeline, runtimeResultType, rowType);
        }

        pipeline = Optimize(pipeline);

        return (pipeline, runtimeResultType, publicResultType);
    }

    private static (Type ProjectionType, Type RuntimeResultType, Type PublicResultType) BuildSelectionProjection<TRow>(IReadOnlyList<string> columnIdentifiers, bool supportsAot)
    {
        if (columnIdentifiers is null || columnIdentifiers.Count == 0)
        {
            throw new InvalidOperationException("At least one column must be specified in SELECT clause.");
        }

        var rowType = typeof(TRow);

        if (columnIdentifiers.Count == 1)
        {
            var column = SchemaRegistry<TRow>.ResolveColumn(columnIdentifiers[0]);
            var runtimeColumnType = column.GetRuntimeColumnType(rowType);
            var runtimeValueType = column.GetRuntimeValueType();
            return (typeof(ColumnProjection<,,>).MakeGenericType(runtimeColumnType, rowType, runtimeValueType), runtimeValueType, column.ValueType);
        }

        var columns = new ColumnMetadata[columnIdentifiers.Count];
        for (var i = 0; i < columnIdentifiers.Count; i++)
        {
            columns[i] = SchemaRegistry<TRow>.ResolveColumn(columnIdentifiers[i]);
        }

        var tupleInfo = BuildTupleSelection(rowType, columns, 0, supportsAot);
        return (tupleInfo.ProjectionType, tupleInfo.RuntimeType, tupleInfo.PublicType);
    }

    private static TupleSelectionInfo BuildTupleSelection(Type rowType, ColumnMetadata[] columns, int offset, bool supportsAot)
    {
        var remaining = columns.Length - offset;
        if (remaining <= 0)
        {
            throw new InvalidOperationException("At least one column must be specified in SELECT clause.");
        }

        if (remaining <= 7)
        {
            var runtimeColumnTypes = new Type[remaining];
            var runtimeValueTypes = new Type[remaining];
            var publicValueTypes = new Type[remaining];

            for (var i = 0; i < remaining; i++)
            {
                var column = columns[offset + i];
                if (!supportsAot)
                {
                    runtimeColumnTypes[i] = column.GetRuntimeColumnType(rowType);
                    runtimeValueTypes[i] = column.GetRuntimeValueType();
                }
                else
                {
                    runtimeColumnTypes[i] = column.ColumnType;
                    runtimeValueTypes[i] = column.ValueType;
                }
                publicValueTypes[i] = column.ValueType;
            }

            var leafProjectionType = CreateLeafTupleProjectionType(rowType, runtimeColumnTypes, runtimeValueTypes);
            var leafRuntimeTupleType = CreateSmallValueTupleType(runtimeValueTypes);
            var leafPublicTupleType = CreateSmallValueTupleType(publicValueTypes);
            return new(leafProjectionType, leafRuntimeTupleType, leafPublicTupleType);
        }

        var headRuntimeColumns = new Type[7];
        var headRuntimeValues = new Type[7];
        var headPublicValues = new Type[7];

        for (var i = 0; i < 7; i++)
        {
            var column = columns[offset + i];
            if (!supportsAot)
            {
                headRuntimeColumns[i] = column.GetRuntimeColumnType(rowType);
                headRuntimeValues[i] = column.GetRuntimeValueType();
            }
            else
            {
                headRuntimeColumns[i] = column.ColumnType;
                headRuntimeValues[i] = column.ValueType;
            }
            headPublicValues[i] = column.ValueType;
        }

        var rest = BuildTupleSelection(rowType, columns, offset + 7, supportsAot);
        var projectionArgs = new Type[17];
        projectionArgs[0] = rowType;
        for (var i = 0; i < 7; i++)
        {
            projectionArgs[i + 1] = headRuntimeColumns[i];
            projectionArgs[i + 9] = headRuntimeValues[i];
        }

        projectionArgs[8] = rest.ProjectionType;
        projectionArgs[16] = rest.RuntimeType;
        var projectionType =  typeof(ValueTupleProjection<,,,,,,,,,,,,,,,,>).MakeGenericType(projectionArgs);

        var runtimeTupleType = typeof(ValueTuple<,,,,,,,>).MakeGenericType(
            headRuntimeValues[0],
            headRuntimeValues[1],
            headRuntimeValues[2],
            headRuntimeValues[3],
            headRuntimeValues[4],
            headRuntimeValues[5],
            headRuntimeValues[6],
            rest.RuntimeType);

        var publicTupleType = typeof(ValueTuple<,,,,,,,>).MakeGenericType(
            headPublicValues[0],
            headPublicValues[1],
            headPublicValues[2],
            headPublicValues[3],
            headPublicValues[4],
            headPublicValues[5],
            headPublicValues[6],
            rest.PublicType);

        return new(projectionType, runtimeTupleType, publicTupleType);
    }

    private static Type CreateLeafTupleProjectionType(Type rowType, ReadOnlySpan<Type> runtimeColumnTypes, ReadOnlySpan<Type> runtimeValueTypes)
    {
        return runtimeColumnTypes.Length switch
        {
            1 => typeof(ValueTupleProjection<,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeValueTypes[0]),
            2 => typeof(ValueTupleProjection<,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeValueTypes[0], runtimeValueTypes[1]),
            3 => typeof(ValueTupleProjection<,,,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeColumnTypes[2], runtimeValueTypes[0], runtimeValueTypes[1], runtimeValueTypes[2]),
            4 => typeof(ValueTupleProjection<,,,,,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeColumnTypes[2], runtimeColumnTypes[3], runtimeValueTypes[0], runtimeValueTypes[1], runtimeValueTypes[2], runtimeValueTypes[3]),
            5 => typeof(ValueTupleProjection<,,,,,,,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeColumnTypes[2], runtimeColumnTypes[3], runtimeColumnTypes[4], runtimeValueTypes[0], runtimeValueTypes[1], runtimeValueTypes[2], runtimeValueTypes[3], runtimeValueTypes[4]),
            6 => typeof(ValueTupleProjection<,,,,,,,,,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeColumnTypes[2], runtimeColumnTypes[3], runtimeColumnTypes[4], runtimeColumnTypes[5], runtimeValueTypes[0], runtimeValueTypes[1], runtimeValueTypes[2], runtimeValueTypes[3], runtimeValueTypes[4], runtimeValueTypes[5]),
            7 => typeof(ValueTupleProjection<,,,,,,,,,,,,,,>).MakeGenericType(rowType, runtimeColumnTypes[0], runtimeColumnTypes[1], runtimeColumnTypes[2], runtimeColumnTypes[3], runtimeColumnTypes[4], runtimeColumnTypes[5], runtimeColumnTypes[6], runtimeValueTypes[0], runtimeValueTypes[1], runtimeValueTypes[2], runtimeValueTypes[3], runtimeValueTypes[4], runtimeValueTypes[5], runtimeValueTypes[6]),
            _ => throw new InvalidOperationException("ValueTuple projection arity is not supported."),
        };
    }

    private static Type CreateSmallValueTupleType(ReadOnlySpan<Type> elementTypes)
    {
        return elementTypes.Length switch
        {
            1 => typeof(ValueTuple<>).MakeGenericType(elementTypes[0]),
            2 => typeof(ValueTuple<,>).MakeGenericType(elementTypes[0], elementTypes[1]),
            3 => typeof(ValueTuple<,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2]),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3]),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4]),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementTypes[5]),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(elementTypes[0], elementTypes[1], elementTypes[2], elementTypes[3], elementTypes[4], elementTypes[5], elementTypes[6]),
            _ => throw new InvalidOperationException("ValueTuple arity is not supported."),
        };
    }

    private readonly record struct TupleSelectionInfo(Type ProjectionType, Type RuntimeType, Type PublicType);

    private static Type BuildPredicate<TRow>(WhereExpression expression)
    {
        return expression switch
        {
            ComparisonExpression comparison => BuildComparisonPredicate<TRow>(comparison),
            AndExpression andExpression => typeof(AndFilter<,,>).MakeGenericType(
                typeof(TRow),
                BuildPredicate<TRow>(andExpression.Left),
                BuildPredicate<TRow>(andExpression.Right)),
            OrExpression orExpression => typeof(OrFilter<,,>).MakeGenericType(
                typeof(TRow),
                BuildPredicate<TRow>(orExpression.Left),
                BuildPredicate<TRow>(orExpression.Right)),
            NotExpression notExpression => typeof(NotFilter<,>).MakeGenericType(
                typeof(TRow),
                BuildPredicate<TRow>(notExpression.Expression)),
            _ => throw new InvalidOperationException($"Unsupported WHERE expression '{expression}'."),
        };
    }

    private static Type BuildComparisonPredicate<TRow>(ComparisonExpression comparison)
    {
        var rowType = typeof(TRow);
        var column = SchemaRegistry<TRow>.ResolveColumn(comparison.ColumnIdentifier);
        var runtimeColumnType = column.GetRuntimeColumnType(rowType);
        var runtimeColumnValueType = column.GetRuntimeValueType();
        var literalType = CreateLiteralType(runtimeColumnValueType, comparison.Literal);
        var filterDefinition = comparison.Operator switch
        {
            ComparisonOperator.Equals => typeof(EqualsFilter<,,,>),
            ComparisonOperator.GreaterThan => typeof(GreaterThanFilter<,,,>),
            ComparisonOperator.LessThan => typeof(LessThanFilter<,,,>),
            ComparisonOperator.GreaterOrEqual => typeof(GreaterOrEqualFilter<,,,>),
            ComparisonOperator.LessOrEqual => typeof(LessOrEqualFilter<,,,>),
            ComparisonOperator.NotEqual => typeof(NotEqualFilter<,,,>),
            _ => throw new InvalidOperationException($"Unsupported operator '{comparison.Operator}'."),
        };

        return filterDefinition.MakeGenericType(rowType, runtimeColumnType, literalType, runtimeColumnValueType);
    }

    private static Type Optimize(Type pipeline)
    {
        if (pipeline is null)
        {
            throw new ArgumentNullException(nameof(pipeline));
        }

        if (!pipeline.IsGenericType)
        {
            return pipeline;
        }

        var definition = pipeline.GetGenericTypeDefinition();

        if (definition == typeof(Where<,,,,>))
        {
            var args = pipeline.GetGenericArguments();
            var rowType = args[Where.TRow];
            var predicateType = args[Where.TPredicate];
            var next = args[Where.TNext];
            var resultType = args[Where.TResult];
            var rootType = args[Where.TRoot];

            if (next.IsGenericType && next.GetGenericTypeDefinition() == typeof(Select<,,,,,>))
            {
                var selectArgs = next.GetGenericArguments();
                if (selectArgs[Select.TRow] == rowType && selectArgs[Select.TResult] == resultType && selectArgs[Select.TRoot] == rootType)
                {
                    var projectionType = selectArgs[Select.TProjection];
                    var selectNext = Optimize(selectArgs[Select.TNext]);
                    var middleType = selectArgs[Select.TMiddle];
                    return typeof(WhereSelect<,,,,,,>).MakeGenericType(rowType, predicateType, projectionType, selectNext, middleType, resultType, rootType);
                }
            }

            var optimizedNext = Optimize(next);
            return typeof(Where<,,,,>).MakeGenericType(rowType, predicateType, optimizedNext, resultType, rootType);
        }

        if (definition == typeof(Select<,,,,,>))
        {
            var args = pipeline.GetGenericArguments();
            var rowType = args[Select.TRow];
            var projectionType = args[Select.TProjection];
            var next = args[Select.TNext];
            var middleType = args[Select.TMiddle];
            var resultType = args[Select.TResult];
            var rootType = args[Select.TRoot];

            if (next.IsGenericType && next.GetGenericTypeDefinition() == typeof(Where<,,,,>))
            {
                var whereArgs = next.GetGenericArguments();
                if (whereArgs[Where.TRow] == middleType && whereArgs[Where.TResult] == resultType && whereArgs[Where.TRoot] == rootType)
                {
                    var predicateType = whereArgs[Where.TPredicate];
                    var whereNext = Optimize(whereArgs[Where.TNext]);
                    return typeof(WhereSelect<,,,,,,>).MakeGenericType(rowType, predicateType, projectionType, whereNext, middleType, resultType, rootType);
                }
            }

            var optimizedNext = Optimize(next);
            return typeof(Select<,,,,,>).MakeGenericType(rowType, projectionType, optimizedNext, middleType, resultType, rootType);
        }

        if (definition == typeof(WhereSelect<,,,,,,>))
        {
            var args = pipeline.GetGenericArguments();
            var next = Optimize(args[WhereSelect.TNext]);
            return typeof(WhereSelect<,,,,,,>).MakeGenericType(
                args[WhereSelect.TRow],
                args[WhereSelect.TPredicate],
                args[WhereSelect.TProjection],
                next,
                args[WhereSelect.TMiddle],
                args[WhereSelect.TResult],
                args[WhereSelect.TRoot]);
        }

        return pipeline;
    }

    private static Type CreateLiteralType(Type columnType, LiteralValue literal)
    {
        if (columnType == typeof(int))
        {
            if (literal.Kind != LiteralKind.Integer)
            {
                throw new InvalidOperationException("Expected an integer literal for this column.");
            }

            return LiteralTypeFactory.CreateIntLiteral(literal.IntValue);
        }

        if (columnType == typeof(ValueString))
        {
            if (literal.Kind != LiteralKind.String || literal.StringValue is null)
            {
                throw new InvalidOperationException("Expected a string literal for this column.");
            }

            return LiteralTypeFactory.CreateStringLiteral(literal.StringValue);
        }

        if (columnType == typeof(float))
        {
            if (literal.Kind == LiteralKind.Integer)
            {
                return LiteralTypeFactory.CreateFloatLiteral(literal.IntValue);
            }

            if (literal.Kind == LiteralKind.Float)
            {
                return LiteralTypeFactory.CreateFloatLiteral(literal.FloatValue);
            }

            throw new InvalidOperationException("Expected a numeric literal for this column.");
        }

        if (columnType == typeof(bool))
        {
            if (literal.Kind != LiteralKind.Boolean)
            {
                throw new InvalidOperationException("Expected a boolean literal for this column.");
            }

            return LiteralTypeFactory.CreateBoolLiteral(literal.BoolValue);
        }

        throw new InvalidOperationException($"Column type '{columnType}' is not supported in WHERE clauses.");
    }
}
