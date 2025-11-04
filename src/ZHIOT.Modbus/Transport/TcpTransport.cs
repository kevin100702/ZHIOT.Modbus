using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using ZHIOT.Modbus.Abstractions;

namespace ZHIOT.Modbus.Transport;

/// <summary>
/// 基于 System.IO.Pipelines 的 TCP 传输层实现
/// </summary>
public class TcpTransport : ITransport
{
    private readonly string _host;
    private readonly int _port;
    private Socket? _socket;
    private IDuplexPipe? _pipe;
    private bool _disposed;

    public IDuplexPipe Pipe => _pipe ?? throw new InvalidOperationException("Not connected");

    public bool IsConnected => _socket?.Connected ?? false;

    public TcpTransport(string host, int port)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _port = port;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_socket != null && _socket.Connected)
            return;

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // 设置 TCP 选项以优化性能
        _socket.NoDelay = true;

        var ipAddress = IPAddress.Parse(_host);
        await _socket.ConnectAsync(new IPEndPoint(ipAddress, _port), cancellationToken);

        // 创建 Pipelines
        var stream = new NetworkStream(_socket, ownsSocket: false);
        _pipe = StreamDuplexPipe.Create(stream);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_socket != null)
        {
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {
                // Ignore shutdown errors
            }
            finally
            {
                _socket.Close();
                _socket = null;
            }
        }

        _pipe = null;
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await CloseAsync();
        _socket?.Dispose();
    }
}

/// <summary>
/// 将 Stream 适配到 IDuplexPipe 的辅助类
/// </summary>
internal class StreamDuplexPipe : IDuplexPipe
{
    private readonly Stream _stream;
    private readonly PipeReader _reader;
    private readonly PipeWriter _writer;

    private StreamDuplexPipe(Stream stream)
    {
        _stream = stream;
        _reader = PipeReader.Create(stream);
        _writer = PipeWriter.Create(stream);
    }

    public PipeReader Input => _reader;
    public PipeWriter Output => _writer;

    public static IDuplexPipe Create(Stream stream)
    {
        return new StreamDuplexPipe(stream);
    }
}
