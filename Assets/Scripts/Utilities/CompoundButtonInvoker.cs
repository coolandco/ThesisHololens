using HoloToolkit.Unity.Buttons;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.utilities
{
    public class CompoundButtonInvoker : MonoBehaviour
    {
        public UnityEvent toInvoke;

        [Tooltip("should we invoke, when tapped? ")]
        public bool isEnabled = true;

        private float lastTimeTapped = 0f;

        [Tooltip("may be manipulated for special needs")]
        public float coolDownTime = 0.5f;

        CompoundButton button;

        private void Awake()
        {
            if (toInvoke == null)
                toInvoke = new UnityEvent();
        }

        private void Start()
        {
            button = GetComponent<CompoundButton>();

            if(button != null)
                button.OnButtonClicked += onButtonClicked;

        }

        private void onButtonClicked(GameObject obj)
        {
            if (isEnabled == false)
            {
                return; //do nothing
            }

            if (Time.time < lastTimeTapped + coolDownTime)
            {
                //do Nothing
            }
            else
            {
                lastTimeTapped = Time.time;

                Debug.Log(name + " tapped");
                toInvoke.Invoke();
            }

        }

    }

}
