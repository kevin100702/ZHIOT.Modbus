using System.IO.Ports;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Client;
using ZHIOT.Modbus.Core;
using ZHIOT.Modbus.Transport;

namespace ZHIOT.Modbus;

/// <summary>
/// Modbus 客户端工厂，提供便捷的创建方法
/// </summary>
public static class ModbusClientFactory
{
    /// <summary>
    /// 创建 Modbus TCP 客户端
    /// </summary>
    /// <param name="host">服务器地址</param>
    /// <param name="port">端口号，默认 502</param>
    /// <returns>Modbus TCP 客户端实例</returns>
    public static IModbusClient CreateTcpClient(string host, int port = 502)
    {
        var transport = new TcpTransport(host, port);
        return new ModbusTcpClient(transport);
    }

    /// <summary>
    /// 创建 Modbus RTU 客户端（使用串口配置对象）
    /// </summary>
    /// <param name="settings">串口配置参数</param>
    /// <returns>Modbus RTU 客户端实例</returns>
    public static IModbusClient CreateRtuClient(SerialPortSettings settings)
    {
        var transport = new SerialPortTransport(settings);
        var client = new ModbusRtuClient(transport);
        client.Crc16Variant = settings.Crc16Variant;
        return client;
    }

    /// <summary>
    /// 创建 Modbus RTU 客户端（便捷重载）
    /// </summary>
    /// <param name="portName">串口名称（例如 "COM1", "/dev/ttyUSB0"）</param>
    /// <param name="baudRate">波特率，默认 9600</param>
    /// <param name="parity">校验位，默认 None</param>
    /// <param name="dataBits">数据位，默认 8</param>
    /// <param name="stopBits">停止位，默认 One</param>
    /// <returns>Modbus RTU 客户端实例</returns>
    public static IModbusClient CreateRtuClient(
        string portName,
        int baudRate = 9600,
        Parity parity = Parity.None,
        int dataBits = 8,
        StopBits stopBits = StopBits.One)
    {
        return CreateRtuClient(portName, baudRate, parity, dataBits, stopBits, Crc16Variant.Even);
    }

    /// <summary>
    /// 创建 Modbus RTU 客户端（便捷重载，支持 CRC 变体指定）
    /// </summary>
    /// <param name="portName">串口名称（例如 "COM1", "/dev/ttyUSB0"）</param>
    /// <param name="baudRate">波特率，默认 9600</param>
    /// <param name="parity">校验位，默认 None</param>
    /// <param name="dataBits">数据位，默认 8</param>
    /// <param name="stopBits">停止位，默认 One</param>
    /// <param name="crc16Variant">CRC-16 变体，默认为偶校验（Modbus 标准）</param>
    /// <returns>Modbus RTU 客户端实例</returns>
    public static IModbusClient CreateRtuClient(
        string portName,
        int baudRate = 9600,
        Parity parity = Parity.None,
        int dataBits = 8,
        StopBits stopBits = StopBits.One,
        Crc16Variant crc16Variant = Crc16Variant.Even)
    {
        var settings = new SerialPortSettings
        {
            PortName = portName,
            BaudRate = baudRate,
            Parity = parity,
            DataBits = dataBits,
            StopBits = stopBits,
            Crc16Variant = crc16Variant
        };
        return CreateRtuClient(settings);
    }
}
