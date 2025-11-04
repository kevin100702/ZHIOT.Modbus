# ZHIOT.Modbus

一个现代化的、高性能的 .NET Modbus 基础库，基于 `System.IO.Pipelines` 和 `Span<T>`/`Memory<T>` 构建。

## 特性

- ✅ **高性能**: 使用 `System.IO.Pipelines` 实现零拷贝网络通信
- ✅ **低内存分配**: 利用 `Span<T>` 和 `Memory<T>` 减少 GC 压力
- ✅ **完全异步**: 基于 `async/await` 的现代异步模式
- ✅ **Modbus TCP**: 完整的 Modbus TCP 客户端实现
- ✅ **易于使用**: 简洁的 API 设计，开箱即用
- ✅ **类型安全**: 强类型接口，编译时类型检查
- ✅ **可扩展**: 清晰的架构设计，易于扩展

## 支持的功能码

- `0x01` - 读取线圈 (Read Coils)
- `0x02` - 读取离散输入 (Read Discrete Inputs)
- `0x03` - 读取保持寄存器 (Read Holding Registers)
- `0x04` - 读取输入寄存器 (Read Input Registers)
- `0x05` - 写单个线圈 (Write Single Coil)
- `0x06` - 写单个寄存器 (Write Single Register)
- `0x0F` - 写多个线圈 (Write Multiple Coils)
- `0x10` - 写多个寄存器 (Write Multiple Registers)

## 快速开始

### 安装

```bash
dotnet add package ZHIOT.Modbus
```

### 基本使用

```csharp
using ZHIOT.Modbus;

// 创建 Modbus TCP 客户端
await using var client = ModbusClientFactory.CreateTcpClient("192.168.1.100", 502);

// 连接到服务器
await client.ConnectAsync();

// 读取保持寄存器
byte slaveId = 1;
ushort[] registers = await client.ReadHoldingRegistersAsync(
    slaveId, 
    startAddress: 0, 
    quantity: 10
);

// 写单个寄存器
await client.WriteSingleRegisterAsync(slaveId, address: 0, value: 1234);

// 写多个寄存器
ushort[] values = { 100, 200, 300 };
await client.WriteMultipleRegistersAsync(slaveId, startAddress: 10, values);

// 读取线圈
bool[] coils = await client.ReadCoilsAsync(slaveId, startAddress: 0, quantity: 16);

// 写单个线圈
await client.WriteSingleCoilAsync(slaveId, address: 0, value: true);

// 断开连接
await client.DisconnectAsync();
```

## 架构设计

本库采用分层架构设计:

```
应用层 (Application Layer)
    └─ IModbusClient 接口
        └─ ModbusTcpClient 实现
             │
协议层 (Protocol Layer)
    ├─ ModbusPduBuilder: PDU 构建
    └─ ModbusPduParser: PDU 解析
             │
传输层 (Transport Layer)
    └─ ITransport 接口
        └─ TcpTransport: 基于 System.IO.Pipelines
```

### 性能优化

1. **零拷贝 I/O**: 使用 `System.IO.Pipelines` 避免不必要的缓冲区拷贝
2. **栈内存分配**: 小数据使用 `stackalloc` 分配在栈上
3. **Span 和 Memory**: 协议解析和构建过程使用 `Span<T>` 避免堆分配
4. **异步设计**: 全异步实现，提高并发能力

## 项目结构

```
ZHIOT.Modbus/
├── src/
│   └── ZHIOT.Modbus/
│       ├── Abstractions/       # 核心抽象接口
│       ├── Core/               # 协议核心实现
│       ├── Client/             # Modbus 客户端
│       ├── Transport/          # 传输层实现
│       └── Utils/              # 辅助工具
├── tests/
│   └── ZHIOT.Modbus.Tests/    # 单元测试
└── samples/
    └── ZHIOT.Modbus.Sample/   # 示例项目
```

## 运行示例

```bash
cd samples/ZHIOT.Modbus.Sample
dotnet run
```

**注意**: 示例代码需要连接到实际的 Modbus TCP 设备或模拟器。你可以使用以下工具进行测试:
- [ModbusPal](http://modbuspal.sourceforge.net/) - Modbus 模拟器
- [pyModSlave](https://github.com/uzumaxy/pyModSlave) - Python Modbus 从站模拟器

## 运行测试

```bash
dotnet test
```

## 路线图

- [ ] Modbus RTU 支持
- [ ] Modbus ASCII 支持
- [ ] Modbus 服务端实现
- [ ] 更多功能码支持 (诊断、文件传输等)
- [ ] 连接池和重连机制
- [ ] 性能基准测试
- [ ] NuGet 包发布

## 技术栈

- .NET 8.0
- System.IO.Pipelines
- System.Memory
- MSTest (测试框架)

## 许可证

MIT License

## 贡献

欢迎提交 Issue 和 Pull Request!

## 参考资料

- [Modbus Protocol Specification](https://modbus.org/docs/Modbus_Application_Protocol_V1_1b3.pdf)
- [System.IO.Pipelines Documentation](https://docs.microsoft.com/en-us/dotnet/standard/io/pipelines)
- [Span<T> and Memory<T>](https://docs.microsoft.com/en-us/dotnet/standard/memory-and-spans/)
