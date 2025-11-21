using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusRtuAduBuilderTests
{
    [TestMethod]
    public void BuildAdu_ValidPdu_ReturnsCompleteFrame()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A }; // Read Holding Registers
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.AreEqual(1 + pdu.Length + 2, length); // SlaveId + PDU + CRC
        Assert.AreEqual(slaveId, buffer[0]);
        
        // 验证 PDU 复制正确
        for (int i = 0; i < pdu.Length; i++)
        {
            Assert.AreEqual(pdu[i], buffer[1 + i]);
        }
        
        // 验证 CRC 存在且正确
        var frame = buffer.Slice(0, length);
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void BuildAdu_ReadCoilsRequest_CreatesValidFrame()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x01, 0x00, 0x00, 0x00, 0x10 }; // Read 16 coils from address 0
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.AreEqual(8, length); // 1 + 5 + 2
        var frame = buffer.Slice(0, length);
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void BuildAdu_WriteSingleRegister_CreatesValidFrame()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x06, 0x00, 0x01, 0x12, 0x34 }; // Write 0x1234 to register 1
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.AreEqual(8, length); // 1 + 5 + 2
        var frame = buffer.Slice(0, length);
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void BuildAdu_LargePdu_CreatesValidFrame()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = new byte[250]; // Large PDU
        pdu[0] = 0x10; // Write Multiple Registers
        Span<byte> buffer = stackalloc byte[260];

        // Act
        int length = ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.AreEqual(1 + pdu.Length + 2, length);
        var frame = buffer.Slice(0, length);
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }

    [TestMethod]
    public void BuildAdu_BufferTooSmall_ThrowsException()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> buffer = stackalloc byte[5]; // Too small

        // Act & Assert
        try
        {
            ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);
            Assert.Fail("Expected ArgumentException was not thrown");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void BuildAdu_DifferentSlaveIds_CreatesCorrectFrames()
    {
        // Arrange
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x01 };
        Span<byte> buffer1 = stackalloc byte[256];
        Span<byte> buffer2 = stackalloc byte[256];

        // Act
        int length1 = ModbusRtuAduBuilder.BuildAdu(buffer1, 0x01, pdu);
        int length2 = ModbusRtuAduBuilder.BuildAdu(buffer2, 0x0F, pdu);

        // Assert
        Assert.AreEqual(0x01, buffer1[0]);
        Assert.AreEqual(0x0F, buffer2[0]);
        Assert.IsTrue(ModbusCrc16.Verify(buffer1.Slice(0, length1)));
        Assert.IsTrue(ModbusCrc16.Verify(buffer2.Slice(0, length2)));
    }

    [TestMethod]
    public void BuildAdu_EmptyPdu_CreatesValidFrame()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = Array.Empty<byte>();
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusRtuAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.AreEqual(3, length); // SlaveId + CRC
        var frame = buffer.Slice(0, length);
        Assert.IsTrue(ModbusCrc16.Verify(frame));
    }
}
