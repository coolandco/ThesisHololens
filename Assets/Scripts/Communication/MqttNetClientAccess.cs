using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System.Collections.Concurrent;
using System;
using UnityEngine.Events;
using MQTTnet.Diagnostics;

namespace ThesisHololens.Communication
{
    public class MqttNetClientAccess
    {


        private bool ranOnce = false;

        private IManagedMqttClient client;
        public bool isConnected
        {
            get
            {
                //returns the status of the client
                return client != null ? client.IsConnected : false;
            }
        }

        public bool isInitialized
        {
            get
            {
                return ranOnce;
            }
        }

        /// <summary>
        /// in here are all currently processing Messages
        /// </summary>
        private List<int> currentlyProcessingMessages = new List<int>();


        //for the message quere
        public struct mqttMessage
        {
            public mqttMessage(object sender, MqttApplicationMessageReceivedEventArgs e)
            {
                payload = e.ApplicationMessage.Payload;
                topic = e.ApplicationMessage.Topic;
                retained = e.ApplicationMessage.Retain;
            }

            public byte[] payload { get; private set; }
            public string topic { get; private set; }
            public bool retained { get; private set; }
        }

        private UnityAction<mqttMessage> messageReceivedCallback;


        /// <summary>
        /// The message quere we need to make threadsafe
        /// </summary>
        private ConcurrentQueue<mqttMessage> messageQueue = new ConcurrentQueue<mqttMessage>();


        public void initialize(string ip, string clientName, int AutoReconnectAfter, UnityAction<mqttMessage> messageReceivedCallback,string loggerName = null)
        {
            if (ranOnce)
                return;

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(AutoReconnectAfter))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithClientId(clientName)
                    .WithTcpServer(ip)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(40))
                    .WithKeepAliveSendInterval(TimeSpan.FromSeconds(5))
                    //.WithCleanSession(true)
                    .WithCommunicationTimeout(TimeSpan.FromSeconds(40))
                    //.WithCleanSession(false)
                    .Build())
                .Build();

            if (loggerName != null)
            {

                client = new MqttFactory().CreateManagedMqttClient(new MqttNetLogger(loggerName));
#if DEBUG
                // Write all trace messages to the console window.
                //MqttNetGlobalLogger.LogMessagePublished += (s, e) =>
                //{
                //    var trace = $">> [{e.TraceMessage.Timestamp:O}] [{e.TraceMessage.ThreadId}] [{e.TraceMessage.Source}] [{e.TraceMessage.Level}]: {e.TraceMessage.Message}";
                //    if (e.TraceMessage.Exception != null)
                //    {
                //        trace += Environment.NewLine + e.TraceMessage.Exception.ToString();
                //    }

                //    Debug.Log("[LOGTRACE]: " + trace);
                //};
#endif
            }
            else
            {
                client = new MqttFactory().CreateManagedMqttClient();
            }

            //when connected
            client.Connected += clientConnected;
            client.Disconnected += clientDisconnected;
            // register to message received
            client.ApplicationMessageReceived += client_MqttMsgPublishReceived;
            client.ApplicationMessageProcessed += client_MqttMsgPublishProcessed;

            //internal Processing of new messages
            this.messageReceivedCallback = messageReceivedCallback;

            Debug.Log("newClient start async");
            client.StartAsync(options);

            ranOnce = true;
        }




        //still the start up procedure
        private void clientConnected(object sender, MqttClientConnectedEventArgs e)
        {


            Debug.Log("newClient connected");
            //now we can subscribe


        }

        private void clientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            Debug.Log("disconected");
                
        }

        private void client_MqttMsgPublishReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            //Debug.Log("newClient received message: " + e.ApplicationMessage.Topic);
            //a message to enquere
            messageQueue.Enqueue(new mqttMessage(sender, e));
        }


        private void client_MqttMsgPublishProcessed(object sender, ApplicationMessageProcessedEventArgs e)
        {
            if (e.HasFailed)
            {
                Debug.Log("A message with the topic \"" + e.ApplicationMessage.ApplicationMessage.Topic + "\" has been Failed to sent, retrying" + e.Exception);
                return;
            }
            //TODO (done): an indicator if all messages have been processes and the application is save to exit
            if (!currentlyProcessingMessages.Remove(e.ApplicationMessage.ApplicationMessage.GetHashCode()))
            {
                Debug.LogError("A message error has occured. A message was processed that should not have been sent");
            }

            //TODO(obsolete): a retransmitt sptop, so that there is no loop --> noticed the managed client retransmitts the message, if it failed
        }

        /// <summary>
        /// this should be called from the main thread of unity
        /// for processing the received messages
        /// </summary>
        public void Update()
        {
            //if (UnityEngine.Random.value > 0.995)
            //{
            //    Debug.Log("currently are " + currentlyProcessingMessages.Count + " Messages in the sending quere");
            //}


            dequereReceivedMessage();
        }

        private void dequereReceivedMessage()
        {
            mqttMessage mqttMsg;
            if (messageQueue.TryDequeue(out mqttMsg) == false)
            {
                return;
            }

            messageReceivedCallback.Invoke(mqttMsg);
        }



        public void Subscribe(string subscription)
        {
            if (!ranOnce)
                return;
            client.SubscribeAsync(new TopicFilterBuilder().WithTopic(subscription).WithExactlyOnceQoS().Build());

        }

        public void Unsubscribe(string subscribtion)
        {
            if (!ranOnce)
                return;
            client.UnsubscribeAsync(new string[] { subscribtion });

        }


        public void MQTTPublishMessage(string topic, byte[] payload)
        {

            topic = topic.Replace("//", "/");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            Debug.Log("newClient publishes message: " + topic);

            client.PublishAsync(message);
            currentlyProcessingMessages.Add(message.GetHashCode());

        }


        /// <summary>
        ///
        /// </summary>
        public void MQTTPublishMessage(string topic, string payload)
        {
            topic = topic.Replace("//", "/");

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            Debug.Log("newClient publishes message: " + topic);

            client.PublishAsync(message);
        }


    }
}
