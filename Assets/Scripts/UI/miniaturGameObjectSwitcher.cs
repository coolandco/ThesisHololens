using HoloToolkit.Unity.Buttons;
using UnityEngine;

namespace ThesisHololens.UI.EditorMenu
{
    [RequireComponent(typeof(MiniaturGameObjectScript))]
    [RequireComponent(typeof(CompoundButton))]
    public class miniaturGameObjectSwitcher : MonoBehaviour
    {

        MiniaturGameObjectScript miniaturGameObjectScript;
        CompoundButton button;

        private GameObject Normal_toCopy;
        /// <summary>
        /// The game object does not need to be Instantiated, because we make a copy
        /// </summary>
        public GameObject normal_toCopy{
            get
            {
                return Normal_toCopy;
            }
            set
            {
                if (value == null)
                    return;

                value.SetActive(false);

                Normal_toCopy = Instantiate(value,this.transform);
                Normal_toCopy.SetActive(false);

                value.SetActive(true);

            }
        }

        private GameObject Alternative_toCopy;
        /// <summary>
        /// The game object does not need to be Instantiated, because we make a copy, just bevore showing
        /// </summary>
        public GameObject alternative_toCopy
        {
            get
            {
                return Alternative_toCopy;
            }
            set
            {
                if (value == null)
                    return;
                value.SetActive(false);

                Alternative_toCopy = Instantiate(value, this.transform);
                Alternative_toCopy.SetActive(false);

                value.SetActive(true);
            }
        }

        // Use this for initialization
        void Start()
        {
            miniaturGameObjectScript = GetComponent<MiniaturGameObjectScript>();
            button = GetComponent<CompoundButton>();

            button.StateChange += stateChange;
        }

        private void stateChange(ButtonStateEnum obj)
        {
            if (normal_toCopy == null || alternative_toCopy == null)
                return;

            if( obj == ButtonStateEnum.ObservationTargeted || obj == ButtonStateEnum.Targeted)
            {
                alternative_toCopy.SetActive(true);
                miniaturGameObjectScript.toShowInstance = Instantiate(alternative_toCopy);
                alternative_toCopy.SetActive(false);
            }
            else if((obj == ButtonStateEnum.Observation || obj == ButtonStateEnum.Interactive) && button.isActiveAndEnabled) // active and enabled, because, when disabeling the go, button fires "observation event"
            {
                normal_toCopy.SetActive(true);
                miniaturGameObjectScript.toShowInstance = Instantiate(normal_toCopy);
                normal_toCopy.SetActive(false);
            }
        }

        void OnDestroy()
        {
            if(button != null)
                button.StateChange -= stateChange;
        }
    }
}
