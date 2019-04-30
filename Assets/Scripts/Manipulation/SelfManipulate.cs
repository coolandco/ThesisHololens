using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using HoloToolkit.Unity.UX;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.Manipulation
{
    /// <summary>
    /// selfManipulate only activates itself, when set active
    /// tells everyone with a sendMessage, that it is active now
    /// onManipulationStart
    /// onManipulationEnd
    /// </summary>
    public class SelfManipulate : MonoBehaviour
    {

        //tells when manipulation starts
        public UnityEvent onManipulationStart = new UnityEvent();

        //tells when manipulation ends
        public UnityEvent onManipulationEnd = new UnityEvent();


        private bool Active = false;
        /// <summary>
        /// activates the manipulation
        /// </summary>
        public bool active
        {
            get
            {
                return Active;
            }
            set
            {
                if (value != Active) //if there is a change
                {
                    if (value)
                    {

                        //do init bevore active
                        //init also resets
                        init();
                    }
                    else
                    {
                        end();

                        //old
                        //SendMessage("onManipulationEnd", SendMessageOptions.DontRequireReceiver);
                    }
                }

                Active = value;
            }
        }

        public bool hasBeenMoved { get; private set; }

        /// <summary>
        ///  set this for visualisation of the bounding box
        /// </summary>
        public BoundingBox myBoundingBox;

        /// <summary>
        /// which manipulation mode?
        /// </summary>
        public ManipulationMode manipulationMode = ManipulationMode.MoveAndScale;//default


        //default false
        private bool ConstraintZMovement = false;
        public bool constraintZMovement
        {
            get
            {
                return ConstraintZMovement;
            }
            set
            {

                ConstraintZMovement = value;
            }
        }

        private bool InvisibleIfNotInManipulationMode;
        public bool invisibleIfNotInManipulationMode
        {
            get
            {
                return InvisibleIfNotInManipulationMode;
            }
            set
            {
                InvisibleIfNotInManipulationMode = value;

                //if set true, we want the renderers to be false
                setRenderes(!value);
            }
        }

        public bool setInvisible
        {
            set
            {
                setRenderes(!value); //makes the object appear
            }
        }


        private TwoHandManipulatable myTwoHandManipulatable;

        //stores the current local z value
        private float zPositionCopy;


        private void init()
        {
            zPositionCopy = transform.localPosition.z;

            myTwoHandManipulatable = gameObject.AddComponent<TwoHandManipulatable>();

            //optional, no null check
            myTwoHandManipulatable.BoundingBoxPrefab = myBoundingBox;


            myTwoHandManipulatable.ManipulationMode = manipulationMode;

            hasBeenMoved = false;

            //make visible
            setRenderes(true);




            myTwoHandManipulatable.StartedManipulating += startedManipulation;//subscribe to manipulationStart
            myTwoHandManipulatable.StoppedManipulating += stoppedmanipulation;

        }


        private void end()
        {
            try
            {
                //unsubscribe
                myTwoHandManipulatable.StartedManipulating -= startedManipulation;
                myTwoHandManipulatable.StoppedManipulating -= stoppedmanipulation;
            }
            catch
            {

            }

            //make invisible
            setRenderes(false);

            //remove the script
            Destroy(myTwoHandManipulatable);
            myTwoHandManipulatable = null;
        }

        private void setRenderes(bool value)
        {
            if (invisibleIfNotInManipulationMode)
            {
                //make visible
                foreach (Renderer r in GetComponentsInChildren<Renderer>())
                {
                    //enables/disables every renderer
                    //TODO: To Test
                    r.enabled = value;
                }
            }
        }

        private void startedManipulation()
        {
            //old
            //tell everyone that manipulate starts
            //SendMessage("onManipulationStart", SendMessageOptions.DontRequireReceiver);

            //sets the indicator true
            hasBeenMoved = true;

            //tell all sublscribers
            onManipulationStart.Invoke();
        }

        private void stoppedmanipulation()
        {
            //tell all subscribers
            onManipulationEnd.Invoke();
        }


        // Update is called once per frame
        void Update()
        {
            if (Active)//only when active
            {
                //if should constrain and difference to z
                if (constraintZMovement && zPositionCopy != transform.localPosition.z)
                    transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, zPositionCopy);
            }
        }

    }
}
