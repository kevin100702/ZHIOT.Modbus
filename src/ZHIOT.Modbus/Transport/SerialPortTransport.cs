using System.IO.Pipelines;
using System.IO.Ports;
using ZHIOT.Modbus.Abstractions;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Transport;

/// <summary>
/// 基于 System.IO.Pipelines 的串口传输层实现
/// 支持 Modbus RTU 通信
/// </summary>
public class SerialPortTransport : ITransport
{
    private readonly SerialPortSettings _settings;
    private SerialPort? _serialPort;
    private IDuplexPipe? _pipe;
    private bool _disposed;

    /// <summary>
    /// 获取用于读写数据的双工管道
    /// </summary>
    public IDuplexPipe Pipe => _pipe ?? throw new InvalidOperationException("Not connected");

    /// <summary>
    /// 获取连接状态
    /// </summary>
    public bool IsConnected => _serialPort?.IsOpen ?? false;

    /// <summary>
    /// 创建串口传输层实例
    /// </summary>
    /// <param name="settings">串口配置参数</param>
    public SerialPortTransport(SerialPortSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// 建立串口连接
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_serialPort != null && _serialPort.IsOpen)
            return;

        // 创建并配置串口
        _serialPort = new SerialPort
        {
            PortName = _settings.PortName,
            BaudRate = _settings.BaudRate,
            Parity = _settings.Parity,
            DataBits = _settings.DataBits,
            StopBits = _settings.StopBits,
            ReadTimeout = _settings.ReadTimeout,
            WriteTimeout = _settings.WriteTimeout,
            Handshake = Handshake.None
        };

        // 打开串口
        _serialPort.Open();

        // 创建 Pipelines（使用串口的 BaseStream）
        var stream = _serialPort.BaseStream;
        _pipe = StreamDuplexPipe.Create(stream);

        await Task.CompletedTask;
    }

    /// <summary>
    /// 关闭串口连接
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        // 关闭串口
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                    _serialPort.Close();
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        _pipe = null;
        await Task.CompletedTask;
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await CloseAsync();
    }
}
