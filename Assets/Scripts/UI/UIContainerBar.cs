using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule.Utilities.Interactions;
using HoloToolkit.Unity.UX;
using System.Collections.Generic;
using ThesisHololens.Manipulation;
using ThesisHololens.Devices;
using ThesisHololens.Settings;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.UI
{
    public class UIContainerBar : Singleton<UIContainerBar>
    {


        /// <summary>
        /// Pushes the app bar away from the object
        /// </summary>
        public float HoverOffsetZ = 0f;

        public GameObject Target
        {
            get
            {
                return target;
            }
            set
            {
                if (value == target)
                    return;//there is no target change

                GameObject oldTarget = target;
                GameObject newTarget = value;

                //give the targets to the subscribed methodes
                //checks for null "?"

                onTargetChange?.Invoke(oldTarget, newTarget);
                    


                //do the change
                target = value;


            }
        }


        /// <summary>
        /// 
        /// onTargetChange(oldTarget,newTarget)
        /// will be called BEVORE target changes
        /// </summary>
        [SerializeField]
        public UnityAction<GameObject, GameObject> onTargetChange;

        [SerializeField]
        private GameObject target;

        /// <summary>
        /// assign for calculating the bounds for appbar
        /// </summary>
        public GameObject BoundingBoxBasic_prefab;

        public BoundingBox target_boundingBox { get; private set; }

        [SerializeField]
        private Transform UIContainerParent;

        /// <summary>
        /// is the container bar following the target?
        /// </summary>
        public bool isEnabled { get; set; }


        /// <summary>
        /// Prefab for a Switch
        /// </summary>
        [SerializeField]
        private GameObject UIButton_Prefab;
        [SerializeField]
        private GameObject UIToggle_Prefab;
        [SerializeField]
        private GameObject UISlider_Prefab;
        [SerializeField]
        private GameObject UIContact_Prefab;
        [SerializeField]
        private GameObject UIText_Prefab;
        [SerializeField]
        private GameObject UIColorPicker_Prefab;
        [SerializeField]
        private GameObject UIRollershutter_Prefab;
        [SerializeField]
        private GameObject UIPlayer_Prefab;



        private Vector3[] forwards = new Vector3[4];
        private Vector3 targetBarSize = Vector3.one;
        private float lastTimeTapped = 0f;
        private float coolDownTime = 0.5f;

        private void Start()
        {
            if(BoundingBoxBasic_prefab == null)
            {
                isEnabled = false;
            }
            else
            {
                isEnabled = true;

                //instanciate with the prefab to get bounds
                target_boundingBox = Instantiate(BoundingBoxBasic_prefab).GetComponent<BoundingBox>();
                target_boundingBox.enabled = false; //deactivates the bounding box, we trigger the calc process manually
                target_boundingBox.IsVisible = false; //we dont want the blue lines

            }
        }

        public void Update()
        {
            //follows the set target
            if(isEnabled)
                FollowTarget(true);


            //adjusts the background bar according to the set UI Elements
            //backgroundBar.transform.localScale = Vector3.Lerp(backgroundBar.transform.localScale, targetBarSize, 0.5f);


        }

        public GameObject getUIElementPrefab(string UIFunction)
        {
            switch (UIFunction)
            {
                case AppData.UIFunction_Toggle:
                    return UIToggle_Prefab;
                case AppData.UIFunction_Switch:
                    return UIButton_Prefab;
                case AppData.UIFunction_Slider:
                    return UISlider_Prefab;
                case AppData.UIFunction_Contact:
                    return UIContact_Prefab;
                case AppData.UIFunction_Text:
                    return UIText_Prefab;
                case AppData.UIFunction_Color:
                    return UIColorPicker_Prefab;
                case AppData.UIFunction_Rollershutter:
                    return UIRollershutter_Prefab;
                case AppData.UIFunction_Player:
                    return UIPlayer_Prefab;
                default:
                    return null;

            };
        }





        private void FollowTarget(bool smooth)
        {
            if (target == null)
            {
                return;
            }

            if (target_boundingBox.Target == null || target_boundingBox.Target != this.gameObject)//we have a changed target
            {
                target_boundingBox.Target = target;
                target_boundingBox.SendMessage("Update"); //run update once to calculate bounds
            }


            // Get positions for each side of the bounding box
            // Choose the one that's closest to us
            forwards[0] = target_boundingBox.transform.forward;
            forwards[1] = target_boundingBox.transform.right;
            forwards[2] = -target_boundingBox.transform.forward;
            forwards[3] = -target_boundingBox.transform.right;
            Vector3 scale = target_boundingBox.TargetBoundsLocalScale;
            float maxXYScale = Mathf.Max(scale.x, scale.y);
            float closestSoFar = Mathf.Infinity;
            Vector3 finalPosition = Vector3.zero;
            Vector3 finalForward = Vector3.zero;
            Vector3 headPosition = Camera.main.transform.position;

            for (int i = 0; i < forwards.Length; i++)
            {
                Vector3 nextPosition = target_boundingBox.transform.position +
                (forwards[i] * -maxXYScale) +
                (Vector3.up * (-scale.y ));//* HoverOffsetYScale

                float distance = Vector3.Distance(nextPosition, headPosition);
                if (distance < closestSoFar)
                {
                    closestSoFar = distance;
                    finalPosition = nextPosition;
                    finalForward = forwards[i];
                }
            }

            // Apply hover offset
            finalPosition += (finalForward * -HoverOffsetZ);

            // Follow our bounding box
            if (smooth)
            {
                transform.position = Vector3.Lerp(transform.position, finalPosition, 0.5f);
                //transform.position = finalPosition;
            }
            else
            {
                transform.position = finalPosition;
            }
            // Rotate on the y axis
            Vector3 eulerAngles = Quaternion.LookRotation((target_boundingBox.transform.position - finalPosition).normalized, Vector3.up).eulerAngles;
            eulerAngles.x = 0f;
            eulerAngles.z = 0f;
            transform.eulerAngles = eulerAngles;

        }


        /// <summary>
        /// This methode takes Information about an UIElement, parses it, and generates it
        /// </summary>
        /// <param name="UIElemProps"></param>
        public void addUiElement(AppData.uniqueIDDevice.UIContainerData UIElemProps)
        {
            GameObject UIElement = null;

            switch (UIElemProps.function_UIContainer)
            {
                case AppData.UIFunction_Switch:
                    //instantiate and
                    //Attach UIButton Script and
                    //give the button the informations
                    UIElement = Instantiate(UIButton_Prefab,UIContainerParent);

                    UIElement.AddComponent<UIButton>().setProperties(UIElemProps);

                    break;
                case AppData.UIFunction_Contact:
                    //instantiate and
                    //Attach UIButton Script and
                    //give the Toggle the informations
                    UIElement = Instantiate(UIContact_Prefab, UIContainerParent);

                    UIElement.AddComponent<UIContact>().setProperties(UIElemProps);
                    break;
                case AppData.UIFunction_Toggle:
                    //instantiate and
                    //Attach UIToggle Script and
                    //give the Toggle the informations
                    UIElement = Instantiate(UIToggle_Prefab, UIContainerParent);

                    UIElement.AddComponent<UIToggle>().setProperties(UIElemProps);
                    break;
                case AppData.UIFunction_Slider:

                    UISlider_Prefab.gameObject.SetActive(false);

                    GameObject UIElement_TEMP = Instantiate(UISlider_Prefab); //instantiate somewhere

                    UISlider_Prefab.gameObject.SetActive(true);

                    //attach UISlider Script and preinitialize 
                    UISlider UIs_TEMP = UIElement_TEMP.AddComponent<UISlider>();
                    UIs_TEMP.preInitialization();

                    //now instantiate after initialisation, because then the properies of the script are initialized (Max Slider Value,remove billboard)
                    UIElement = Instantiate(UIElement_TEMP, UIContainerParent);

                    //get the new script, we added earlier
                    UISlider UIs = UIElement.GetComponent<UISlider>();

                    //init
                    UIs.initialization();

                    UIElement.SetActive(true);

                    //then set Properties
                    UIs.setProperties(UIElemProps);



                    //destroy the tmp gameObj
                    Destroy(UIElement_TEMP);

                    break;
                case AppData.UIFunction_Text:

                    //instantiate and
                    //Attach UIText Script and
                    //give the UIText the informations
                    UIElement = Instantiate(UIText_Prefab, UIContainerParent);

                    UIElement.AddComponent<UIText>().setProperties(UIElemProps);
                    break;
                case AppData.UIFunction_Color:

                    UIElement = Instantiate(UIColorPicker_Prefab, UIContainerParent);

                    UIElement.AddComponent<UIColorPicker>().setProperties(UIElemProps);
                    break;
                case AppData.UIFunction_Rollershutter:
                    UIElement = Instantiate(UIRollershutter_Prefab, UIContainerParent);

                    UIElement.GetComponent<UIRollershutter>().setProperties(UIElemProps);
                    break;
                case AppData.UIFunction_Player:
                    UIElement = Instantiate(UIPlayer_Prefab, UIContainerParent);

                    UIElement.GetComponent<UIPlayer>().setProperties(UIElemProps);
                    break;
                default:
                    Debug.Log("Did not understand " + UIElemProps.function_UIContainer + " as a UIFunction");
                    //do nothing if nothing was recognized
                    break;
            }

            //TODO: make dynamic with action somehow
            GetComponent<ToggleEdit>().prepareGameobjectForManipulation(UIElement, ManipulationMode.MoveAndScale);

            Debug.Log("added Ui Element: " + UIElement.name);

        }

        /// <summary>
        /// Destroys all UI elements currently on the container bar
        /// </summary>
        public void clearUIElements()
        {
            foreach (UIContainer uic in UIContainerParent.GetComponentsInChildren<UIContainer>()){
                Destroy(uic.gameObject);
            }
        }

        /// <summary>
        /// returns all the information about the current attached uicd
        /// </summary>
        /// <returns></returns>
        public List<AppData.uniqueIDDevice.UIContainerData> getCurrentUIInformation()
        {
            List<AppData.uniqueIDDevice.UIContainerData> uicd = new List<AppData.uniqueIDDevice.UIContainerData>();

            foreach (UIContainer uic in UIContainerParent.GetComponentsInChildren<UIContainer>())
            {
                //generates a list from all devices
                uicd.Add(uic.getUIContainerData());
            }

            return uicd;

        }
    }
    
}
