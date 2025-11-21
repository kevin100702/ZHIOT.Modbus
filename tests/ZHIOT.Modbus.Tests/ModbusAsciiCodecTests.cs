using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusAsciiCodecTests
{
    [TestMethod]
    public void Encode_SimpleData_ReturnsCorrectAscii()
    {
        // Arrange
        byte[] data = { 0xAB, 0xCD };
        Span<byte> ascii = stackalloc byte[4];
        string expected = "ABCD";

        // Act
        int length = ModbusAsciiCodec.Encode(data, ascii);

        // Assert
        Assert.AreEqual(4, length);
        string result = System.Text.Encoding.ASCII.GetString(ascii.Slice(0, length));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void Decode_SimpleAscii_ReturnsCorrectData()
    {
        // Arrange
        byte[] ascii = System.Text.Encoding.ASCII.GetBytes("ABCD");
        Span<byte> data = stackalloc byte[2];
        byte[] expected = { 0xAB, 0xCD };

        // Act
        int length = ModbusAsciiCodec.Decode(ascii, data);

        // Assert
        Assert.AreEqual(2, length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(expected[i], data[i]);
        }
    }

    [TestMethod]
    public void Encode_AllHexDigits_ReturnsCorrectAscii()
    {
        // Arrange
        byte[] data = { 0x00, 0x0F, 0xF0, 0xFF };
        Span<byte> ascii = stackalloc byte[8];

        // Act
        int length = ModbusAsciiCodec.Encode(data, ascii);

        // Assert
        Assert.AreEqual(8, length);
        string result = System.Text.Encoding.ASCII.GetString(ascii.Slice(0, length));
        // 0x00 -> "00", 0x0F -> "0F", 0xF0 -> "F0", 0xFF -> "FF"
        Assert.AreEqual("000FF0FF", result);
    }

    [TestMethod]
    public void Encode_Decode_Roundtrip()
    {
        // Arrange
        byte[] original = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        Span<byte> ascii = stackalloc byte[original.Length * 2];
        ModbusAsciiCodec.Encode(original, ascii);

        // Act
        Span<byte> decoded = stackalloc byte[original.Length];
        ModbusAsciiCodec.Decode(ascii, decoded);

        // Assert
        for (int i = 0; i < original.Length; i++)
        {
            Assert.AreEqual(original[i], decoded[i]);
        }
    }

    [TestMethod]
    public void Decode_OddLengthAscii_ThrowsArgumentException()
    {
        // Arrange
        byte[] ascii = System.Text.Encoding.ASCII.GetBytes("ABC");
        Span<byte> data = stackalloc byte[2];

        // Act & Assert
        try
        {
            ModbusAsciiCodec.Decode(ascii, data);
            Assert.Fail("Should have thrown ArgumentException");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void Decode_InvalidHexCharacter_ThrowsFormatException()
    {
        // Arrange
        byte[] ascii = System.Text.Encoding.ASCII.GetBytes("AG");
        Span<byte> data = stackalloc byte[1];

        // Act & Assert
        try
        {
            ModbusAsciiCodec.Decode(ascii, data);
            Assert.Fail("Should have thrown FormatException");
        }
        catch (FormatException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void EncodeToString_SimpleData_ReturnsCorrectString()
    {
        // Arrange
        byte[] data = { 0xAB, 0xCD };
        string expected = "ABCD";

        // Act
        string result = ModbusAsciiCodec.EncodeToString(data);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void DecodeFromString_SimpleString_ReturnsCorrectData()
    {
        // Arrange
        string ascii = "ABCD";
        byte[] expected = { 0xAB, 0xCD };

        // Act
        byte[] result = ModbusAsciiCodec.DecodeFromString(ascii);

        // Assert
        CollectionAssert.AreEqual(expected, result);
    }
}
