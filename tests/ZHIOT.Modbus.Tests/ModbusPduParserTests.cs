using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusPduParserTests
{
    [TestMethod]
    public void ParseReadHoldingRegistersResponse_ShouldParseCorrectly()
    {
        // Arrange
        byte[] pdu = { 0x03, 0x04, 0x00, 0x0A, 0x01, 0x02 }; // Function code, byte count, two registers

        // Act
        var result = ModbusPduParser.ParseReadHoldingRegistersResponse(pdu);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(0x000A, result[0]);
        Assert.AreEqual(0x0102, result[1]);
    }

    [TestMethod]
    public void ParseWriteSingleRegisterResponse_ShouldParseCorrectly()
    {
        // Arrange
        byte[] pdu = { 0x06, 0x00, 0x01, 0x12, 0x34 }; // Function code, address, value

        // Act
        var (address, value) = ModbusPduParser.ParseWriteSingleRegisterResponse(pdu);

        // Assert
        Assert.AreEqual(0x0001, address);
        Assert.AreEqual(0x1234, value);
    }

    [TestMethod]
    public void ParseReadCoilsResponse_ShouldParseCorrectly()
    {
        // Arrange
        byte[] pdu = { 0x01, 0x02, 0xCD, 0x01 }; // Function code, byte count, two bytes of coil data
        ushort requestedQuantity = 10;

        // Act
        var result = ModbusPduParser.ParseReadCoilsResponse(pdu, requestedQuantity);

        // Assert
        Assert.AreEqual(10, result.Length);
        Assert.IsTrue(result[0]); // bit 0 of 0xCD
        Assert.IsFalse(result[1]); // bit 1 of 0xCD
        Assert.IsTrue(result[2]); // bit 2 of 0xCD
        Assert.IsTrue(result[3]); // bit 3 of 0xCD
        Assert.IsFalse(result[4]); // bit 4 of 0xCD
        Assert.IsFalse(result[5]); // bit 5 of 0xCD
        Assert.IsTrue(result[6]); // bit 6 of 0xCD
        Assert.IsTrue(result[7]); // bit 7 of 0xCD
        Assert.IsTrue(result[8]); // bit 0 of 0x01
        Assert.IsFalse(result[9]); // bit 1 of 0x01
    }

    [TestMethod]
    public void ParseMbapHeader_ShouldParseCorrectly()
    {
        // Arrange
        byte[] buffer = { 0x00, 0x01, 0x00, 0x00, 0x00, 0x06, 0x11 };

        // Act
        var header = ModbusPduParser.ParseMbapHeader(buffer);

        // Assert
        Assert.AreEqual(0x0001, header.TransactionId);
        Assert.AreEqual(0x0000, header.ProtocolId);
        Assert.AreEqual(0x0006, header.Length);
        Assert.AreEqual(0x11, header.UnitId);
    }

    [TestMethod]
    public void CheckForException_ShouldThrowWhenExceptionFlagSet()
    {
        // Arrange
        byte[] pdu = { 0x83, 0x02 }; // Exception response: function code 0x03 + 0x80, exception code 0x02

        // Act & Assert
        try
        {
            ModbusPduParser.CheckForException(pdu);
            Assert.Fail("Expected ModbusException was not thrown");
        }
        catch (ModbusException ex)
        {
            Assert.AreEqual(ModbusExceptionCode.IllegalDataAddress, ex.ExceptionCode);
        }
    }

    [TestMethod]
    public void CheckForException_ShouldNotThrowForNormalResponse()
    {
        // Arrange
        byte[] pdu = { 0x03, 0x04, 0x00, 0x0A, 0x01, 0x02 };

        // Act & Assert - Should not throw
        ModbusPduParser.CheckForException(pdu);
    }

    [TestMethod]
    public void ParseRegistersResponsePayload_ShouldReturnCorrectSpan()
    {
        // Arrange
        byte[] pdu = { 0x03, 0x06, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC };

        // Act
        var payload = ModbusPduParser.ParseRegistersResponsePayload(pdu);

        // Assert
        Assert.AreEqual(6, payload.Length);
        Assert.AreEqual(0x12, payload[0]);
        Assert.AreEqual(0x34, payload[1]);
        Assert.AreEqual(0x56, payload[2]);
        Assert.AreEqual(0x78, payload[3]);
        Assert.AreEqual(0x9A, payload[4]);
        Assert.AreEqual(0xBC, payload[5]);
    }

    [TestMethod]
    public void ParseRegistersResponsePayload_ShouldThrowOnIncompletePdu()
    {
        // Arrange
        byte[] pdu = { 0x03, 0x06, 0x12, 0x34 }; // Claims 6 bytes but only has 2

        // Act & Assert
        try
        {
            ModbusPduParser.ParseRegistersResponsePayload(pdu);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }
}
