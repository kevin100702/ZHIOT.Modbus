using System.IO.Ports;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// CRC-16 校验变体
/// Modbus RTU 有两种 CRC-16 实际变体，区别在于最终异或值（XorOut）
/// </summary>
public enum Crc16Variant
{
    /// <summary>
    /// 偶校验（Even）- XorOut = 0x0000
    /// Modbus RTU 官方标准规范使用此变体
    /// </summary>
    Even = 0,

    /// <summary>
    /// 奇校验（Odd）- XorOut = 0xFFFF
    /// 大量现实设备使用此变体，当标准偶校验失败时可尝试切换
    /// </summary>
    Odd = 1
}

/// <summary>
/// Modbus RTU 应用数据单元 (ADU)
/// </summary>
public readonly struct RtuAdu
{
    /// <summary>
    /// 从站 ID (1 字节)
    /// </summary>
    public byte SlaveId { get; init; }

    /// <summary>
    /// 协议数据单元 (PDU)
    /// </summary>
    public ReadOnlyMemory<byte> Pdu { get; init; }

    /// <summary>
    /// CRC-16 校验码 (2 字节)
    /// </summary>
    public ushort Crc { get; init; }

    /// <summary>
    /// RTU ADU 最小长度 (SlaveId + FunctionCode + CRC)
    /// </summary>
    public const int MinSize = 4;
}

/// <summary>
/// 串口配置参数
/// </summary>
public class SerialPortSettings
{
    /// <summary>
    /// 串口名称 (例如 "COM1", "/dev/ttyUSB0")
    /// </summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>
    /// 波特率
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 校验位
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 读取超时 (毫秒)
    /// </summary>
    public int ReadTimeout { get; set; } = 1000;

    /// <summary>
    /// 写入超时 (毫秒)
    /// </summary>
    public int WriteTimeout { get; set; } = 1000;

    /// <summary>
    /// 帧间延迟 (毫秒)，用于实现 RTU 的静默期
    /// </summary>
    public int InterFrameDelay { get; set; } = 10;

    /// <summary>
    /// CRC-16 校验变体，默认为偶校验（Modbus 标准）
    /// 当遇到 CRC 校验失败时，可尝试切换到奇校验
    /// </summary>
    public Crc16Variant Crc16Variant { get; set; } = Crc16Variant.Even;
}
