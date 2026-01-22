using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Net.Exceptions;
using Modbus.Net.Protocol;

namespace Modbus.Net.Framing
{
    public sealed class ModbusTcpFramer : IModbusFramer
    {
        // MBAP header is 7 bytes:
        // TransactionId (2) + ProtocolId (2, must be 0) + Length (2) + UnitId (1)
        // Length = UnitId(1) + PDU length (FunctionCode + Data)
        private const int MbapLength = 7;

        public async Task WriteAduAsync(Stream stream, ModbusAdu adu, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            ushort transactionId = adu.TransactionId ?? 0;
            byte unitId = adu.UnitId;

            byte functionCode = adu.Pdu.FunctionCode;
            byte[] pduData = adu.Pdu.Data ?? Array.Empty<byte>();

            // PDU bytes = Function(1) + Data(N)
            int pduLength = 1 + pduData.Length;

            // MBAP Length field counts bytes following ProtocolId: UnitId(1) + PDU
            ushort lengthField = checked((ushort)(1 + pduLength));

            int frameLength = MbapLength + pduLength;
            byte[] buffer = new byte[frameLength];

            // MBAP
            buffer[0] = (byte)(transactionId >> 8);
            buffer[1] = (byte)(transactionId & 0xFF);

            // ProtocolId = 0
            buffer[2] = 0;
            buffer[3] = 0;

            buffer[4] = (byte)(lengthField >> 8);
            buffer[5] = (byte)(lengthField & 0xFF);

            buffer[6] = unitId;

            // PDU
            buffer[7] = functionCode;
            if (pduData.Length > 0)
                Buffer.BlockCopy(pduData, 0, buffer, 8, pduData.Length);

            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new ModbusTransportException("Failed to write Modbus TCP frame.", ex);
            }
        }

        public async Task<ModbusAdu> ReadAduAsync(Stream stream, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            byte[] mbap = new byte[MbapLength];

            try
            {
                await ReadExactlyAsync(stream, mbap, 0, MbapLength, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                throw new ModbusTransportException("Failed to read MBAP header.", ex);
            }

            ushort transactionId = (ushort)((mbap[0] << 8) | mbap[1]);
            ushort protocolId = (ushort)((mbap[2] << 8) | mbap[3]);
            ushort lengthField = (ushort)((mbap[4] << 8) | mbap[5]);
            byte unitId = mbap[6];

            if (protocolId != 0)
                throw new ModbusProtocolException($"Invalid Modbus TCP ProtocolId: {protocolId} (expected 0).");

            // lengthField counts UnitId(1) + PDU bytes (Function + Data)
            if (lengthField < 2) // UnitId(1) + Function(1) minimum
                throw new ModbusProtocolException($"Invalid Modbus TCP Length field: {lengthField} (minimum is 2).");

            int pduBytes = lengthField - 1; // subtract UnitId
            byte[] pdu = new byte[pduBytes];

            try
            {
                await ReadExactlyAsync(stream, pdu, 0, pduBytes, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException)
            {
                throw new ModbusTransportException("Failed to read Modbus TCP PDU.", ex);
            }

            if (pduBytes < 1)
                throw new ModbusProtocolException("Invalid PDU length.");

            byte functionCode = pdu[0];
            byte[] data = (pduBytes > 1) ? Slice(pdu, 1, pduBytes - 1) : Array.Empty<byte>();

            var pduStruct = new ModbusPdu(functionCode, data);
            return new ModbusAdu(transactionId, unitId, pduStruct);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int readTotal = 0;
            while (readTotal < count)
            {
                int n = await stream.ReadAsync(buffer, offset + readTotal, count - readTotal, ct).ConfigureAwait(false);
                if (n <= 0)
                    throw new IOException("Remote closed the connection while reading Modbus TCP frame.");
                readTotal += n;
            }
        }

        private static byte[] Slice(byte[] src, int offset, int count)
        {
            if (count <= 0) return Array.Empty<byte>();
            var dst = new byte[count];
            Buffer.BlockCopy(src, offset, dst, 0, count);
            return dst;
        }
    }
}
