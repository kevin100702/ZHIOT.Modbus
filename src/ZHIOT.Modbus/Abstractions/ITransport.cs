using System.IO.Pipelines;

namespace ZHIOT.Modbus.Abstractions;

/// <summary>
/// 定义传输层接口，负责底层数据传输
/// </summary>
public interface ITransport : IAsyncDisposable
{
    /// <summary>
    /// 获取用于读写数据的双工管道
    /// </summary>
    IDuplexPipe Pipe { get; }

    /// <summary>
    /// 建立连接
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 关闭连接
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取连接状态
    /// </summary>
    bool IsConnected { get; }
}
