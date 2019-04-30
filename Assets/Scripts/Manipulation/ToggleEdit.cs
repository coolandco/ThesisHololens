using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using HoloToolkit.Unity.UX;
using UnityEngine;

namespace ThesisHololens.Manipulation
{
    public class ToggleEdit : MonoBehaviour
    {
        /// <summary>
        /// all game objects in this parent that have the script "selfManipulate" will be made movable
        /// </summary>
        public Transform Parent;

        public BoundingBox boundingBoxBasicPrefab;

        private bool isToggleEditOn = false;
        public bool IsToggleEditOn
        {
            get
            {
                return isToggleEditOn;
            }
            private set
            {
                isToggleEditOn = value;
            }
        }

        /// <summary>
        /// The Z Movement will be prohibited
        /// </summary>
        public bool constraintZMovement = false;


        public void toggleEditOn()
        {

            if (Parent == null)
            {
                Parent = this.transform;
            }



            SelfManipulate[] Elements = Parent.GetComponentsInChildren<SelfManipulate>();

            //set every Manipulation Script active
            foreach (SelfManipulate Element in Elements)
            {

                //activate manipulation
                Element.active = true;
            }

            isToggleEditOn = true;


        }

        public void toggleEditOff()
        {
            if (Parent == null)
            {
                Parent = this.transform;
            }

            SelfManipulate[] Elements = Parent.GetComponentsInChildren<SelfManipulate>();

            foreach (SelfManipulate Element in Elements)
            {

                //activate manipulation
                Element.active = false;
            }

            isToggleEditOn = false;


        }


        /// <summary>
        /// adds a self manipulate to the object
        /// all devices should go through here bevore manipulation
        /// </summary>
        /// <param name="go"></param>
        public void prepareGameobjectForManipulation(GameObject go, ManipulationMode manipulationMode,bool invisibleIfNotInManipulationMode = false)
        {
            SelfManipulate manip = go.GetComponent<SelfManipulate>();
            if (manip == null)
                manip = go.AddComponent<SelfManipulate>();



            manip.myBoundingBox = boundingBoxBasicPrefab;
            manip.manipulationMode = manipulationMode;
            manip.constraintZMovement = constraintZMovement;
            manip.invisibleIfNotInManipulationMode = invisibleIfNotInManipulationMode;

            //if there changes anything in the hierarchy, reeact to new elements
            if (IsToggleEditOn)
            {
                manip.active = true;
                manip.setInvisible = false;
            }




        }


    }
}
