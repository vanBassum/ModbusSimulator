using Modbus.Net.Protocol;

namespace Modbus.Net.Server
{
    public interface IModbusRequestHandler
    {
        // Return a response PDU for the given request PDU, or null to indicate "no response"
        // (useful for broadcast semantics later, or for intentionally silent behavior).
        Task<ModbusPdu?> HandleRequestAsync(byte unitId, ModbusPdu request, CancellationToken ct);
    }
}
