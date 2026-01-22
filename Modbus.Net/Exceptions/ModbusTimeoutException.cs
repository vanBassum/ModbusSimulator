namespace Modbus.Net.Exceptions
{
    public sealed class ModbusTimeoutException : ModbusExceptionBase
    {
        public ModbusTimeoutException(string message)
            : base(message)
        {
        }
    }

}
