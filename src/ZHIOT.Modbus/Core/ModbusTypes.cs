namespace ZHIOT.Modbus.Core;

/// <summary>
/// Modbus 功能码
/// </summary>
public enum ModbusFunctionCode : byte
{
    ReadCoils = 0x01,
    ReadDiscreteInputs = 0x02,
    ReadHoldingRegisters = 0x03,
    ReadInputRegisters = 0x04,
    WriteSingleCoil = 0x05,
    WriteSingleRegister = 0x06,
    WriteMultipleCoils = 0x0F,
    WriteMultipleRegisters = 0x10,
    ExceptionFlag = 0x80
}

/// <summary>
/// Modbus 异常代码
/// </summary>
public enum ModbusExceptionCode : byte
{
    IllegalFunction = 0x01,
    IllegalDataAddress = 0x02,
    IllegalDataValue = 0x03,
    ServerDeviceFailure = 0x04,
    Acknowledge = 0x05,
    ServerDeviceBusy = 0x06,
    MemoryParityError = 0x08,
    GatewayPathUnavailable = 0x0A,
    GatewayTargetDeviceFailedToRespond = 0x0B
}

/// <summary>
/// Modbus 异常
/// </summary>
public class ModbusException : Exception
{
    public ModbusExceptionCode ExceptionCode { get; }

    public ModbusException(ModbusExceptionCode exceptionCode)
        : base($"Modbus exception: {exceptionCode}")
    {
        ExceptionCode = exceptionCode;
    }

    public ModbusException(ModbusExceptionCode exceptionCode, string message)
        : base(message)
    {
        ExceptionCode = exceptionCode;
    }
}

/// <summary>
/// Modbus TCP 协议头 (MBAP Header)
/// </summary>
public readonly struct MbapHeader
{
    public ushort TransactionId { get; init; }
    public ushort ProtocolId { get; init; }
    public ushort Length { get; init; }
    public byte UnitId { get; init; }

    public const int Size = 7;
}
