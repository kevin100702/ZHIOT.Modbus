using System.Buffers.Binary;
using System.Runtime.InteropServices;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Utils;

/// <summary>
/// Modbus 数据转换器，提供高性能的数据类型转换
/// 使用 Span&lt;T&gt; 实现零分配或最小分配的转换
/// </summary>
public static class ModbusDataConverter
{
    /// <summary>
    /// 将原始字节数据转换为目标类型数组（用于读取操作）
    /// </summary>
    /// <typeparam name="T">目标数据类型（支持 ushort, short, uint, int, float, double）</typeparam>
    /// <param name="source">原始字节数据</param>
    /// <param name="byteOrder">字节序</param>
    /// <returns>转换后的目标类型数组</returns>
    public static T[] ToArray<T>(ReadOnlySpan<byte> source, ByteOrder byteOrder) where T : struct
    {
        if (source.IsEmpty)
            return Array.Empty<T>();

        var type = typeof(T);
        var elementSize = GetElementSize<T>();

        if (source.Length % elementSize != 0)
            throw new ArgumentException($"Source length must be a multiple of {elementSize} bytes for type {type.Name}");

        int count = source.Length / elementSize;
        var result = new T[count];
        var resultSpan = result.AsSpan();

        if (type == typeof(ushort))
            ConvertToUInt16Array(source, byteOrder, MemoryMarshal.Cast<T, ushort>(resultSpan));
        else if (type == typeof(short))
            ConvertToInt16Array(source, byteOrder, MemoryMarshal.Cast<T, short>(resultSpan));
        else if (type == typeof(uint))
            ConvertToUInt32Array(source, byteOrder, MemoryMarshal.Cast<T, uint>(resultSpan));
        else if (type == typeof(int))
            ConvertToInt32Array(source, byteOrder, MemoryMarshal.Cast<T, int>(resultSpan));
        else if (type == typeof(float))
            ConvertToFloatArray(source, byteOrder, MemoryMarshal.Cast<T, float>(resultSpan));
        else if (type == typeof(double))
            ConvertToDoubleArray(source, byteOrder, MemoryMarshal.Cast<T, double>(resultSpan));
        else
            throw new NotSupportedException($"Type {type.Name} is not supported");

        return result;
    }

    /// <summary>
    /// 将单个值从原始字节数据中提取出来
    /// </summary>
    /// <typeparam name="T">目标数据类型</typeparam>
    /// <param name="source">原始字节数据</param>
    /// <param name="byteOrder">字节序</param>
    /// <returns>转换后的值</returns>
    public static T ToValue<T>(ReadOnlySpan<byte> source, ByteOrder byteOrder) where T : struct
    {
        var array = ToArray<T>(source, byteOrder);
        return array.Length > 0 ? array[0] : default;
    }

    /// <summary>
    /// 将目标类型数组转换为 Modbus 寄存器数组（用于写入操作）
    /// </summary>
    /// <typeparam name="T">源数据类型</typeparam>
    /// <param name="values">要转换的值数组</param>
    /// <param name="byteOrder">字节序</param>
    /// <returns>Modbus 寄存器数组（ushort[]）</returns>
    public static ushort[] ToRegisters<T>(T[] values, ByteOrder byteOrder) where T : struct
    {
        if (values == null || values.Length == 0)
            return Array.Empty<ushort>();

        var type = typeof(T);
        var elementSize = GetElementSize<T>();
        int registerCount = values.Length * elementSize / 2;
        var registers = new ushort[registerCount];

        if (type == typeof(ushort))
            ConvertFromUInt16Array(MemoryMarshal.Cast<T, ushort>(values), byteOrder, registers);
        else if (type == typeof(short))
            ConvertFromInt16Array(MemoryMarshal.Cast<T, short>(values), byteOrder, registers);
        else if (type == typeof(uint))
            ConvertFromUInt32Array(MemoryMarshal.Cast<T, uint>(values), byteOrder, registers);
        else if (type == typeof(int))
            ConvertFromInt32Array(MemoryMarshal.Cast<T, int>(values), byteOrder, registers);
        else if (type == typeof(float))
            ConvertFromFloatArray(MemoryMarshal.Cast<T, float>(values), byteOrder, registers);
        else if (type == typeof(double))
            ConvertFromDoubleArray(MemoryMarshal.Cast<T, double>(values), byteOrder, registers);
        else
            throw new NotSupportedException($"Type {type.Name} is not supported");

        return registers;
    }

    /// <summary>
    /// 将单个值转换为 Modbus 寄存器数组
    /// </summary>
    public static ushort[] ToRegisters<T>(T value, ByteOrder byteOrder) where T : struct
    {
        return ToRegisters(new[] { value }, byteOrder);
    }

    #region Private Helper Methods

    private static int GetElementSize<T>() where T : struct
    {
        var type = typeof(T);
        if (type == typeof(ushort) || type == typeof(short))
            return 2;
        if (type == typeof(uint) || type == typeof(int) || type == typeof(float))
            return 4;
        if (type == typeof(double))
            return 8;
        throw new NotSupportedException($"Type {type.Name} is not supported");
    }

    #region UInt16 Conversion

    private static void ConvertToUInt16Array(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            var bytes = source.Slice(i * 2, 2);
            destination[i] = byteOrder switch
            {
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt16BigEndian(bytes),
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt16LittleEndian(bytes),
                ByteOrder.BigEndianSwap => BinaryPrimitives.ReadUInt16LittleEndian(bytes),
                ByteOrder.LittleEndianSwap => BinaryPrimitives.ReadUInt16BigEndian(bytes),
                _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
            };
        }
    }

    private static void ConvertFromUInt16Array(ReadOnlySpan<ushort> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        if (byteOrder == ByteOrder.BigEndian)
        {
            source.CopyTo(destination);
        }
        else
        {
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = byteOrder switch
                {
                    ByteOrder.LittleEndian => ReverseBytes(source[i]),
                    ByteOrder.BigEndianSwap => ReverseBytes(source[i]),
                    ByteOrder.LittleEndianSwap => source[i],
                    _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
                };
            }
        }
    }

    #endregion

    #region Int16 Conversion

    private static void ConvertToInt16Array(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<short> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            var bytes = source.Slice(i * 2, 2);
            destination[i] = byteOrder switch
            {
                ByteOrder.BigEndian => BinaryPrimitives.ReadInt16BigEndian(bytes),
                ByteOrder.LittleEndian => BinaryPrimitives.ReadInt16LittleEndian(bytes),
                ByteOrder.BigEndianSwap => BinaryPrimitives.ReadInt16LittleEndian(bytes),
                ByteOrder.LittleEndianSwap => BinaryPrimitives.ReadInt16BigEndian(bytes),
                _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
            };
        }
    }

    private static void ConvertFromInt16Array(ReadOnlySpan<short> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        var asUshort = MemoryMarshal.Cast<short, ushort>(source);
        ConvertFromUInt16Array(asUshort, byteOrder, destination);
    }

    #endregion

    #region UInt32 Conversion

    private static void ConvertToUInt32Array(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<uint> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            var bytes = source.Slice(i * 4, 4);
            destination[i] = byteOrder switch
            {
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt32BigEndian(bytes),
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt32LittleEndian(bytes),
                ByteOrder.BigEndianSwap => SwapWords(BinaryPrimitives.ReadUInt32BigEndian(bytes)),
                ByteOrder.LittleEndianSwap => SwapWords(BinaryPrimitives.ReadUInt32LittleEndian(bytes)),
                _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
            };
        }
    }

    private static void ConvertFromUInt32Array(ReadOnlySpan<uint> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        Span<byte> buffer = stackalloc byte[4];
        for (int i = 0; i < source.Length; i++)
        {
            uint value = source[i];
            
            if (byteOrder == ByteOrder.BigEndianSwap || byteOrder == ByteOrder.LittleEndianSwap)
                value = SwapWords(value);

            if (byteOrder == ByteOrder.BigEndian || byteOrder == ByteOrder.BigEndianSwap)
                BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);

            destination[i * 2] = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2));
            destination[i * 2 + 1] = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2, 2));
        }
    }

    #endregion

    #region Int32 Conversion

    private static void ConvertToInt32Array(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<int> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            var bytes = source.Slice(i * 4, 4);
            destination[i] = byteOrder switch
            {
                ByteOrder.BigEndian => BinaryPrimitives.ReadInt32BigEndian(bytes),
                ByteOrder.LittleEndian => BinaryPrimitives.ReadInt32LittleEndian(bytes),
                ByteOrder.BigEndianSwap => (int)SwapWords(BinaryPrimitives.ReadUInt32BigEndian(bytes)),
                ByteOrder.LittleEndianSwap => (int)SwapWords(BinaryPrimitives.ReadUInt32LittleEndian(bytes)),
                _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
            };
        }
    }

    private static void ConvertFromInt32Array(ReadOnlySpan<int> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        var asUint = MemoryMarshal.Cast<int, uint>(source);
        ConvertFromUInt32Array(asUint, byteOrder, destination);
    }

    #endregion

    #region Float Conversion

    private static void ConvertToFloatArray(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<float> destination)
    {
        Span<uint> intValues = stackalloc uint[destination.Length];
        ConvertToUInt32Array(source, byteOrder, intValues);
        
        var floatBytes = MemoryMarshal.Cast<uint, float>(intValues);
        floatBytes.CopyTo(destination);
    }

    private static void ConvertFromFloatArray(ReadOnlySpan<float> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        var asUint = MemoryMarshal.Cast<float, uint>(source);
        ConvertFromUInt32Array(asUint, byteOrder, destination);
    }

    #endregion

    #region Double Conversion

    private static void ConvertToDoubleArray(ReadOnlySpan<byte> source, ByteOrder byteOrder, Span<double> destination)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            var bytes = source.Slice(i * 8, 8);
            ulong longValue = byteOrder switch
            {
                ByteOrder.BigEndian => BinaryPrimitives.ReadUInt64BigEndian(bytes),
                ByteOrder.LittleEndian => BinaryPrimitives.ReadUInt64LittleEndian(bytes),
                ByteOrder.BigEndianSwap => SwapWords(BinaryPrimitives.ReadUInt64BigEndian(bytes)),
                ByteOrder.LittleEndianSwap => SwapWords(BinaryPrimitives.ReadUInt64LittleEndian(bytes)),
                _ => throw new ArgumentException($"Unsupported byte order: {byteOrder}")
            };
            
            destination[i] = BitConverter.Int64BitsToDouble((long)longValue);
        }
    }

    private static void ConvertFromDoubleArray(ReadOnlySpan<double> source, ByteOrder byteOrder, Span<ushort> destination)
    {
        Span<byte> buffer = stackalloc byte[8];
        for (int i = 0; i < source.Length; i++)
        {
            ulong longValue = (ulong)BitConverter.DoubleToInt64Bits(source[i]);
            
            if (byteOrder == ByteOrder.BigEndianSwap || byteOrder == ByteOrder.LittleEndianSwap)
                longValue = SwapWords(longValue);

            if (byteOrder == ByteOrder.BigEndian || byteOrder == ByteOrder.BigEndianSwap)
                BinaryPrimitives.WriteUInt64BigEndian(buffer, longValue);
            else
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, longValue);

            for (int j = 0; j < 4; j++)
            {
                destination[i * 4 + j] = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(j * 2, 2));
            }
        }
    }

    #endregion

    #region Byte Manipulation Helpers

    private static ushort ReverseBytes(ushort value)
    {
        return (ushort)((value >> 8) | (value << 8));
    }

    private static uint SwapWords(uint value)
    {
        return ((value & 0xFFFF0000) >> 16) | ((value & 0x0000FFFF) << 16);
    }

    private static ulong SwapWords(ulong value)
    {
        return ((value & 0xFFFF000000000000) >> 48) |
               ((value & 0x0000FFFF00000000) >> 16) |
               ((value & 0x00000000FFFF0000) << 16) |
               ((value & 0x000000000000FFFF) << 48);
    }

    #endregion

    #endregion
}
