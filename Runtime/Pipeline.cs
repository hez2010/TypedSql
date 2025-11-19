using System.Runtime.CompilerServices;

namespace TypedSql.Runtime;

internal static class Where
{
    internal const int TRow = 0;
    internal const int TPredicate = 1;
    internal const int TNext = 2;
    internal const int TResult = 3;
    internal const int TRoot = 4;
}

internal static class Select
{
    internal const int TRow = 0;
    internal const int TProjection = 1;
    internal const int TNext = 2;
    internal const int TMiddle = 3;
    internal const int TResult = 4;
    internal const int TRoot = 5;
}

internal static class WhereSelect
{
    internal const int TRow = 0;
    internal const int TPredicate = 1;
    internal const int TProjection = 2;
    internal const int TNext = 3;
    internal const int TMiddle = 4;
    internal const int TResult = 5;
    internal const int TRoot = 6;
}

internal interface IQueryNode<TRow, TResult, TRoot>
{
    abstract static void Run(ReadOnlySpan<TRow> rows, scoped ref QueryRuntime<TResult> runtime);

    abstract static void Process(in TRow row, scoped ref QueryRuntime<TResult> runtime);
}

internal readonly struct Where<TRow, TPredicate, TNext, TResult, TRoot> : IQueryNode<TRow, TResult, TRoot>
    where TPredicate : IFilter<TRow>
    where TNext : IQueryNode<TRow, TResult, TRoot>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ReadOnlySpan<TRow> rows, scoped ref QueryRuntime<TResult> runtime)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            Process(in rows[i], ref runtime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(in TRow row, scoped ref QueryRuntime<TResult> runtime)
    {
        if (TPredicate.Evaluate(in row))
        {
            TNext.Process(in row, ref runtime);
        }
    }
}

internal readonly struct Select<TRow, TProjection, TNext, TMiddle, TResult, TRoot> : IQueryNode<TRow, TResult, TRoot>
    where TProjection : IProjection<TRow, TMiddle>
    where TNext : IQueryNode<TMiddle, TResult, TRoot>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ReadOnlySpan<TRow> rows, scoped ref QueryRuntime<TResult> runtime)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            Process(in rows[i], ref runtime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(in TRow row, scoped ref QueryRuntime<TResult> runtime)
    {
        var projected = TProjection.Project(in row);
        TNext.Process(in projected, ref runtime);
    }
}

internal readonly struct WhereSelect<TRow, TPredicate, TProjection, TNext, TMiddle, TResult, TRoot> : IQueryNode<TRow, TResult, TRoot>
    where TPredicate : IFilter<TRow>
    where TProjection : IProjection<TRow, TMiddle>
    where TNext : IQueryNode<TMiddle, TResult, TRoot>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ReadOnlySpan<TRow> rows, scoped ref QueryRuntime<TResult> runtime)
    {
        for (var i = 0; i < rows.Length; i++)
        {
            Process(in rows[i], ref runtime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(in TRow row, scoped ref QueryRuntime<TResult> runtime)
    {
        if (TPredicate.Evaluate(in row))
        {
            var projected = TProjection.Project(in row);
            TNext.Process(in projected, ref runtime);
        }
    }
}

internal readonly struct Stop<TResult, TRoot> : IQueryNode<TResult, TResult, TRoot>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Run(ReadOnlySpan<TResult> rows, scoped ref QueryRuntime<TResult> runtime)
    {
        runtime.AddRange(rows);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Process(in TResult row, scoped ref QueryRuntime<TResult> runtime)
    {
        runtime.Add(in row);
    }
}
