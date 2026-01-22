using Modbus.Net.Protocol;

namespace Modbus.Net.Sessions
{
    public interface IModbusSession
    {
        Task SendAsync(ModbusAdu adu, CancellationToken ct);
        Task<ModbusAdu> ReceiveAsync(CancellationToken ct);
    }
}
