using System;
using Modbus.Net.Exceptions;
using Modbus.Net.Protocol;

namespace Modbus.Net.Codec
{
    public sealed class ModbusCodec : IModbusCodec
    {
        public ModbusPdu CreateReadHoldingRegistersRequest(ushort start, ushort count)
        {
            // Function 0x03 payload: start (2 bytes BE) + count (2 bytes BE)
            var data = new byte[4];
            data[0] = (byte)(start >> 8);
            data[1] = (byte)(start & 0xFF);
            data[2] = (byte)(count >> 8);
            data[3] = (byte)(count & 0xFF);

            return new ModbusPdu(functionCode: 0x03, data: data);
        }

        public ushort[] ParseReadHoldingRegistersResponse(ModbusPdu response)
        {
            // If device returned exception response, surface via exceptions route.
            // Exception response: function = requestFunction | 0x80, data[0] = ModbusErrorCode
            if ((response.FunctionCode & 0x80) != 0)
            {
                var code = ParseErrorCode(response);
                // Request function is response function without 0x80 bit.
                byte requestFc = (byte)(response.FunctionCode & 0x7F);
                throw new ModbusDeviceException(unitId: 0, requestFunctionCode: requestFc, errorCode: code);
                // UnitId is not available at PDU level. ClientRole should rethrow with UnitId context later.
            }

            if (response.FunctionCode != 0x03)
                throw new ModbusProtocolException($"Unexpected function code: 0x{response.FunctionCode:X2} (expected 0x03).");

            byte[] data = response.Data ?? Array.Empty<byte>();
            if (data.Length < 1)
                throw new ModbusProtocolException("Invalid response PDU: missing byte count.");

            int byteCount = data[0];
            if (byteCount < 0)
                throw new ModbusProtocolException("Invalid response PDU: invalid byte count.");

            if (data.Length != 1 + byteCount)
                throw new ModbusProtocolException($"Invalid response PDU: expected {1 + byteCount} data bytes, got {data.Length}.");

            if ((byteCount % 2) != 0)
                throw new ModbusProtocolException("Invalid response PDU: byte count must be even for registers.");

            int registerCount = byteCount / 2;
            var registers = new ushort[registerCount];

            int idx = 1;
            for (int i = 0; i < registerCount; i++)
            {
                ushort value = (ushort)((data[idx] << 8) | data[idx + 1]);
                registers[i] = value;
                idx += 2;
            }

            return registers;
        }

        public ModbusPdu CreateWriteSingleRegisterRequest(ushort address, ushort value)
        {
            // Function 0x06 payload: address (2 bytes BE) + value (2 bytes BE)
            var data = new byte[4];
            data[0] = (byte)(address >> 8);
            data[1] = (byte)(address & 0xFF);
            data[2] = (byte)(value >> 8);
            data[3] = (byte)(value & 0xFF);

            return new ModbusPdu(functionCode: 0x06, data: data);
        }

        public void ValidateWriteSingleRegisterResponse(ModbusPdu response, ushort address, ushort value)
        {
            // If device returned exception response, surface via exceptions route.
            if ((response.FunctionCode & 0x80) != 0)
            {
                var code = ParseErrorCode(response);
                byte requestFc = (byte)(response.FunctionCode & 0x7F);
                throw new ModbusDeviceException(unitId: 0, requestFunctionCode: requestFc, errorCode: code);
            }

            // Normal response echoes request:
            // Function 0x06
            // Data: address (2) + value (2)
            if (response.FunctionCode != 0x06)
                throw new ModbusProtocolException($"Unexpected function code: 0x{response.FunctionCode:X2} (expected 0x06).");

            byte[] data = response.Data ?? Array.Empty<byte>();
            if (data.Length != 4)
                throw new ModbusProtocolException($"Invalid response PDU: expected 4 data bytes, got {data.Length}.");

            ushort respAddr = (ushort)((data[0] << 8) | data[1]);
            ushort respVal = (ushort)((data[2] << 8) | data[3]);

            if (respAddr != address || respVal != value)
                throw new ModbusProtocolException(
                    $"WriteSingleRegister mismatch. Expected addr={address}, val={value}; got addr={respAddr}, val={respVal}.");
        }

        private static ModbusErrorCode ParseErrorCode(ModbusPdu response)
        {
            byte[] data = response.Data ?? Array.Empty<byte>();
            if (data.Length < 1)
                throw new ModbusProtocolException("Invalid exception response PDU: missing error code byte.");

            return (ModbusErrorCode)data[0];
        }
    }
}
