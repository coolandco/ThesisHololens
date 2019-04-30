using System;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.Manipulation;
using ThesisHololens.Settings;
using ThesisHololens.States;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.UI
{
    public abstract class UIContainer : MonoBehaviour {

        /// <summary>
        /// the adress is also the name of the UIcontainer. The adress is Unique.
        /// something like: Multimediawand_HUE1_Toggle
        /// </summary>
        protected string adress = null;

        //is there any string to show with the container?
        //every container may decide itself how to use it
        public string displayText { get; set; }

        public string function_UIContainer { get; private set; }

        public bool readOnly { get; private set; }


        public string specificContainerData1 { get; protected set; }
        public string specificContainerData2 { get; protected set; }
        public string specificContainerData3 { get; protected set; }
        public string specificContainerData4 { get; protected set; }
        public string specificContainerData5 { get; protected set; }


        private float lastTimeTapped = 0f;
        //may be manipulated for special needs
        protected float coolDownTime = 0.5f;
        

        //indicator if interaction is allowed
        private bool isInteractionAllowed = true;


        //is the element ready to go?
        private bool initialized = false;

        public bool Initialized //public get private set
        {
            get
            {
                return initialized;
            } 
            private set
            {
                initialized = value;
            }
        }


        public virtual void Update()
        {
            if (!initialized)
                return;

        }

        /// <summary>
        /// checks cooldown time, prevents double fireings
        /// </summary>
        /// <returns></returns>
        protected bool interactionAllowed()
        {

            if(isInteractionAllowed == false)
            {
                return false;
            }


            if (Time.time < lastTimeTapped + coolDownTime)
            {
                return false;
            }
            else
            {
                lastTimeTapped = Time.time;
                return true;
            }
            
        }

        /// <summary>
        /// will be called from external to tell game objectis beeing manipulated
        /// </summary>
        private void onManipulationStart()
        {
            isInteractionAllowed = false;
            onManipulationStart_internal();
        }

        /// <summary>
        /// will be called from external to tell game objectis stopped beeing manipulated
        /// </summary>
        private void onManipulationEnd()
        {

            isInteractionAllowed = true;
            onManipulationEnd_internal();
        }


        public AppData.uniqueIDDevice.UIContainerData getUIContainerData()
        {

            //give the current container data
            AppData.uniqueIDDevice.UIContainerData myUiContainerData = new AppData.uniqueIDDevice.UIContainerData();


            myUiContainerData.adress = adress;
            myUiContainerData.displayText = displayText;
            myUiContainerData.function_UIContainer = function_UIContainer;
            myUiContainerData.readOnly = readOnly;

            myUiContainerData.posX = transform.localPosition.x;
            myUiContainerData.posY = transform.localPosition.y;
            myUiContainerData.posZ = transform.localPosition.z;

            myUiContainerData.scaleX = transform.localScale.x;
            myUiContainerData.scaleY = transform.localScale.y;
            myUiContainerData.scaleZ = transform.localScale.z;

            myUiContainerData.specificContainerData1 = specificContainerData1;
            myUiContainerData.specificContainerData2 = specificContainerData2;
            myUiContainerData.specificContainerData3 = specificContainerData3;
            myUiContainerData.specificContainerData4 = specificContainerData4;
            myUiContainerData.specificContainerData5 = specificContainerData5;

            return myUiContainerData;
        }


        /// <summary>
        /// this methode gets the new state,if an state is changed
        /// </summary>
        /// <param name="state"></param>
        protected abstract void stateChanged_internal(string state);

        //precheck
        private void stateChanged(string state)
        {
            if (string.IsNullOrEmpty(state))
                return;

            stateChanged_internal(state);
        }

        /// <summary>
        /// this methode will be called, when the editor Mode starts
        /// all interaction should be prohibited
        /// </summary>
        protected abstract void onManipulationStart_internal();

        /// <summary>
        /// this methode will be called, when the editor Mode starts
        /// all interaction can continue
        /// </summary>
        protected abstract void onManipulationEnd_internal();


        /// <summary>
        /// This function needs to be called for initialization
        /// </summary>
        /// <param name="UIE"></param>
        public virtual void setProperties(AppData.uniqueIDDevice.UIContainerData UIE)
        {
            //WHATCH OUT, START HAS NOT BEEN RUN
            //set the properties
            adress = UIE.adress;

            displayText = UIE.displayText;

            readOnly = UIE.readOnly;

            function_UIContainer = UIE.function_UIContainer;

            specificContainerData1 = UIE.specificContainerData1;
            specificContainerData2 = UIE.specificContainerData2;
            specificContainerData3 = UIE.specificContainerData3;
            specificContainerData4 = UIE.specificContainerData4;
            specificContainerData5 = UIE.specificContainerData5;

            //sets the properties for this element
            //the object is now at the desired offset location
            Debug.Log(name + " posx: " + UIE.posX);
            this.transform.localPosition = new Vector3(UIE.posX, UIE.posY, UIE.posZ);
            Debug.Log(name + " posx real: " + transform.position.x);
            this.transform.localScale = new Vector3(UIE.scaleX, UIE.scaleY, UIE.scaleZ);

            //register as Subscriber for a change state
            ItemStates.Instance.subscribeToItem(adress, stateChanged);
            StartCoroutine(SetInitialState());//sets the current state after "start" ran

            //we want to be notified when manipulation starts, if our game object has a self manipulate attached
            GetComponent<SelfManipulate>()?.onManipulationStart.AddListener(onManipulationStart);
            //we want to be notified when manipulation ends, if our game object has a self manipulate attached
            GetComponent<SelfManipulate>()?.onManipulationEnd.AddListener(onManipulationEnd);

            //now UI element is ready to go
            Initialized = true;
        }

        private IEnumerator SetInitialState()
        {
            //to be safe, that the start ran, we wait 2 frames
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            stateChanged(ItemStates.Instance.getState(adress));
        }

        void OnDestroy()
        {
            //unsubscribe, because we will be dead
            if(ItemStates.Instance != null)
                ItemStates.Instance.unsubscribeToItem(adress, stateChanged);


            //tidy up manipulation starts, if our game object has a self manipulate attached
            GetComponent<SelfManipulate>()?.onManipulationStart.RemoveListener(onManipulationStart);
            //tidy up manipulation ends, if our game object has a self manipulate attached
            GetComponent<SelfManipulate>()?.onManipulationEnd.RemoveListener(onManipulationEnd);
        }

    }


}
