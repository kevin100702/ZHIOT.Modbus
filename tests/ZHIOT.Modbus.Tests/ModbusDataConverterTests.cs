using ZHIOT.Modbus.Core;
using ZHIOT.Modbus.Utils;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusDataConverterTests
{
    #region UInt16 Tests

    [TestMethod]
    public void ToArray_UInt16_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<ushort>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x1234, result[0]);
        Assert.AreEqual((ushort)0x5678, result[1]);
    }

    [TestMethod]
    public void ToArray_UInt16_LittleEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<ushort>(data, ByteOrder.LittleEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x3412, result[0]);
        Assert.AreEqual((ushort)0x7856, result[1]);
    }

    [TestMethod]
    public void ToRegisters_UInt16_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        ushort[] values = { 0x1234, 0x5678 };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x1234, result[0]);
        Assert.AreEqual((ushort)0x5678, result[1]);
    }

    #endregion

    #region Int16 Tests

    [TestMethod]
    public void ToArray_Int16_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0xFF, 0xFE, 0x00, 0x7F };

        // Act
        var result = ModbusDataConverter.ToArray<short>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((short)-2, result[0]);
        Assert.AreEqual((short)127, result[1]);
    }

    [TestMethod]
    public void ToRegisters_Int16_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        short[] values = { -2, 127 };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0xFFFE, result[0]);
        Assert.AreEqual((ushort)0x007F, result[1]);
    }

    #endregion

    #region UInt32 Tests

    [TestMethod]
    public void ToArray_UInt32_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<uint>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x12345678u, result[0]);
    }

    [TestMethod]
    public void ToArray_UInt32_LittleEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<uint>(data, ByteOrder.LittleEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x78563412u, result[0]);
    }

    [TestMethod]
    public void ToArray_UInt32_BigEndianSwap_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<uint>(data, ByteOrder.BigEndianSwap);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x56781234u, result[0]);
    }

    [TestMethod]
    public void ToArray_UInt32_LittleEndianSwap_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56, 0x78 };

        // Act
        var result = ModbusDataConverter.ToArray<uint>(data, ByteOrder.LittleEndianSwap);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(0x34127856u, result[0]);
    }

    [TestMethod]
    public void ToRegisters_UInt32_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        uint[] values = { 0x12345678u };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x1234, result[0]);
        Assert.AreEqual((ushort)0x5678, result[1]);
    }

    #endregion

    #region Int32 Tests

    [TestMethod]
    public void ToArray_Int32_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        byte[] data = { 0xFF, 0xFF, 0xFF, 0xFE };

        // Act
        var result = ModbusDataConverter.ToArray<int>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(-2, result[0]);
    }

    [TestMethod]
    public void ToRegisters_Int32_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        int[] values = { -2 };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0xFFFF, result[0]);
        Assert.AreEqual((ushort)0xFFFE, result[1]);
    }

    #endregion

    #region Float Tests

    [TestMethod]
    public void ToArray_Float_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        // 3.14159274f = 0x40490FDB
        byte[] data = { 0x40, 0x49, 0x0F, 0xDB };

        // Act
        var result = ModbusDataConverter.ToArray<float>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(3.14159274f, result[0], 0.00001f);
    }

    [TestMethod]
    public void ToArray_Float_LittleEndian_ReturnsCorrectValues()
    {
        // Arrange
        // 3.14159274f = 0x40490FDB (little endian: DB 0F 49 40)
        byte[] data = { 0xDB, 0x0F, 0x49, 0x40 };

        // Act
        var result = ModbusDataConverter.ToArray<float>(data, ByteOrder.LittleEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(3.14159274f, result[0], 0.00001f);
    }

    [TestMethod]
    public void ToRegisters_Float_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        float[] values = { 3.14159274f };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x4049, result[0]);
        Assert.AreEqual((ushort)0x0FDB, result[1]);
    }

    [TestMethod]
    public void ToRegisters_Float_BigEndianSwap_ReturnsCorrectValues()
    {
        // Arrange
        float[] values = { 3.14159274f };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndianSwap);

        // Assert
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual((ushort)0x0FDB, result[0]);
        Assert.AreEqual((ushort)0x4049, result[1]);
    }

    #endregion

    #region Double Tests

    [TestMethod]
    public void ToArray_Double_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        // 3.141592653589793 = 0x400921FB54442D18
        byte[] data = { 0x40, 0x09, 0x21, 0xFB, 0x54, 0x44, 0x2D, 0x18 };

        // Act
        var result = ModbusDataConverter.ToArray<double>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(3.141592653589793, result[0], 0.000000000001);
    }

    [TestMethod]
    public void ToRegisters_Double_BigEndian_ReturnsCorrectValues()
    {
        // Arrange
        double[] values = { 3.141592653589793 };

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(4, result.Length);
        Assert.AreEqual((ushort)0x4009, result[0]);
        Assert.AreEqual((ushort)0x21FB, result[1]);
        Assert.AreEqual((ushort)0x5444, result[2]);
        Assert.AreEqual((ushort)0x2D18, result[3]);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void ToArray_EmptySpan_ReturnsEmptyArray()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        var result = ModbusDataConverter.ToArray<ushort>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ToArray_OddByteLength_ThrowsException()
    {
        // Arrange
        byte[] data = { 0x12, 0x34, 0x56 };

        // Act & Assert
        try
        {
            ModbusDataConverter.ToArray<ushort>(data, ByteOrder.BigEndian);
            Assert.Fail("Expected ArgumentException was not thrown");
        }
        catch (ArgumentException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void ToValue_SingleValue_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = { 0x12, 0x34 };

        // Act
        var result = ModbusDataConverter.ToValue<ushort>(data, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual((ushort)0x1234, result);
    }

    [TestMethod]
    public void ToRegisters_EmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        ushort[] values = Array.Empty<ushort>();

        // Act
        var result = ModbusDataConverter.ToRegisters(values, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(0, result.Length);
    }

    #endregion

    #region Round-trip Tests

    [TestMethod]
    public void RoundTrip_Float_BigEndian()
    {
        // Arrange
        float[] original = { 1.23f, -456.789f, 0.0f, float.MaxValue };

        // Act
        var registers = ModbusDataConverter.ToRegisters(original, ByteOrder.BigEndian);
        byte[] bytes = new byte[registers.Length * 2];
        for (int i = 0; i < registers.Length; i++)
        {
            bytes[i * 2] = (byte)(registers[i] >> 8);
            bytes[i * 2 + 1] = (byte)(registers[i] & 0xFF);
        }
        var result = ModbusDataConverter.ToArray<float>(bytes, ByteOrder.BigEndian);

        // Assert
        Assert.AreEqual(original.Length, result.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.AreEqual(original[i], result[i], 0.00001f);
        }
    }

    [TestMethod]
    public void RoundTrip_Double_LittleEndianSwap()
    {
        // Arrange
        double[] original = { Math.PI, Math.E, -1.234567890123456 };

        // Act
        var registers = ModbusDataConverter.ToRegisters(original, ByteOrder.LittleEndianSwap);
        byte[] bytes = new byte[registers.Length * 2];
        for (int i = 0; i < registers.Length; i++)
        {
            bytes[i * 2] = (byte)(registers[i] >> 8);
            bytes[i * 2 + 1] = (byte)(registers[i] & 0xFF);
        }
        var result = ModbusDataConverter.ToArray<double>(bytes, ByteOrder.LittleEndianSwap);

        // Assert
        Assert.AreEqual(original.Length, result.Length);
        for (int i = 0; i < original.Length; i++)
        {
            Assert.AreEqual(original[i], result[i], 0.000000000001);
        }
    }

    #endregion
}
