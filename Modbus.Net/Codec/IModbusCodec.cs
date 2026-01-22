using Modbus.Net.Protocol;

namespace Modbus.Net.Codec
{
    public interface IModbusCodec
    {
        // 0x03
        ModbusPdu CreateReadHoldingRegistersRequest(ushort start, ushort count);
        ushort[] ParseReadHoldingRegistersResponse(ModbusPdu response);

        // 0x06
        ModbusPdu CreateWriteSingleRegisterRequest(ushort address, ushort value);
        void ValidateWriteSingleRegisterResponse(ModbusPdu response, ushort address, ushort value);

        // Server-side: optionally decode a request to decide how to respond.
        // You can keep this minimal at first and expand later.
    }
}
