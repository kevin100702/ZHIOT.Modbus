using System.Buffers.Binary;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus RTU CRC-16 校验算法实现
/// 使用查表法实现高性能计算
/// 多项式: 0xA001 (反向)
/// 初始值: 0xFFFF
/// 最终异或: 0x0000
/// </summary>
public static class ModbusCrc16
{
    /// <summary>
    /// CRC-16 查找表
    /// </summary>
    private static readonly ushort[] CrcTable = GenerateCrcTable();

    /// <summary>
    /// 计算数据的 CRC-16 校验码
    /// </summary>
    /// <param name="data">要计算 CRC 的数据</param>
    /// <returns>CRC-16 校验码</returns>
    public static ushort Calculate(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;

        foreach (byte b in data)
        {
            byte tableIndex = (byte)(crc ^ b);
            crc = (ushort)((crc >> 8) ^ CrcTable[tableIndex]);
        }

        return crc;
    }

    /// <summary>
    /// 验证包含 CRC 的完整帧 (最后两字节为 CRC)
    /// </summary>
    /// <param name="frame">包含 CRC 的完整帧</param>
    /// <returns>如果 CRC 校验通过返回 true，否则返回 false</returns>
    public static bool Verify(ReadOnlySpan<byte> frame)
    {
        if (frame.Length < 3)
            return false;

        // 提取数据部分（不包含最后两字节的 CRC）
        var data = frame.Slice(0, frame.Length - 2);
        
        // 提取帧中的 CRC（小端序）
        ushort frameCrc = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(frame.Length - 2));
        
        // 计算数据的 CRC
        ushort calculatedCrc = Calculate(data);

        return frameCrc == calculatedCrc;
    }

    /// <summary>
    /// 生成 CRC-16/MODBUS 查找表
    /// </summary>
    private static ushort[] GenerateCrcTable()
    {
        const ushort polynomial = 0xA001; // Modbus 使用反向多项式
        var table = new ushort[256];

        for (int i = 0; i < 256; i++)
        {
            ushort crc = (ushort)i;
            
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 0x0001) != 0)
                {
                    crc = (ushort)((crc >> 1) ^ polynomial);
                }
                else
                {
                    crc >>= 1;
                }
            }

            table[i] = crc;
        }

        return table;
    }
}
