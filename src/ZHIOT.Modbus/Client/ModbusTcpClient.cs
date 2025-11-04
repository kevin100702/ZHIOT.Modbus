using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Core;

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

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadCoilsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadCoilsResponse(response, quantity);
    }

    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 2000);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadDiscreteInputsRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadDiscreteInputsResponse(response, quantity);
    }

    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadHoldingRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadHoldingRegistersResponse(response);
    }

    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort startAddress, ushort quantity, CancellationToken cancellationToken = default)
    {
        ValidateQuantity(quantity, 1, 125);

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildReadInputRegistersRequest(pdu, startAddress, quantity);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        return ModbusPduParser.ParseReadInputRegistersResponse(response);
    }

    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool value, CancellationToken cancellationToken = default)
    {
        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteSingleCoilRequest(pdu, address, value);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteSingleCoilResponse(response);
    }

    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
    {
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

        Span<byte> pdu = stackalloc byte[256];
        int pduLength = ModbusPduBuilder.BuildWriteMultipleRegistersRequest(pdu, startAddress, values);

        var response = await SendRequestAsync(slaveId, pdu.Slice(0, pduLength).ToArray(), cancellationToken);
        ModbusPduParser.ParseWriteMultipleRegistersResponse(response);
    }

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

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _transport.DisposeAsync();
        _sendLock.Dispose();
    }
}
