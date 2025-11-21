using System.IO.Ports;

namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus ASCII ADU 结构定义
/// 格式: ':' + SlaveId(2) + FunctionCode(2) + Data(N×2) + LRC(2) + '\r\n'
/// 其中 SlaveId, FunctionCode, Data, LRC 都是 ASCII 十六进制表示
/// </summary>
public readonly struct AsciiAdu
{
    /// <summary>
    /// 从站 ID
    /// </summary>
    public byte SlaveId { get; init; }

    /// <summary>
    /// PDU 数据
    /// </summary>
    public ReadOnlyMemory<byte> Pdu { get; init; }

    /// <summary>
    /// LRC 校验码
    /// </summary>
    public byte Lrc { get; init; }

    /// <summary>
    /// ASCII ADU 最小长度: ':' + SlaveId(2) + FunctionCode(2) + LRC(2) + '\r\n'
    /// = 1 + 2 + 2 + 2 + 2 = 9 字符
    /// </summary>
    public const int MinSize = 9;
}

/// <summary>
/// Modbus ASCII 线路设置
/// ASCII 通常通过串口或网络连接使用，使用文本行协议
/// </summary>
public class AsciiLineSettings
{
    /// <summary>
    /// 端口名称（串口如 "COM1"，或网络地址）
    /// </summary>
    public string PortName { get; set; } = "COM1";

    /// <summary>
    /// 波特率（仅当使用串口时有效）
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 奇偶校验（仅当使用串口时有效）
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位（仅当使用串口时有效）
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位（仅当使用串口时有效）
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 读取超时（毫秒）
    /// </summary>
    public int ReadTimeout { get; set; } = 1000;

    /// <summary>
    /// 写入超时（毫秒）
    /// </summary>
    public int WriteTimeout { get; set; } = 1000;
}
