using HoloToolkit.Examples.InteractiveElements;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.UI
{
    /// <summary>
    /// The toggle is a function that switches on on one click and off on another.
    /// </summary>
    public class UIToggle : UIContainer
    {
        //private CompoundButton cButton;
        private LabelTheme text;
        private InteractiveToggle interactiveToggle;

        // Use this for initialization
        void Start()
        {

            interactiveToggle = GetComponent<InteractiveToggle>();
            interactiveToggle.OnSelection.AddListener(onToggleOn);
            interactiveToggle.OnDeselection.AddListener(onToggleOff);

        }

        private void onToggleOff()
        {
            if (!interactionAllowed())
                return;

            Debug.Log(adress + " onToggleOff");
            ItemStates.Instance.updateItem(adress, "OFF", true);
        }

        private void onToggleOn()
        {
            if (!interactionAllowed())
                return;


            Debug.Log(adress + " onToggleOn");
            ItemStates.Instance.updateItem(adress, "ON", true);
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


            Debug.Log("NEW STATE TOGGLE: " + newState);

            //if butt is pressed but new state is "Off"
            if (interactiveToggle.IsSelected && (newState.Equals("OFF"))){
                interactiveToggle.SetState(false);
            }
            //if butt is not pressed but new state is "ON"
            else if (!interactiveToggle.IsSelected && (newState.Equals("ON")))
            {
                interactiveToggle.SetState(true);
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
            text.Default = "On";
            text.Selected = "Off";

            base.setProperties(UIE);
        }
    }
}
