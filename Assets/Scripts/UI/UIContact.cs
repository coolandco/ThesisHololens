using HoloToolkit.Examples.InteractiveElements;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.UI
{
    public class UIContact : UIContainer
    {

        //private CompoundButton cButton;
        private LabelTheme text;
        private InteractiveToggle interactiveToggle;

        // Use this for initialization
        void Start()
        {

            interactiveToggle = GetComponent<InteractiveToggle>();

            if (!readOnly)
            {

                interactiveToggle.OnSelection.AddListener(onToggleOn);
                interactiveToggle.OnDeselection.AddListener(onToggleOff);
            }

        }

        private void onToggleOff()
        {
            if (!interactionAllowed())
                return;

            Debug.Log(adress + " onContactOff");
            ItemStates.Instance.updateItem(adress, "CLOSED", true);
        }

        private void onToggleOn()
        {
            if (!interactionAllowed())
                return;


            Debug.Log(adress + " onContactOn");
            ItemStates.Instance.updateItem(adress, "OPEN", true);
        }


        protected override void onManipulationStart_internal()
        {
            //deactivate input
            interactiveToggle.IsEnabled = false;

        }

        protected override void onManipulationEnd_internal()
        {
            //activate input
            interactiveToggle.IsEnabled = true;
        }


        protected override void stateChanged_internal(string newState)
        {
            //trigger timer to prevent double fireings
            interactionAllowed();


            Debug.Log("NEW STATE CONTACT: " + newState);

            //if butt is pressed but new state is "Off"
            if (interactiveToggle.IsSelected && (newState.Equals("CLOSED")))
            {
                interactiveToggle.SetState(true);
            }
            //if butt is not pressed but new state is "ON"
            else if (!interactiveToggle.IsSelected && (newState.Equals("OPENED")))
            {
                interactiveToggle.SetState(false);
            }

            Debug.Log(adress + " state changed");
            //react to the new state
        }


        // Update is called once per frame
        public override void Update()
        {
            base.Update();
        }

        public override void setProperties(AppData.uniqueIDDevice.UIContainerData UIE)
        {

            text = GetComponent<LabelTheme>();
            text.Default = "OPEN";
            text.Selected = "CLOSED";

            base.setProperties(UIE);
        }


    }
}
