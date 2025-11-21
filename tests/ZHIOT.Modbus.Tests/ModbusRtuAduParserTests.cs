using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusRtuAduParserTests
{
    [TestMethod]
    public void ExtractPdu_ValidAdu_ReturnsPdu()
    {
        // Arrange: 构建一个有效的 RTU ADU
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(pdu.Length, extractedPdu.Length);
        for (int i = 0; i < pdu.Length; i++)
        {
            Assert.AreEqual(pdu[i], extractedPdu[i]);
        }
    }

    [TestMethod]
    public void ExtractSlaveId_ValidAdu_ReturnsSlaveId()
    {
        // Arrange
        byte expectedSlaveId = 0x0F;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x01 };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, expectedSlaveId, pdu);

        // Act
        byte actualSlaveId = ModbusRtuAduParser.ExtractSlaveId(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(expectedSlaveId, actualSlaveId);
    }

    [TestMethod]
    public void ExtractPdu_TooShort_ThrowsException()
    {
        // Arrange
        byte[] adu = { 0x01, 0x03, 0x00 }; // Too short

        // Act & Assert
        try
        {
            ModbusRtuAduParser.ExtractPdu(adu);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ExtractPdu_InvalidCrc_ThrowsException()
    {
        // Arrange: 构建一个 CRC 错误的 ADU
        byte[] adu = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xFF, 0xFF };

        // Act & Assert
        try
        {
            ModbusRtuAduParser.ExtractPdu(adu);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void VerifyCrc_ValidFrame_ReturnsTrue()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

        // Act
        bool isValid = ModbusRtuAduParser.VerifyCrc(adu.Slice(0, aduLength));

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifyCrc_InvalidCrc_ReturnsFalse()
    {
        // Arrange
        byte[] adu = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xFF, 0xFF };

        // Act
        bool isValid = ModbusRtuAduParser.VerifyCrc(adu);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void VerifyCrc_TooShort_ReturnsFalse()
    {
        // Arrange
        byte[] adu = { 0x01, 0x03 };

        // Act
        bool isValid = ModbusRtuAduParser.VerifyCrc(adu);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ExtractPdu_ReadCoilsResponse_ReturnsCorrectPdu()
    {
        // Arrange: 模拟读取线圈响应
        byte slaveId = 0x01;
        byte[] pdu = { 0x01, 0x02, 0xFF, 0x00 }; // Function code, byte count, 2 bytes of data
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(pdu.Length, extractedPdu.Length);
        Assert.AreEqual(0x01, extractedPdu[0]); // Function code
        Assert.AreEqual(0x02, extractedPdu[1]); // Byte count
    }

    [TestMethod]
    public void ExtractPdu_WriteRegisterResponse_ReturnsCorrectPdu()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x06, 0x00, 0x01, 0x12, 0x34 }; // Write register response
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(pdu.Length, extractedPdu.Length);
        for (int i = 0; i < pdu.Length; i++)
        {
            Assert.AreEqual(pdu[i], extractedPdu[i]);
        }
    }

    [TestMethod]
    public void ExtractPdu_ExceptionResponse_ReturnsCorrectPdu()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x83, 0x02 }; // Exception response (function code with high bit set + exception code)
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(pdu.Length, extractedPdu.Length);
        Assert.AreEqual(0x83, extractedPdu[0]);
        Assert.AreEqual(0x02, extractedPdu[1]);
    }

    [TestMethod]
    public void ExtractSlaveId_EmptyAdu_ThrowsException()
    {
        // Arrange
        byte[] adu = Array.Empty<byte>();

        // Act & Assert
        try
        {
            ModbusRtuAduParser.ExtractSlaveId(adu);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ExtractPdu_RoundTrip_PreservesData()
    {
        // Arrange
        byte slaveId = 0x07;
        byte[] originalPdu = { 0x03, 0x04, 0x12, 0x34, 0x56, 0x78 };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, originalPdu);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength));

        // Assert
        Assert.AreEqual(originalPdu.Length, extractedPdu.Length);
        for (int i = 0; i < originalPdu.Length; i++)
        {
            Assert.AreEqual(originalPdu[i], extractedPdu[i]);
        }
    }

    [TestMethod]
    public void ExtractPdu_OddVariant_ExtractsCorrectly()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu, Crc16Variant.Odd);

        // Act
        var extractedPdu = ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength), Crc16Variant.Odd);

        // Assert
        Assert.IsTrue(pdu.AsSpan().SequenceEqual(extractedPdu));
    }

    [TestMethod]
    public void VerifyCrc_OddVariant_ValidFrame_ReturnsTrue()
    {
        // Arrange
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu, Crc16Variant.Odd);

        // Act
        bool isValid = ModbusRtuAduParser.VerifyCrc(adu.Slice(0, aduLength), Crc16Variant.Odd);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void VerifyCrc_WrongVariant_ReturnsFalse()
    {
        // Arrange: 用奇校验创建的 ADU，用偶校验验证应该失败
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu, Crc16Variant.Odd);

        // Act
        bool isValid = ModbusRtuAduParser.VerifyCrc(adu.Slice(0, aduLength), Crc16Variant.Even);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void ExtractPdu_OddVariant_WrongVariant_ThrowsException()
    {
        // Arrange: 用奇校验创建的 ADU，用偶校验解析应该抛出异常
        byte slaveId = 0x01;
        byte[] pdu = { 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> adu = stackalloc byte[256];
        int aduLength = ModbusRtuAduBuilder.BuildAdu(adu, slaveId, pdu, Crc16Variant.Odd);

        // Act & Assert
        try
        {
            ModbusRtuAduParser.ExtractPdu(adu.Slice(0, aduLength), Crc16Variant.Even);
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }
}