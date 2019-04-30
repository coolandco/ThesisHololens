
using HoloToolkit.Unity;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.Communication;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.Settings
{
    public class AppDataManager_MQTT : Singleton<AppDataManager_MQTT>
    {

        private UnityAction<AppData.uniqueIDDevice> invokeOnAppDataReceived;

        [SerializeField]
        [Tooltip("Subscribe to this string + #")]
        private string mqttPathForAppData = "/AppData/";

        /// <summary>
        /// The first time this is called, it will every device (retained)
        /// after that it will only callback for new devices
        /// </summary>
        /// <param name="mqttPathForAppData"></param>
        /// <param name="onAppDataReceived"></param>
        /// //todo return false if subscription failed
        public void subscribeToDeviceDataMqtt(UnityAction<AppData.uniqueIDDevice> onAppDataReceived)
        {
            if (onAppDataReceived == null)
                return;//null check

            if (invokeOnAppDataReceived != null)
            {
                invokeOnAppDataReceived += onAppDataReceived;
            }
            else
            {
                invokeOnAppDataReceived = new UnityAction<AppData.uniqueIDDevice>(onAppDataReceived);
            }

            StartCoroutine(loadDataFromMqtt());

        }

        /// <summary>
        /// first subscribe to the appdata channel
        /// </summary>
        private IEnumerator loadDataFromMqtt()
        {
            //int counter = 3;

            //while (!AppDataExportManager.Instance. && counter > 0)
            //{//first try every 2 sec
            //    yield return new WaitForSeconds(2);
            //    counter--;
            //}


            //while (!AppDataExportManager.Instance.isConnected)
            //{
            //    //then every 20
            //    yield return new WaitForSeconds(20);
            //}
            yield return new WaitForFixedUpdate();

            //wait to receive the app data
            AppDataExportManager.Instance.subscribe(mqttPathForAppData + "#", appDataReceived);

        }


        private void appDataReceived(MqttNetClientAccess.mqttMessage message)
        {
            if(message.payload.Length == 0)
            {
                Debug.Log("AppDatamanager_MQTT received a device whith payload length 0");
                return;
                //this is a deletion message
            }

            string decodedMessage = System.Text.Encoding.UTF8.GetString(message.payload);

            AppData.uniqueIDDevice device = null;
            try
            {
                device = JsonConvert.DeserializeObject<AppData.uniqueIDDevice>(decodedMessage);
            }
            catch {
                Debug.Log("AppDatamanager_MQTT could not deserialize device "+ message.topic);
            }

            //if device was not encoded
            if (device == null || string.IsNullOrEmpty(device.baseAdress))
            {
                return;
            }

            //tell others
            invokeOnAppDataReceived?.Invoke(device);
        }


        /// <summary>
        /// 
        /// </summary>
        public void publishDeviceOverMQTT(AppData.uniqueIDDevice device)
        {
            if (device == null)
                return;

            StartCoroutine(publishDevice(device));

        }

        private IEnumerator publishDevice(AppData.uniqueIDDevice device)
        {
            //int counter = 3;

            //while (!AppDataExportManager.Instance.isConnected && counter > 0)
            //{//first try every 2 sec
            //    yield return new WaitForSeconds(2);
            //    counter--;
            //}


            //while (!AppDataExportManager.Instance.isConnected)
            //{
            //    //then every 20
            //    yield return new WaitForSeconds(20);
            //}

            yield return new WaitForFixedUpdate();

            //publish app data in th baseAdress channel
            AppDataExportManager.Instance.MQTTPublishMessage(mqttPathForAppData + device.baseAdress, JsonConvert.SerializeObject(device, Formatting.Indented));

        }


        /// <summary>
        /// sends an empty message to remove the device
        /// </summary>
        public void removeDeviceOverMQTT(AppData.uniqueIDDevice device)
        {
            if (device == null)
                return;

            StartCoroutine(publishRemoveDevice(device));

        }

        /// <summary>
        /// 
        /// </summary>
        private IEnumerator publishRemoveDevice(AppData.uniqueIDDevice deviceToRemove)
        {
            //int counter = 3;

            //while (!AppDataExportManager.Instance.isConnected && counter > 0)
            //{//first try every 2 sec
            //    yield return new WaitForSeconds(2);
            //    counter--;
            //}


            //while (!AppDataExportManager.Instance.isConnected)
            //{
            //    //then every 20
            //    yield return new WaitForSeconds(20);
            //}

            yield return new WaitForFixedUpdate();

            AppDataExportManager.Instance.MQTTPublishMessage(mqttPathForAppData + deviceToRemove.baseAdress, "");//empty message to remove
        }
    }
}
