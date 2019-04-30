using HoloToolkit.Unity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThesisHololens.Devices;
using ThesisHololens.utilities;
using UnityEngine;

namespace ThesisHololens.Settings
{
    public class AppDataManager : Singleton<AppDataManager>
    {

        //represents the current loaded Data
        private AppData loadedData = null;

        //thes represents the current data From the network to check if it is the same as the data from the local file.
        //there are courutines to check the state of the local data
        //if there is a device missing in the network, it sould be deleted in the local data
        //it has only relevance in the beginning of the application
        private AppData networkData = null;

        //this is a score that tells if the network is working
        //it has only relevance in the beginning of the application
        private int NetworkWorkingConfidence = 0;

        //if this timer hits 0, a comparison between the network received devices and the local devices will be held
        private int timerForNetworkDeviceCheck = 30;

        [Tooltip("The time to wait before synchronizing the network and local devices after a device has been received the first time.")]
        public int timerForNetworkDeviceCheckDefault = 30;


        [SerializeField]
        private string ResourcePath = "DeviceTypes/";

        public string resourcePath
        {
            get
            {
                return ResourcePath;
            }
        }

        /// <summary>
        /// Required: Which transform is parent of the devices
        /// </summary>
        [SerializeField]
        private Transform deviceParent;

        [SerializeField]
        [Tooltip("Time to wait, before assuming, that there is no mqtt data")]
        private float mqttTimeout = 10;


        [SerializeField]
        [Tooltip("between each try is 2 seconds")]
        private int maxRetriesForMqttReconnectWait = 5;


        //UTF8-BOM encodin präambel
        private readonly string _byteOrderMarkUtf8 =
            Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());



        // Use this for initialization
        void Start()
        {
            networkData = new AppData();
            networkData.MqttDeviceData = new List<AppData.uniqueIDDevice>();

            LoadAppDataFromFile();

        }

        /// <summary>
        /// loads the app data first from file and then from  MQTT
        /// </summary>
        private void LoadAppDataFromFile()
        {
            //try to load from file
            string filePath = getDataFilePath();
            if (File.Exists(filePath))
            {
                // Read the json from the file into a string
                string dataAsJson = File.ReadAllText(filePath);
                // Pass the json to JsonUtility, and tell it to create a GameData object from it
                //loadedData = JsonUtility.FromJson<AppData>(dataAsJson);
                loadedData = Newtonsoft.Json.JsonConvert.DeserializeObject<AppData>(dataAsJson);

                //Load each mqtt Device
                for (int i = 0; i < loadedData.MqttDeviceData.Count; i++)
                {
                    //INFO:
                    // there should not be deveices to remove in the app data from the file, so we dont have to do that
                    generateOrRefreshDevice(loadedData.MqttDeviceData[i]);

                }
            }
            else
            {
                Debug.LogError("Cannot load data from disk!\n now trying from Mqtt");
            }


            Debug.Log("Trying to load mqtt appData");
            AppDataManager_MQTT.Instance.subscribeToDeviceDataMqtt(newDeviceReceived);


            //for the case, that there is no data to load in Mqtt, we need to set up loaded data manually
            StartCoroutine(setUpLoadedData());
        }


        private IEnumerator setUpLoadedData()
        {
            yield return new WaitForSeconds(mqttTimeout);

            if (loadedData == null)
            {
                loadedData = new AppData();
                loadedData.MqttDeviceData = new List<AppData.uniqueIDDevice>();
            }
        }



        /// <summary>
        ///
        /// </summary>
        /// <param name="device"></param>
        /// <param name="isFreshlyAdded"></param>
        /// <param name="getsTargetFromAppBar">Tries to attach the app bar to the newly generated device</param>
        private void generateOrRefreshDevice(AppData.uniqueIDDevice device, bool isFreshlyAdded = false, bool getsTargetFromAppBar = false, Device oldDeviceToRefresh = null)
        {
            if(device == null || device.baseAdress == null)
            {
                Debug.LogError("Error generating the device");
                throw new ArgumentOutOfRangeException();
            }

            GameObject mqttDevice;

            //this instantiates a Prefab
            //the prefab is in the "Recources" folder
            try
            {

                if(oldDeviceToRefresh == null)
                {
                    //we have to figure first out, of there is already a device with the same name attached
                    if (DeviceManager.Instance.hasDeviceAttached(device.baseAdress))
                    {
                        Debug.LogError("There is already a Device with the same name attached. Remove that first bevore trying to generate a new one");
                        return;
                    }

                    //use resource path to access
                    mqttDevice = Instantiate(Resources.Load(ResourcePath + device.DeviceType3D), DeviceManager.Instance.transform) as GameObject;

                    //get the MqttDevice script from the instantiated prefab and give it the neccesary init
                    mqttDevice.GetComponent<Device>().initialize(device, isFreshlyAdded);

                    //tell the device manager, that a new device is attached
                    DeviceManager.Instance.newDeviceAttached(mqttDevice);


                }
                else
                {
                    //this prevents the scripts from the device to be reinitialized. Could be the solution tho world anchor problems
                    oldDeviceToRefresh.initialize(device, isFreshlyAdded, true);

                    mqttDevice = oldDeviceToRefresh.gameObject;

                }

                if (getsTargetFromAppBar)
                    mqttDevice.GetComponent<Device>().attachAppBar();
            }
            catch(Exception e)
            {
                //TODO: different error
                Debug.LogError("The Device " + device.DeviceType3D + " could not be generated. Error: " + e.Message);
            }
        }

        // in here come all received devices from mqtt
        private void newDeviceReceived(AppData.uniqueIDDevice receivedDevice)
        {
            //set up loaded data if there is none
            if (loadedData == null)
            {
                loadedData = new AppData();
                loadedData.MqttDeviceData = new List<AppData.uniqueIDDevice>();
            }

            if (receivedDevice == null || string.IsNullOrEmpty(receivedDevice.baseAdress))
            {
                Debug.LogError("AppDataManager: error Receiving device data over MQTT");
                return;
            }

            //The more devices we receive from the network, the more confident we are, that the network works properly
            NetworkWorkingConfidence++;
            if (NetworkWorkingConfidence == 1)
                StartCoroutine(CompareNetworkDevicesTimer());
            timerForNetworkDeviceCheck = timerForNetworkDeviceCheckDefault; //reset the timer when we receive devices


            //is is a delete order?
            if (receivedDevice.DeviceType3D == "delete")
            {
                removeDeviceFromAppDataAndDeleteFromHierarchie(receivedDevice.baseAdress);
            }
            else
            { //add stuff

                if(networkData != null)
                //add the device to the from network received device
                networkData.MqttDeviceData.Add(receivedDevice);

                if (doesDeviceExistAndIsSame(receivedDevice) == false)
                { //create or regenerate device

                    //we remove the existing device (if there is one in hierarchie)
                    //DeviceManager.Instance.RemoveDevice(receivedDevice.baseAdress);

                    //then wereplace the device in our loadedData
                    loadedData.MqttDeviceData.RemoveAll(x => x.baseAdress == receivedDevice.baseAdress);
                    loadedData.MqttDeviceData.Add(receivedDevice);

                    //then we save the loaded Data to file
                    saveLoadedDataToFile();

                    //uses existing device if there is one
                    generateOrRefreshDevice(receivedDevice,false,false,DeviceManager.Instance.getDevice(receivedDevice.baseAdress)?.GetComponent<Device>());

                }//else we have to do nothing

            }
        }

        private IEnumerator CompareNetworkDevicesTimer()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);

                timerForNetworkDeviceCheck--;

                if (timerForNetworkDeviceCheck < 1) // time ran up
                {
                    if(NetworkWorkingConfidence < 3)//if we are not shure, that the network is working properly, then refresh the timer
                    {
                        timerForNetworkDeviceCheck = timerForNetworkDeviceCheckDefault;
                        continue;
                    }


                    CompareNetworkDevices();
                    yield break;
                }
            }
        }

        //this should be called once from CompareNetworkDevicesTimer()
        //this should not affect devices that exist in the network, but not locally
        private void CompareNetworkDevices()
        {
            List<AppData.uniqueIDDevice> remeberDevice = new List<AppData.uniqueIDDevice>();
            foreach(AppData.uniqueIDDevice localDevice in loadedData.MqttDeviceData)
            {
                bool matches = false;
                foreach (AppData.uniqueIDDevice networkDevice in networkData.MqttDeviceData)
                {
                    if (localDevice.baseAdress == networkDevice.baseAdress)
                    {
                        matches = true;
                        break;
                    }
                        
                }

                //matches false
                if (matches == false)
                {
                    remeberDevice.Add(localDevice);
                }

            }

            //if matches now false then remove the local device, because it didnt exist in the network
            foreach (AppData.uniqueIDDevice localDevice in remeberDevice)
            { 
                removeDeviceFromAppDataAndDeleteFromHierarchie(localDevice.baseAdress);
            }

            //now free the ram up:
            networkData.MqttDeviceData = null;
            networkData = null;

        }


        /// <summary>
        /// removes a device from hierarchie and from the current app data and saves it to file
        /// Basically removes a device locally
        /// </summary>
        /// <param name="baseAdress"></param>
        public void removeDeviceFromAppDataAndDeleteFromHierarchie(string baseAdress)
        {
            //remove stuff
            if (!Application.isEditor)
                WorldAnchorManager.Instance.removeAnchor(baseAdress);

            DeviceManager.Instance.RemoveDevice(baseAdress);

            //deletes wa info from the prefs
            PlayerPrefs.DeleteKey(baseAdress);


            //remove from local storage
            if (0 < loadedData.MqttDeviceData.RemoveAll(x => x.baseAdress == baseAdress))//if something was removed
                //then we save the loaded Data to file
                saveLoadedDataToFile();

        }


        /// <summary>
        /// modifies the device 3d type to "delete" and sends it to MQTT
        /// also tries to delete the WorldAnchors
        /// </summary>
        /// <param name="baseAdress"></param>
        public void sendDeleteOrderForWorldAnchorAndAppData(AppData.uniqueIDDevice device)
        {
            device.DeviceType3D = "delete";

            AppDataManager_MQTT.Instance.publishDeviceOverMQTT(device);//sends a device with the remove command to all active members in the network
            AppDataManager_MQTT.Instance.removeDeviceOverMQTT(device);//sends a remove command to clean up the server


            var anchorInfo = WorldAnchorAvailableOnlineManager.Instance.getAnchorAvailableOnline(device.baseAdress);
            if(!anchorInfo.Equals(default(KeyValuePair<string, DateTime>))){//if there is an anchor available
                AppDataExportManager.Instance.MQTTPublishMessage(AppDataExportManager.Instance.worldAnchorBaseSubscriptionPath +
                    TimeMethodesForAnchors.getAnchorNameWithTimeFromKeyValuePair(anchorInfo),
                    "");//deletion order for retained messages

                AppDataExportManager.Instance.MQTTPublishMessage(WorldAnchorAvailableOnlineManager.Instance.worldAnchorStatusSubscriptionPath +
                    TimeMethodesForAnchors.getAnchorNameWithTimeFromKeyValuePair(anchorInfo),
                    "");//deletion order for retained messages
            }


            //deletes wa info from the prefs
            PlayerPrefs.DeleteKey(device.baseAdress);

            //now remove the device locally
            removeDeviceFromAppDataAndDeleteFromHierarchie(device.baseAdress);
        }



        /// <summary>
        /// the editor of a new device should call this
        /// </summary>
        /// <param name="finishedDevice">the device</param>
        public void onEditorFinished(AppData.uniqueIDDevice finishedDevice)
        {

            //set up loaded data if there is none
            if (loadedData == null)
            {
                loadedData = new AppData();
                loadedData.MqttDeviceData = new List<AppData.uniqueIDDevice>();
            }

            if (finishedDevice == null || string.IsNullOrEmpty(finishedDevice.baseAdress))
            {
                Debug.LogError("AppDataManager: error finalizing device data after Editor");
                return;
            }

            //we look, if the editor modifing or creating a device
            if (doesDeviceExistAndIsSame(finishedDevice))
                return; //editor did not change anything so abort

            //the device was modified or is new
            //we replace the device in our loadedData
            int devicesRemoved = loadedData.MqttDeviceData.RemoveAll(x => x.baseAdress == finishedDevice.baseAdress);
            loadedData.MqttDeviceData.Add(finishedDevice);

            //if more than 
            bool IsNewDevice = devicesRemoved > 0 ? false : true;

            //then we save the modified Data to file
            saveLoadedDataToFile();

            //remove device fromHierarchie if it exists
            // DeviceManager.Instance.RemoveDevice(finishedDevice.baseAdress);

            //set the target of the container bar to device

            //refreshes the existing device, if there is one

            var existingDevice = DeviceManager.Instance.getDevice(finishedDevice.baseAdress);

            generateOrRefreshDevice(finishedDevice, IsNewDevice, IsNewDevice, existingDevice?.GetComponent<Device>()) ;

            //refreshes the UI, so it will be saved properly
            existingDevice?.GetComponent<Device>()?.generateUI();




            //now we can tell everyone over mqtt
            AppDataManager_MQTT.Instance.publishDeviceOverMQTT(finishedDevice);

        }


        public void onManipulationFinished(AppData.uniqueIDDevice deviceData)
        {
            //we need to force the data to be published over mqtt
            //this will only be called if the device was manipulated

            //we do not need to swap the data in loaded data, because we have a call by reference in the mqttDevice class

            //save the modified Data to file
            saveLoadedDataToFile();

            //now we can tell everyone over mqtt
            AppDataManager_MQTT.Instance.publishDeviceOverMQTT(deviceData);
        }


        /// <summary>
        /// saves the vurrently loaded data to file
        /// </summary>
        private void saveLoadedDataToFile()
        {
            if (loadedData == null)
                return;


            string filePath = getDataFilePath();


            string jsonData = JsonConvert.SerializeObject(loadedData, Formatting.Indented);//pretty Print

            try
            {
                //overwrites all the file or creates it
                File.WriteAllText(filePath, jsonData);
            }
            catch
            {
                Debug.LogError("Failed to save AppData to file");
            }
        }

        /// <summary>
        /// if found device in loadedData does not exist or 
        /// is not the same as the deviceToCompare or 
        /// the deviceToCompare /found device does not exist in hierarchie or
        /// the container data of the device is different
        /// return false
        /// </summary>
        /// <param name="deviceToCompare"></param>
        /// <returns></returns>
        private bool doesDeviceExistAndIsSame(AppData.uniqueIDDevice deviceToCompare)
        {
            if (loadedData == null)
                return false;

            if (deviceToCompare == null || string.IsNullOrEmpty(deviceToCompare.baseAdress))
            {
                Debug.LogError("AppDataManager: error comparing state of the device");
                throw new ArgumentOutOfRangeException();
            }

            //if found device does not exist or is not the same as the new device or the device does not exist in hierarchie
            var foundDevice = loadedData.MqttDeviceData.Find(x => x.baseAdress == deviceToCompare.baseAdress);
            if (foundDevice == null || 
                !foundDevice.Equals(deviceToCompare) || 
                !DeviceManager.Instance.hasDeviceAttached(deviceToCompare.baseAdress) || 
                !checkUiContainerData(foundDevice.myUIContainerData,deviceToCompare.myUIContainerData)) //TODO: To test
            {
                return false;
            }
            else
            {
                return true;
            }

        }


        private string getDataFilePath()
        {

            // Path.Combine combines strings into a file path
            // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build
            string filePath; //= Path.Combine(Application.persistentDataPath, "/data.json");

            if (Application.isEditor)
            {
                filePath = Application.dataPath + "/data.json";
                Debug.Log(filePath);

            }
            else
            {

                filePath = Application.persistentDataPath + "/data.json";
                Debug.Log(filePath);
            }

            return filePath;

        }


        /// <summary>
        /// returns tru, if same
        /// </summary>
        /// <param name="myUIContainerData1"></param>
        /// <param name="myUIContainerData2"></param>
        /// <returns></returns>
        public static bool checkUiContainerData(List<AppData.uniqueIDDevice.UIContainerData> myUIContainerData1, List<AppData.uniqueIDDevice.UIContainerData> myUIContainerData2)
        {
            if (myUIContainerData1.Count != myUIContainerData2.Count)
                return false;
            try
            {
                foreach (AppData.uniqueIDDevice.UIContainerData container in myUIContainerData1)//foreach must be a counterpart
                {
                    //and every counterpart must be true
                    if (!myUIContainerData2.Find(x => x.adress == container.adress).Equals(container))
                        return false;

                }
            }
            catch
            {
                return false;
            }

            return true;
        }


    }
}
