namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus ASCII ADU 解析器
/// 负责从完整的 ASCII ADU 中提取和验证数据
/// 格式: ':' + SlaveId(2) + FunctionCode(2) + Data(N×2) + LRC(2) + '\r\n'
/// </summary>
public static class ModbusAsciiAduParser
{
    /// <summary>
    /// 从完整的 ASCII ADU 中提取 PDU
    /// </summary>
    /// <param name="adu">完整的 ASCII ADU</param>
    /// <returns>PDU 部分的数据</returns>
    /// <exception cref="InvalidOperationException">当 ADU 格式错误或 LRC 校验失败时抛出</exception>
    public static byte[] ExtractPdu(ReadOnlySpan<byte> adu)
    {
        // 验证帧头和帧尾
        if (adu.Length < AsciiAdu.MinSize)
            throw new InvalidOperationException($"ADU too short: {adu.Length} bytes (minimum {AsciiAdu.MinSize})");

        if (adu[0] != (byte)':')
            throw new InvalidOperationException("Invalid frame header: expected ':'");

        if (adu[adu.Length - 2] != (byte)'\r' || adu[adu.Length - 1] != (byte)'\n')
            throw new InvalidOperationException("Invalid frame trailer: expected '\\r\\n'");

        // 移除帧头帧尾，保留 ASCII 数据: SlaveId(2) + PDU(N×2) + LRC(2)
        var asciiData = adu.Slice(1, adu.Length - 3);

        // 需要至少 2(SlaveId) + 2(FunctionCode) + 2(LRC) = 6 字符
        if (asciiData.Length < 6 || asciiData.Length % 2 != 0)
            throw new InvalidOperationException("Invalid ASCII data length");

        // 解码 ASCII 数据
        Span<byte> binaryData = stackalloc byte[asciiData.Length / 2];
        ModbusAsciiCodec.Decode(asciiData, binaryData);

        // 验证 LRC
        // 格式: SlaveId(1) + PDU(N) + LRC(1)
        if (!ModbusLrc.Verify(binaryData))
            throw new InvalidOperationException("LRC verification failed");

        // 返回 PDU 部分（从第 1 字节开始，到倒数第 2 字节结束，跳过最后的 LRC）
        return binaryData.Slice(1, binaryData.Length - 2).ToArray();
    }

    /// <summary>
    /// 从完整的 ASCII ADU 中提取 SlaveId
    /// </summary>
    /// <param name="adu">完整的 ASCII ADU</param>
    /// <returns>从站 ID</returns>
    public static byte ExtractSlaveId(ReadOnlySpan<byte> adu)
    {
        if (adu.Length < 3)
            throw new InvalidOperationException("ADU is too short");

        if (adu[0] != (byte)':')
            throw new InvalidOperationException("Invalid frame header");

        // 解码 SlaveId (第 1-2 个字符)
        Span<byte> slaveIdBinary = stackalloc byte[1];
        ModbusAsciiCodec.Decode(adu.Slice(1, 2), slaveIdBinary);

        return slaveIdBinary[0];
    }

    /// <summary>
    /// 验证 ADU 的 LRC
    /// </summary>
    /// <param name="adu">完整的 ASCII ADU</param>
    /// <returns>如果 LRC 校验通过返回 true</returns>
    public static bool VerifyLrc(ReadOnlySpan<byte> adu)
    {
        try
        {
            // 验证帧头帧尾
            if (adu.Length < AsciiAdu.MinSize)
                return false;

            if (adu[0] != (byte)':')
                return false;

            if (adu[adu.Length - 2] != (byte)'\r' || adu[adu.Length - 1] != (byte)'\n')
                return false;

            // 移除帧头帧尾
            var asciiData = adu.Slice(1, adu.Length - 3);

            if (asciiData.Length < 6 || asciiData.Length % 2 != 0)
                return false;

            // 解码并验证
            Span<byte> binaryData = stackalloc byte[asciiData.Length / 2];
            ModbusAsciiCodec.Decode(asciiData, binaryData);

            return ModbusLrc.Verify(binaryData);
        }
        catch
        {
            return false;
        }
    }
}
