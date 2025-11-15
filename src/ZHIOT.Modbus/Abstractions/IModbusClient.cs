namespace ZHIOT.Modbus.Abstractions;

/// <summary>
/// 定义 Modbus 客户端接口
/// </summary>
public interface IModbusClient : IAsyncDisposable
{
    /// <summary>
    /// 获取或设置是否使用 1 基地址（true）或 0 基地址（false，默认）
    /// 当设置为 true 时，所有地址参数将自动减 1 以符合 Modbus 协议规范
    /// </summary>
    bool IsOneBasedAddressing { get; set; }

    /// <summary>
    /// 获取或设置字节序，默认为 BigEndian（Modbus 标准）
    /// </summary>
    Core.ByteOrder ByteOrder { get; set; }

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

    #region 扩展数据类型读取方法

    /// <summary>
    /// 读取保持寄存器并转换为字节数组 (功能码 0x03)
    /// </summary>
    Task<byte[]> ReadHoldingRegistersBytesAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为字节数组 (功能码 0x04)
    /// </summary>
    Task<byte[]> ReadInputRegistersBytesAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器并转换为 short 数组 (功能码 0x03)
    /// </summary>
    Task<short[]> ReadHoldingRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为 short 数组 (功能码 0x04)
    /// </summary>
    Task<short[]> ReadInputRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器并转换为 int 数组 (功能码 0x03)
    /// </summary>
    Task<int[]> ReadHoldingRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为 int 数组 (功能码 0x04)
    /// </summary>
    Task<int[]> ReadInputRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器并转换为 uint 数组 (功能码 0x03)
    /// </summary>
    Task<uint[]> ReadHoldingRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为 uint 数组 (功能码 0x04)
    /// </summary>
    Task<uint[]> ReadInputRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器并转换为 float 数组 (功能码 0x03)
    /// </summary>
    Task<float[]> ReadHoldingRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为 float 数组 (功能码 0x04)
    /// </summary>
    Task<float[]> ReadInputRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取保持寄存器并转换为 double 数组 (功能码 0x03)
    /// </summary>
    Task<double[]> ReadHoldingRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 读取输入寄存器并转换为 double 数组 (功能码 0x04)
    /// </summary>
    Task<double[]> ReadInputRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default);

    #endregion

    #region 扩展数据类型写入方法

    /// <summary>
    /// 写字节数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersBytesAsync(byte slaveId, ushort startAddress, byte[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写 short 数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersInt16Async(byte slaveId, ushort startAddress, short[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写 int 数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersInt32Async(byte slaveId, ushort startAddress, int[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写 uint 数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersUInt32Async(byte slaveId, ushort startAddress, uint[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写 float 数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersFloatAsync(byte slaveId, ushort startAddress, float[] values, CancellationToken cancellationToken = default);

    /// <summary>
    /// 写 double 数组到多个寄存器 (功能码 0x10)
    /// </summary>
    Task WriteMultipleRegistersDoubleAsync(byte slaveId, ushort startAddress, double[] values, CancellationToken cancellationToken = default);

    #endregion
}
