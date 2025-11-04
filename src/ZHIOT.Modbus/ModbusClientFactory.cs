using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Client;
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
}
