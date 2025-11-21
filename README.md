# ZHIOT.Modbus

一个现代化的、高性能的 .NET Modbus 基础库，基于 `System.IO.Pipelines` 和 `Span<T>`/`Memory<T>` 构建。

## 特性

- ✅ **高性能**: 使用 `System.IO.Pipelines` 实现零拷贝网络通信
- ✅ **低内存分配**: 利用 `Span<T>` 和 `Memory<T>` 减少 GC 压力
- ✅ **完全异步**: 基于 `async/await` 的现代异步模式
- ✅ **Modbus TCP**: 完整的 Modbus TCP 客户端实现
- ✅ **Modbus RTU**: 完整的 Modbus RTU 串口通信实现（基于 System.IO.Pipelines + System.IO.Ports）
- ✅ **Modbus ASCII**: 完整的 Modbus ASCII 串口通信实现，采用 LRC-8 校验和 ASCII 编码
- ✅ **扩展数据类型**: 原生支持 `float`、`double`、`int32`、`uint32` 等数据类型
- ✅ **灵活字节序**: 支持大端、小端及字交换等多种字节序模式
- ✅ **原始字节访问**: 提供 `byte[]` 读写接口用于高级场景
- ✅ **地址兼容性**: 支持 0 基和 1 基地址模式切换
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

// --- Modbus RTU 示例 (串口) ---
// 使用 RTU 客户端连接真实设备或虚拟串口
await using var rtu = ModbusClientFactory.CreateRtuClient(
    portName: "COM1",      // Windows 示例：COM1；Linux 示例：/dev/ttyUSB0
    baudRate: 9600,
    parity: Parity.None,
    dataBits: 8,
    stopBits: StopBits.One
);

await rtu.ConnectAsync();

// 从站 ID = 1，读取 10 个保持寄存器
var rtuRegisters = await rtu.ReadHoldingRegistersAsync(1, startAddress: 0, quantity: 10);

await rtu.DisconnectAsync();

// --- Modbus ASCII 示例 (串口) ---
// ASCII 模式使用 LRC-8 校验和 ASCII 编码，对长距离串口通信更有容错性
await using var ascii = ModbusClientFactory.CreateAsciiClient(
    portName: "COM1",
    baudRate: 9600,
    parity: Parity.None,
    dataBits: 8,
    stopBits: StopBits.One
);

await ascii.ConnectAsync();

// 使用方式与 RTU 相同
var asciiRegisters = await ascii.ReadHoldingRegistersAsync(1, startAddress: 0, quantity: 10);
await ascii.WriteSingleRegisterAsync(1, address: 0, value: 5678);

await ascii.DisconnectAsync();
```

### 扩展数据类型

库提供了对常见工业数据类型的原生支持：

```csharp
using ZHIOT.Modbus;

await using var client = ModbusClientFactory.CreateTcpClient("192.168.1.100", 502);
await client.ConnectAsync();

byte slaveId = 1;

// 读写 Float (32位浮点数，占用2个寄存器)
float[] floatValues = { 3.14159f, -123.456f };
await client.WriteMultipleRegistersFloatAsync(slaveId, 0, floatValues);
var readFloats = await client.ReadHoldingRegistersFloatAsync(slaveId, 0, 4);

// 读写 Double (64位浮点数，占用4个寄存器)
double[] doubleValues = { Math.PI, Math.E };
await client.WriteMultipleRegistersDoubleAsync(slaveId, 10, doubleValues);
var readDoubles = await client.ReadHoldingRegistersDoubleAsync(slaveId, 10, 8);

// 读写 Int32 (32位有符号整数，占用2个寄存器)
int[] intValues = { 123456, -987654 };
await client.WriteMultipleRegistersInt32Async(slaveId, 20, intValues);
var readInts = await client.ReadHoldingRegistersInt32Async(slaveId, 20, 4);

// 读写 UInt32 (32位无符号整数，占用2个寄存器)
uint[] uintValues = { 0x12345678, 0xABCDEF00 };
await client.WriteMultipleRegistersUInt32Async(slaveId, 30, uintValues);
var readUInts = await client.ReadHoldingRegistersUInt32Async(slaveId, 30, 4);

// 原始字节访问（用于自定义数据格式）
byte[] rawBytes = { 0x12, 0x34, 0x56, 0x78 };
await client.WriteMultipleRegistersBytesAsync(slaveId, 40, rawBytes);
var readBytes = await client.ReadHoldingRegistersBytesAsync(slaveId, 40, 2);
```

### 字节序配置

不同设备可能使用不同的字节序，库支持灵活配置：

```csharp
using ZHIOT.Modbus;
using ZHIOT.Modbus.Core;

await using var client = ModbusClientFactory.CreateTcpClient("192.168.1.100", 502);
await client.ConnectAsync();

// 设置字节序（默认为 BigEndian，符合 Modbus 标准）
client.ByteOrder = ByteOrder.BigEndian;        // ABCD (标准)
// client.ByteOrder = ByteOrder.LittleEndian;     // DCBA
// client.ByteOrder = ByteOrder.BigEndianSwap;    // BADC
// client.ByteOrder = ByteOrder.LittleEndianSwap; // CDAB

float value = 3.14159f;
await client.WriteMultipleRegistersFloatAsync(1, 0, new[] { value });
var result = await client.ReadHoldingRegistersFloatAsync(1, 0, 2);
```

### Modbus ASCII 协议特性

Modbus ASCII 是 Modbus 的 ASCII 编码变体，相比 RTU 二进制模式提供了更好的长距离通信容错性。

**主要特点:**

- **LRC-8 校验**: 使用纵向冗余校验 (LRC) 代替 CRC-16，算法更简单
- **ASCII 编码**: 数据使用 ASCII 十六进制编码，便于调试和监控
- **帧格式**: `:` + SlaveID(2hex) + FunctionCode(2hex) + Data(Nhex) + LRC(2hex) + `\r\n`
- **容错性更好**: ASCII 编码对单字节翻转更敏感，便于检测传输错误
- **传输效率**: 相比 RTU 增加约 50% 的开销，但在嘈杂的长距离链路上可靠性更高

**何时使用 ASCII:**

- 长距离串口通信（>100 米）
- 工业环境中干扰较大的场景
- 需要实时监控和调试协议流量
- 对数据的可靠性要求高于传输效率

**配置 ASCII 客户端:**

```csharp
using ZHIOT.Modbus;
using ZHIOT.Modbus.Core;

// 方式 1: 使用便利方法
var client = ModbusClientFactory.CreateAsciiClient("COM1", 9600);

// 方式 2: 使用详细配置
var settings = new AsciiLineSettings
{
    PortName = "COM1",
    BaudRate = 9600,
    Parity = Parity.None,
    DataBits = 8,
    StopBits = StopBits.One,
    ReadTimeout = 1000,    // 读取超时 (毫秒)
    WriteTimeout = 1000    // 写入超时 (毫秒)
};
var client = ModbusClientFactory.CreateAsciiClient(settings);

await client.ConnectAsync();

// 所有操作与 TCP/RTU 客户端相同
byte slaveId = 1;
var registers = await client.ReadHoldingRegistersAsync(slaveId, 0, 10);

await client.DisconnectAsync();
```

### 地址模式配置

某些设备使用 1 基地址（地址从 1 开始），库可以自动转换：

```csharp
await using var client = ModbusClientFactory.CreateTcpClient("192.168.1.100", 502);
await client.ConnectAsync();

// 启用 1 基地址模式
client.IsOneBasedAddressing = true;

// 现在可以使用从 1 开始的地址，客户端会自动转换为协议的 0 基地址
var registers = await client.ReadHoldingRegistersAsync(1, startAddress: 1, quantity: 10);
// 实际读取的是协议地址 0-9

await client.WriteMultipleRegistersAsync(1, startAddress: 1, new ushort[] { 100, 200 });
// 实际写入的是协议地址 0-1
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
5. **高性能数据转换**: 扩展数据类型转换采用零分配或最小分配策略
   - 直接从 PDU 字节流转换为目标类型，避免中间 `ushort[]` 分配
   - 使用 `MemoryMarshal.Cast` 实现零拷贝类型转换
   - 利用 `BinaryPrimitives` 进行高效的字节序转换

## 项目结构

```
ZHIOT.Modbus/
├── src/
│   └── ZHIOT.Modbus/
│       ├── Abstractions/       # 核心抽象接口
│       ├── Core/               # 协议核心实现
│       │   ├── ModbusPduBuilder.cs
│       │   ├── ModbusPduParser.cs
│       │   ├── ModbusTypes.cs
│       │   ├── ModbusRtuTypes.cs
│       │   ├── ModbusRtuAduBuilder.cs
│       │   ├── ModbusRtuAduParser.cs
│       │   ├── ModbusAsciiTypes.cs      # ASCII 类型定义
│       │   ├── ModbusLrc.cs             # LRC-8 校验算法
│       │   ├── ModbusAsciiCodec.cs      # ASCII 编码/解码
│       │   ├── ModbusAsciiAduBuilder.cs # ASCII ADU 构建
│       │   ├── ModbusAsciiAduParser.cs  # ASCII ADU 解析
│       │   ├── ModbusCrc16.cs
│       │   └── ByteOrder.cs             # 字节序定义
│       ├── Client/             # Modbus 客户端
│       │   ├── ModbusTcpClient.cs
│       │   ├── ModbusRtuClient.cs
│       │   └── ModbusAsciiClient.cs     # ASCII 客户端实现
│       ├── Transport/          # 传输层实现
│       │   ├── TcpTransport.cs
│       │   └── SerialPortTransport.cs
│       ├── Utils/              # 辅助工具
│       │   └── ModbusDataConverter.cs  # 高性能数据转换器
│       └── ModbusClientFactory.cs      # 客户端工厂
├── tests/
│   └── ZHIOT.Modbus.Tests/    # 单元测试
│       ├── ModbusPduBuilderTests.cs
│       ├── ModbusPduParserTests.cs
│       ├── ModbusRtuAduBuilderTests.cs
│       ├── ModbusRtuAduParserTests.cs
│       ├── ModbusLrcTests.cs            # ASCII LRC 测试
│       ├── ModbusAsciiCodecTests.cs     # ASCII 编码测试
│       ├── ModbusAsciiAduBuilderTests.cs # ASCII 构建测试
│       ├── ModbusAsciiAduParserTests.cs # ASCII 解析测试
│       ├── ModbusDataConverterTests.cs
│       ├── ModbusCrc16Tests.cs
│       └── Test1.cs
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

## 协议对比

| 特性 | Modbus TCP | Modbus RTU | Modbus ASCII |
|------|-----------|-----------|--------------|
| **传输介质** | 以太网 | 串口 | 串口 |
| **编码方式** | 二进制 | 二进制 | ASCII 十六进制 |
| **校验方式** | 无 | CRC-16 | LRC-8 |
| **帧头** | MBAP 报头 | 无 | `:` 字符 |
| **帧尾** | 无 | 无 | `\r\n` |
| **帧大小** | 较小 | 较小 | 约 RTU 的 1.5 倍 |
| **容错性** | 一般 | 一般 | 优秀（ASCII 编码） |
| **传输距离** | 通常 <100m | <1000m | >1000m |
| **适用场景** | 局域网设备 | 工业环保距离 | 长距离恶劣环境 |
| **调试难度** | 需要专门工具 | 需要专门工具 | 可直接查看 ASCII 内容 |
| **实时性** | 优秀 | 优秀 | 较好 |

## 路线图

- [x] Modbus TCP 基础实现
- [x] 扩展数据类型支持 (float, double, int32, uint32)
- [x] 字节序配置
- [x] 1基地址模式
- [x] 原始字节访问
- [x] Modbus RTU 支持
- [x] Modbus ASCII 支持 (LRC-8 校验，ASCII 编码)
- [ ] Modbus 服务端实现
- [ ] 重连机制
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
