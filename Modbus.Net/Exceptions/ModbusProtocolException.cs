namespace Modbus.Net.Exceptions
{
    /// <summary>
    /// Thrown when a received frame/PDU is malformed, violates spec, or cannot be parsed.
    /// </summary>
    public class ModbusProtocolException : ModbusExceptionBase
    {
        public ModbusProtocolException(string message, System.Exception? inner = null)
            : base(message, inner)
        {
        }
    }

}
