using ZHIOT.Modbus;

Console.WriteLine("=== ZHIOT.Modbus Sample ===");
Console.WriteLine();

// 示例 1: 连接到 Modbus TCP 服务器并读取保持寄存器
await RunModbusTcpSample();

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
