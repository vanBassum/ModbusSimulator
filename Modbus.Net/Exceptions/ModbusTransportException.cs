namespace Modbus.Net.Exceptions
{
    /// <summary>
    /// Optional wrapper for IO/socket/stream errors if you prefer a single catch point.
    /// </summary>
    public sealed class ModbusTransportException : ModbusExceptionBase
    {
        public ModbusTransportException(string message, System.Exception inner)
            : base(message, inner)
        {
        }
    }

}
