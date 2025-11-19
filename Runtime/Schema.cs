namespace TypedSql.Runtime;

internal readonly record struct ColumnMetadata(string Name, Type ColumnType, Type ValueType)
{
    public Type GetRuntimeValueType() => ValueType != typeof(string) ? ValueType : typeof(ValueString);

    public Type GetRuntimeColumnType(Type rowType)
    {
        if (ValueType != typeof(string))
        {
            return ColumnType;
        }

        return typeof(ValueStringColumn<,>).MakeGenericType(ColumnType, rowType);
    }
}

internal static class SchemaRegistry<TRow>
{
    private static IReadOnlyDictionary<string, ColumnMetadata>? _columns;

    public static void Register(IReadOnlyDictionary<string, ColumnMetadata> columns)
    {
        ArgumentNullException.ThrowIfNull(columns);
        _columns = columns;
    }

    public static ColumnMetadata ResolveColumn(string identifier)
    {
        if (_columns is null)
        {
            throw new InvalidOperationException($"Schema for row type '{typeof(TRow)}' has not been registered.");
        }

        if (_columns.TryGetValue(identifier, out var column))
        {
            return column;
        }

        throw new KeyNotFoundException($"Column '{identifier}' is not registered for row type '{typeof(TRow)}'.");
    }
}
