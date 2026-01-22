using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Modbus.Net.Exceptions;
using Modbus.Net.Protocol;

namespace Modbus.Net.Framing
{
    public sealed class ModbusRtuFramer : IModbusFramer
    {
        // Minimum RTU frame:
        // Address (1) + Function (1) + CRC (2)
        private const int MinFrameLength = 4;

        // Typical RTU implementations rely on a silent interval (~3.5 char times).
        // On PC, we approximate this with a read timeout.
        private readonly TimeSpan _interFrameTimeout;

        public ModbusRtuFramer(TimeSpan? interFrameTimeout = null)
        {
            _interFrameTimeout = interFrameTimeout ?? TimeSpan.FromMilliseconds(5);
        }

        public async Task WriteAduAsync(Stream stream, ModbusAdu adu, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            byte unitId = adu.UnitId;
            byte functionCode = adu.Pdu.FunctionCode;
            byte[] data = adu.Pdu.Data ?? Array.Empty<byte>();

            int frameLen = 1 + 1 + data.Length + 2; // addr + fc + data + CRC
            byte[] buffer = new byte[frameLen];

            buffer[0] = unitId;
            buffer[1] = functionCode;

            if (data.Length > 0)
                Buffer.BlockCopy(data, 0, buffer, 2, data.Length);

            ushort crc = ComputeCrc(buffer, 0, frameLen - 2);
            buffer[frameLen - 2] = (byte)(crc & 0xFF);       // CRC low
            buffer[frameLen - 1] = (byte)(crc >> 8);         // CRC high

            try
            {
                await stream.WriteAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                await stream.FlushAsync(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new ModbusTransportException("Failed to write Modbus RTU frame.", ex);
            }
        }

        public async Task<ModbusAdu> ReadAduAsync(Stream stream, CancellationToken ct)
        {
            if (stream is null) throw new ArgumentNullException(nameof(stream));

            using var ms = new MemoryStream();
            byte[] single = new byte[1];

            try
            {
                while (true)
                {
                    int read = await ReadWithTimeoutAsync(stream, single, ct).ConfigureAwait(false);
                    if (read == 0)
                        break;

                    ms.WriteByte(single[0]);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                throw new ModbusTransportException("Failed to read Modbus RTU frame.", ex);
            }

            byte[] frame = ms.ToArray();

            if (frame.Length < MinFrameLength)
                throw new ModbusProtocolException($"RTU frame too short: {frame.Length} bytes.");

            ushort receivedCrc = (ushort)(frame[^2] | (frame[^1] << 8));
            ushort computedCrc = ComputeCrc(frame, 0, frame.Length - 2);

            if (receivedCrc != computedCrc)
                throw new ModbusProtocolException("RTU CRC check failed.");

            byte unitId = frame[0];
            byte functionCode = frame[1];

            int dataLen = frame.Length - 1 - 1 - 2;
            byte[] data = dataLen > 0 ? Slice(frame, 2, dataLen) : Array.Empty<byte>();

            var pdu = new ModbusPdu(functionCode, data);
            return new ModbusAdu(transactionId: null, unitId: unitId, pdu: pdu);
        }

        private async Task<int> ReadWithTimeoutAsync(Stream stream, byte[] buffer, CancellationToken ct)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(_interFrameTimeout);

            try
            {
                return await stream.ReadAsync(buffer, 0, 1, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Timeout → end of frame
                if (!ct.IsCancellationRequested)
                    return 0;
                throw;
            }
        }

        private static ushort ComputeCrc(byte[] buffer, int offset, int count)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < count; i++)
            {
                crc ^= buffer[offset + i];
                for (int bit = 0; bit < 8; bit++)
                {
                    bool lsb = (crc & 0x0001) != 0;
                    crc >>= 1;
                    if (lsb)
                        crc ^= 0xA001;
                }
            }

            return crc;
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
