using HoloToolkit.Unity;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.Communication
{
    public class MqttManager : Singleton<MqttManager>
    {

        //Verbindung
        [SerializeField]
        private string ip = "192.168.0.5";
        [SerializeField]
        private string publishPath = "/messages/commands/";
        [SerializeField]
        private string subscribePath = "/messages/states/";

        private MqttNetClientAccess client = new MqttNetClientAccess();


        void Start()
        {
            if (client.isInitialized)
                return;

            client.initialize(ip,
                System.Guid.NewGuid().ToString(),
                10,
                client_MqttMsgPublishReceived,
                "MqttManager");

            client.Subscribe(subscribePath + "#");
        }


        void Update()
        {
            client.Update();

        }


        void client_MqttMsgPublishReceived(MqttNetClientAccess.mqttMessage mqttMsg)
        {

            //gets the name of the Item
            string name = mqttMsg.topic.Substring(mqttMsg.topic.LastIndexOf('/') + 1);


            //updates the incomming state
            ItemStates.Instance.updateItem(
                name,
                System.Text.Encoding.UTF8.GetString(mqttMsg.payload), 
                false);
        }


        //MQTT-Publish-Methode
        public void MQTTPublish(string name, string message)
        {
            if (!client.isInitialized)
            {
                Debug.LogError("client is not initialized and you tried to publish a message. No message will be send");
                return;
            }

            Debug.Log("path: " + publishPath + ", item: " + name + ", message: " + message);
            client.MQTTPublishMessage(publishPath + name, message);
        }

    }
}



