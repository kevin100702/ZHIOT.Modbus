namespace ZHIOT.Modbus.Core;

/// <summary>
/// 字节序枚举，定义不同的字节序模式
/// </summary>
public enum ByteOrder
{
    /// <summary>
    /// 大端字节序 (ABCD) - Modbus 标准
    /// 高位字节在前，低位字节在后
    /// 例如: 0x12345678 存储为 [0x12, 0x34, 0x56, 0x78]
    /// </summary>
    BigEndian,

    /// <summary>
    /// 小端字节序 (DCBA)
    /// 低位字节在前，高位字节在后
    /// 例如: 0x12345678 存储为 [0x78, 0x56, 0x34, 0x12]
    /// </summary>
    LittleEndian,

    /// <summary>
    /// 大端字节序 + 字交换 (BADC)
    /// 在每个 16 位字内交换字节，但字本身保持大端序
    /// 例如: 0x12345678 存储为 [0x34, 0x12, 0x78, 0x56]
    /// </summary>
    BigEndianSwap,

    /// <summary>
    /// 小端字节序 + 字交换 (CDAB)
    /// 在每个 16 位字内交换字节，但字本身保持小端序
    /// 例如: 0x12345678 存储为 [0x56, 0x78, 0x12, 0x34]
    /// </summary>
    LittleEndianSwap
}
