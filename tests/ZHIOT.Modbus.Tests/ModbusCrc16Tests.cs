using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusCrc16Tests
{
    [TestMethod]
    public void Calculate_ValidFrame_ReturnsCorrectCrc()
    {
        // Arrange: 使用已知的 Modbus RTU 帧
        // 示例: 01 03 00 00 00 0A -> CRC: 0xCDC5 (CD low byte, C5 high byte)
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        ushort expectedCrc = 0xCDC5;

        // Act
        ushort actualCrc = ModbusCrc16.Calculate(data);

        // Assert
        Assert.AreEqual(expectedCrc, actualCrc);
    }

    [TestMethod]
    public void Calculate_EmptyData_ReturnsInitialValue()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        ushort crc = ModbusCrc16.Calculate(data);

        // Assert
        Assert.AreEqual((ushort)0xFFFF, crc);
    }

    [TestMethod]
    public void Calculate_SingleByte_ReturnsCorrectCrc()
    {
        // Arrange
        byte[] data = { 0x01 };

        // Act
        ushort crc = ModbusCrc16.Calculate(data);

        // Assert
        Assert.AreNotEqual((ushort)0xFFFF, crc);
        Assert.IsTrue(crc > 0);
    }

    [TestMethod]
    public void Verify_ValidFrame_ReturnsTrue()
    {
        // Arrange: 完整的 Modbus RTU 帧（包含正确的 CRC，小端序：CD C5）
        // 01 03 00 00 00 0A CD C5
        byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };

        // Act
        bool isValid = ModbusCrc16.Verify(frame);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Verify_InvalidCrc_ReturnsFalse()
    {
        // Arrange: 包含错误 CRC 的帧
        byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xFF, 0xFF };

        // Act
        bool isValid = ModbusCrc16.Verify(frame);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void Verify_TooShort_ReturnsFalse()
    {
        // Arrange: 长度不足的帧
        byte[] frame = { 0x01, 0x03 };

        // Act
        bool isValid = ModbusCrc16.Verify(frame);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void Calculate_ReadCoilsRequest_ReturnsCorrectCrc()
    {
        // Arrange: 读取线圈请求
        // Slave ID: 0x01, Function Code: 0x01, Start: 0x0000, Quantity: 0x0010
        byte[] data = { 0x01, 0x01, 0x00, 0x00, 0x00, 0x10 };
        
        // Act
        ushort crc = ModbusCrc16.Calculate(data);
        
        // Assert
        Assert.IsTrue(crc > 0);
        
        // Verify round-trip
        byte[] frame = new byte[data.Length + 2];
        data.CopyTo(frame, 0);
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void Calculate_WriteRegisterRequest_ReturnsCorrectCrc()
    {
        // Arrange: 写单个寄存器请求
        // Slave ID: 0x01, Function Code: 0x06, Address: 0x0001, Value: 0x1234
        byte[] data = { 0x01, 0x06, 0x00, 0x01, 0x12, 0x34 };
        
        // Act
        ushort crc = ModbusCrc16.Calculate(data);
        
        // Assert
        Assert.IsTrue(crc > 0);
        
        // Verify round-trip
        byte[] frame = new byte[data.Length + 2];
        data.CopyTo(frame, 0);
        frame[6] = (byte)(crc & 0xFF);
        frame[7] = (byte)(crc >> 8);
        
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void Calculate_ConsistentResults()
    {
        // Arrange
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        
        // Act
        ushort crc1 = ModbusCrc16.Calculate(data);
        ushort crc2 = ModbusCrc16.Calculate(data);
        ushort crc3 = ModbusCrc16.Calculate(data);
        
        // Assert
        Assert.AreEqual(crc1, crc2);
        Assert.AreEqual(crc2, crc3);
    }

    [TestMethod]
    public void Calculate_EvenVariant_ReturnsCorrectCrc()
    {
        // Arrange: 使用已知的 Modbus 标准帧
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        ushort expectedCrc = 0xCDC5; // 已知正确值

        // Act
        ushort actualCrc = ModbusCrc16.Calculate(data, Crc16Variant.Even);

        // Assert
        Assert.AreEqual(expectedCrc, actualCrc);
    }

    [TestMethod]
    public void Calculate_OddVariant_ReturnsInvertedCrc()
    {
        // Arrange
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        ushort evenCrc = ModbusCrc16.Calculate(data, Crc16Variant.Even);
        
        // Act
        ushort oddCrc = ModbusCrc16.Calculate(data, Crc16Variant.Odd);

        // Assert
        // 奇校验 = 偶校验取反
        Assert.AreEqual((ushort)(evenCrc ^ 0xFFFF), oddCrc);
    }

    [TestMethod]
    public void Verify_EvenVariant_ValidFrame_ReturnsTrue()
    {
        // Arrange: 标准 Modbus 偶校验帧
        byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
        
        // Act
        bool isValid = ModbusCrc16.Verify(frame, Crc16Variant.Even);
        
        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Verify_OddVariant_ValidFrame_ReturnsTrue()
    {
        // Arrange: 构建奇校验帧
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        ushort oddCrc = ModbusCrc16.Calculate(data, Crc16Variant.Odd);
        
        byte[] frame = new byte[data.Length + 2];
        data.CopyTo(frame, 0);
        frame[data.Length] = (byte)(oddCrc & 0xFF);
        frame[data.Length + 1] = (byte)(oddCrc >> 8);
        
        // Act
        bool isValid = ModbusCrc16.Verify(frame, Crc16Variant.Odd);
        
        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void Verify_WrongVariant_ReturnsFalse()
    {
        // Arrange: 偶校验帧用奇校验验证应该失败
        byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD };
        
        // Act
        bool isValid = ModbusCrc16.Verify(frame, Crc16Variant.Odd);
        
        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void Calculate_BothVariants_DifferentResults()
    {
        // Arrange
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        
        // Act
        ushort evenCrc = ModbusCrc16.Calculate(data, Crc16Variant.Even);
        ushort oddCrc = ModbusCrc16.Calculate(data, Crc16Variant.Odd);
        
        // Assert
        Assert.AreNotEqual(evenCrc, oddCrc);
    }
}
