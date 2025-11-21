using System.IO.Ports;
using ZHIOT.Modbus;
using ZHIOT.Modbus.Core;

Console.WriteLine("=== ZHIOT.Modbus Sample - 高性能扩展功能演示 ===");
Console.WriteLine();

// 示例 1: 连接到 Modbus TCP 服务器并读取保持寄存器
await RunModbusTcpSample();

// 示例 2: 演示新的扩展数据类型功能
await RunExtendedDataTypesSample();

// 示例 3: 演示字节序和1基地址功能
await RunByteOrderAndAddressingSample();

// 示例 4: Modbus RTU 串口通信
await RunModbusRtuSample();

Console.WriteLine();
Console.WriteLine("按任意键退出...");
Console.ReadKey();

static async Task RunModbusTcpSample()
{
    Console.WriteLine("示例: Modbus TCP 客户端");
    Console.WriteLine("------------------------");

    // 创建 Modbus TCP 客户端
    // 注意: 这里使用的是示例地址，实际使用时需要替换为真实的 Modbus TCP 设备地址
    await using var client = ModbusClientFactory.CreateTcpClient("127.0.0.1", 502);

    try
    {
        // 连接到服务器
        Console.WriteLine("正在连接到 Modbus TCP 服务器...");
        await client.ConnectAsync();
        Console.WriteLine("已连接!");
        Console.WriteLine();

        byte slaveId = 1;

        // 示例 1: 读取保持寄存器
        Console.WriteLine("1. 读取保持寄存器 (地址 0-9):");
        try
        {
            var registers = await client.ReadHoldingRegistersAsync(slaveId, startAddress: 0, quantity: 10);
            Console.WriteLine($"   读取到 {registers.Length} 个寄存器:");
            for (int i = 0; i < registers.Length; i++)
            {
                Console.WriteLine($"   寄存器 {i}: {registers[i]} (0x{registers[i]:X4})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   读取失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 2: 写单个寄存器
        Console.WriteLine("2. 写单个寄存器 (地址 0, 值 1234):");
        try
        {
            await client.WriteSingleRegisterAsync(slaveId, address: 0, value: 1234);
            Console.WriteLine("   写入成功!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   写入失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 3: 写多个寄存器
        Console.WriteLine("3. 写多个寄存器 (地址 10-12):");
        try
        {
            ushort[] values = { 100, 200, 300 };
            await client.WriteMultipleRegistersAsync(slaveId, startAddress: 10, values: values);
            Console.WriteLine($"   写入 {values.Length} 个寄存器成功!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   写入失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 4: 读取线圈
        Console.WriteLine("4. 读取线圈 (地址 0-15):");
        try
        {
            var coils = await client.ReadCoilsAsync(slaveId, startAddress: 0, quantity: 16);
            Console.WriteLine($"   读取到 {coils.Length} 个线圈:");
            for (int i = 0; i < coils.Length; i++)
            {
                Console.WriteLine($"   线圈 {i}: {(coils[i] ? "ON" : "OFF")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   读取失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 5: 写单个线圈
        Console.WriteLine("5. 写单个线圈 (地址 0, 值 ON):");
        try
        {
            await client.WriteSingleCoilAsync(slaveId, address: 0, value: true);
            Console.WriteLine("   写入成功!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   写入失败: {ex.Message}");
        }

        // 断开连接
        await client.DisconnectAsync();
        Console.WriteLine();
        Console.WriteLine("已断开连接");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
        Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
    }
}

static async Task RunExtendedDataTypesSample()
{
    Console.WriteLine();
    Console.WriteLine("示例: 扩展数据类型读写");
    Console.WriteLine("------------------------");

    await using var client = ModbusClientFactory.CreateTcpClient("127.0.0.1", 502);

    try
    {
        await client.ConnectAsync();
        Console.WriteLine("已连接!");
        Console.WriteLine();

        byte slaveId = 1;

        // 示例 1: 读写 Float 数据
        Console.WriteLine("1. 读写 Float 数据:");
        try
        {
            // 写入 float 数组
            float[] floatValues = { 3.14159f, -123.456f, 0.0f };
            await client.WriteMultipleRegistersFloatAsync(slaveId, startAddress: 0, values: floatValues);
            Console.WriteLine($"   写入 {floatValues.Length} 个 float 值成功!");

            // 读取 float 数组
            var readFloats = await client.ReadHoldingRegistersFloatAsync(slaveId, startAddress: 0, quantity: 6);
            Console.WriteLine($"   读取到 {readFloats.Length} 个 float 值:");
            for (int i = 0; i < readFloats.Length; i++)
            {
                Console.WriteLine($"   Float {i}: {readFloats[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 2: 读写 Int32 数据
        Console.WriteLine("2. 读写 Int32 数据:");
        try
        {
            int[] intValues = { 123456, -987654, 0 };
            await client.WriteMultipleRegistersInt32Async(slaveId, startAddress: 10, values: intValues);
            Console.WriteLine($"   写入 {intValues.Length} 个 int32 值成功!");

            var readInts = await client.ReadHoldingRegistersInt32Async(slaveId, startAddress: 10, quantity: 6);
            Console.WriteLine($"   读取到 {readInts.Length} 个 int32 值:");
            for (int i = 0; i < readInts.Length; i++)
            {
                Console.WriteLine($"   Int32 {i}: {readInts[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 3: 读写 Double 数据
        Console.WriteLine("3. 读写 Double 数据:");
        try
        {
            double[] doubleValues = { Math.PI, Math.E };
            await client.WriteMultipleRegistersDoubleAsync(slaveId, startAddress: 20, values: doubleValues);
            Console.WriteLine($"   写入 {doubleValues.Length} 个 double 值成功!");

            var readDoubles = await client.ReadHoldingRegistersDoubleAsync(slaveId, startAddress: 20, quantity: 8);
            Console.WriteLine($"   读取到 {readDoubles.Length} 个 double 值:");
            for (int i = 0; i < readDoubles.Length; i++)
            {
                Console.WriteLine($"   Double {i}: {readDoubles[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 4: 原始字节访问
        Console.WriteLine("4. 原始字节访问:");
        try
        {
            byte[] rawBytes = { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
            await client.WriteMultipleRegistersBytesAsync(slaveId, startAddress: 30, values: rawBytes);
            Console.WriteLine($"   写入 {rawBytes.Length} 字节成功!");

            var readBytes = await client.ReadHoldingRegistersBytesAsync(slaveId, startAddress: 30, quantity: 4);
            Console.WriteLine($"   读取到 {readBytes.Length} 字节:");
            Console.WriteLine($"   数据: {BitConverter.ToString(readBytes)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }

        await client.DisconnectAsync();
        Console.WriteLine();
        Console.WriteLine("已断开连接");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
    }
}

static async Task RunByteOrderAndAddressingSample()
{
    Console.WriteLine();
    Console.WriteLine("示例: 字节序和地址模式");
    Console.WriteLine("------------------------");

    await using var client = ModbusClientFactory.CreateTcpClient("127.0.0.1", 502);

    try
    {
        await client.ConnectAsync();
        Console.WriteLine("已连接!");
        Console.WriteLine();

        byte slaveId = 1;

        // 示例 1: 不同字节序的 Float 读写
        Console.WriteLine("1. 不同字节序的 Float 读写:");
        try
        {
            float testValue = 3.14159f;

            // 使用大端字节序（Modbus 标准）
            client.ByteOrder = ByteOrder.BigEndian;
            await client.WriteMultipleRegistersFloatAsync(slaveId, 0, new[] { testValue });
            var bigEndianResult = await client.ReadHoldingRegistersFloatAsync(slaveId, 0, 2);
            Console.WriteLine($"   BigEndian: {bigEndianResult[0]}");

            // 使用小端字节序
            client.ByteOrder = ByteOrder.LittleEndian;
            await client.WriteMultipleRegistersFloatAsync(slaveId, 2, new[] { testValue });
            var littleEndianResult = await client.ReadHoldingRegistersFloatAsync(slaveId, 2, 2);
            Console.WriteLine($"   LittleEndian: {littleEndianResult[0]}");

            // 使用大端字节序 + 字交换
            client.ByteOrder = ByteOrder.BigEndianSwap;
            await client.WriteMultipleRegistersFloatAsync(slaveId, 4, new[] { testValue });
            var bigEndianSwapResult = await client.ReadHoldingRegistersFloatAsync(slaveId, 4, 2);
            Console.WriteLine($"   BigEndianSwap: {bigEndianSwapResult[0]}");

            // 恢复默认字节序
            client.ByteOrder = ByteOrder.BigEndian;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 2: 1基地址模式
        Console.WriteLine("2. 1基地址模式:");
        try
        {
            // 启用 1 基地址（用户地址从 1 开始）
            client.IsOneBasedAddressing = true;
            Console.WriteLine("   已启用 1 基地址模式");

            // 现在可以使用从 1 开始的地址，客户端会自动转换为 0 基地址
            ushort[] values = { 111, 222, 333 };
            await client.WriteMultipleRegistersAsync(slaveId, startAddress: 1, values: values);
            Console.WriteLine("   写入地址 1-3 成功（实际使用 0-2）");

            var readValues = await client.ReadHoldingRegistersAsync(slaveId, startAddress: 1, quantity: 3);
            Console.WriteLine($"   读取地址 1-3（实际使用 0-2）:");
            for (int i = 0; i < readValues.Length; i++)
            {
                Console.WriteLine($"   地址 {i + 1}: {readValues[i]}");
            }

            // 恢复 0 基地址
            client.IsOneBasedAddressing = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }

        await client.DisconnectAsync();
        Console.WriteLine();
        Console.WriteLine("已断开连接");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
    }
}

static async Task RunModbusRtuSample()
{
    Console.WriteLine();
    Console.WriteLine("=== Modbus RTU 示例 ===");
    Console.WriteLine("------------------------");
    Console.WriteLine("注意: 这个示例需要连接真实的 Modbus RTU 设备或串口模拟器");
    Console.WriteLine();

    // 创建 RTU 客户端
    // 注意: 将 COM1 替换为实际的串口名称（Windows: COM1, COM2... / Linux: /dev/ttyUSB0, /dev/ttyS0...）
    await using var client = ModbusClientFactory.CreateRtuClient(
        portName: "COM1",
        baudRate: 9600,
        parity: Parity.None,
        dataBits: 8,
        stopBits: StopBits.One
    );

    try
    {
        // 连接串口
        Console.WriteLine("正在打开串口 COM1 (9600, 8N1)...");
        await client.ConnectAsync();
        Console.WriteLine("串口已打开!");
        Console.WriteLine();

        byte slaveId = 1;

        // 示例 1: 读取保持寄存器
        Console.WriteLine("1. 读取保持寄存器 (地址 0-9):");
        try
        {
            var registers = await client.ReadHoldingRegistersAsync(slaveId, startAddress: 0, quantity: 10);
            Console.WriteLine($"   读取到 {registers.Length} 个寄存器:");
            for (int i = 0; i < registers.Length; i++)
            {
                Console.WriteLine($"   寄存器 {i}: {registers[i]} (0x{registers[i]:X4})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   读取失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 2: 写单个寄存器
        Console.WriteLine("2. 写单个寄存器 (地址 0, 值 1234):");
        try
        {
            await client.WriteSingleRegisterAsync(slaveId, address: 0, value: 1234);
            Console.WriteLine("   写入成功!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   写入失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 3: 读写 Float 数据
        Console.WriteLine("3. 读写 Float 数据:");
        try
        {
            float[] floatValues = { 3.14159f, -123.456f };
            await client.WriteMultipleRegistersFloatAsync(slaveId, startAddress: 10, values: floatValues);
            Console.WriteLine($"   写入 {floatValues.Length} 个 float 值成功!");

            var readFloats = await client.ReadHoldingRegistersFloatAsync(slaveId, startAddress: 10, quantity: 4);
            Console.WriteLine($"   读取到 {readFloats.Length} 个 float 值:");
            for (int i = 0; i < readFloats.Length; i++)
            {
                Console.WriteLine($"   Float {i}: {readFloats[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   操作失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 4: 读取线圈
        Console.WriteLine("4. 读取线圈 (地址 0-15):");
        try
        {
            var coils = await client.ReadCoilsAsync(slaveId, startAddress: 0, quantity: 16);
            Console.WriteLine($"   读取到 {coils.Length} 个线圈:");
            for (int i = 0; i < coils.Length; i++)
            {
                Console.WriteLine($"   线圈 {i}: {(coils[i] ? "ON" : "OFF")}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   读取失败: {ex.Message}");
        }
        Console.WriteLine();

        // 示例 5: 使用自定义串口配置
        Console.WriteLine("5. 自定义串口配置示例:");
        Console.WriteLine("   可以使用 SerialPortSettings 类进行更详细的配置:");
        Console.WriteLine("   - 不同的波特率 (1200, 2400, 4800, 9600, 19200, 38400, 57600, 115200)");
        Console.WriteLine("   - 不同的校验位 (None, Even, Odd, Mark, Space)");
        Console.WriteLine("   - 不同的数据位 (7, 8)");
        Console.WriteLine("   - 不同的停止位 (One, Two, OnePointFive)");
        Console.WriteLine("   - 自定义超时设置");

        await client.DisconnectAsync();
        Console.WriteLine();
        Console.WriteLine("串口已关闭");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"错误: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"内部错误: {ex.InnerException.Message}");
        }
    }
}
