using HoloToolkit.Unity;
using System;
using System.Collections.Generic;
using ThesisHololens.Communication;
using ThesisHololens.utilities;
using UnityEngine;
using UnityEngine.Events;

namespace ThesisHololens.Settings
{
    public class WorldAnchorAvailableOnlineManager : Singleton<WorldAnchorAvailableOnlineManager>
    {

        [Tooltip("Enter the World Anchor Status Topic without wildcard '#' , the status of the anchors is managed server side")]
        public string worldAnchorStatusSubscriptionPath = "/WorldAnchorStatus/";



        //all currently all anchors available online
        //TODO: only the newest anchors should be here
        Dictionary<string, DateTime> anchorsAvailableOnMqtt = new Dictionary< string, DateTime>();

        //notification for newAnchorAvailable
        /// <summary>
        /// key: name of anchor, that we listen to, without date
        /// value: callback of methode. We pass the name with with the time as key value pair
        /// </summary>
        Dictionary<string, UnityAction<KeyValuePair<string, DateTime>>> newAnchorAvailableDic = new Dictionary<string, UnityAction<KeyValuePair<string, DateTime>>>();


        private AppDataExportManager waem;

        /// <summary>
        /// did we already gather the anchors online?
        /// </summary>
        private bool ranOnce = false;

        private void Start()
        {
            waem = AppDataExportManager.Instance;


            //get all anchors on the status channel
            waem.subscribe(worldAnchorStatusSubscriptionPath + "#", addWorldAnchorStatus);
        }


        /// <summary>
        /// subscribe for getting new anchors and optional old ones
        /// </summary>
        /// <param name="name">name without the time</param>
        /// <param name="onAnchorAvailable"></param>
        /// <param name="getPastAnchors"> also get the anchors that have already been received</param>
        public void subscribeToNewAnchors(string anchorname, UnityAction<KeyValuePair<string, DateTime>> onAnchorAvailable , bool getPastAnchors = false)
        {

            if (getPastAnchors && anchorsAvailableOnMqtt.ContainsKey(anchorname))
                onAnchorAvailable.Invoke(new KeyValuePair<string, DateTime>(anchorname, anchorsAvailableOnMqtt[anchorname]));

            //add the callback to list
            if (newAnchorAvailableDic.ContainsKey(anchorname))
            {
                //only add if methode is not already in list;
                if(! new List<Delegate>(newAnchorAvailableDic[anchorname].GetInvocationList()).Contains(onAnchorAvailable))
                    newAnchorAvailableDic[anchorname] += onAnchorAvailable;
            }
            else//we need to add an entry
            {
                newAnchorAvailableDic.Add(anchorname, onAnchorAvailable);
            }
        }

        /// <summary>
        /// unsubscribe for getting new anchors
        /// </summary>
        /// <param name="name">name without the time</param>
        /// <param name="onAnchorAvailable"></param>
        public void unsubscribeToNewAnchors(string anchorname, UnityAction<KeyValuePair<string, DateTime>> onAnchorAvailable)
        {
            if (newAnchorAvailableDic.ContainsKey(anchorname))
            {
                //if there is only one entry in the delegate
                if (newAnchorAvailableDic[anchorname].GetInvocationList().Length <= 1)
                {
                    //delete entry
                    newAnchorAvailableDic.Remove(anchorname);
                }
                else
                {
                    newAnchorAvailableDic[anchorname] -= onAnchorAvailable;
                }
            }
        }



        /// <summary>
        /// new anchor status will bee added trough here
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void addWorldAnchorStatus(MqttNetClientAccess.mqttMessage message)
        {
            KeyValuePair<string, DateTime> newAnchorParsed = default(KeyValuePair<string, DateTime>);
            string[] tpcPth = null;
            string tpcPath_last = null;
            try
            {
                tpcPth = message.topic.Split('/'); //splits the topic path segments
                tpcPath_last = tpcPth[tpcPth.Length - 1]; //gets the last segment (name of the anchor with timestamp)

                Debug.Log("received message, added: " + tpcPath_last);


                newAnchorParsed = TimeMethodesForAnchors.parseAnchor(tpcPath_last);
            }
            catch
            {
                Debug.LogError("could not parse anchor: " + tpcPath_last);
                return;
            }

            if (message.payload.Length == 0)
                return; //only valid messages get processed

            //if there is already an anchor, remove it
            if (anchorsAvailableOnMqtt.ContainsKey(newAnchorParsed.Key)) //anchorsAvailableOnMqtt[newAnchorParsed.Key] < newAnchorParsed.Value)
            {
                if (anchorsAvailableOnMqtt[newAnchorParsed.Key] >= newAnchorParsed.Value)
                    return;//only lets newer anchors come into the list. older anchors gets dropped

                anchorsAvailableOnMqtt.Remove(newAnchorParsed.Key);
            }

            //adds the anchor to the available anchors
            anchorsAvailableOnMqtt.Add(newAnchorParsed.Key, newAnchorParsed.Value);

            //now check if someone wants to be notified
            if (newAnchorAvailableDic.ContainsKey(newAnchorParsed.Key))
                newAnchorAvailableDic[newAnchorParsed.Key].Invoke(newAnchorParsed);

        }


        /// <summary>
        /// returns the anchor available or default 
        /// </summary>
        /// <param name="anchorname"></param>
        /// <returns></returns>
        public KeyValuePair<string, DateTime> getAnchorAvailableOnline(string anchorname)
        {
            if (anchorsAvailableOnMqtt.ContainsKey(anchorname))
                return new KeyValuePair<string, DateTime>(anchorname, anchorsAvailableOnMqtt[anchorname]);
            else
                return default(KeyValuePair<string, DateTime>);


        }



        ///// <summary>
        ///// checks if a newer anchor than
        ///// </summary>
        ///// <param name="referenceAnchor"></param>
        ///// <returns></returns>
        //public bool isNewerAnchorOnlineAvailable(string referenceAnchor)
        //{


        //    bool newAnchorIsAvailable = false;

        //    //name is only the anchor name without the date attached
        //    KeyValuePair<string,DateTime> refAnchorParsed;

        //    try
        //    {
        //        refAnchorParsed = TimeMethodesForAnchors.parseAnchor(referenceAnchor);
        //    }
        //    catch
        //    {
        //        Debug.LogError("Parser failed To parse anchor: " + referenceAnchor);
        //        return false;
        //    }


        //    //every anchor available online
        //    foreach (KeyValuePair<string,DateTime> availableAnchor in anchorsAvailableOnMqtt)
        //    {
        //        //if anchor starts with the reference anchor name
        //        if (availableAnchor.Key.StartsWith(refAnchorParsed.Key))
        //        {

        //            if (refAnchorParsed.Value < availableAnchor.Value)
        //            {
        //                newAnchorIsAvailable = true;
        //            }

        //        }

        //    }

        //    //report back
        //    return newAnchorIsAvailable;

        //}
        
    }
}
