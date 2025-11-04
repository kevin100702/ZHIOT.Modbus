# 功能实施计划: 现代化 Modbus .NET 基础库

## ✅ Todo 清单
- [ ] 创建项目基本结构和解决方案
- [ ] 定义核心抽象接口 (Transport, Protocol)
- [ ] 实现基于 `System.IO.Pipelines` 的 TCP 传输层
- [ ] 使用 `Span<T>` 和 `Memory<T>` 实现 Modbus ADU/PDU 的解析和构建
- [ ] 实现核心 Modbus 功能码（0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x0F, 0x10）
- [ ] 构建 Modbus TCP 客户端
- [ ] 编写单元测试和集成测试
- [ ] 最终审查和测试

## ?? 分析与调查

### 代码库结构
当前目录为空，我们将从头开始创建一个结构良好、易于维护和扩展的 .NET 项目。推荐的结构如下：
```
/ZHIOT.Modbus
|-- ZHIOT.Modbus.sln
|-- /src
|   |-- /ZHIOT.Modbus
|   |   |-- ZHIOT.Modbus.csproj
|   |   |-- /Abstractions       # 核心抽象接口
|   |   |-- /Core               # 核心逻辑，如 Pipeline 处理、帧解析
|   |   |-- /Client             # Modbus 客户端实现
|   |   |-- /Server             # Modbus 服务端实现 (未来扩展)
|   |   |-- /Transport          # 传输层实现 (TCP)
|   |   |-- /Utils              # 辅助工具
|-- /tests
|   |-- /ZHIOT.Modbus.Tests
|   |   |-- ZHIOT.Modbus.Tests.csproj
|   |   |-- ... (测试文件)
|-- /samples
|   |-- /ZHIOT.Modbus.Samples
|   |   |-- ... (示例项目)
```

### 架构设计
我们将采用分层架构，将传输、协议解析和应用逻辑解耦。

1.  **传输层 (Transport Layer)**: 负责原始数据流的读写。这是 `System.IO.Pipelines` 的主要应用场景。它将从网络或串行端口读取数据，并将其写入 `PipeWriter`。
2.  **协议层 (Protocol Layer)**: 负责 Modbus 协议帧的解析和构建。它将从 `PipeReader` 读取数据，利用 `ReadOnlySequence<byte>` 进行高效的帧边界检测和解析。`Span<T>` 和 `Memory<T>` 将在这里用于无GC的报文构建和字段读写。
3.  **应用层 (Application Layer)**: 实现 Modbus 客户端/服务端逻辑，调用协议层来发送和接收 Modbus 请求/响应。

### 依赖与集成点
- **`System.IO.Pipelines`**: 作为网络通信的核心，提供高效的流处理能力。
- **`System.Memory`**: 提供 `Span<T>` 和 `Memory<T>`，用于实现零内存分配的协议解析和构建。
- **`.NET Standard 2.1` 或更高**: 为了利用 `Span<T>` 和 `Pipelines` 的最佳性能，建议目标框架为 `.NET Standard 2.1` 或 `.NET 6/8`。

### 注意事项与挑战
1.  **帧边界处理**: Modbus RTU 和 TCP 的帧边界检测机制不同。TCP 基于 MBAP 头中的长度字段，而 RTU 依赖于传输间隙。`Pipelines` 非常适合处理 TCP 这种基于长度的协议。对于 RTU，需要额外的逻辑来检测静默期。
2.  **异步处理**: 整个库应设计为完全异步，充分利用 `async/await` 和 `ValueTask` 来提高吞吐量。
3.  **`ReadOnlySequence<byte>` 的使用**: `Pipelines` 产生的数据可能是分段的 `ReadOnlySequence<byte>`。在解析时，需要正确处理跨段的数据，避免性能陷阱。

## ?? 实施计划

### 先决条件
- 安装 .NET 8 SDK。

### 分步实施

1.  **步骤 1: 初始化项目结构**
    - **操作**: 使用 `dotnet new` CLI 命令创建解决方案和项目。
      ```bash
      dotnet new sln -n ZHIOT.Modbus
      mkdir src tests samples
      dotnet new classlib -n ZHIOT.Modbus -o src/ZHIOT.Modbus 
      dotnet new mstest -n ZHIOT.Modbus.Tests -o tests/ZHIOT.Modbus.Tests
      dotnet sln add src/ZHIOT.Modbus/ZHIOT.Modbus.csproj
      dotnet sln add tests/ZHIOT.Modbus.Tests/ZHIOT.Modbus.Tests.csproj
      ```
    - **文件修改**: `ZHIOT.Modbus.csproj` - 将 TargetFramework 设置为 `netstandard2.1` 或 `net8.0`。

2.  **步骤 2: 定义核心抽象**
    - **操作**: 在 `src/ZHIOT.Modbus/Abstractions` 目录下创建接口。
    - **文件创建**:
        - `ITransport.cs`: 定义传输层接口，包含连接、断开和 `IDuplexPipe` 属性。
        - `IModbusProtocol.cs`: 定义协议层接口，用于解析响应和构建请求。
        - `IModbusClient.cs`: 定义客户端接口，包含读写等操作。
    - **代码示例 (`ITransport.cs`)**:
      ```csharp
      using System.IO.Pipelines;
      using System.Threading.Tasks;

      namespace ZHIOT.Modbus.Abstractions;

      public interface ITransport : IAsyncDisposable
      {
          IDuplexPipe Pipe { get; }
          Task ConnectAsync(CancellationToken cancellationToken = default);
          Task CloseAsync(CancellationToken cancellationToken = default);
      }
      ```

3.  **步骤 3: 实现 TCP 传输层**
    - **操作**: 在 `src/ZHIOT.Modbus/Transport` 目录下创建 `TcpTransport` 类。
    - **文件创建**: `TcpTransport.cs`
    - **所需变更**:
        - 实现 `ITransport` 接口。
        - 在 `ConnectAsync` 方法中，使用 `Socket` 或 `TcpClient` 连接到目标终点。
        - 将 `NetworkStream` 适配到 `IDuplexPipe`。可以使用 `Stream.UsePipeReader` 和 `Stream.UsePipeWriter` (在 .NET 8 中) 或自定义实现。
        - 管理连接生命周期。

4.  **步骤 4: 实现 Modbus 帧解析器**
    - **操作**: 在 `src/ZHIOT.Modbus/Core` 目录下创建 `ModbusProtocol` 类。
    - **文件创建**: `ModbusProtocol.cs`
    - **所需变更**:
        - 创建一个核心的 `ProcessPipeAsync` 方法，该方法循环从 `PipeReader` 读取数据。
        - 在循环中，检查 `ReadOnlySequence<byte>` 以查找完整的 Modbus ADU (Application Data Unit)。对于 TCP，先读取 7 字节的 MBAP 头，然后根据长度字段读取剩余部分。
        - 使用 `SequenceReader<byte>` 来安全、高效地从 `ReadOnlySequence<byte>` 中读取数据。
        - 将解析出的数据包传递给上层进行处理。

5.  **步骤 5: 实现报文构建与解析**
    - **操作**: 在 `Core` 目录中创建静态类或辅助类，用于 PDU (Protocol Data Unit) 的构建和解析。
    - **文件创建**: `ModbusPduBuilder.cs`, `ModbusPduParser.cs`
    - **所需变更**:
        - **构建**: 创建方法如 `public static void BuildReadHoldingRegistersRequest(Span<byte> buffer, ushort startAddress, ushort quantity)`。使用 `System.Buffers.Binary.BinaryPrimitives` 将 `ushort` 等类型以 Big-Endian 格式写入 `Span<byte>`。
        - **解析**: 创建方法如 `public static ReadHoldingRegistersResponse ParseReadHoldingRegistersResponse(ReadOnlySpan<byte> buffer)`。同样使用 `BinaryPrimitives` 来读取数据。

6.  **步骤 6: 实现 Modbus TCP 客户端**
    - **操作**: 在 `src/ZHIOT.Modbus/Client` 目录下创建 `ModbusTcpClient`。
    - **文件创建**: `ModbusTcpClient.cs`
    - **所需变更**:
        - 实现 `IModbusClient` 接口。
        - 内部持有一个 `ITransport` 和 `ModbusProtocol` 实例。
        - 实现 `ReadHoldingRegistersAsync` 等方法。这些方法将：
            1. 构建请求 PDU (`Span<byte>`)。
            2. 构建 MBAP 头，并将其与 PDU 组合成完整的 ADU。
            3. 将 ADU 写入 `PipeWriter`。
            4. 等待 `ModbusProtocol` 解析返回的响应。

### 测试策略
1.  **单元测试 (`ZHIOT.Modbus.Tests`)**:
    - **协议解析**: 针对 `ModbusPduBuilder` 和 `ModbusPduParser` 编写测试，提供已知的字节序列，验证其能否正确构建和解析为 Modbus PDU 对象。
    - **帧处理**: 针对 `ModbusProtocol` 的帧边界检测逻辑编写测试，模拟正常、粘包、半包等场景。
2.  **集成测试**:
    - 创建一个简单的 Modbus TCP 服务器模拟器（或使用现有工具）。
    - `ModbusTcpClient` 连接到模拟器，执行所有支持的读写操作，并验证结果的正确性。

## ?? 成功标准
- 所有在 Todo 清单中列出的任务都已完成。
- 单元测试和集成测试覆盖所有核心功能，且全部通过。
- 库能够成功地与标准的 Modbus TCP 设备或模拟器进行通信。
- 性能分析显示，在协议解析和构建过程中没有不必要的内存分配。
- 提供一个简单的示例项目 (`samples` 目录)，演示如何使用 `ModbusTcpClient`。
