using HoloToolkit.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.Settings;
using ThesisHololens.States;
using UnityEngine;

namespace ThesisHololens.UI.EditorMenu
{
    /// <summary>
    /// startEditorMode should
    /// - Generate menu 1
    /// - set all resources3D
    /// - set the toggle writing
    /// - set set the onSelect
    /// 
    /// - when selected, generate menu 2
    /// - it should have all button functions
    /// - for every Function clicked, generate a new one, at the same position
    /// - for every Function clicked, display menu 3
    /// 
    /// -  menu 3 should have every adress suitable for the function
    /// </summary>
    public class EditorManager : Singleton<EditorManager>
    {
        //the menu, that holds the 3dDisplay stuff
        public MenuController MenuResources;
        public MenuController MenuFunction;
        public MenuController MenuAdress;

        //al the information we currently gathered are in here
        private AppData.uniqueIDDevice currentMenuInformationState;

        //A prefab of a 3d Text mesh
        public TextMesh Text_Prefab;

        protected override void Awake()
        {
            //sets the instance variable and deactivates itself after thet
            base.Awake();
            this.gameObject.SetActive(false);

        }


        public void startmenuFresh()
        {
            prepare();

            currentMenuInformationState = new AppData.uniqueIDDevice();
            currentMenuInformationState.myUIContainerData = new List<AppData.uniqueIDDevice.UIContainerData>();
            generateMenu_Ressources();
        }

        public void startmenuWithExistingItem(AppData.uniqueIDDevice existingItem)
        {
            prepare();

            currentMenuInformationState = (AppData.uniqueIDDevice)existingItem.Clone();
            generateMenu_Ressources();
        }

        private void prepare()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            MenuResources.resetMenu();
            MenuFunction.resetMenu();
            MenuAdress.resetMenu();
        }

        public void EndEditor()
        {
            gameObject.SetActive(false);

            MenuResources.resetMenu();
            MenuFunction.resetMenu();
            MenuAdress.resetMenu();
        }

        private void generateMenu_Ressources()
        {
            MenuResources.resetMenu();

            UnityEngine.Object[] resources = Resources.LoadAll(AppDataManager.Instance.resourcePath);

            foreach (UnityEngine.Object o in resources)
            {
                GameObject toAdd = (GameObject)o;

                TextMesh text = Instantiate(Text_Prefab.gameObject).GetComponent<TextMesh>();
                text.text = toAdd.name; // alternative text

                try//if something goes wrong during one element
                {
                    //generates the menu element, we listen to select events
                    MenuResources.addMenuElement(
                        toAdd,
                        toAdd.name,
                        text.gameObject,
                        OnSelectEvents_resourcesMenu);
                }
                catch
                {
                    Debug.Log("Element " + toAdd.name + " could not be generated");
                }


                //Tidy up
                Destroy(text.gameObject); //they made a copy by now
            }

            if (!string.IsNullOrEmpty(currentMenuInformationState.baseAdress))//we start with data
            {
                MenuController.MenuElementInfo mei = MenuResources.getElementByID(currentMenuInformationState.DeviceType3D);
                //MenuResources.catchNextDeselectionEvent = true;
                mei.interactiveToggle.SetSelection(true);

                //proceed to next function
                generateMenu_ButtonFunction();
            }

            MenuResources.updateCollection(true);
        }


        //starts the next menu step
        private void OnSelectEvents_resourcesMenu(MenuController.MenuElementInfo sender)
        {
            if (sender.isSelected)
            { // we have nothing to do on deselection events

                currentMenuInformationState.DeviceType3D = sender.ID; //id is current state


                MenuAdress.resetMenu();

                generateMenu_ButtonFunction();
            }
            else
            {
                MenuFunction.resetMenu();
                MenuAdress.resetMenu();
            }

        }


        private void generateMenu_ButtonFunction()
        {
            MenuFunction.resetMenu();
            
            List<string> functionAlreadyDisplayed = new List<string>();

            foreach(KeyValuePair<string,string[]> zuordnung in AppData.function_UIContainer_to_realFunction)
            {
                foreach(string function in zuordnung.Value)
                {
                    if (functionAlreadyDisplayed.Contains(function))
                        continue;

                    TextMesh text = Instantiate(Text_Prefab.gameObject).GetComponent<TextMesh>();
                    text.text = function; // alternative text

                    try//if something goes wrong during one element
                    {
                        GameObject go = UIContainerBar.Instance.getUIElementPrefab(function);
                        if(go == null)
                        {
                            Destroy(text.gameObject);
                            Debug.Log("Element " + function + " could not be translatet in Function Menu");
                            continue;
                        }

                        //generates the menu element, we listen to select events
                        MenuFunction.addMenuElement(
                            go,
                            function, 
                            text.gameObject, 
                            OnSelectEvents_functionMenu
                            );
                    }
                    catch
                    {
                        Debug.Log("Element " + function + " could not be generated");
                    }

                    //Tidy up
                    Destroy(text.gameObject); //they made a copy by now

                    functionAlreadyDisplayed.Add(function);
                }

                
            }

            //for the already given state
            foreach(AppData.uniqueIDDevice.UIContainerData uicd in currentMenuInformationState.myUIContainerData)
            {
                //check all nessesary values for null
                if (uicd.function_UIContainer == null || uicd.adress == null)
                    continue;

                TextMesh text = Instantiate(Text_Prefab.gameObject).GetComponent<TextMesh>();
                text.text = uicd.adress; // alternative text

                GameObject go = UIContainerBar.Instance.getUIElementPrefab(uicd.function_UIContainer);

                //there must be one element in the list, that has this uicd.function_UIContainer as ID
                int? placement = MenuFunction.getElementByID(uicd.function_UIContainer)?.collectionNode.transform.GetSiblingIndex() + 1;//we want +1


                MenuFunction.addMenuElement(
                    go,
                    uicd.adress,
                    text.gameObject,
                    OnSelectEvents_functionMenu,
                    placement.GetValueOrDefault(),
                    OnHoldEvents_functionMenu
                    );

                //MenuFunction.catchNextDeselectionEvent = true;
                MenuFunction.getElementByID(uicd.adress).interactiveToggle.SetSelection(true);

                //they made a copy
                Destroy(text);

            }

            MenuFunction.updateCollection(true);
        }

        private void OnSelectEvents_functionMenu(MenuController.MenuElementInfo sender)
        {
            //finds out if it is an existing, or a new Item
            var ExistingUIElement = currentMenuInformationState.myUIContainerData.Find(x =>
                x.adress == sender.ID);
            var FreshUIElement = UIContainerBar.Instance.getUIElementPrefab(sender.ID);

            if (sender.isSelected) //on selection, we procede to the next menu
            {
                //we have to differentiate between new menu items, and already existing menu items
                //so we have to check if the ID of a menu item is a function or a adress

                if (FreshUIElement != null)//fresh element is a new type
                {
                    //if it is a fresh unset item
                    //we want to display all adresses, that this element can handle
                    generateMenu_Adress(sender.ID);
                }
                else
                    Debug.LogError("should not happen " + sender.ID);
            }
            else//on deselection, we to remove the item if it is an existin item
            {
                if(ExistingUIElement != null)
                {
                    MenuFunction.removeElement(ExistingUIElement.adress);
                    currentMenuInformationState.myUIContainerData.RemoveAll(x => x.adress == ExistingUIElement.adress);//removes the element
                }
                else if (FreshUIElement != null)//fresh element is a new type
                {
                    //if it is a fresh unset item
                    MenuAdress.resetMenu();
                }

            }
            
        }

        private void OnHoldEvents_functionMenu(MenuController.MenuElementInfo sender)
        {
            //finds out if it is an existing, or a new Item
            var ExistingUIElement = currentMenuInformationState.myUIContainerData.Find(x =>
                x.adress == sender.ID);

            if (ExistingUIElement != null)
            {
                //we want to display the adress already ticked in the menu
                generateMenu_Adress(ExistingUIElement.function_UIContainer, ExistingUIElement.adress);
            }
        }


        private void generateMenu_Adress(string UIFunction, string adress = null)
        {
            MenuAdress.resetMenu();

            List<string> adressesToDisplay = new List<string>();

            foreach (KeyValuePair<string,string[]> zuordnung in AppData.function_UIContainer_to_realFunction)
            {
                if (new List<string>(zuordnung.Value).Contains(UIFunction))//if the visual representation of an ui elemnt is compatible with the type of an open hab type
                {
                    adressesToDisplay.AddRange(
                        ItemStates.Instance.getAdressesByType(zuordnung.Key)
                        );
                }
            }

            //now generate the visual representation if allowed
            foreach(string adressToDisplay in adressesToDisplay)
            {
                //check if the adress belongs to the current device

                TextMesh text = Instantiate(Text_Prefab.gameObject).GetComponent<TextMesh>();
                text.text = adressToDisplay; // alternative text

                MenuAdress.addMenuElement(
                    text.gameObject,
                    adressToDisplay,
                    null,
                    OnSelectEvents_AdressMenu
                    );

                //Tidy up
                Destroy(text.gameObject); //they made a copy by now

                if(adress != null && adress == adressToDisplay)//so we tick the preset element
                {
                    //MenuAdress.catchNextDeselectionEvent = true;
                    MenuAdress.getElementByID(adressToDisplay).interactiveToggle.SetSelection(true);
                }
            }

            MenuAdress.updateCollection(true);
        }

        private void OnSelectEvents_AdressMenu(MenuController.MenuElementInfo sender)
        {
            if (!sender.isSelected) // we have nothing to do on deselection events
                return;

            bool newItem = false;
            //we insert part of one sub-tupel (address - Function)

            //find out if it is a new item
            var currentlySelected = MenuAdress.getSelectedItem().ID; //currently selected in adress menu
            AppData.uniqueIDDevice.UIContainerData UIContainerData = null;
            if (currentlySelected != null)
            {
                //is there a container data to the currently selected
                UIContainerData = currentMenuInformationState.myUIContainerData.Find(x => x.adress == currentlySelected);
            }

            if (UIContainerData == null)//if there is no container data
            { // new item needs to be generated
                UIContainerData = new AppData.uniqueIDDevice.UIContainerData();
                newItem = true;
            }



            //new chosen adress
            UIContainerData.adress = sender.ID;

            if (newItem)
            {
                //get the ui Function, that has not been assingned --> uifunction
                UIContainerData.function_UIContainer = MenuFunction.getSelectedItems().Find(x => UIContainerBar.Instance.getUIElementPrefab(x.ID) != null).ID;

                //add only if ne item
                currentMenuInformationState.myUIContainerData.Add(UIContainerData);
            }



            //back to the function menu
            StartCoroutine(restartFunctionMenu());
        }

        //so the user can see, that he toggled the adress
        private IEnumerator restartFunctionMenu()
        {
            yield return new WaitForSeconds(0.3f);

            MenuAdress.resetMenu();
            MenuFunction.resetMenu();

            generateMenu_ButtonFunction();
        }

        public void finalizeAndSaveTupel()
        {
            //we check for a complete tupel

            if (string.IsNullOrEmpty(currentMenuInformationState.DeviceType3D) || //device 3D Type empty --> bad
                currentMenuInformationState.myUIContainerData.Find(x => x.adress == null) != null || //if there is an adress null --> bad
                currentMenuInformationState.myUIContainerData.Find(x => x.function_UIContainer == null) != null //if there is a function null --> bad
                )
            {
                Debug.LogError("The menu Tupel has errors, that cant be fixed");
                return;
            }


            if (string.IsNullOrEmpty(currentMenuInformationState.baseAdress))//must be a new item
            {
                currentMenuInformationState.baseAdress = AppData.generateGuid();


                //position should be on an anchor
                currentMenuInformationState.posX = 0;
                currentMenuInformationState.posY = 0;
                currentMenuInformationState.posZ = 0;

                currentMenuInformationState.rotX = 0f;
                currentMenuInformationState.rotY = 0f;
                currentMenuInformationState.rotZ = 0f;

                //get the scale from the ressources
                GameObject prefabGO = (GameObject) Resources.Load(AppDataManager.Instance.resourcePath + currentMenuInformationState.DeviceType3D);
                currentMenuInformationState.scaleX = prefabGO.transform.localScale.x;
                currentMenuInformationState.scaleY = prefabGO.transform.localScale.y;
                currentMenuInformationState.scaleZ = prefabGO.transform.localScale.z;
            }

            foreach(AppData.uniqueIDDevice.UIContainerData uiData in currentMenuInformationState.myUIContainerData)
            {
                uiData.posX = uiData.posX == default(float) ? UnityEngine.Random.value : uiData.posX; //if posx is default, then return random betwen 0 and 1 otherwise return posx
                uiData.posY = uiData.posY == default(float) ? UnityEngine.Random.value : uiData.posY;
                uiData.posZ = uiData.posZ == default(float) ? 0f : uiData.posZ;

                uiData.scaleX = uiData.scaleX == default(float) ? getScaleOrOneFromPrefab(uiData).x : uiData.scaleX;
                uiData.scaleY = uiData.scaleY == default(float) ? getScaleOrOneFromPrefab(uiData).y : uiData.scaleY;
                uiData.scaleZ = uiData.scaleZ == default(float) ? getScaleOrOneFromPrefab(uiData).z : uiData.scaleZ;

            }


            Debug.Log("Device " + currentMenuInformationState.DeviceType3D + " finished");

            AppDataManager.Instance.onEditorFinished(currentMenuInformationState);

            //restart editor
            startmenuFresh();

        }

        private Vector3 getScaleOrOneFromPrefab(AppData.uniqueIDDevice.UIContainerData uiData)
        {
            var vec = UIContainerBar.Instance.getUIElementPrefab(uiData.function_UIContainer);
            if (vec == null)
                return Vector3.one;
            else
                return vec.transform.localScale;

        }
    }
}