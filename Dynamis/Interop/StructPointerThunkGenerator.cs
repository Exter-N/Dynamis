using System.Reflection;
using System.Reflection.Emit;
using FFXIVClientStructs.Interop;

namespace Dynamis.Interop;

public static class StructPointerThunkGenerator
{
    public static MethodInfo GeneratePointerThunk(MethodInfo method)
    {
        if (method is
            {
                IsStatic: false,
                ReflectedType: not
                {
                    IsValueType: true,
                },
            }) {
            throw new NotSupportedException(
                "Can only generate pointer thunks for static methods, and unmanaged struct instance methods"
            );
        }

        var parameters = method.GetParameters();
        Type[] innerParameterTypes = method.IsStatic
            ? [..parameters.Select(p => p.ParameterType),]
            : [method.ReflectedType!.MakePointerType(), ..parameters.Select(p => p.ParameterType),];
        var outerParameterInfo = Array.ConvertAll(innerParameterTypes, GetOuterTypeInfo);
        var outerReturnInfo = GetOuterTypeInfo(method.ReturnType);
        if (method.IsStatic && Array.TrueForAll(outerParameterInfo, param => !param.TransformsParameter)
                            && !outerReturnInfo.TransformsReturn) {
            return method;
        }

        var thunk = new DynamicMethod(
            $"<PointerThunk>{method.Name}", MethodAttributes.Public | MethodAttributes.Static,
            CallingConventions.Standard, outerReturnInfo.OuterType,
            Array.ConvertAll(outerParameterInfo, param => param.OuterType), method.ReflectedType!, false
        );

        var i = 1;
        if (!method.IsStatic) {
            thunk.DefineParameter(i++, ParameterAttributes.In, "this");
        }

        foreach (var param in parameters) {
            thunk.DefineParameter(i++, param.Attributes, param.Name);
        }

        var ilGen = thunk.GetILGenerator();
        for (short j = 0; j < outerParameterInfo.Length; j++) {
            outerParameterInfo[j].EmitLdParameter(ilGen, j);
        }

        if (outerReturnInfo.TransformsReturn) {
            ilGen.Emit(OpCodes.Call, method);
            outerReturnInfo.EmitConvReturn(ilGen);
        } else {
            ilGen.Emit(OpCodes.Tailcall);
            ilGen.Emit(OpCodes.Call, method);
        }

        ilGen.Emit(OpCodes.Ret);

        return thunk;
    }

    private static void EmitLdarg(ILGenerator ilGen, short index)
    {
        switch (index) {
            case 0:
                ilGen.Emit(OpCodes.Ldarg_0);
                break;
            case 1:
                ilGen.Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                ilGen.Emit(OpCodes.Ldarg_2);
                break;
            case 3:
                ilGen.Emit(OpCodes.Ldarg_3);
                break;
            case <= byte.MaxValue:
                ilGen.Emit(OpCodes.Ldarg_S, (byte)index);
                break;
            default:
                ilGen.Emit(OpCodes.Ldarg, index);
                break;
        }
    }

    private static void EmitLdarga(ILGenerator ilGen, short index)
    {
        switch (index) {
            case <= byte.MaxValue:
                ilGen.Emit(OpCodes.Ldarga_S, (byte)index);
                break;
            default:
                ilGen.Emit(OpCodes.Ldarga, index);
                break;
        }
    }

    private static OuterTypeInfo GetOuterTypeInfo(Type type)
    {
        if (type.IsGenericType) {
            if (type.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>)) {
                return new MemoryTypeInfo(type.GetGenericArguments()[0], true);
            }

            if (type.GetGenericTypeDefinition() == typeof(Span<>)) {
                return new MemoryTypeInfo(type.GetGenericArguments()[0], false);
            }
        }

        if (type.IsByRefLike) {
            throw new NotSupportedException(
                $"Unsupported parameter/return type for pointer thunk generation: {type.FullName}"
            );
        }

        if (type.IsPointer || type.IsByRef) {
            var outerType = GetCsPointerType(type);
            return outerType == typeof(nint)
                ? new BlitTypeInfo(outerType)
                : new CsPointerTypeInfo(outerType);
        }

        return new BlitTypeInfo(type);
    }

    private static Type GetCsPointerType(Type type)
    {
        if (type == typeof(void*)) {
            return typeof(nint);
        }

        if (type.IsPointer || type.IsByRef) {
            return typeof(Pointer<>).MakeGenericType(GetCsPointerType(type.GetElementType()!));
        }

        if (!type.IsValueType) {
            throw new NotSupportedException(
                $"Unsupported type within pointer/reference for pointer thunk generation: {type.FullName}"
            );
        }

        return type;
    }

    private abstract class OuterTypeInfo
    {
        public abstract Type OuterType { get; }

        public abstract bool TransformsParameter { get; }

        public abstract bool TransformsReturn { get; }

        public abstract void EmitLdParameter(ILGenerator ilGen, short index);

        public abstract void EmitConvReturn(ILGenerator ilGen);
    }

    private sealed class BlitTypeInfo(Type type) : OuterTypeInfo
    {
        public override Type OuterType
            => type;

        public override bool TransformsParameter
            => false;

        public override bool TransformsReturn
            => false;

        public override void EmitLdParameter(ILGenerator ilGen, short j)
            => EmitLdarg(ilGen, j);

        public override void EmitConvReturn(ILGenerator ilGen)
        {
        }
    }

    private sealed class MemoryTypeInfo(Type elementType, bool readOnly) : OuterTypeInfo
    {
        private Type InnerType
            => (readOnly ? typeof(ReadOnlySpan<>) : typeof(Span<>)).MakeGenericType(elementType);

        public override Type OuterType
            => (readOnly ? typeof(ReadOnlyMemory<>) : typeof(Memory<>)).MakeGenericType(elementType);

        public override bool TransformsParameter
            => true;

        public override bool TransformsReturn
            => true;

        public override void EmitLdParameter(ILGenerator ilGen, short j)
        {
            EmitLdarga(ilGen, j);
            ilGen.Emit(
                OpCodes.Call,
                OuterType.GetProperty("Span", BindingFlags.Public | BindingFlags.Instance)!.GetGetMethod()!
            );
        }

        public override void EmitConvReturn(ILGenerator ilGen)
        {
            var innerType = InnerType;
            var managerType = typeof(BorrowedUnmanagedMemory<>).MakeGenericType(innerType.GetGenericArguments()[0]);
            ilGen.Emit(OpCodes.Tailcall);
            ilGen.Emit(
                OpCodes.Call,
                managerType.GetMethod("ToMemory", BindingFlags.Public | BindingFlags.Static, [innerType,])!
            );
        }
    }

    private sealed class CsPointerTypeInfo(Type csPointerType) : OuterTypeInfo
    {
        public override Type OuterType
            => csPointerType;

        public override bool TransformsParameter
            => true;

        public override bool TransformsReturn
            => true;

        public override void EmitLdParameter(ILGenerator ilGen, short j)
        {
            EmitLdarga(ilGen, j);
            ilGen.Emit(
                OpCodes.Call,
                csPointerType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)!.GetGetMethod()!
            );
        }

        public override void EmitConvReturn(ILGenerator ilGen)
        {
            ilGen.Emit(OpCodes.Tailcall);
            ilGen.Emit(
                OpCodes.Call,
                csPointerType.GetMethod(
                    "op_Implicit", BindingFlags.Public | BindingFlags.Static,
                    [csPointerType.GetGenericArguments()[0].MakePointerType(),]
                )!
            );
        }
    }
}
