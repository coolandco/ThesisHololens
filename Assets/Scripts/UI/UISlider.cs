using HoloToolkit.Examples.InteractiveElements;
using HoloToolkit.Unity;
using System;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.UI
{
    public class UISlider : UIContainer
    {
        //the interactive element of the slider
        private SliderGestureControl sliderGestureControl;

        private GestureInteractive gestureInteractive;


        void Start()
        {
            coolDownTime = 0.2f; //bit faster response

        }


        private void onSliderBarUpdate(float newValue)
        {

            //if interaction is allowed, or it is a limit dimmer number
            if (interactionAllowed() || newValue == 0f || newValue == 100f)
            {

                //gives the state to the itemStates as integer string and publish flag on

                ItemStates.Instance.updateItem(adress, ((int)newValue).ToString(), true);
            }
        }

        protected override void stateChanged_internal(string state)
        {
            float value;

            try
            {
                value = float.Parse(state);
                //Debug.Log("Sliderbar changed: " + value);

                //put new value in
                sliderGestureControl.SetSliderValue(value);
            }
            catch(Exception e)
            {
                Debug.LogError("clould not parse " + state + " to float. Error: " + e.StackTrace);
                Debug.LogError(e.StackTrace);
                return;
            }

        }

        protected override void onManipulationStart_internal()
        {
            gestureInteractive.IsEnabled = false;
        }

        protected override void onManipulationEnd_internal()
        {
            gestureInteractive.IsEnabled = true;
        }


        // Update is called once per frame
        public override void Update()
        {
            //reset rotation
            //this.transform.eulerAngles = new Vector3(0, 0, 0);

            base.Update();

        }

        //in this case, initialiation will be called before instantiation of the game object due to
        //the "maxSliderValue" can only be manipulated before instantiation
        public void preInitialization()
        {
            Debug.Log("preInitialization on UISlider");
            //deactivate this script. we dont need it

            //there were some problems deactivating the billboard script, this semm to work:
            GetComponent<Billboard>().enabled = false;
            GetComponent<Billboard>().TargetTransform = null;
            Destroy(GetComponent<Billboard>());

            //for sliders, we have almost always a range from 0 to 100
            //https://docs.openhab.org/configuration/items.html#type
            GetComponent<SliderGestureControl>().MaxSliderValue = 100;
        }

        public void initialization()
        {

            sliderGestureControl = GetComponent<SliderGestureControl>();
            gestureInteractive = GetComponent<GestureInteractive>();

            //this fires, when the slider bar changes its value
            sliderGestureControl.OnUpdateEvent.AddListener(onSliderBarUpdate);
            //sliderGestureControl.OnUpdateEvent += ;
        }



        public override void setProperties(AppData.uniqueIDDevice.UIContainerData UIE)
        {
            //reset rotation
            //this.transform.eulerAngles = new Vector3(0, 0, 0);

            //finalizes in base class
            base.setProperties(UIE);
        }
    }
}
