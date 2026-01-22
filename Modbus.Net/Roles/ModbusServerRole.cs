using Modbus.Net.Server;
using Modbus.Net.Sessions;

namespace Modbus.Net.Roles
{
    public sealed class ModbusServerRole
    {
        private readonly IModbusRequestHandler _handler;

        public ModbusServerRole(IModbusRequestHandler handler)
        {
            _handler = handler;
        }

        public Task HandleConnectionAsync(IModbusSession session, CancellationToken ct)
            => Task.CompletedTask;
    }
}
