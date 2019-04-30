using System;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.Manipulation;
using ThesisHololens.Settings;
using UnityEngine;
using UnityEngine.XR.WSA;

/// <summary>
/// 
/// </summary>
namespace ThesisHololens.Settings
{
    /// <summary>
    /// at the moment this class is only local to store the indormation about ant
    /// </summary>
   [RequireComponent(typeof(SelfManipulate))]
    public class WorldAnchorUserForFixedElements : WorldAnchorUser
    {
        SelfManipulate selfManipulate;

        // Use this for initialization
        void Start()
        {
            selfManipulate = GetComponent<SelfManipulate>();
            if (selfManipulate == null)
                return;
            //listen to manipulation events
            selfManipulate.onManipulationEnd.AddListener(onManipulationEnd);
            selfManipulate.onManipulationStart.AddListener(onManipulationStart);


            WorldAnchorManager.Instance.LoadAnchor(this.gameObject, onComplete);

        }

        private void onComplete(bool arg0)
        {
            //dummy
        }


        private void onManipulationStart()
        {
            detachWorldAnchor();
        }

        private void onManipulationEnd()
        {
            attachWorldAnchor();
        }

        public void activatemanipulation()
        {
            //selfManipulate.active = true;
        }

        public void deactivatemanipulation()
        {
            //selfManipulate.active = false;
        }

    }
}