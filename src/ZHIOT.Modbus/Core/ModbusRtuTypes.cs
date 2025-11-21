using System.IO.Ports;

namespace ZHIOT.Modbus.Core;

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
}
