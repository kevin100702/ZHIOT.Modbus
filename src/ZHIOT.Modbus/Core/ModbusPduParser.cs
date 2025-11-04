using System.Buffers.Binary;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus PDU 解析器，使用 ReadOnlySpan 实现零分配
/// </summary>
public static class ModbusPduParser
{
    /// <summary>
    /// 解析 MBAP 头
    /// </summary>
    public static MbapHeader ParseMbapHeader(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length < MbapHeader.Size)
            throw new ArgumentException("Buffer too small for MBAP header", nameof(buffer));

        return new MbapHeader
        {
            TransactionId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(0, 2)),
            ProtocolId = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(2, 2)),
            Length = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(4, 2)),
            UnitId = buffer[6]
        };
    }

    /// <summary>
    /// 检查是否为异常响应
    /// </summary>
    public static void CheckForException(ReadOnlySpan<byte> pdu)
    {
        if (pdu.Length < 2)
            throw new InvalidOperationException("PDU too short");

        byte functionCode = pdu[0];
        if ((functionCode & 0x80) != 0)
        {
            byte exceptionCode = pdu[1];
            throw new ModbusException((ModbusExceptionCode)exceptionCode);
        }
    }

    /// <summary>
    /// 解析读线圈响应 (功能码 0x01)
    /// </summary>
    public static bool[] ParseReadCoilsResponse(ReadOnlySpan<byte> pdu, ushort requestedQuantity)
    {
        CheckForException(pdu);

        if (pdu.Length < 2)
            throw new InvalidOperationException("PDU too short");

        byte byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
            throw new InvalidOperationException("PDU data incomplete");

        var result = new bool[requestedQuantity];
        var data = pdu.Slice(2, byteCount);

        for (int i = 0; i < requestedQuantity; i++)
        {
            int byteIndex = i / 8;
            int bitIndex = i % 8;
            result[i] = (data[byteIndex] & (1 << bitIndex)) != 0;
        }

        return result;
    }

    /// <summary>
    /// 解析读离散输入响应 (功能码 0x02)
    /// </summary>
    public static bool[] ParseReadDiscreteInputsResponse(ReadOnlySpan<byte> pdu, ushort requestedQuantity)
    {
        return ParseReadCoilsResponse(pdu, requestedQuantity);
    }

    /// <summary>
    /// 解析读保持寄存器响应 (功能码 0x03)
    /// </summary>
    public static ushort[] ParseReadHoldingRegistersResponse(ReadOnlySpan<byte> pdu)
    {
        CheckForException(pdu);

        if (pdu.Length < 2)
            throw new InvalidOperationException("PDU too short");

        byte byteCount = pdu[1];
        if (pdu.Length < 2 + byteCount)
            throw new InvalidOperationException("PDU data incomplete");

        int registerCount = byteCount / 2;
        var result = new ushort[registerCount];
        var data = pdu.Slice(2, byteCount);

        for (int i = 0; i < registerCount; i++)
        {
            result[i] = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(i * 2, 2));
        }

        return result;
    }

    /// <summary>
    /// 解析读输入寄存器响应 (功能码 0x04)
    /// </summary>
    public static ushort[] ParseReadInputRegistersResponse(ReadOnlySpan<byte> pdu)
    {
        return ParseReadHoldingRegistersResponse(pdu);
    }

    /// <summary>
    /// 解析写单个线圈响应 (功能码 0x05)
    /// </summary>
    public static (ushort address, bool value) ParseWriteSingleCoilResponse(ReadOnlySpan<byte> pdu)
    {
        CheckForException(pdu);

        if (pdu.Length < 5)
            throw new InvalidOperationException("PDU too short");

        ushort address = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(1, 2));
        ushort valueWord = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(3, 2));
        bool value = valueWord == 0xFF00;

        return (address, value);
    }

    /// <summary>
    /// 解析写单个寄存器响应 (功能码 0x06)
    /// </summary>
    public static (ushort address, ushort value) ParseWriteSingleRegisterResponse(ReadOnlySpan<byte> pdu)
    {
        CheckForException(pdu);

        if (pdu.Length < 5)
            throw new InvalidOperationException("PDU too short");

        ushort address = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(1, 2));
        ushort value = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(3, 2));

        return (address, value);
    }

    /// <summary>
    /// 解析写多个线圈响应 (功能码 0x0F)
    /// </summary>
    public static (ushort startAddress, ushort quantity) ParseWriteMultipleCoilsResponse(ReadOnlySpan<byte> pdu)
    {
        CheckForException(pdu);

        if (pdu.Length < 5)
            throw new InvalidOperationException("PDU too short");

        ushort startAddress = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(1, 2));
        ushort quantity = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(3, 2));

        return (startAddress, quantity);
    }

    /// <summary>
    /// 解析写多个寄存器响应 (功能码 0x10)
    /// </summary>
    public static (ushort startAddress, ushort quantity) ParseWriteMultipleRegistersResponse(ReadOnlySpan<byte> pdu)
    {
        CheckForException(pdu);

        if (pdu.Length < 5)
            throw new InvalidOperationException("PDU too short");

        ushort startAddress = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(1, 2));
        ushort quantity = BinaryPrimitives.ReadUInt16BigEndian(pdu.Slice(3, 2));

        return (startAddress, quantity);
    }
}
