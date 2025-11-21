using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Core;
using ZHIOT.Modbus.Utils;

namespace ZHIOT.Modbus.Client;

/// <summary>
/// Modbus ASCII 客户端实现
/// </summary>
public class ModbusAsciiClient : IModbusClient
{
    private readonly ITransport _transport;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsOneBasedAddressing { get; set; }

    /// <inheritdoc/>
    public ByteOrder ByteOrder { get; set; } = ByteOrder.BigEndian;

    /// <summary>
    /// 创建 Modbus ASCII 客户端
    /// </summary>
    /// <param name="transport">传输层实现</param>
    public ModbusAsciiClient(ITransport transport)
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
    /// 发送 Modbus ASCII 请求并接收响应
    /// </summary>
    private async Task<byte[]> SendRequestAsync(byte slaveId, byte[] pdu, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
            throw new InvalidOperationException("Not connected");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            // 构建完整的 ASCII ADU
            Span<byte> adu = stackalloc byte[512];
            int aduLength = ModbusAsciiAduBuilder.BuildAdu(adu, slaveId, pdu);

            // 发送请求
            var output = _transport.Pipe.Output;
            var memory = output.GetMemory(aduLength);
            adu.Slice(0, aduLength).CopyTo(memory.Span);
            output.Advance(aduLength);
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
            throw new TimeoutException("Timeout waiting for Modbus ASCII response");
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 尝试从缓冲区解析 ASCII 响应
    /// </summary>
    private bool TryParseResponse(ref ReadOnlySequence<byte> buffer, byte expectedSlaveId, out byte[] pdu)
    {
        pdu = Array.Empty<byte>();

        // ASCII 帧的最小长度: ':' + SlaveId(2) + FunctionCode(2) + LRC(2) + '\r\n' = 9
        if (buffer.Length < AsciiAdu.MinSize)
            return false;

        // 查找帧头 ':'
        var indexOfStart = buffer.PositionOf((byte)':');
        if (indexOfStart == null)
            return false; // 没有找到帧头

        // 跳过帧头前的任何数据
        buffer = buffer.Slice(indexOfStart.Value);

        // 查找帧尾 '\r\n'
        int crIndex = -1;
        for (int i = 1; i < buffer.Length - 1; i++)
        {
            if (buffer.Slice(i, 1).First.Span[0] == (byte)'\r' && 
                buffer.Slice(i + 1, 1).First.Span[0] == (byte)'\n')
            {
                crIndex = i;
                break;
            }
        }

        if (crIndex == -1)
            return false; // 帧未完成

        // 提取完整的帧
        int frameLength = crIndex + 2;

        if (buffer.Length < frameLength)
            return false;

        Span<byte> frame = stackalloc byte[frameLength];
        buffer.Slice(0, frameLength).CopyTo(frame);

        // 验证 LRC
        if (!ModbusAsciiAduParser.VerifyLrc(frame))
        {
            // LRC 错误，跳过这一行
            buffer = buffer.Slice(frameLength);
            return false;
        }

        // 提取 SlaveId
        byte slaveId = ModbusAsciiAduParser.ExtractSlaveId(frame);
        if (slaveId != expectedSlaveId)
        {
            // 不是期望的从站，跳过
            buffer = buffer.Slice(frameLength);
            return false;
        }

        try
        {
            // 提取 PDU
            pdu = ModbusAsciiAduParser.ExtractPdu(frame);
            
            // 移动缓冲区位置
            buffer = buffer.Slice(frameLength);
            
            return true;
        }
        catch
        {
            // 解析失败，跳过这一行
            buffer = buffer.Slice(frameLength);
            return false;
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
