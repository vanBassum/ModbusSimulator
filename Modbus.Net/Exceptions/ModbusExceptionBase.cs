namespace Modbus.Net.Exceptions
{

    public abstract class ModbusExceptionBase : Exception
    {
        protected ModbusExceptionBase(string message, Exception? inner = null)
            : base(message, inner)
        {
        }
    }

}
