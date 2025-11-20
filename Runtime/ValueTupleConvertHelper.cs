using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;

namespace TypedSql.Runtime;

internal static class ValueTupleConvertHelper<TPublicResult, TRuntimeResult>
{
    private delegate void CopyDelegate(ref TPublicResult dest, ref readonly TRuntimeResult source);

    private static readonly CopyDelegate? _helper;
    private static readonly bool _isInvalid;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Copy(ref TPublicResult dest, ref readonly TRuntimeResult source)
    {
        if (_isInvalid)
        {
            throw new Exception("Invalid types for ValueTupleConvertHelper.");
        }
        if (typeof(TPublicResult) == typeof(TRuntimeResult)) // optional potential optimisation
        {
            dest = Unsafe.As<TRuntimeResult, TPublicResult>(ref Unsafe.AsRef(in source));
        }
        else
        {
            _helper!(ref dest, in source);
        }
    }

    static ValueTupleConvertHelper()
    {
        DynamicMethod dm = new($"ValueTupleConvertHelper+{typeof(TPublicResult)}+typeof(T2)", typeof(void), [typeof(TPublicResult).MakeByRefType(), typeof(TRuntimeResult).MakeByRefType()], true);
        ILGenerator ilGen = dm.GetILGenerator();
        if (typeof(TPublicResult) == typeof(TRuntimeResult))
        {
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.Emit(OpCodes.Ldarg_1);
            ilGen.Emit(OpCodes.Cpobj, typeof(TPublicResult));
            ilGen.Emit(OpCodes.Ret);
            return;
        }
        var isInvalidTmp = false;
        var tmp1 = ilGen.DeclareLocal(typeof(byte).MakeByRefType());
        var tmp2 = ilGen.DeclareLocal(typeof(byte).MakeByRefType());
        ilGen.Emit(OpCodes.Ldarg_0);
        ilGen.Emit(OpCodes.Stloc, tmp1);
        ilGen.Emit(OpCodes.Ldarg_1);
        ilGen.Emit(OpCodes.Stloc, tmp2);
        TypeHelper(typeof(TPublicResult), typeof(TRuntimeResult));
        void TypeHelper(Type t1, Type t2)
        {
            var gDefn = t1.GetGenericTypeDefinition();
            if (gDefn != t2.GetGenericTypeDefinition() || !gDefn.IsGenericType)
            {
                isInvalidTmp = true;
                return;
            }
            if (gDefn == typeof(ValueTuple<>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<>.Item1));
            }
            else if (gDefn == typeof(ValueTuple<,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,>.Item2));
            }
            else if (gDefn == typeof(ValueTuple<,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,>.Item3));
            }
            else if (gDefn == typeof(ValueTuple<,,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,>.Item3));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,>.Item4));
            }
            else if (gDefn == typeof(ValueTuple<,,,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,>.Item3));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,>.Item4));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,>.Item5));
            }
            else if (gDefn == typeof(ValueTuple<,,,,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item3));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item4));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item5));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,>.Item6));
            }
            else if (gDefn == typeof(ValueTuple<,,,,,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item3));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item4));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item5));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item6));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,>.Item7));
            }
            else if (gDefn == typeof(ValueTuple<,,,,,,,>))
            {
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item1));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item2));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item3));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item4));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item5));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item6));
                FieldHelper(t1, t2, nameof(ValueTuple<,,,,,,,>.Item7));
                if (isInvalidTmp) return;
                var rest1 = t1.GetField(nameof(ValueTuple<,,,,,,,>.Rest))!;
                var rest2 = t2.GetField(nameof(ValueTuple<,,,,,,,>.Rest))!;
                ilGen.Emit(OpCodes.Ldloc, tmp1);
                ilGen.Emit(OpCodes.Ldflda, rest1);
                ilGen.Emit(OpCodes.Stloc, tmp1);
                ilGen.Emit(OpCodes.Ldloc, tmp2);
                ilGen.Emit(OpCodes.Ldflda, rest2);
                ilGen.Emit(OpCodes.Stloc, tmp2);
                TypeHelper(rest1.FieldType, rest2.FieldType);
            }
            else
            {
                isInvalidTmp = true;
                return;
            }
        }

        void FieldHelper(Type t1, Type t2, string fieldName)
        {
            var f1 = t1.GetField(fieldName)!;
            var f2 = t2.GetField(fieldName)!;
            if (f1.FieldType == f2.FieldType)
            {
                ilGen.Emit(OpCodes.Ldloc, tmp1);
                ilGen.Emit(OpCodes.Ldloc, tmp2);
                ilGen.Emit(OpCodes.Ldfld, f2);
                ilGen.Emit(OpCodes.Stfld, f1);
            }
            else if (f1.FieldType == typeof(string) && f2.FieldType == typeof(ValueString))
            {
                ilGen.Emit(OpCodes.Ldloc, tmp1);
                ilGen.Emit(OpCodes.Ldloc, tmp2);
                ilGen.Emit(OpCodes.Ldflda, f2);
                ilGen.Emit(OpCodes.Ldfld, typeof(ValueString).GetField(nameof(ValueString.Value))!);
                ilGen.Emit(OpCodes.Stfld, f1);
            }
            else if (f1.FieldType == typeof(ValueString) && f2.FieldType == typeof(string))
            {
                ilGen.Emit(OpCodes.Ldloc, tmp1);
                ilGen.Emit(OpCodes.Ldloc, tmp2);
                ilGen.Emit(OpCodes.Ldfld, f2);
                ilGen.Emit(OpCodes.Newobj, typeof(ValueString).GetConstructor([typeof(string)])!);
                ilGen.Emit(OpCodes.Stfld, f1);
            }
            else
            {
                isInvalidTmp = true;
            }
        }

        _isInvalid = isInvalidTmp;
        if (isInvalidTmp) return;
        ilGen.Emit(OpCodes.Ret);
        _helper = dm.CreateDelegate<CopyDelegate>();
    }
}
