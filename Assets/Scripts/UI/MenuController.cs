using HoloToolkit.Examples.InteractiveElements;
using HoloToolkit.Unity.Buttons;
using HoloToolkit.Unity.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace ThesisHololens.UI.EditorMenu
{
    [System.Serializable]
    public class MyOnSelectEvent : UnityEvent<MenuController.MenuElementInfo> { } // we declare the event with MenuElementInfo generic


    [RequireComponent(typeof(ObjectCollection))]
    [RequireComponent(typeof(ScrollBarForObjectCollection))]
    public class MenuController : MonoBehaviour {

        public const string preambleForElementName = "Element";

        public class MenuElementInfo
        {
            public string ID; //number is behind collectionNode.name
            public int number; //this number is also unique
            public CollectionNode collectionNode; // access to the collectionnode info
            public CompoundButton backgroundButton; //access to the button and events
            private InteractiveToggle InteractiveToggle;
            public InteractiveToggle interactiveToggle//access to the toggle
            {
                get
                {
                    return InteractiveToggle;
                }
                set
                {
                    InteractiveToggle?.OnSelection.RemoveListener(OnSelectEvents);
                    InteractiveToggle?.OnDeselection.RemoveListener(OnDeselectEvents);

                    InteractiveToggle = value;

                    InteractiveToggle.DetectHold = true;
                    InteractiveToggle.OnHoldEvent.AddListener(OnHoldEvent);
                    InteractiveToggle.OnSelection.AddListener(OnSelectEvents);
                    InteractiveToggle.OnDeselection.AddListener(OnDeselectEvents);
                }
            }

            public MiniaturGameObjectScript miniaturGameObjectScript; //access to the miniature object
            public miniaturGameObjectSwitcher miniaturGameObjectSwitcher;


            public GameObject miniatureGO; //the object that is shown
            public GameObject alternativeMiniatureGO; //the object that is shown when you are looking at

            public bool isSelected = false;

            private void OnHoldEvent()
            {
                OnHold?.Invoke(this);
            }

            public void OnSelectEvents()
            {
                isSelected = true;
                onSelect?.Invoke(this);
            }

            public void OnDeselectEvents()
            {
                isSelected = false;
                onSelect?.Invoke(this);
            }

            //who sould be notified, when this item is selected or deselected?
            public MyOnSelectEvent onSelect;
            public MyOnSelectEvent OnHold;
        }

        [Tooltip("this Prefab needs to have a compundButton (getComponent),an interactiveToggle ( getComponentInChildren ), a miniaturGameObjectSwitcher ( getComponentInChildren ) and a miniatureGmeObjectScript ( getComponentInChildren ) attached ")]
        public GameObject MenuElement_Prefab;


        //in here are all list items that can be accessed
        private List<MenuElementInfo> MenuElements = new List<MenuElementInfo>();


        [Tooltip("This defines if only one toggle Item is selectable at a time")]
        public bool OnlyOneSelectable = true;

        private MyOnSelectEvent onSelectMenuController;


        private ObjectCollection objectCollection;
        private ScrollBarForObjectCollection scrollBar;

        private void Awake()
        {
            //need to be set bevore start, because we might add items in "start"
            objectCollection = GetComponent<ObjectCollection>();
            scrollBar = GetComponent<ScrollBarForObjectCollection>();
        }


        public int addMenuElement(
            GameObject miniatureGO,
            string ID,//we save a key string, so we know later what the menu element is about
            GameObject alternativeMiniatureGO = null, 
            UnityAction<MenuElementInfo> onSelect = null,
            int placement = default(int),
            UnityAction<MenuElementInfo> onHold = null,
            string textOnToggleSelect = "On", 
            string textOnToggleUnselect = "Off" )
        {
            if (miniatureGO == null)
                return 0;

            MenuElementInfo mei = new MenuElementInfo();
            if (mei.onSelect == null)
                mei.onSelect = new MyOnSelectEvent();
            mei.onSelect.AddListener(OnSelectEvents);//connection to this controller

            GameObject MenuElement = null;

            try
            {

                //fill all of the menu element info, that we already know
                if (onSelect != null)
                {
                    //adds the  listener
                    if (onSelectMenuController == null)
                        onSelectMenuController = new MyOnSelectEvent();
                    onSelectMenuController.RemoveListener(onSelect); //we need to ensure only one listener of the methiode is in there
                    onSelectMenuController.AddListener(onSelect);
                }

                //fill all of the menu element info, that we already know
                if (onHold != null)
                {
                    //adds the hold listener
                    if (mei.OnHold == null)
                        mei.OnHold = new MyOnSelectEvent();
                    mei.OnHold.AddListener(onHold);
                }


                mei.miniatureGO = miniatureGO;
                mei.alternativeMiniatureGO = alternativeMiniatureGO; // doesnt matter if null

                mei.ID = ID;

                //now lets instantiate the prefab for the menu element
                MenuElement = Instantiate(MenuElement_Prefab, this.transform); //the menu element is now part of the collection
                MenuElement.transform.SetSiblingIndex(placement);//TODO: out of range exceptions

                //generate a name and a number
                var nameAndNumber = generateMenuElementName();
                MenuElement.name = nameAndNumber.Key;
                mei.number = nameAndNumber.Value;

                mei.backgroundButton = MenuElement.GetComponent<CompoundButton>();
                mei.interactiveToggle = MenuElement.GetComponentInChildren<InteractiveToggle>();//registers automatiacally the event

                mei.miniaturGameObjectScript = MenuElement.GetComponentInChildren<MiniaturGameObjectScript>();
                mei.miniaturGameObjectSwitcher = MenuElement.GetComponentInChildren<miniaturGameObjectSwitcher>();


                //Last step is to refresh the collection.
                updateCollection();

                //now we can access the node
                mei.collectionNode = objectCollection.NodeList.Find(x => x.Name == nameAndNumber.Key);

                //so it shows a miniature object immediately
                mei.miniaturGameObjectScript.toShowInstance = Instantiate(miniatureGO);

                //set references
                mei.miniaturGameObjectSwitcher.normal_toCopy = miniatureGO;
                mei.miniaturGameObjectSwitcher.alternative_toCopy = alternativeMiniatureGO;

                //set text of the toggle
                mei.interactiveToggle.GetComponent<LabelTheme>().Selected = textOnToggleSelect;
                mei.interactiveToggle.GetComponent<LabelTheme>().Default = textOnToggleUnselect;


                //We now have a complete MenuElementInfo

                MenuElements.Add(mei);

                //id of the element
                return mei.number;
            }
            catch (System.Exception e)
            {
                //deleta already instantiated stuff
                Debug.LogError(e.Message);

                Destroy(MenuElement);
                throw e;
            }

        }

        /// <summary>
        /// updates the collection
        /// 
        /// </summary>
        /// <param name="resetToStart"> resets the position to the beginning of the list</param>
        public void updateCollection(bool resetToStart = false)
        {
            //for that , we need to set the rotation of this to 0 and after refreshment back.
            Vector3 currenPosition = this.transform.localPosition;

            this.transform.localPosition = Vector3.zero;

            objectCollection.UpdateCollection();

            if(resetToStart)
                this.transform.localPosition  = new Vector3(currenPosition.x,
                    -( (objectCollection.Rows * objectCollection.CellHeight ) / 2f) + scrollBar.showSmallerThanThis,
                    currenPosition.z);
            else
                this.transform.localPosition = currenPosition;
        }

        //in here manage the onlyOneSelectionAllowed
        private void OnSelectEvents(MenuElementInfo arg0)
        {

            if (OnlyOneSelectable && arg0.isSelected)
            {

                foreach (MenuElementInfo mei in MenuElements)
                {
                    //if someone isSelected and not the freshly selected item
                    if (mei.isSelected && mei != arg0)
                    {
                        //catchNextDeselectionEvent = true;
                        mei.interactiveToggle.SetSelection(false);
                        //mei.OnDeselectEvents(); // we need to fire the deselect event ourself
                    }
                }

            }
            else if (OnlyOneSelectable && !arg0.isSelected)//it is not allowed to deselect
            {
                arg0.isSelected = true;
                arg0.interactiveToggle.SetSelection(true);
                arg0.interactiveToggle.HasSelection = true;
                arg0.interactiveToggle.Selection = true;
                return;//do not invoke
            }

            //in the end, invoke subscribers
            onSelectMenuController.Invoke(arg0);
        }


        public GameObject[] getAllElementsGOs()
        {
            List<GameObject> allGOs = new List<GameObject>();

            foreach (MenuElementInfo mei in MenuElements)
            {
                allGOs.Add(mei.collectionNode.transform.gameObject);
            }

            return allGOs.ToArray();
        }

        /// <summary>
        /// Return the first occurence
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public MenuElementInfo getElementByID(string ID)
        {
            return MenuElements.Find(x => x.ID == ID);
        }

        public MenuElementInfo[] getElementsByID(string ID)
        {
            List<MenuElementInfo> allMEs = new List<MenuElementInfo>();

            foreach (MenuElementInfo mei in MenuElements)
            {
                if(mei.ID == ID)
                    allMEs.Add(mei);
            }

            return allMEs.ToArray();
        }

        /// <summary>
        /// returns the first selected item, null if none are selected
        /// </summary>
        /// <returns></returns>
        public MenuElementInfo getSelectedItem()
        {
            return MenuElements.Find(x => x.interactiveToggle.IsSelected == true);
        }

        /// <summary>
        /// returns all currently selected items
        /// </summary>
        /// <returns></returns>
        public List<MenuElementInfo> getSelectedItems()
        {

            return MenuElements.FindAll(x => x.interactiveToggle.IsSelected == true);
        }

        /// <summary>
        /// also removes al listeners
        /// </summary>
        public void resetMenu()
        {
            foreach(MenuElementInfo mei in MenuElements)
            {
                mei.onSelect.RemoveListener(OnSelectEvents);
                Destroy(mei.collectionNode.transform.gameObject);

                Transform t = mei.collectionNode.transform.parent;

                mei.collectionNode.transform.parent = null;
                mei.collectionNode.transform = null;

            }

            onSelectMenuController?.RemoveAllListeners();

            updateCollection();

            MenuElements.Clear();
        }

        /// <summary>
        /// removes the element and updates the collection.
        /// removes the listener as well, if nessesary
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="onSelectToRemove"></param>
        public void removeElement(string ID,UnityAction<MenuElementInfo> onSelectToRemove = null)
        {
            MenuElementInfo mei = MenuElements.Find(x => x.ID == ID);
            Destroy(mei?.collectionNode.transform.gameObject);
            if(mei != null)
            {
                mei.onSelect.RemoveListener(OnSelectEvents);
                mei.collectionNode.transform.parent = null;
                mei.collectionNode.transform = null;
            }

            if(onSelectToRemove  != null)
                onSelectMenuController.RemoveListener(onSelectToRemove);

            updateCollection();

            MenuElements.Remove(mei);
        }


        private KeyValuePair<string,int> generateMenuElementName()
        {
            if (MenuElements.Count == 0)
                return new KeyValuePair<string, int>(preambleForElementName + "1" , 1);

            int biggestNumber = 0;
            foreach(MenuElementInfo mei in MenuElements)
            {
                if (mei.number > biggestNumber)
                    biggestNumber = mei.number;
            }

            //add 1
            biggestNumber++;

            return new KeyValuePair<string, int>(preambleForElementName + biggestNumber.ToString(), biggestNumber);

        }
    }
}
