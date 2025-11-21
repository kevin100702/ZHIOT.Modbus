using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Core;
using ZHIOT.Modbus.Utils;

namespace ZHIOT.Modbus.Client;

/// <summary>
/// Modbus RTU 客户端实现
/// </summary>
public class ModbusRtuClient : IModbusClient
{
    private readonly ITransport _transport;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsOneBasedAddressing { get; set; }

    /// <inheritdoc/>
    public ByteOrder ByteOrder { get; set; } = ByteOrder.BigEndian;

    /// <summary>
    /// 创建 Modbus RTU 客户端
    /// </summary>
    /// <param name="transport">传输层实现</param>
    public ModbusRtuClient(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    /// <inheritdoc/>
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return _transport.ConnectAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return _transport.CloseAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 2000);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadCoilsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadCoilsResponse(response, quantity);
    }

    /// <inheritdoc/>
    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 2000);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadDiscreteInputsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadDiscreteInputsResponse(response, quantity);
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadHoldingRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadHoldingRegistersResponse(response);
    }

    /// <inheritdoc/>
    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadInputRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadInputRegistersResponse(response);
    }

    /// <inheritdoc/>
    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default)
    {
        address = ConvertAddress(address);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteSingleCoilRequest(pdu, address, value);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteSingleCoilResponse(response);
    }

    /// <inheritdoc/>
    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        address = ConvertAddress(address);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteSingleRegisterRequest(pdu, address, value);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteSingleRegisterResponse(response);
    }

    /// <inheritdoc/>
    public async Task WriteMultipleCoilsAsync(byte slaveId, ushort startAddress, bool[] values, CancellationToken cancellationToken = default)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty", nameof(values));

        ValidateQuantity((ushort)values.Length, 1, 1968);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteMultipleCoilsRequest(pdu, startAddress, values);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteMultipleCoilsResponse(response);
    }

    /// <inheritdoc/>
    public async Task WriteMultipleRegistersAsync(byte slaveId, ushort startAddress, ushort[] values, CancellationToken cancellationToken = default)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty", nameof(values));

        ValidateQuantity((ushort)values.Length, 1, 123);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteMultipleRegistersRequest(pdu, startAddress, values);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteMultipleRegistersResponse(response);
    }

    #region 扩展数据类型读取方法实现

    /// <inheritdoc/>
    public async Task<byte[]> ReadHoldingRegistersBytesAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadHoldingRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        var payload = ModbusPduParser.ParseRegistersResponsePayload(response);
        
        return payload.ToArray();
    }

    /// <inheritdoc/>
    public async Task<byte[]> ReadInputRegistersBytesAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadInputRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        var payload = ModbusPduParser.ParseRegistersResponsePayload(response);
        
        return payload.ToArray();
    }

    /// <inheritdoc/>
    public Task<short[]> ReadHoldingRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<short>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<short[]> ReadInputRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<short>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int[]> ReadHoldingRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<int>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<int[]> ReadInputRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<int>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<uint[]> ReadHoldingRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<uint>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<uint[]> ReadInputRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<uint>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<float[]> ReadHoldingRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<float>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<float[]> ReadInputRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<float>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<double[]> ReadHoldingRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<double>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<double[]> ReadInputRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<double>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    #endregion

    #region 扩展数据类型写入方法实现

    /// <inheritdoc/>
    public async Task WriteMultipleRegistersBytesAsync(byte slaveId, ushort startAddress, byte[] values, CancellationToken cancellationToken = default)
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty", nameof(values));

        if (values.Length % 2 != 0)
            throw new ArgumentException("Byte array length must be even (multiples of 2)", nameof(values));

        startAddress = ConvertAddress(startAddress);

        // 将字节数组转换为寄存器数组
        int registerCount = values.Length / 2;
        var registers = new ushort[registerCount];
        for (int i = 0; i < registerCount; i++)
        {
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(values.AsSpan().Slice(i * 2, 2));
        }

        ValidateQuantity((ushort)registers.Length, 1, 123);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteMultipleRegistersRequest(pdu, startAddress, registers);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteMultipleRegistersResponse(response);
    }

    /// <inheritdoc/>
    public Task WriteMultipleRegistersInt16Async(byte slaveId, ushort startAddress, short[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteMultipleRegistersInt32Async(byte slaveId, ushort startAddress, int[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteMultipleRegistersUInt32Async(byte slaveId, ushort startAddress, uint[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteMultipleRegistersFloatAsync(byte slaveId, ushort startAddress, float[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteMultipleRegistersDoubleAsync(byte slaveId, ushort startAddress, double[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    #endregion

    /// <summary>
    /// 发送 Modbus RTU 请求并接收响应
    /// </summary>
    private async Task<byte[]> SendRequestAsync(byte slaveId, byte[] pdu, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
            throw new InvalidOperationException("Not connected to serial port");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            // 构建完整的 RTU ADU (SlaveId + PDU + CRC)
            var adu = new byte[1 + pdu.Length + 2];
            ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

            // 发送请求
            var output = _transport.Pipe.Output;
            var memory = output.GetMemory(adu.Length);
            adu.CopyTo(memory.Span);
            output.Advance(adu.Length);
            await output.FlushAsync(cancellationToken);

            // 接收响应
            var input = _transport.Pipe.Input;
            var timeout = TimeSpan.FromMilliseconds(1000);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            while (true)
            {
                var result = await input.ReadAsync(cts.Token);
                var buffer = result.Buffer;

                if (TryParseResponse(ref buffer, slaveId, out var responsePdu))
                {
                    input.AdvanceTo(buffer.Start);
                    return responsePdu;
                }

                input.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    throw new IOException("Connection closed while waiting for response");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Timeout waiting for Modbus RTU response");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 尝试从缓冲区解析 RTU 响应
    /// </summary>
    private bool TryParseResponse(ref ReadOnlySequence<byte> buffer, byte expectedSlaveId, out byte[] pdu)
    {
        pdu = Array.Empty<byte>();

        // 需要至少 4 字节 (SlaveId + FunctionCode + CRC)
        if (buffer.Length < RtuAdu.MinSize)
            return false;

        // RTU 帧边界检测策略：
        // 1. 查找以预期 SlaveId 开头的帧
        // 2. 根据功能码推断预期的响应长度
        // 3. 验证 CRC

        // 需要读取至少前几个字节以确定帧长度
        byte[] headerBuffer = new byte[256];
        int headerLength = (int)Math.Min(buffer.Length, 256);
        buffer.Slice(0, headerLength).CopyTo(headerBuffer);
        ReadOnlySpan<byte> span = headerBuffer.AsSpan(0, headerLength);

        // 检查 SlaveId
        byte slaveId = span[0];
        if (slaveId != expectedSlaveId)
        {
            // 可能是噪声或其他设备的响应，跳过这个字节
            buffer = buffer.Slice(1);
            return false;
        }

        // 检查功能码
        byte functionCode = span[1];
        bool isException = (functionCode & 0x80) != 0;

        // 根据功能码确定预期的帧长度
        int expectedLength;
        if (isException)
        {
            // 异常响应: SlaveId + FunctionCode + ExceptionCode + CRC = 5 字节
            expectedLength = 5;
        }
        else
        {
            // 根据功能码确定长度
            expectedLength = GetExpectedResponseLength(functionCode, span);
        }

        // 检查是否接收到完整的帧
        if (buffer.Length < expectedLength)
            return false;

        // 提取完整帧
        Span<byte> frame = stackalloc byte[expectedLength];
        buffer.Slice(0, expectedLength).CopyTo(frame);

        // 验证 CRC
        if (!ModbusRtuAduParser.VerifyCrc(frame))
        {
            // CRC 错误，跳过这个字节
            buffer = buffer.Slice(1);
            return false;
        }

        // 提取 PDU
        var pduSpan = ModbusRtuAduParser.ExtractPdu(frame);
        pdu = pduSpan.ToArray();

        // 移动缓冲区位置
        buffer = buffer.Slice(expectedLength);

        return true;
    }

    /// <summary>
    /// 根据功能码和数据推断预期的响应长度
    /// </summary>
    private int GetExpectedResponseLength(byte functionCode, ReadOnlySpan<byte> data)
    {
        switch (functionCode)
        {
            case 0x01: // Read Coils
            case 0x02: // Read Discrete Inputs
            case 0x03: // Read Holding Registers
            case 0x04: // Read Input Registers
                // 响应格式: SlaveId + FunctionCode + ByteCount + Data + CRC
                if (data.Length >= 3)
                {
                    byte byteCount = data[2];
                    return 1 + 1 + 1 + byteCount + 2; // SlaveId + FC + BC + Data + CRC
                }
                return RtuAdu.MinSize; // 最小长度
            
            case 0x05: // Write Single Coil
            case 0x06: // Write Single Register
                // 响应格式: SlaveId + FunctionCode + Address(2) + Value(2) + CRC
                return 8;
            
            case 0x0F: // Write Multiple Coils
            case 0x10: // Write Multiple Registers
                // 响应格式: SlaveId + FunctionCode + Address(2) + Quantity(2) + CRC
                return 8;
            
            default:
                return RtuAdu.MinSize;
        }
    }

    private static void ValidateQuantity(ushort quantity, ushort min, ushort max)
    {
        if (quantity < min || quantity > max)
            throw new ArgumentOutOfRangeException(nameof(quantity), $"Quantity must be between {min} and {max}");
    }

    /// <summary>
    /// 转换用户地址为协议地址（处理 1 基地址）
    /// </summary>
    private ushort ConvertAddress(ushort address)
    {
        if (IsOneBasedAddressing && address > 0)
            return (ushort)(address - 1);
        return address;
    }

    /// <summary>
    /// 高性能读取寄存器并转换为指定类型（通用方法）
    /// </summary>
    private async Task<T[]> ReadRegistersAsync<T>(
        byte slaveId,
        ushort startAddress,
        ushort quantity,
        ModbusFunctionCode functionCode,
        CancellationToken cancellationToken) where T : struct
    {
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength;

        if (functionCode == ModbusFunctionCode.ReadHoldingRegisters)
            pduLength = ModbusPduBuilder.BuildReadHoldingRegistersRequest(pdu, startAddress, quantity);
        else if (functionCode == ModbusFunctionCode.ReadInputRegisters)
            pduLength = ModbusPduBuilder.BuildReadInputRegistersRequest(pdu, startAddress, quantity);
        else
            throw new ArgumentException($"Unsupported function code: {functionCode}");

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        var payload = ModbusPduParser.ParseRegistersResponsePayload(response);

        return ModbusDataConverter.ToArray<T>(payload, ByteOrder);
    }

    /// <summary>
    /// 高性能写入寄存器（通用方法）
    /// </summary>
    private async Task WriteRegistersAsync<T>(
        byte slaveId,
        ushort startAddress,
        T[] values,
        CancellationToken cancellationToken) where T : struct
    {
        if (values == null || values.Length == 0)
            throw new ArgumentException("Values cannot be null or empty", nameof(values));

        startAddress = ConvertAddress(startAddress);

        var registers = ModbusDataConverter.ToRegisters(values, ByteOrder);
        ValidateQuantity((ushort)registers.Length, 1, 123);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteMultipleRegistersRequest(pdu, startAddress, registers);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteMultipleRegistersResponse(response);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _transport.DisposeAsync();
        _sendLock.Dispose();
    }
}
