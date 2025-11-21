using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusAsciiAduParserTests
{
    [TestMethod]
    public void ExtractPdu_ValidAdu_ExtractsPduCorrectly()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> aduBuffer = stackalloc byte[256];
        int aduLength = ModbusAsciiAduBuilder.BuildAdu(aduBuffer, slaveId, pdu);

        // Act
        byte[] extractedPdu = ModbusAsciiAduParser.ExtractPdu(aduBuffer.Slice(0, aduLength));

        // Assert
        CollectionAssert.AreEqual(pdu, extractedPdu);
    }

    [TestMethod]
    public void ExtractSlaveId_ValidAdu_ExtractsSlaveIdCorrectly()
    {
        // Arrange
        byte slaveId = 0x05;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> aduBuffer = stackalloc byte[256];
        int aduLength = ModbusAsciiAduBuilder.BuildAdu(aduBuffer, slaveId, pdu);

        // Act
        byte extractedSlaveId = ModbusAsciiAduParser.ExtractSlaveId(aduBuffer.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(slaveId, extractedSlaveId);
    }

    [TestMethod]
    public void VerifyLrc_ValidAdu_ReturnsTrue()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> aduBuffer = stackalloc byte[256];
        int aduLength = ModbusAsciiAduBuilder.BuildAdu(aduBuffer, slaveId, pdu);

        // Act
        bool result = ModbusAsciiAduParser.VerifyLrc(aduBuffer.Slice(0, aduLength));

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyLrc_InvalidAdu_ReturnsFalse()
    {
        // Arrange
        byte[] invalidAdu = System.Text.Encoding.ASCII.GetBytes(":0103000000AAFFFF\r\n");

        // Act
        bool result = ModbusAsciiAduParser.VerifyLrc(invalidAdu);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ExtractPdu_MissingFrameHeader_ThrowsException()
    {
        // Arrange
        byte[] invalidAdu = System.Text.Encoding.ASCII.GetBytes("0103000000AAFFFF\r\n");

        // Act & Assert
        try
        {
            ModbusAsciiAduParser.ExtractPdu(invalidAdu);
            Assert.Fail("Should have thrown InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ExtractPdu_MissingFrameTrailer_ThrowsException()
    {
        // Arrange
        byte[] invalidAdu = System.Text.Encoding.ASCII.GetBytes(":0103000000AAFFFF");

        // Act & Assert
        try
        {
            ModbusAsciiAduParser.ExtractPdu(invalidAdu);
            Assert.Fail("Should have thrown InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ExtractPdu_InvalidLrc_ThrowsException()
    {
        // Arrange
        byte[] invalidAdu = System.Text.Encoding.ASCII.GetBytes(":0103000000AAFFFF\r\n");

        // Act & Assert
        try
        {
            ModbusAsciiAduParser.ExtractPdu(invalidAdu);
            Assert.Fail("Should have thrown InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ExtractPdu_TooShort_ThrowsException()
    {
        // Arrange
        byte[] shortAdu = System.Text.Encoding.ASCII.GetBytes(":01\r\n");

        // Act & Assert
        try
        {
            ModbusAsciiAduParser.ExtractPdu(shortAdu);
            Assert.Fail("Should have thrown InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }
}
