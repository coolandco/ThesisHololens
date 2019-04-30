using HoloToolkit.Unity;
using System.Collections.Generic;
using ThesisHololens.Communication;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.States
{
    public class ItemStates : Singleton<ItemStates>
    {

        private List<ItemStateOpenHAB> States; //in here are the different states for each individual item

        private Dictionary<string, UnityAction<string>> subscriberList;//in here are the subscriptions for the item state changes

        /// <summary>
        /// Because some other threads can trigger actions by modifying the state, we need to call all state updates from the "update" methode
        /// </summary>
        private List<ItemStateOpenHAB> hasChangedList;


        private bool initialized = false;


        void Start()
        {
            subscriberList = new Dictionary<string, UnityAction<string>>();//init
            hasChangedList = new List<ItemStateOpenHAB>();//init

            restCommunicator.Instance.initializeStates();//try to init
            //TODO: check if successfull
            //TODO: go to backup mode mqtt
        }


        public void initialize(ItemStateOpenHAB[] allStates) {
            if (initialized == true)
                return;

            if (allStates != null)
                States = new List<ItemStateOpenHAB>(allStates); //puts the states in the variable
            else
                States = new List<ItemStateOpenHAB>();

            Debug.Log("Initialized");
            //we are now initialized
            initialized = true;
        }

        /// <summary>
        /// The Name is Unique for each OpenHAB Item
        /// Source: https://docs.openhab.org/configuration/items.html#name
        /// 
        /// Assumption: The name is used as well for MQTT Messages as Topic Identifier
        /// </summary>
        /// <param name="name"></param>
        /// <returns>returns null if nothing found</returns>
        public ItemStateOpenHAB getItemByName(string name)
        {
            if (initialized != true || name == null)
                return null;

            return States.Find(x => x.name.Equals(name));

        }

        /// <summary>
        /// Updates a State from an Item if item exist.
        /// </summary>
        /// <param name="updatedItem"></param>
        /// <param name="publishMessage">should the message be send to OpenHAB?</param>
        /// /// <returns>returns false, if Item doesnt exist</returns>
        public bool updateItem(ItemStateOpenHAB updatedItem, bool publishMessage)
        {
            if (!States.Contains(updatedItem))
            {
                //Debug.Log("UpdateItem 1: contains no item named: " + updatedItem.name);
                return false;
            }

            ItemStateOpenHAB olditem = getItemByName(updatedItem.name);
            string oldState = olditem.state;

            //Item should exist 
            //search for it and remove it
            States.Remove(olditem);
            States.Add(updatedItem);

            //only if state changed then invoke subscribtions
            if (!updatedItem.state.Equals(oldState))
            {
                //Debug.Log("UpdateItem 1: put on changed list: " + updatedItem.name);
                // invokeSubscriber(updatedItem.name);
                if (!hasChangedList.Contains(updatedItem))
                    hasChangedList.Add(updatedItem);
            }

            //publish if wanted
            if (publishMessage)
            {
                //Debug.Log("UpdateItem 1: send Message async: " + updatedItem.name);
                restCommunicator.Instance.publishItemStateopenHAB(updatedItem);
            }

            //Debug.Log("UpdateItem 1: returns " + updatedItem.name);
            return true;
        }

        /// <summary>
        /// Updates a State from an Item if item exist.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newState"></param>
        ///         /// <param name="publishMessage">should the message be send to OpenHAB?</param>
        /// <returns></returns>
        public bool updateItem(string name, string newState,bool publishMessage)
        {
            ItemStateOpenHAB toUpdate = getItemByName(name);

            if (toUpdate == null)
            {
                //Debug.Log("UpdateItem 2: contains no item named: " + toUpdate.name);
                return false;
            }
            else
            {


                string oldState = toUpdate.state;

                toUpdate.state = newState;

                //Debug.Log("Old State = " + oldState + " New State = " + newState);

                //if state change then invoke subscribtions
                if (! newState.Equals(oldState))
                {
                    //Debug.Log("UpdateItem 2: put on changed list: " + toUpdate.name);
                    if(!hasChangedList.Contains(toUpdate))
                        hasChangedList.Add(toUpdate);
                }

                //but publish anyways
                if(publishMessage)
                {
                   // Debug.Log("UpdateItem 2: send Message async: " + toUpdate.name);
                    restCommunicator.Instance.publishItemStateopenHAB(toUpdate);
                }

                //Debug.Log("UpdateItem 2: returns " + toUpdate.name);
                return true;
            }

        }

        /// <summary>
        /// checks if something is on the hasChanged list invokes and removes it
        /// </summary>
        void Update()
        {

            foreach(ItemStateOpenHAB changeditem in hasChangedList)
            {
                invokeSubscriber(changeditem.name);
            }

            hasChangedList.Clear();
            
        }


        /// <summary>
        /// invokes all methodes registered to this itemname and passes the changed state
        /// </summary>
        /// <param name="itemName"></param>
        private void invokeSubscriber(string itemName)
        {
            if (itemName == null)
                return;

            if (subscriberList.ContainsKey(itemName))
            {
                //Debug.Log("invokeSubscriber: invokes: " + itemName);
                //invokes all methodes registered to this action
                subscriberList[itemName].Invoke(getItemByName(itemName).state);
            }
        }


        /// <summary>
        /// adds an Item, only if it doesnt exist.
        /// 
        /// </summary>
        /// <param name="newItem"></param>
        /// <returns>returns false, if Item already existed</returns>
        public bool addItem(ItemStateOpenHAB newItem)
        {
            if (!States.Contains(newItem))
            {
                States.Add(newItem);
                return true;
            }
            else
                return false;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <returns>returns the state as string</returns>
        public string getState (string name)
        {
            if (name == null)
                return null;

            return getItemByName(name)?.state;
        }

        /// <summary>
        /// returns all known item adresses, that fit the adress type
        /// Adress type can be https://www.openhab.org/docs/concepts/items.html#items
        /// 
        /// adress = name
        /// 
        /// </summary>
        /// <param name="adressType"></param>
        /// <returns></returns>
        public string[] getAdressesByType(string type)
        {
            List<string> adresses = new List<string>();

            foreach(ItemStateOpenHAB state in States)
            {
                if(state.type == type)
                    adresses.Add(state.name);
            }

            return adresses.ToArray();

        }

        /// <summary>
        /// returns the type this item has.
        /// returns null, if it doesnt dind anything
        /// </summary>
        /// <param name="adress"></param>
        /// <returns></returns>
        public string getOpenHabTypeByAdress(string adress)
        {
            return States.Find(x => x.name == adress).type;
        }


        /// <summary>
        /// this methode lets you subscribe to an item state change.
        /// if an Item state has changed, the Unity action object will be invoked
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="action"></param>
        public void subscribeToItem(string itemName, UnityAction<string> action)
        {
            if (itemName == null)
                return;

            //checks if action is already there
            if(subscriberList.ContainsKey(itemName)){
                //if yes, then adds action to it
                //Debug.Log("subscribeToItem: subscribes to containing: " + itemName);
                subscriberList[itemName] += action;
            }
            else
            {
                //Debug.Log("subscribeToItem: subscribes to new action: " + itemName);
                //if no, then creates a new one
                subscriberList.Add(itemName, new UnityAction<string>(action));
            }

        }

        /// <summary>
        /// this methode lets you unsubsubscribe to an item state change.
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="action"></param>
        public void unsubscribeToItem(string itemName, UnityAction<string> action)
        {
            //checks if action is there
            if (itemName!= null && subscriberList.ContainsKey(itemName))
            {
                //if yes, then removes action 
                subscriberList[itemName] -= action;
            }

        }

    }
}
