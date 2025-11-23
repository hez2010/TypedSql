using System;
using System.Runtime.CompilerServices;

namespace TypedSql.Runtime;

internal interface IColumn<TRow, TValue>
{
    abstract static string Identifier { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract static TValue Get(in TRow row);
}

internal interface IProjection<TRow, TResult>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract static TResult Project(in TRow row);
}

internal readonly struct IdentityProjection<TRow> : IProjection<TRow, TRow>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TRow Project(in TRow row) => row;
}

internal readonly struct ColumnProjection<TColumn, TRow, TValue> : IProjection<TRow, TValue>
    where TColumn : IColumn<TRow, TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue Project(in TRow row) => TColumn.Get(row);
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TValue1> : IProjection<TRow, ValueTuple<TValue1>>
    where TColumn1 : IColumn<TRow, TValue1>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1> Project(in TRow row)
        => new(TColumn1.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TValue1, TValue2> : IProjection<TRow, ValueTuple<TValue1, TValue2>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2> Project(in TRow row)
        => new(TColumn1.Get(row), TColumn2.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TValue1, TValue2, TValue3> : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3> Project(in TRow row)
        => new(TColumn1.Get(row), TColumn2.Get(row), TColumn3.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TColumn4, TValue1, TValue2, TValue3, TValue4> : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3, TValue4>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
    where TColumn4 : IColumn<TRow, TValue4>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3, TValue4> Project(in TRow row)
        => new(TColumn1.Get(row), TColumn2.Get(row), TColumn3.Get(row), TColumn4.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5, TValue1, TValue2, TValue3, TValue4, TValue5> : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
    where TColumn4 : IColumn<TRow, TValue4>
    where TColumn5 : IColumn<TRow, TValue5>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5> Project(in TRow row)
        => new(TColumn1.Get(row), TColumn2.Get(row), TColumn3.Get(row), TColumn4.Get(row), TColumn5.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5, TColumn6, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6> : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
    where TColumn4 : IColumn<TRow, TValue4>
    where TColumn5 : IColumn<TRow, TValue5>
    where TColumn6 : IColumn<TRow, TValue6>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6> Project(in TRow row)
        => new(TColumn1.Get(row), TColumn2.Get(row), TColumn3.Get(row), TColumn4.Get(row), TColumn5.Get(row), TColumn6.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5, TColumn6, TColumn7, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7> : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
    where TColumn4 : IColumn<TRow, TValue4>
    where TColumn5 : IColumn<TRow, TValue5>
    where TColumn6 : IColumn<TRow, TValue6>
    where TColumn7 : IColumn<TRow, TValue7>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7> Project(in TRow row)
        => new(
            TColumn1.Get(row),
            TColumn2.Get(row),
            TColumn3.Get(row),
            TColumn4.Get(row),
            TColumn5.Get(row),
            TColumn6.Get(row),
            TColumn7.Get(row));
}

internal readonly struct ValueTupleProjection<TRow, TColumn1, TColumn2, TColumn3, TColumn4, TColumn5, TColumn6, TColumn7, TNextProjection, TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TRest>
    : IProjection<TRow, ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TRest>>
    where TColumn1 : IColumn<TRow, TValue1>
    where TColumn2 : IColumn<TRow, TValue2>
    where TColumn3 : IColumn<TRow, TValue3>
    where TColumn4 : IColumn<TRow, TValue4>
    where TColumn5 : IColumn<TRow, TValue5>
    where TColumn6 : IColumn<TRow, TValue6>
    where TColumn7 : IColumn<TRow, TValue7>
    where TNextProjection : IProjection<TRow, TRest>
    where TRest : struct
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTuple<TValue1, TValue2, TValue3, TValue4, TValue5, TValue6, TValue7, TRest> Project(in TRow row)
        => new(
            TColumn1.Get(row),
            TColumn2.Get(row),
            TColumn3.Get(row),
            TColumn4.Get(row),
            TColumn5.Get(row),
            TColumn6.Get(row),
            TColumn7.Get(row),
            TNextProjection.Project(in row));
}

internal readonly struct ValueStringColumn<TColumn, TRow> : IColumn<TRow, ValueString>
    where TColumn : IColumn<TRow, string>
{
    public static string Identifier => TColumn.Identifier;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueString Get(in TRow row) => new(TColumn.Get(in row));
}

internal interface IFilter<TRow>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    abstract static bool Evaluate(in TRow row);
}

internal readonly struct EqualsFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) == 0;
}

internal readonly struct GreaterThanFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) > 0;
}

internal readonly struct LessThanFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) < 0;
}

internal readonly struct GreaterOrEqualFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) >= 0;
}

internal readonly struct LessOrEqualFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) <= 0;
}

internal readonly struct NotEqualFilter<TRow, TColumn, TLiteral, TValue> : IFilter<TRow>
    where TColumn : IColumn<TRow, TValue>
    where TLiteral : ILiteral<TValue>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
        => Comparer<TValue>.Default.Compare(TColumn.Get(row), TLiteral.Value) != 0;
}

internal readonly struct AndFilter<TRow, TLeftPredicate, TRightPredicate> : IFilter<TRow>
    where TLeftPredicate : IFilter<TRow>
    where TRightPredicate : IFilter<TRow>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
    {
        if (!TLeftPredicate.Evaluate(in row))
        {
            return false;
        }

        return TRightPredicate.Evaluate(in row);
    }
}

internal readonly struct OrFilter<TRow, TLeftPredicate, TRightPredicate> : IFilter<TRow>
    where TLeftPredicate : IFilter<TRow>
    where TRightPredicate : IFilter<TRow>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row)
    {
        if (TLeftPredicate.Evaluate(in row))
        {
            return true;
        }

        return TRightPredicate.Evaluate(in row);
    }
}

internal readonly struct NotFilter<TRow, TPredicate> : IFilter<TRow>
    where TPredicate : IFilter<TRow>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Evaluate(in TRow row) => !TPredicate.Evaluate(in row);
}