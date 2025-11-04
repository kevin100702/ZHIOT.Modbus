using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusPduBuilderTests
{
    [TestMethod]
    public void BuildReadHoldingRegistersRequest_ShouldGenerateCorrectPdu()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[256];
        ushort startAddress = 0x0064; // 100
        ushort quantity = 10;

        // Act
        int length = ModbusPduBuilder.BuildReadHoldingRegistersRequest(buffer, startAddress, quantity);

        // Assert
        Assert.AreEqual(5, length);
        Assert.AreEqual(0x03, buffer[0]); // Function code
        Assert.AreEqual(0x00, buffer[1]); // Start address high byte
        Assert.AreEqual(0x64, buffer[2]); // Start address low byte
        Assert.AreEqual(0x00, buffer[3]); // Quantity high byte
        Assert.AreEqual(0x0A, buffer[4]); // Quantity low byte
    }

    [TestMethod]
    public void BuildWriteSingleRegisterRequest_ShouldGenerateCorrectPdu()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[256];
        ushort address = 0x0001;
        ushort value = 0x1234;

        // Act
        int length = ModbusPduBuilder.BuildWriteSingleRegisterRequest(buffer, address, value);

        // Assert
        Assert.AreEqual(5, length);
        Assert.AreEqual(0x06, buffer[0]); // Function code
        Assert.AreEqual(0x00, buffer[1]); // Address high byte
        Assert.AreEqual(0x01, buffer[2]); // Address low byte
        Assert.AreEqual(0x12, buffer[3]); // Value high byte
        Assert.AreEqual(0x34, buffer[4]); // Value low byte
    }

    [TestMethod]
    public void BuildWriteMultipleRegistersRequest_ShouldGenerateCorrectPdu()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[256];
        ushort startAddress = 0x0001;
        ushort[] values = { 0x000A, 0x0102 };

        // Act
        int length = ModbusPduBuilder.BuildWriteMultipleRegistersRequest(buffer, startAddress, values);

        // Assert
        Assert.AreEqual(10, length); // Fixed: 1 (func) + 2 (addr) + 2 (qty) + 1 (byte count) + 4 (data) = 10
        Assert.AreEqual(0x10, buffer[0]); // Function code
        Assert.AreEqual(0x00, buffer[1]); // Start address high byte
        Assert.AreEqual(0x01, buffer[2]); // Start address low byte
        Assert.AreEqual(0x00, buffer[3]); // Quantity high byte
        Assert.AreEqual(0x02, buffer[4]); // Quantity low byte (2 registers)
        Assert.AreEqual(0x04, buffer[5]); // Byte count (4 bytes)
        Assert.AreEqual(0x00, buffer[6]); // First value high byte
        Assert.AreEqual(0x0A, buffer[7]); // First value low byte
        Assert.AreEqual(0x01, buffer[8]); // Second value high byte
        Assert.AreEqual(0x02, buffer[9]); // Second value low byte
    }

    [TestMethod]
    public void BuildMbapHeader_ShouldGenerateCorrectHeader()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[7];
        ushort transactionId = 0x0001;
        byte unitId = 0x11;
        ushort pduLength = 5;

        // Act
        ModbusPduBuilder.BuildMbapHeader(buffer, transactionId, unitId, pduLength);

        // Assert
        Assert.AreEqual(0x00, buffer[0]); // Transaction ID high byte
        Assert.AreEqual(0x01, buffer[1]); // Transaction ID low byte
        Assert.AreEqual(0x00, buffer[2]); // Protocol ID high byte
        Assert.AreEqual(0x00, buffer[3]); // Protocol ID low byte
        Assert.AreEqual(0x00, buffer[4]); // Length high byte
        Assert.AreEqual(0x06, buffer[5]); // Length low byte (pduLength + 1)
        Assert.AreEqual(0x11, buffer[6]); // Unit ID
    }
}
