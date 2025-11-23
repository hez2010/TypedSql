using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TypedSql.Runtime;

internal readonly struct ValueString(string? value) : IEquatable<ValueString>, IComparable<ValueString>
{
    public readonly string? Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(ValueString other)
        => string.Compare(Value, other.Value, StringComparison.Ordinal);

    public bool Equals(ValueString other)
    {
        return string.Equals(Value, other.Value, StringComparison.Ordinal);
    }

    public override string? ToString() => Value;

    public static implicit operator ValueString(string value) => new(value);

    public static implicit operator string?(ValueString value) => value.Value;

    public override bool Equals(object? obj)
    {
        return obj is ValueString str && Equals(str);
    }

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }
}

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal ref struct QueryRuntime<TResult>(int expectedCount)
{
    private readonly TResult[] _buffer = new TResult[expectedCount];
    private int _count = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(in TResult value)
    {
        // The resulted rows are never larger than the input rows, so no resizing is needed.
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), _count++) = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(ReadOnlySpan<TResult> values)
    {
        if (values.Length == 0)
        {
            return;
        }
        // The resulted rows are never larger than the input rows, so no resizing is needed.
        values.CopyTo(MemoryMarshal.CreateSpan(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), _count), values.Length));
        _count += values.Length;
    }

    public readonly IReadOnlyList<TResult> Rows
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return new QueryResult<TResult>(_buffer, _count);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IReadOnlyList<string> AsStringRows()
    {
        var buffer = (ValueString[])(object)_buffer;
        return new ValueStringQueryResult(buffer, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly IReadOnlyList<TPublicResult> AsValueTupleRows<TPublicResult>()
    {
        return new ValueTupleQueryResult<TPublicResult, TResult>(_buffer, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<TResult> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_buffer), _count);
    }
}

internal readonly struct ValueStringQueryResult : IReadOnlyList<string?>
{
    private readonly ValueString[] _buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueStringQueryResult(ValueString[] buffer, int count)
    {
        _buffer = buffer;
        Count = count;
    }

    public string? this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), index).Value;
        }
    }

    public int Count { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_buffer, Count);

    IEnumerator<string> IEnumerable<string?>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal struct Enumerator : IEnumerator<string?>
    {
        private readonly ValueString[] _buffer;
        private readonly int _count;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(ValueString[] buffer, int count)
        {
            _buffer = buffer;
            _count = count;
            _index = -1;
        }

        public readonly string? Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), _index).Value;
            }
        }

        readonly object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;

        public void Reset() => _index = -1;

        public readonly void Dispose() { }
    }
}

internal readonly struct QueryResult<TResult> : IReadOnlyList<TResult>
{
    private readonly TResult[] _buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal QueryResult(TResult[] buffer, int count)
    {
        _buffer = buffer;
        Count = count;
    }

    public TResult this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), index);
        }
    }

    public int Count { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_buffer, Count);

    IEnumerator<TResult> IEnumerable<TResult>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal struct Enumerator : IEnumerator<TResult>
    {
        private readonly TResult[] _buffer;
        private readonly int _count;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(TResult[] buffer, int count)
        {
            _buffer = buffer;
            _count = count;
            _index = -1;
        }

        public readonly TResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), _index);
            }
        }

        readonly object? IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;

        public void Reset() => _index = -1;

        public readonly void Dispose() { }
    }
}

internal readonly struct ValueTupleQueryResult<TPublicResult, TRuntimeResult> : IReadOnlyList<TPublicResult>
{
    private readonly TRuntimeResult[] _buffer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueTupleQueryResult(TRuntimeResult[] buffer, int count)
    {
        _buffer = buffer;
        Count = count;
    }

    public TPublicResult this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
            TPublicResult result = default!;
            ValueTupleConvertHelper<TPublicResult, TRuntimeResult>.Copy(ref result, ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), index));
            return result;
        }
    }

    public int Count { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_buffer, Count);

    IEnumerator<TPublicResult> IEnumerable<TPublicResult>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal struct Enumerator : IEnumerator<TPublicResult>
    {
        private readonly TRuntimeResult[] _buffer;
        private readonly int _count;
        private int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(TRuntimeResult[] buffer, int count)
        {
            _buffer = buffer;
            _count = count;
            _index = -1;
        }

        public readonly TPublicResult Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                TPublicResult result = default!;
                ValueTupleConvertHelper<TPublicResult, TRuntimeResult>.Copy(ref result, ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_buffer), _index));
                return result;
            }
        }

        readonly object IEnumerator.Current => Current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _count;

        public void Reset() => _index = -1;

        public readonly void Dispose() { }
    }
}
