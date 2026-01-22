using Modbus.Net.Codec;
using Modbus.Net.Framing;
using Modbus.Net.Roles;
using Modbus.Net.Sessions;
using System.Net.Sockets;
using Timer = System.Windows.Forms.Timer;

namespace ModbusSimulator
{
    public partial class Form1 : Form
    {
        private readonly Timer _timer;
        private ModbusClientRole? _client;

        public Form1()
        {
            InitializeComponent();

            this.Load += Form1_Load;

            _timer = new Timer
            {
                Interval = 1000 // 1 second
            };

            _timer.Tick += Timer_Tick;


        }

        private async void Form1_Load(object? sender, EventArgs e)
        {
            await ConnectAsync();
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            ushort value = await _client!.ReadHoldingRegistersAsync(1, 0, 1, default).ContinueWith(t => t.Result[0]);
            value++;
            await _client.WriteSingleRegisterAsync(1, 0, value, default);
            this.Text = value.ToString();
            _timer.Start(); 
        }


        private async Task ConnectAsync()
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("127.0.0.1", 502);

            Stream stream = tcpClient.GetStream();

            IModbusFramer framer = new ModbusTcpFramer();
            IModbusSession session = new ModbusSession(stream, framer);
            IModbusCodec codec = new ModbusCodec();

            _client = new ModbusClientRole(session, codec);
        }


    }
}
