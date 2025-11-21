using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusAsciiAduBuilderTests
{
    [TestMethod]
    public void BuildAdu_SimpleRequest_BuildsCorrectAdu()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusAsciiAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.IsTrue(length > 0);
        Assert.AreEqual((byte)':', buffer[0]);
        Assert.AreEqual((byte)'\r', buffer[length - 2]);
        Assert.AreEqual((byte)'\n', buffer[length - 1]);
    }

    [TestMethod]
    public void BuildAdu_WithLrc_IncludesCorrectChecksum()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusAsciiAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert - verify LRC by parsing with parser
        bool lrcValid = ModbusAsciiAduParser.VerifyLrc(buffer.Slice(0, length));
        Assert.IsTrue(lrcValid);
    }

    [TestMethod]
    public void BuildAdu_BufferTooSmall_ThrowsArgumentException()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> buffer = stackalloc byte[5]; // Too small

        // Act & Assert
        try
        {
            ModbusAsciiAduBuilder.BuildAdu(buffer, slaveId, pdu);
            Assert.Fail("Should have thrown ArgumentException");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void BuildAdu_SingleBytePdu_BuildsValidAdu()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03 }; // Minimal PDU: just function code
        Span<byte> buffer = stackalloc byte[256];

        // Act
        int length = ModbusAsciiAduBuilder.BuildAdu(buffer, slaveId, pdu);

        // Assert
        Assert.IsTrue(length > 0);
        bool lrcValid = ModbusAsciiAduParser.VerifyLrc(buffer.Slice(0, length));
        Assert.IsTrue(lrcValid);
    }
}
