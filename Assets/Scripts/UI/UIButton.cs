using System;
using System.Collections;
using HoloToolkit.Unity.Buttons;
using ThesisHololens.States;
using UnityEngine;


namespace ThesisHololens.UI
{
    /// <summary>
    /// This script will be attached to a compoundButton
    /// Abutton is normaly a Function that switches from off to on and then back again
    /// </summary>
    public class UIButton : UIContainer
    {
        //private CompoundButton cButton;
        private CompoundButtonText text;
        private CompoundButtonIcon icon;
        private CompoundButton button;


        // Use this for initialization
        void Start()
        {
            button = GetComponent<CompoundButton>();

            //WATCH OUT, GETS FIRED TWO TIMES
            //GetComponent<CompoundButton>().OnButtonClicked += onButtonClicked; //subscribe to this event
            button.OnButtonPressed += onButtonClicked;
            

        }

        private void onButtonClicked(GameObject obj)
        {
            if (!interactionAllowed())
                return;
            //do something:

            //change text
            //icon

            //send Mqtt Message
            Debug.Log("Clicked " + obj.name);

            ItemStates.Instance.updateItem(adress, "ON", true);

            StartCoroutine(SendOff());

        }

        private IEnumerator SendOff()
        {
            yield return new WaitForSeconds(0.5f);

            ItemStates.Instance.updateItem(adress, "OFF", true);
        }

        protected override void stateChanged_internal(string state)
        {

            //react to the new state
        }

        protected override void onManipulationStart_internal()
        {
            //TODO: Test
            button.ButtonState = ButtonStateEnum.Disabled;
        }

        protected override void onManipulationEnd_internal()
        {
            button.ButtonState = ButtonStateEnum.Observation;//back to normal state
        }

        // Update is called once per frame
        public override void Update()
        {
            //call the update form the base
            base.Update();

        }

        
        public override void setProperties(AppData.uniqueIDDevice.UIContainerData UIE)
        {


            text = GetComponent<CompoundButtonText>();
            text.Text = UIE.displayText;

            icon = GetComponent<CompoundButtonIcon>();
            icon.IconName = UIE.specificContainerData1;
            

            //finalizes in base class
            base.setProperties(UIE);
        }
    }
}
