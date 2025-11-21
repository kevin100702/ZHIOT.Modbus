namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus ASCII LRC-8 校验算法实现
/// LRC (Longitudinal Redundancy Check) - 纵向冗余校验
/// 算法: LRC = (sum of bytes) XOR 0xFF + 1 = -sum mod 256
/// </summary>
public static class ModbusLrc
{
    /// <summary>
    /// 计算数据的 LRC-8 校验码
    /// </summary>
    /// <param name="data">要计算 LRC 的数据</param>
    /// <returns>LRC-8 校验码</returns>
    public static byte Calculate(ReadOnlySpan<byte> data)
    {
        // 求和所有字节
        int sum = 0;
        foreach (byte b in data)
        {
            sum += b;
        }

        // LRC = (-sum) mod 256 = (~sum + 1) mod 256
        return (byte)((-sum) & 0xFF);
    }

    /// <summary>
    /// 验证包含 LRC 的完整帧（最后一字节为 LRC）
    /// </summary>
    /// <param name="frame">包含 LRC 的完整帧</param>
    /// <returns>如果 LRC 校验通过返回 true</returns>
    public static bool Verify(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 2)
            return false;

        // 提取数据部分（不包含最后一字节的 LRC）
        var data = frame.Slice(0, frame.Length - 1);

        // 提取帧中的 LRC
        byte frameLrc = frame[frame.Length - 1];

        // 计算数据的 LRC
        byte calculatedLrc = Calculate(data);

        return frameLrc == calculatedLrc;
    }
}
