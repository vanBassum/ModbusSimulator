using System;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Net.Codec;
using Modbus.Net.Exceptions;
using Modbus.Net.Protocol;
using Modbus.Net.Sessions;

namespace Modbus.Net.Roles
{
    public sealed class ModbusClientRole
    {
        private readonly IModbusSession _session;
        private readonly IModbusCodec _codec;

        // Minimal transaction id generator for Modbus TCP. For RTU it is ignored.
        private ushort _nextTransactionId = 1;

        public ModbusClientRole(IModbusSession session, IModbusCodec codec)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        public async Task<ushort[]> ReadHoldingRegistersAsync(byte unitId, ushort start, ushort count, CancellationToken ct)
        {
            ModbusPdu request = _codec.CreateReadHoldingRegistersRequest(start, count);
            ModbusPdu response = await ExecuteAsync(unitId, request, ct).ConfigureAwait(false);
            return _codec.ParseReadHoldingRegistersResponse(response);
        }

        public async Task WriteSingleRegisterAsync(byte unitId, ushort address, ushort value, CancellationToken ct)
        {
            ModbusPdu request = _codec.CreateWriteSingleRegisterRequest(address, value);
            ModbusPdu response = await ExecuteAsync(unitId, request, ct).ConfigureAwait(false);
            _codec.ValidateWriteSingleRegisterResponse(response, address, value);
        }

        private async Task<ModbusPdu> ExecuteAsync(byte unitId, ModbusPdu request, CancellationToken ct)
        {
            // For a minimal first implementation we enforce "single inflight request" implicitly by awaiting in sequence.
            // Later: add a semaphore or transaction-id routing table for true parallelism.

            ushort txId = GetNextTransactionId();

            var requestAdu = new ModbusAdu(transactionId: txId, unitId: unitId, pdu: request);

            await _session.SendAsync(requestAdu, ct).ConfigureAwait(false);

            // Read responses until we find the matching one. (Useful if the stream has leftovers or if you later pipeline.)
            while (true)
            {
                ModbusAdu responseAdu = await _session.ReceiveAsync(ct).ConfigureAwait(false);

                // If unit id doesn't match, ignore (or later: route to another consumer).
                if (responseAdu.UnitId != unitId)
                    continue;

                // If transaction id is present, match it.
                if (responseAdu.TransactionId.HasValue && responseAdu.TransactionId.Value != txId)
                    continue;

                // Device exception response: function | 0x80 and data[0] = ModbusErrorCode
                ThrowIfDeviceError(responseAdu, request.FunctionCode);

                return responseAdu.Pdu;
            }
        }

        private void ThrowIfDeviceError(ModbusAdu responseAdu, byte requestFunctionCode)
        {
            byte fc = responseAdu.Pdu.FunctionCode;

            // Exception response has MSB set.
            if ((fc & 0x80) == 0)
                return;

            // According to Modbus, exception response function code equals request function code + 0x80.
            byte expected = (byte)(requestFunctionCode | 0x80);
            if (fc != expected)
                throw new ModbusProtocolException(
                    $"Unexpected exception function code: 0x{fc:X2} (expected 0x{expected:X2}).");

            byte[] data = responseAdu.Pdu.Data ?? Array.Empty<byte>();
            if (data.Length < 1)
                throw new ModbusProtocolException("Invalid exception response PDU: missing error code byte.");

            var code = (ModbusErrorCode)data[0];
            throw new ModbusDeviceException(
                unitId: responseAdu.UnitId,
                requestFunctionCode: requestFunctionCode,
                errorCode: code,
                transactionId: responseAdu.TransactionId);
        }

        private ushort GetNextTransactionId()
        {
            // Wrap naturally on overflow; avoid returning 0 if you prefer (not required).
            ushort tx = _nextTransactionId++;
            if (_nextTransactionId == 0)
                _nextTransactionId = 1;
            return tx;
        }
    }
}
