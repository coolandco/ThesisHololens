using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThesisHololens.Manipulation;
using ThesisHololens.utilities;
using UnityEngine;

namespace ThesisHololens.Devices
{
    public class DeviceManager : Singleton<DeviceManager>
    {



        /// <summary>
        /// deactivates interaction on all children, that have the tappedReceiver
        /// </summary>
        public void deactivateInteraction()
        {
            foreach (TappedReceiver tr in GetComponentsInChildren<TappedReceiver>())
            {
                tr.isEnabled = false;
            }

        }


        /// <summary>
        /// activates interaction on all children, that have the tappedReceiver
        /// </summary>
        public void activateInteraction()
        {
            foreach (TappedReceiver tr in GetComponentsInChildren<TappedReceiver>())
            {
                tr.isEnabled = true;
            }

        }


        public void newDeviceAttached(GameObject newDevice)
        {
            //TODO: maybe fire a event to tell others?

            GetComponent<ToggleEdit>().prepareGameobjectForManipulation(newDevice, ManipulationMode.MoveScaleAndRotate, true);
                       
            
        }

        public void RemoveDevice(string deviceName)
        {
            if (deviceName == null)
                return;

            var GO = gameObject.transform.Find(deviceName);

            if (GO != null)
            {
                //soit is out of the way
                GO.gameObject.transform.parent = null;
                Destroy(GO.gameObject);
            }
                
        }

        public GameObject getDevice(string deviceName)
        {
            if (deviceName == null)
                return null;

            var GO = gameObject.transform.Find(deviceName);

            if (GO == null)
                return null;
            else
                return GO.gameObject;

        }

        public bool hasDeviceAttached(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                throw new ArgumentOutOfRangeException();

            return gameObject.transform.Find(deviceName) != null;
        }

    }
}