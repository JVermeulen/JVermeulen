using JVermeulen.Processing;
using System;
using System.Text;
using uPLibrary.Networking.M2Mqtt;

namespace JVermeulen.MQTT
{
    public class MqttClient : Actor
    {
        private const string ClientId = "B8E79CEF-077C-4121-9F3D-EDAAD274C2B7";

        private uPLibrary.Networking.M2Mqtt.MqttClient Client { get; set; }

        public MqttClient(string brokerAddress) : base()
        {
            Client = new uPLibrary.Networking.M2Mqtt.MqttClient(brokerAddress);
            Client.MqttMsgPublishReceived += Client_MqttMsgPublishReceived;
            Client.Connect(ClientId);
            Client.Subscribe(new string[] { "zigbee2mqtt/Blitzwolf BW-SHP13" }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            Client.Subscribe(new string[] { "zigbee2mqtt/Aqara Sensor" }, new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        private void Client_MqttMsgPublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e)
        {
            string message = Encoding.UTF8.GetString(e.Message);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"topic '{e.Topic}', payload '{message}'");
            Console.ResetColor();
        }

        protected override void OnStarting()
        {
            base.OnStarting();
        }

        protected override void OnStarted()
        {
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            base.OnStopping();
        }

        protected override void OnStopped()
        {
            base.OnStopped();
        }

        protected override void OnReceive(SessionMessage message)
        {
            base.OnReceive(message);
        }
    }
}
