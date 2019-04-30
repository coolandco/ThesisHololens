using System;
using ThesisHololens.Manipulation;
using ThesisHololens.Settings;
using ThesisHololens.UI;
using ThesisHololens.utilities;
using UnityEngine;

namespace ThesisHololens.Devices
{
    [RequireComponent(typeof(WorldAnchorUser))]
    [RequireComponent(typeof(SelfManipulate))]
    [RequireComponent(typeof(TappedReceiver))]
    public class Device : MonoBehaviour//this class needs refactoring
    {

        //this unique baseAdress is for identifying the Mqtt Device

        public string baseAdress { get; private set; }

        private AppData.uniqueIDDevice deviceData;
        public AppData.uniqueIDDevice DeviceData {
            //returns a clone of the device data
            get
            {
                return (AppData.uniqueIDDevice) deviceData.Clone();
            }
        }

        public float HoverOffsetZ { get; set; }

        private WorldAnchorUser worldAnchorUser;

        private SelfManipulate selfManipulate;
        private Vector3 onManipStartPos;

        public void initialize(AppData.uniqueIDDevice deviceData, bool freshlyAddedDevice = false, bool isDeviceRefresh = false)
        {
            
            this.deviceData = deviceData;


            //baseadress
            baseAdress = deviceData.baseAdress;
            //name
            this.name = deviceData.baseAdress;

            HoverOffsetZ = deviceData.HoverOffsetZ;

            //self manipulate
            selfManipulate = GetComponent<SelfManipulate>();

            //anchor
            worldAnchorUser = GetComponent<WorldAnchorUser>();

            if (!isDeviceRefresh)//if this script is refreshed, we do not need to run this again
            {

                //Manipulation Events:
                selfManipulate.onManipulationStart.AddListener(onManipulationStart);
                selfManipulate.onManipulationEnd.AddListener(onManipulationEnd);



                //worldAnchorUser.linkedAnchor = deviceData.linkedAnchor;

                //start the process of restoring an anchor
                worldAnchorUser.WorldAnchorOperations();


            }


            //set the properties of this mqtt device
            //the position will be set relative to the parent "AllDevices"
            //TODO: is the positioning idea right?


            if (freshlyAddedDevice)
            {
                //set the device infront of the user
                this.transform.position = Camera.main.transform.position + Camera.main.transform.forward * 2f;

                //editor mode must be on
                selfManipulate.active = true;
            }


            //transform.localPosition= new Vector3(deviceData.posX, deviceData.posY, deviceData.posZ);//we have 1 anchor per device and dont need that


            if(isDeviceRefresh)
                //we need to move, so detach
                worldAnchorUser.detachWorldAnchor();

            transform.localEulerAngles = new Vector3(deviceData.rotX, deviceData.rotY, deviceData.rotZ);

            transform.localScale = new Vector3(deviceData.scaleX, deviceData.scaleY, deviceData.scaleZ);

            if (isDeviceRefresh)
                //we need to move, so detach
                worldAnchorUser.attachWorldAnchor();


            if (isDeviceRefresh)
                Debug.Log(name + " refreshed");
            else
                Debug.Log(name + " initialized");
        }


        //will be called from the "TappedResponder"
        public void attachAppBar()
        {

            //gets the containerBar from the singelton
            //moves the containerBar to the tapped gameObject
            UIContainerBar containerBar = UIContainerBar.Instance;
            if(containerBar.Target != this.gameObject)//only if change
            {
                containerBar.Target = this.gameObject;
                containerBar.HoverOffsetZ = HoverOffsetZ;
                containerBar.onTargetChange += deattachedAppBar;

                generateUI();
            }

        }

        private void deattachedAppBar(GameObject oldDevice, GameObject newDevice)
        {
            UIContainerBar.Instance.onTargetChange -= deattachedAppBar;

            //we should have been there
            if (oldDevice == null || oldDevice != this.gameObject)
            {
                Debug.LogWarning("not saving ui data, game object was probably destroyed");
                return;
            }

            saveCurrentUIData();
        }

        private void OnDestroy()
        {

            selfManipulate?.onManipulationStart?.RemoveListener(onManipulationStart);
            selfManipulate?.onManipulationEnd?.RemoveListener(onManipulationEnd);

            //on destroy of this game object we need to set the target away from us;
            if (UIContainerBar.Instance.Target  != null && UIContainerBar.Instance.Target == this.gameObject)
            {
                UIContainerBar.Instance.clearUIElements();
                UIContainerBar.Instance.Target = null;
            }
                


        }

        /// <summary>
        /// saves the current UI data, if the app bar is to this device attached
        /// </summary>
        public void saveCurrentUIData()
        {
            if (UIContainerBar.Instance.Target != this.gameObject)
            {
                Debug.LogWarning("App bar was not attached, while saving ui data");
                return;
            }
            var newUIdata = UIContainerBar.Instance.getCurrentUIInformation();

            //if the new and the old is not the same
            if(! AppDataManager.checkUiContainerData(newUIdata, deviceData.myUIContainerData))
            {
                //overwrite all the ui data
                deviceData.myUIContainerData = newUIdata;

                //tell the app data manager
                AppDataManager.Instance.onManipulationFinished(deviceData);
            }



        }

        /// <summary>
        /// Also refreshes the current UI, so it is properly set up after a change
        /// </summary>
        public void generateUI()
        {

            //gets the containerBar from the singelton
            UIContainerBar containerBar = UIContainerBar.Instance;

            //DeviceManager devMan = GetComponentInParent<DeviceManager>();

            containerBar.clearUIElements();

            //do something for each Device element 
            foreach (AppData.uniqueIDDevice.UIContainerData IUElem in deviceData.myUIContainerData)
            {
                //passes the UI informations to the UIContainerBar
                Debug.Log("add UIElement");
                containerBar.addUiElement(IUElem);

            }

        }

        private void updateMyDeviceData()
        {
            deviceData.baseAdress = baseAdress;
            //stays the same: jDD.DeviceType3D
            deviceData.HoverOffsetZ = HoverOffsetZ;

            //always 0
            //TODO:
            deviceData.posX = 0;
            deviceData.posY = 0;
            deviceData.posZ = 0;

            //deviceData.posX = transform.localPosition.x;
            //deviceData.posY = transform.localPosition.y;
            //deviceData.posZ = transform.localPosition.z;

            deviceData.scaleX = transform.localScale.x;
            deviceData.scaleY = transform.localScale.y;
            deviceData.scaleZ = transform.localScale.z;

            var rotVector = transform.localEulerAngles;

            deviceData.rotX = rotVector.x;
            deviceData.rotY = rotVector.y;
            deviceData.rotZ = rotVector.z;

            //dont bother about UI data.
            //the ui manages that itself

        }

        //gets called from the self manipulate
        public void onManipulationStart()
        {
            //we need to move, so detach
            worldAnchorUser.detachWorldAnchor();

            onManipStartPos = transform.position;
        }

        //gets called from self manipulate on a end of manipulation
        public void onManipulationEnd()
        {
            //after manipulation, lock device in place again
            if (selfManipulate.hasBeenMoved)
            {
                if (Vector3.Distance(onManipStartPos, transform.position) < 0.15)
                {
                    transform.position = onManipStartPos;//if movement to small

                    //device has not been moved, so restore the old anchor, but first check if device has been manipulated in other properties
                    updateMyDeviceData();

                    //save new properties on file
                    AppDataManager.Instance.onManipulationFinished(deviceData);

                    //device has not been moved, so restore the old anchor
                    worldAnchorUser.attachWorldAnchor(); //new anchor but not export

                    return;
                }

                worldAnchorUser.attachWorldAnchor();

                //exports the anchor to mqtt
                worldAnchorUser.exportAnchorToWorldAnchorExportManager();

                updateMyDeviceData();

                //save new properties to file
                AppDataManager.Instance.onManipulationFinished(deviceData);
            }
            else
            {
                worldAnchorUser.attachWorldAnchor(); //new anchor but not export
            }


        }
        //TODO: on editor mode start / end
        //bc. only update devices on en of editor mode, that have been moved

    }
}
