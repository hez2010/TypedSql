using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace TypedSql.Runtime;

public static class QueryEngine
{
    public readonly struct CompiledQuery<TRow, TResult>(MethodInfo executeMethod)
    {
        private readonly unsafe delegate* managed<ReadOnlySpan<TRow>, IReadOnlyList<TResult>> _entryPoint = (delegate* managed<ReadOnlySpan<TRow>, IReadOnlyList<TResult>>)executeMethod.MethodHandle.GetFunctionPointer();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IReadOnlyList<TResult> Execute(ReadOnlySpan<TRow> rows)
        {
            unsafe
            {
                return _entryPoint(rows);
            }
        }

        public override string ToString()
        {
            return Visualize(executeMethod.DeclaringType!, friendly: true);
        }
    }

    public static CompiledQuery<TRow, TResult> Compile<TRow, TResult>([StringSyntax("sql")] string sql)
    {
        var parsed = SqlParser.Parse(sql);
        var (pipelineType, runtimeResultType, publicResultType) = SqlCompiler.Compile<TRow>(parsed);
        if (publicResultType != typeof(TResult))
        {
            throw new InvalidOperationException($"Query result type '{publicResultType}' does not match '{typeof(TResult)}'.");
        }

        var programType = typeof(QueryProgram<,,,>).MakeGenericType(typeof(TRow), pipelineType, runtimeResultType, publicResultType);
        var executeMethod = programType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static)
                         ?? throw new InvalidOperationException("Query program entry point is missing.");
        return CreateRunner<TRow, TResult>(executeMethod);
    }

    public static IReadOnlyList<TResult> Execute<TRow, TResult>([StringSyntax("sql")] string sql, ReadOnlySpan<TRow> rows)
    {
        return Compile<TRow, TResult>(sql).Execute(rows);
    }

    private static CompiledQuery<TRow, TResult> CreateRunner<TRow, TResult>(MethodInfo executeMethod)
    {
        return new CompiledQuery<TRow, TResult>(executeMethod);
    }

    internal static string Visualize(Type t, bool friendly = false)
    {
        if (!t.IsGenericType) return t.Name;
        if (friendly && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILiteral<>))) return GetLiteralValue(t);
        var sb = new StringBuilder();
        sb.Append($"{t.Name.AsSpan(0, t.Name.IndexOf('`'))}<");
        var cnt = 0;
        foreach (var arg in t.GetGenericArguments())
        {
            if (cnt > 0) sb.Append(", ");
            sb.Append(Visualize(arg, friendly));
            cnt++;
        }
        return sb.Append('>').ToString();
    }

    private static string GetLiteralValue(Type t)
    {
        if (t.GetInterfaces().Any(i => i == typeof(ILiteral<ValueString>)))
        {
            var literalValue = (ValueString)t.GetProperty("Value")!.GetValue(null)!;
            return $"'{literalValue}'";
        }

        var hex = t.GetGenericArguments();
        var num = 0;
        for (int i = 0; i < hex.Length; i++)
        {
            num <<= 4;
            num |= (int)hex[i].GetProperty("Value")!.GetValue(null)!;
        }
        var targetType = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ILiteral<>)).GetGenericArguments()[0];
        var converted = typeof(Unsafe).GetMethod("As", 2, BindingFlags.Public | BindingFlags.Static, [Type.MakeGenericMethodParameter(0).MakeByRefType()])!.MakeGenericMethod(typeof(int), targetType).Invoke(null, [num])!;
        return converted!.ToString()!;
    }

}

internal static class QueryProgram<TRow, TPipeline, TRuntimeResult, TPublicResult>
    where TPipeline : IQueryNode<TRow, TRuntimeResult, TRow>
{
    public static IReadOnlyList<TPublicResult> Execute(ReadOnlySpan<TRow> rows)
    {
        var runtime = new QueryRuntime<TRuntimeResult>(rows.Length);
        TPipeline.Run(rows, ref runtime);

        return ConvertResult(ref runtime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyList<TPublicResult> ConvertResult(ref QueryRuntime<TRuntimeResult> runtime)
    {
        if (typeof(TRuntimeResult) == typeof(TPublicResult))
        {
            return (IReadOnlyList<TPublicResult>)(object)runtime.Rows;
        }
        else if (typeof(TRuntimeResult) == typeof(ValueString) && typeof(TPublicResult) == typeof(string))
        {
            return (IReadOnlyList<TPublicResult>)(object)runtime.AsStringRows();
        }
        
        return Throw();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static IReadOnlyList<TPublicResult> Throw() => throw new InvalidOperationException($"Cannot convert query result from '{typeof(TRuntimeResult)}' to '{typeof(TPublicResult)}'.");
}
