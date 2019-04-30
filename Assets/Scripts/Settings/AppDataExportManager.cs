
using HoloToolkit.Unity;
using System;
using System.Collections.Generic;
using ThesisHololens.Communication;
using ThesisHololens.utilities;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.Settings
{
    public class AppDataExportManager : Singleton<AppDataExportManager>
    {

        //Verbindung
        [SerializeField]
        public string ip = "192.168.0.25";

        [Tooltip("Enter the World Anchor Topic without wildcard '#'")]
        public string worldAnchorBaseSubscriptionPath = "/WorldAnchors/";

        private MqttNetClientAccess client = new MqttNetClientAccess();






        /// <summary>
        /// in here are all callbakcs for when a message for a specific topic comes in
        /// </summary>
        private Dictionary<string, UnityAction<MqttNetClientAccess.mqttMessage>> invocationListForTopic = new Dictionary<string, UnityAction<MqttNetClientAccess.mqttMessage>>();


        protected override void Awake()
        {
            base.Awake();
        }


        void Start()
        {
            if (client.isInitialized)
                return;


            client.initialize(ip,
                Guid.NewGuid().ToString(),
                10,
                messageReceived,
                "AppDataExportmanager");
        }

        private void Update()
        {
            client.Update();
        }





        /// <summary>
        /// subscribes to a given full path topic
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="callback">the callback to be notified</param>
        public void subscribe(string subscription,UnityAction<MqttNetClientAccess.mqttMessage> callback)
        {
            if (!client.isInitialized)
                Start();

            try
            {
                // subscribe to topic
                if (client != null && subscription != null)
                {
                    client.Subscribe(subscription);

                    //add the callback to list
                    if (invocationListForTopic.ContainsKey(subscription))
                        invocationListForTopic[subscription] += callback;
                    else//we need to add an entry
                    {
                        invocationListForTopic.Add(subscription, callback);
                    }
                }

            }
            catch (Exception e)
            {
                Debug.LogError("cannot subscribe to path: " + subscription + "\n" + e.Message);
            }
        }

        public void unsubscribe(string subscribtion, UnityAction<MqttNetClientAccess.mqttMessage> callback)
        {
            if (!client.isInitialized)
                Start();

            try
            {
                // subscribe to topic
                if (client != null && subscribtion != null)
                    client.Unsubscribe(subscribtion);

                if (invocationListForTopic.ContainsKey(subscribtion))
                {
                    //if there is only one entry in the delegate
                    if(invocationListForTopic[subscribtion].GetInvocationList().Length <= 1 )
                    {
                        //delete entry
                        invocationListForTopic.Remove(subscribtion);
                    }
                    else
                    {
                        invocationListForTopic[subscribtion] -= callback;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("cannot unsubscribe to path: " + subscribtion + "\n" + e.Message);
            }
        }

        private void messageReceived(MqttNetClientAccess.mqttMessage mqttMsg)
        {

            //now we have a message to analyze

            //we may not invoke direktly, because we (the possibility exists) modify the invovationListForTopic as a reason of invoking. We need another list
            List<KeyValuePair<string, UnityAction<MqttNetClientAccess.mqttMessage>>> toInvoke = new List<KeyValuePair<string, UnityAction<MqttNetClientAccess.mqttMessage>>>();

            foreach (KeyValuePair<string, UnityAction<MqttNetClientAccess.mqttMessage>> invokationElement in invocationListForTopic)
            {
                string invokation_topic = invokationElement.Key;
                if (invokation_topic.EndsWith("#"))
                {
                    invokation_topic = invokation_topic.Remove(invokation_topic.Length - 1); // removes last char
                }

                //for all messages, that start with the invokation term. pass the message to the subscriber and invoke
                if (mqttMsg.topic.StartsWith(invokation_topic))
                {
                    toInvoke.Add(invokationElement);

                }
            }

            //now we can invoke
            foreach (KeyValuePair<string, UnityAction<MqttNetClientAccess.mqttMessage>> invokationElement in toInvoke)
            {
                invokationElement.Value.Invoke(mqttMsg);
            }
        }


        


        /// <summary>
        /// Only For Anchors. Time will be attached to the anchor string as well
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="Anchor"></param>
        public void MQTTPublishAnchor(string topic, byte[] Anchor)
        {
            if (!client.isInitialized)
                Start();

            
            //construct a good string that functions as topic
            topic =  worldAnchorBaseSubscriptionPath+topic + "__" + TimeMethodesForAnchors.getCurrentTimeforAnchorAsString();

            topic = topic.Replace("//", "/");


            client.MQTTPublishMessage(topic, Anchor);
        }

        /// <summary>
        ///
        /// </summary>
        public void MQTTPublishMessage(string topic, string payload)
        {
            if (!client.isInitialized)
                Start();

            client.MQTTPublishMessage(topic, payload);
        }


        /// <summary>
        ///
        /// </summary>
        public void MQTTPublishMessage(string topic, byte[] payload)
        {
            if (!client.isInitialized)
                Start();

            client.MQTTPublishMessage(topic, payload);
        }






    }
}
