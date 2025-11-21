using Microsoft.VisualStudio.TestTools.UnitTesting;
using ZHIOT.Modbus.Core;

namespace ZHIOT.Modbus.Tests;

[TestClass]
public class ModbusLrcTests
{
    [TestMethod]
    public void Calculate_SimpleData_ReturnsCorrectLrc()
    {
        // Arrange
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        // Manual calculation: sum = 0x01+0x03+0x00+0x00+0x00+0x0A = 0x0E
        // LRC = -0x0E mod 256 = 242 = 0xF2
        
        // Act
        byte lrc = ModbusLrc.Calculate(data);

        // Assert
        Assert.AreEqual(242, lrc);
    }

    [TestMethod]
    public void Calculate_EmptyData_ReturnsZero()
    {
        // Arrange
        byte[] data = Array.Empty<byte>();

        // Act
        byte lrc = ModbusLrc.Calculate(data);

        // Assert
        Assert.AreEqual(0, lrc);
    }

    [TestMethod]
    public void Calculate_SingleByte_ReturnsNegated()
    {
        // Arrange
        byte[] data = { 0x05 };
        byte expected = (byte)((-0x05) & 0xFF);

        // Act
        byte lrc = ModbusLrc.Calculate(data);

        // Assert
        Assert.AreEqual(expected, lrc);
    }

    [TestMethod]
    public void Verify_ValidFrame_ReturnsTrue()
    {
        // Arrange
        byte[] data = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A };
        byte lrc = ModbusLrc.Calculate(data);
        byte[] frame = new byte[data.Length + 1];
        data.CopyTo(frame, 0);
        frame[frame.Length - 1] = lrc;

        // Act
        bool result = ModbusLrc.Verify(frame);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Verify_InvalidFrame_ReturnsFalse()
    {
        // Arrange
        byte[] frame = { 0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xFF };

        // Act
        bool result = ModbusLrc.Verify(frame);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Verify_TooShortFrame_ReturnsFalse()
    {
        // Arrange
        byte[] frame = { 0x01 };

        // Act
        bool result = ModbusLrc.Verify(frame);

        // Assert
        Assert.IsFalse(result);
    }
}
