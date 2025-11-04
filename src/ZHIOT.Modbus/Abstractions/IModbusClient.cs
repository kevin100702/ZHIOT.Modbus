namespace ZHIOT.Modbus.Abstractions;

/// <summary>
/// 定义 Modbus 客户端接口
/// </summary>
public interface IModbusClient : IAsyncDisposable
{
    /// <summary>
    /// 连接到 Modbus 服务器
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取线圈状态 (功能码 0x01)
    /// </summary>
    Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取离散输入 (功能码 0x02)
    /// </summary>
    Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器 (功能码 0x03)
    /// </summary>
    Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器 (功能码 0x04)
    /// </summary>
    Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写单个线圈 (功能码 0x05)
    /// </summary>
    Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写单个寄存器 (功能码 0x06)
    /// </summary>
    Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个线圈 (功能码 0x0F)
    /// </summary>
    Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default);
}
