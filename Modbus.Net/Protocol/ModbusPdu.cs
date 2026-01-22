namespace Modbus.Net.Protocol
{
    public readonly struct ModbusPdu
    {
        public byte FunctionCode { get; }
        public byte[] Data { get; }

        public ModbusPdu(byte functionCode, byte[] data)
        {
            FunctionCode = functionCode;
            Data = data ?? Array.Empty<byte>();
        }
    }
}
