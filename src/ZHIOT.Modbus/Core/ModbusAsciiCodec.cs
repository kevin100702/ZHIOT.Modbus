namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus ASCII 编解码工具
/// 负责二进制数据与 ASCII 十六进制的相互转换
/// 
/// 二进制格式: 0xAB -> 字节 171
/// ASCII 格式:  "AB" -> 字符 'A'(65) 和 'B'(66)
/// </summary>
public static class ModbusAsciiCodec
{
    /// <summary>
    /// 十六进制字符集
    /// </summary>
    private static readonly string HexChars = "0123456789ABCDEF";

    /// <summary>
    /// 将二进制数据编码为 ASCII 十六进制字符
    /// 例如: 0xAB -> "AB"
    /// </summary>
    /// <param name="data">二进制数据</param>
    /// <param name="ascii">输出 ASCII 十六进制字符缓冲区（长度应为 data.Length * 2）</param>
    /// <returns>写入的字符数</returns>
    public static int Encode(ReadOnlySpan<byte> data, Span<byte> ascii)
    {
        if (ascii.Length < data.Length * 2)
            throw new ArgumentException("ASCII buffer is too small", nameof(ascii));

        int asciiIndex = 0;
        foreach (byte b in data)
        {
            // 高 4 位
            ascii[asciiIndex++] = (byte)HexChars[b >> 4];
            // 低 4 位
            ascii[asciiIndex++] = (byte)HexChars[b & 0x0F];
        }

        return data.Length * 2;
    }

    /// <summary>
    /// 将 ASCII 十六进制字符解码为二进制数据
    /// 例如: "AB" -> 0xAB
    /// </summary>
    /// <param name="ascii">ASCII 十六进制字符</param>
    /// <param name="data">输出二进制数据缓冲区（长度应为 ascii.Length / 2）</param>
    /// <returns>写入的字节数</returns>
    public static int Decode(ReadOnlySpan<byte> ascii, Span<byte> data)
    {
        if (ascii.Length % 2 != 0)
            throw new ArgumentException("ASCII data length must be even", nameof(ascii));

        if (data.Length < ascii.Length / 2)
            throw new ArgumentException("Data buffer is too small", nameof(data));

        int dataIndex = 0;
        for (int i = 0; i < ascii.Length; i += 2)
        {
            byte high = HexCharToNibble(ascii[i]);
            byte low = HexCharToNibble(ascii[i + 1]);

            if (high == 0xFF || low == 0xFF)
                throw new FormatException($"Invalid hex character at position {i}");

            data[dataIndex++] = (byte)((high << 4) | low);
        }

        return ascii.Length / 2;
    }

    /// <summary>
    /// 将单个十六进制字符转换为 4 位值
    /// 返回 0xFF 表示无效字符
    /// </summary>
    private static byte HexCharToNibble(byte ascii)
    {
        if (ascii >= (byte)'0' && ascii <= (byte)'9')
            return (byte)(ascii - (byte)'0');
        if (ascii >= (byte)'A' && ascii <= (byte)'F')
            return (byte)(ascii - (byte)'A' + 10);
        if (ascii >= (byte)'a' && ascii <= (byte)'f')
            return (byte)(ascii - (byte)'a' + 10);

        return 0xFF; // 无效字符
    }

    /// <summary>
    /// 将二进制数据编码为 ASCII 十六进制字符串
    /// </summary>
    public static string EncodeToString(ReadOnlySpan<byte> data)
    {
        Span<byte> ascii = stackalloc byte[data.Length * 2];
        Encode(data, ascii);
        return System.Text.Encoding.ASCII.GetString(ascii);
    }

    /// <summary>
    /// 将 ASCII 十六进制字符串解码为二进制数据
    /// </summary>
    public static byte[] DecodeFromString(string ascii)
    {
        var asciiBytes = System.Text.Encoding.ASCII.GetBytes(ascii);
        var data = new byte[asciiBytes.Length / 2];
        Decode(asciiBytes, data);
        return data;
    }
}
