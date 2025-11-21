namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus RTU ADU 解析器
/// 负责从完整的 RTU ADU 中提取和验证数据
/// </summary>
public static class ModbusRtuAduParser
{
    /// <summary>
    /// 从完整的 RTU ADU 中提取 PDU
    /// </summary>
    /// <param name="adu">完整的 RTU ADU (SlaveId + PDU + CRC)</param>
    /// <returns>PDU 部分的数据</returns>
    /// <exception cref="InvalidOperationException">当 ADU 长度不足或 CRC 校验失败时抛出</exception>
    public static ReadOnlySpan<byte> ExtractPdu(ReadOnlySpan<byte> adu)
    {
        return ExtractPdu(adu, Crc16Variant.Even);
    }

    /// <summary>
    /// 从完整的 RTU ADU 中提取 PDU
    /// </summary>
    /// <param name="adu">完整的 RTU ADU (SlaveId + PDU + CRC)</param>
    /// <param name="crc16Variant">CRC-16 变体（偶校验或奇校验）</param>
    /// <returns>PDU 部分的数据</returns>
    /// <exception cref="InvalidOperationException">当 ADU 长度不足或 CRC 校验失败时抛出</exception>
    public static ReadOnlySpan<byte> ExtractPdu(ReadOnlySpan<byte> adu, Crc16Variant crc16Variant)
    {
        // 验证最小长度
        if (adu.Length < RtuAdu.MinSize)
            throw new InvalidOperationException($"ADU too short: {adu.Length} bytes (minimum {RtuAdu.MinSize})");

        // 验证 CRC
        if (!ModbusCrc16.Verify(adu, crc16Variant))
            throw new InvalidOperationException("CRC verification failed");

        // 返回 PDU 部分（跳过 SlaveId [1 字节]，去除 CRC [2 字节]）
        return adu.Slice(1, adu.Length - 3);
    }

    /// <summary>
    /// 从完整的 RTU ADU 中提取 SlaveId
    /// </summary>
    /// <param name="adu">完整的 RTU ADU</param>
    /// <returns>从站 ID</returns>
    public static byte ExtractSlaveId(ReadOnlySpan<byte> adu)
    {
        if (adu.Length < 1)
            throw new InvalidOperationException("ADU is empty");

        return adu[0];
    }

    /// <summary>
    /// 验证 ADU 的 CRC 但不提取数据
    /// </summary>
    /// <param name="adu">完整的 RTU ADU</param>
    /// <returns>如果 CRC 校验通过返回 true</returns>
    public static bool VerifyCrc(ReadOnlySpan<byte> adu)
    {
        return VerifyCrc(adu, Crc16Variant.Even);
    }

    /// <summary>
    /// 验证 ADU 的 CRC 但不提取数据
    /// </summary>
    /// <param name="adu">完整的 RTU ADU</param>
    /// <param name="crc16Variant">CRC-16 变体（偶校验或奇校验）</param>
    /// <returns>如果 CRC 校验通过返回 true</returns>
    public static bool VerifyCrc(ReadOnlySpan<byte> adu, Crc16Variant crc16Variant)
    {
        if (adu.Length < RtuAdu.MinSize)
            return false;

        return ModbusCrc16.Verify(adu, crc16Variant);
    }
}
