using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Core;
using ZHIOT.Modbus.Utils;

namespace ZHIOT.Modbus.Client;

/// <summary>
/// Modbus TCP 客户端实现
/// </summary>
public class ModbusTcpClient : IModbusClient
{
    private readonly ITransport _transport;
    private ushort _transactionId;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsOneBasedAddressing { get; set; }

    /// <inheritdoc/>
    public ByteOrder ByteOrder { get; set; } = ByteOrder.BigEndian;

    public ModbusTcpClient(ITransport transport)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return _transport.ConnectAsync(cancellationToken);
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        return _transport.CloseAsync(cancellationToken);
    }

    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 2000);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadCoilsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadCoilsResponse(response, quantity);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 2000);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadDiscreteInputsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadDiscreteInputsResponse(response, quantity);
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadHoldingRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadHoldingRegistersResponse(response);
    }

    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        startAddress = ConvertAddress(startAddress);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadInputRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadInputRegistersResponse(response);
    }

    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default)
    {
        address = ConvertAddress(address);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteSingleCoilRequest(pdu, address, value);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteSingleCoilResponse(response);
    }

    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        address = ConvertAddress(address);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteSingleRegisterRequest(pdu, address, value);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteSingleRegisterResponse(response);
    }

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

    public Task<short[]> ReadHoldingRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<short>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    public Task<short[]> ReadInputRegistersInt16Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<short>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    public Task<int[]> ReadHoldingRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<int>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    public Task<int[]> ReadInputRegistersInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<int>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    public Task<uint[]> ReadHoldingRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<uint>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    public Task<uint[]> ReadInputRegistersUInt32Async(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<uint>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    public Task<float[]> ReadHoldingRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<float>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    public Task<float[]> ReadInputRegistersFloatAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<float>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    public Task<double[]> ReadHoldingRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<double>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadHoldingRegisters, cancellationToken);
    }

    public Task<double[]> ReadInputRegistersDoubleAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);
        return ReadRegistersAsync<double>(slaveId, startAddress, quantity, ModbusFunctionCode.ReadInputRegisters, cancellationToken);
    }

    #endregion

    #region 扩展数据类型写入方法实现

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

    public Task WriteMultipleRegistersInt16Async(byte slaveId, ushort startAddress, short[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    public Task WriteMultipleRegistersInt32Async(byte slaveId, ushort startAddress, int[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    public Task WriteMultipleRegistersUInt32Async(byte slaveId, ushort startAddress, uint[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    public Task WriteMultipleRegistersFloatAsync(byte slaveId, ushort startAddress, float[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    public Task WriteMultipleRegistersDoubleAsync(byte slaveId, ushort startAddress, double[] values, CancellationToken cancellationToken = default)
    {
        return WriteRegistersAsync(slaveId, startAddress, values, cancellationToken);
    }

    #endregion

    private async Task<byte[]> SendRequestAsync(byte unitId, byte[] pdu, CancellationToken cancellationToken)
    {
        if (!_transport.IsConnected)
            throw new InvalidOperationException("Not connected to server");

        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            // 生成事务 ID
            ushort transactionId = unchecked(_transactionId++);

            // 构建完整的 ADU (MBAP + PDU)
            var adu = new byte[MbapHeader.Size + pdu.Length];
            ModbusPduBuilder.BuildMbapHeader(adu, transactionId, unitId, (ushort)pdu.Length);
            pdu.CopyTo(adu.AsSpan().Slice(MbapHeader.Size));

            // 发送请求
            var output = _transport.Pipe.Output;
            var memory = output.GetMemory(adu.Length);
            adu.CopyTo(memory.Span);
            output.Advance(adu.Length);
            await output.FlushAsync(cancellationToken);

            // 接收响应
            var input = _transport.Pipe.Input;
            while (true)
            {
                var result = await input.ReadAsync(cancellationToken);
                var buffer = result.Buffer;

                if (TryParseResponse(ref buffer, transactionId, out var responsePdu))
                {
                    input.AdvanceTo(buffer.Start);
                    return responsePdu;
                }

                input.AdvanceTo(buffer.Start, buffer.End);

                if (result.IsCompleted)
                    throw new IOException("Connection closed while waiting for response");
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private bool TryParseResponse(ref ReadOnlySequence<byte> buffer, ushort expectedTransactionId, out byte[] pdu)
    {
        pdu = Array.Empty<byte>();

        // 需要至少 7 字节的 MBAP 头
        if (buffer.Length < MbapHeader.Size)
            return false;

        // 读取 MBAP 头
        Span<byte> headerBytes = stackalloc byte[MbapHeader.Size];
        buffer.Slice(0, MbapHeader.Size).CopyTo(headerBytes);
        var header = ModbusPduParser.ParseMbapHeader(headerBytes);

        // 检查协议 ID
        if (header.ProtocolId != 0)
            throw new InvalidOperationException($"Invalid protocol ID: {header.ProtocolId}");

        // 计算总长度
        long totalLength = MbapHeader.Size + header.Length - 1; // Length 包含 UnitId

        // 检查是否接收到完整的消息
        if (buffer.Length < totalLength)
            return false;

        // 提取 PDU
        int pduLength = header.Length - 1;
        pdu = new byte[pduLength];
        buffer.Slice(MbapHeader.Size, pduLength).CopyTo(pdu);

        // 移动缓冲区位置
        buffer = buffer.Slice(totalLength);

        return true;
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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _transport.DisposeAsync();
        _sendLock.Dispose();
    }
}
