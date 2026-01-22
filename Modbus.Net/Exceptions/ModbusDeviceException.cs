using Modbus.Net.Protocol;

namespace Modbus.Net.Exceptions
{
    /// <summary>
    /// Thrown when a device returns a Modbus exception response (function | 0x80).
    /// </summary>
    public sealed class ModbusDeviceException : ModbusExceptionBase
    {
        public byte UnitId { get; }
        public byte RequestFunctionCode { get; }
        public ModbusErrorCode ErrorCode { get; }
        public ushort? TransactionId { get; }

        public ModbusDeviceException(
            byte unitId,
            byte requestFunctionCode,
            ModbusErrorCode errorCode,
            ushort? transactionId = null)
            : base($"Modbus device error: unit={unitId}, fc=0x{requestFunctionCode:X2}, code=0x{(byte)errorCode:X2} ({errorCode}).")
        {
            UnitId = unitId;
            RequestFunctionCode = requestFunctionCode;
            ErrorCode = errorCode;
            TransactionId = transactionId;
        }
    }

}
