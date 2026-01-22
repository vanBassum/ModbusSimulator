using Modbus.Net.Protocol;

namespace Modbus.Net.Framing
{
    public interface IModbusFramer
    {
        Task WriteAduAsync(Stream stream, ModbusAdu adu, CancellationToken ct);
        Task<ModbusAdu> ReadAduAsync(Stream stream, CancellationToken ct);
    }
}
