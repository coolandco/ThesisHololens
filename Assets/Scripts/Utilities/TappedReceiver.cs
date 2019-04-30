using HoloToolkit.Unity.InputModule;
using UnityEngine;
using UnityEngine.Events;


namespace ThesisHololens.utilities
{

    public class TappedReceiver : MonoBehaviour, IInputHandler {

        public UnityEvent toInvoke;

        [Tooltip("should we invoke, when tapped? ")]
        public bool isEnabled = true;

        private float lastTimeTapped = 0f;


        [Tooltip("may be manipulated for special needs")]
        public float coolDownTime = 0.5f;

        private void Awake()
        {
            if (toInvoke == null)
                toInvoke = new UnityEvent();
        }

        private void Start()
        {
            //adds this game object, so we receive events
            //base.interactables.Add(this.gameObject);
        }


        //receives an event for tapped
        public void OnInputDown(InputEventData eventData)
        {
            if(isEnabled == false)
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

            //we did someshing with the tap, so we can use it
            eventData.Use();
        }
        public void OnInputUp(InputEventData eventData)
        {
            eventData.Use();
        }


        private void OnDestroy()
        {
            //base.interactables.Remove(this.gameObject);
            
        }

    }
}
