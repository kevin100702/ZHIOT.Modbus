using System.Buffers.Binary;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus RTU ADU 构建器
/// 负责将 SlaveId + PDU 封装为完整的 RTU ADU (包含 CRC)
/// </summary>
public static class ModbusRtuAduBuilder
{
    /// <summary>
    /// 构建完整的 RTU ADU (SlaveId + PDU + CRC)
    /// </summary>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="slaveId">从站 ID</param>
    /// <param name="pdu">协议数据单元</param>
    /// <returns>写入的总字节数</returns>
    public static int BuildAdu(Span<byte> buffer, byte slaveId, ReadOnlySpan<byte> pdu)
    {
        if (buffer.Length < 1 + pdu.Length + 2)
            throw new ArgumentException("Buffer is too small", nameof(buffer));

        // 写入 SlaveId
        buffer[0] = slaveId;

        // 写入 PDU
        pdu.CopyTo(buffer.Slice(1));

        int frameLength = 1 + pdu.Length;

        // 计算并添加 CRC (小端序)
        var crc = ModbusCrc16.Calculate(buffer.Slice(0, frameLength));
        BinaryPrimitives.WriteUInt16LittleEndian(buffer.Slice(frameLength), crc);

        return frameLength + 2; // +2 for CRC
    }
}
