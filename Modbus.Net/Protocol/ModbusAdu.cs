namespace Modbus.Net.Protocol
{
    public readonly struct ModbusAdu
    {
        public ushort? TransactionId { get; }
        public byte UnitId { get; }
        public ModbusPdu Pdu { get; }

        public ModbusAdu(ushort? transactionId, byte unitId, ModbusPdu pdu)
        {
            TransactionId = transactionId;
            UnitId = unitId;
            Pdu = pdu;
        }
    }
}
