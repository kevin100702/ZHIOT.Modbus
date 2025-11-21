namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus ASCII ADU 构建器
/// 负责将 SlaveId + PDU 封装为完整的 ASCII ADU
/// 格式: ':' + SlaveId(2) + FunctionCode(2) + Data(N×2) + LRC(2) + '\r\n'
/// 其中所有十六进制字段都是 ASCII 编码
/// </summary>
public static class ModbusAsciiAduBuilder
{
    /// <summary>
    /// 构建完整的 ASCII ADU
    /// </summary>
    /// <param name="buffer">目标缓冲区（应足够大以容纳完整 ADU）</param>
    /// <param name="slaveId">从站 ID</param>
    /// <param name="pdu">协议数据单元</param>
    /// <returns>写入的总字节数</returns>
    public static int BuildAdu(Span<byte> buffer, byte slaveId, ReadOnlySpan<byte> pdu)
    {
        // 计算所需的缓冲区大小:
        // ':' (1) + SlaveId(2) + PDU(N×2) + LRC(2) + '\r\n' (2)
        int requiredSize = 1 + 2 + pdu.Length * 2 + 2 + 2;
        if (buffer.Length < requiredSize)
            throw new ArgumentException("Buffer is too small", nameof(buffer));

        int offset = 0;

        // 1. 写入帧头 ':'
        buffer[offset++] = (byte)':';

        // 2. 编码 SlaveId
        Span<byte> slaveIdHex = stackalloc byte[2];
        ModbusAsciiCodec.Encode(new[] { slaveId }, slaveIdHex);
        slaveIdHex.CopyTo(buffer.Slice(offset));
        offset += 2;

        // 3. 编码 PDU (SlaveId + FunctionCode + Data)
        Span<byte> pduHex = stackalloc byte[pdu.Length * 2];
        ModbusAsciiCodec.Encode(pdu, pduHex);
        pduHex.CopyTo(buffer.Slice(offset));
        offset += pdu.Length * 2;

        // 4. 计算 LRC (包括 SlaveId 和 PDU)
        Span<byte> lrcData = stackalloc byte[1 + pdu.Length];
        lrcData[0] = slaveId;
        pdu.CopyTo(lrcData.Slice(1));
        byte lrc = ModbusLrc.Calculate(lrcData);

        // 5. 编码 LRC
        Span<byte> lrcHex = stackalloc byte[2];
        ModbusAsciiCodec.Encode(new[] { lrc }, lrcHex);
        lrcHex.CopyTo(buffer.Slice(offset));
        offset += 2;

        // 6. 写入帧尾 '\r\n'
        buffer[offset++] = (byte)'\r';
        buffer[offset++] = (byte)'\n';

        return offset;
    }
}
