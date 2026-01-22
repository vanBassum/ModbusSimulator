using Modbus.Net.Framing;
using Modbus.Net.Protocol;

namespace Modbus.Net.Sessions
{
    public sealed class ModbusSession : IModbusSession
    {
        private readonly Stream _stream;
        private readonly IModbusFramer _framer;

        public ModbusSession(Stream stream, IModbusFramer framer)
        {
            _stream = stream;
            _framer = framer;
        }

        public Task SendAsync(ModbusAdu adu, CancellationToken ct)
            => _framer.WriteAduAsync(_stream, adu, ct);

        public Task<ModbusAdu> ReceiveAsync(CancellationToken ct)
            => _framer.ReadAduAsync(_stream, ct);
    }
}
