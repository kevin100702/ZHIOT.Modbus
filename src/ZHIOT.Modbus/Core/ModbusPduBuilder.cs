using System.Buffers.Binary;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus PDU 构建器，使用 Span 实现零分配
/// </summary>
public static class ModbusPduBuilder
{
    /// <summary>
    /// 构建读线圈请求 (功能码 0x01)
    /// </summary>
    public static int BuildReadCoilsRequest(Span<byte> buffer, ushort startAddress, ushort quantity)
    {
        buffer[0] = (byte)ModbusFunctionCode.ReadCoils;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), quantity);
        return 5;
    }

    /// <summary>
    /// 构建读离散输入请求 (功能码 0x02)
    /// </summary>
    public static int BuildReadDiscreteInputsRequest(Span<byte> buffer, ushort startAddress, ushort quantity)
    {
        buffer[0] = (byte)ModbusFunctionCode.ReadDiscreteInputs;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), quantity);
        return 5;
    }

    /// <summary>
    /// 构建读保持寄存器请求 (功能码 0x03)
    /// </summary>
    public static int BuildReadHoldingRegistersRequest(Span<byte> buffer, ushort startAddress, ushort quantity)
    {
        buffer[0] = (byte)ModbusFunctionCode.ReadHoldingRegisters;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), quantity);
        return 5;
    }

    /// <summary>
    /// 构建读输入寄存器请求 (功能码 0x04)
    /// </summary>
    public static int BuildReadInputRegistersRequest(Span<byte> buffer, ushort startAddress, ushort quantity)
    {
        buffer[0] = (byte)ModbusFunctionCode.ReadInputRegisters;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), quantity);
        return 5;
    }

    /// <summary>
    /// 构建写单个线圈请求 (功能码 0x05)
    /// </summary>
    public static int BuildWriteSingleCoilRequest(Span<byte> buffer, ushort address, bool value)
    {
        buffer[0] = (byte)ModbusFunctionCode.WriteSingleCoil;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), value ? (ushort)0xFF00 : (ushort)0x0000);
        return 5;
    }

    /// <summary>
    /// 构建写单个寄存器请求 (功能码 0x06)
    /// </summary>
    public static int BuildWriteSingleRegisterRequest(Span<byte> buffer, ushort address, ushort value)
    {
        buffer[0] = (byte)ModbusFunctionCode.WriteSingleRegister;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), address);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), value);
        return 5;
    }

    /// <summary>
    /// 构建写多个线圈请求 (功能码 0x0F)
    /// </summary>
    public static int BuildWriteMultipleCoilsRequest(Span<byte> buffer, ushort startAddress, bool[] values)
    {
        buffer[0] = (byte)ModbusFunctionCode.WriteMultipleCoils;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), (ushort)values.Length);

        int byteCount = (values.Length + 7) / 8;
        buffer[5] = (byte)byteCount;

        // 将布尔数组转换为字节
        for (int i = 0; i < byteCount; i++)
        {
            byte b = 0;
            for (int bit = 0; bit < 8 && (i * 8 + bit) < values.Length; bit++)
            {
                if (values[i * 8 + bit])
                {
                    b |= (byte)(1 << bit);
                }
            }
            buffer[6 + i] = b;
        }

        return 6 + byteCount;
    }

    /// <summary>
    /// 构建写多个寄存器请求 (功能码 0x10)
    /// </summary>
    public static int BuildWriteMultipleRegistersRequest(Span<byte> buffer, ushort startAddress, ushort[] values)
    {
        buffer[0] = (byte)ModbusFunctionCode.WriteMultipleRegisters;
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(1, 2), startAddress);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(3, 2), (ushort)values.Length);

        int byteCount = values.Length * 2;
        buffer[5] = (byte)byteCount;

        for (int i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(6 + i * 2, 2), values[i]);
        }

        return 6 + byteCount;
    }

    /// <summary>
    /// 构建 MBAP 头
    /// </summary>
    public static void BuildMbapHeader(Span<byte> buffer, ushort transactionId, byte unitId, ushort pduLength)
    {
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(0, 2), transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(2, 2), 0); // Protocol ID (0 for Modbus)
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(4, 2), (ushort)(pduLength + 1)); // Length = PDU + UnitId
        buffer[6] = unitId;
    }
}
