using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ThesisHololens.Communication;
using ThesisHololens.utilities;
using UnityEngine;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Sharing;

namespace ThesisHololens.Settings
{
    /// <summary>
    /// we are assuming, that the name of a device is unique. We will use the name for the world anchor user, too.
    /// </summary>
    public class WorldAnchorUser2 : MonoBehaviour
    {
        //in here is the world anchor while exporting
        private List<Byte> toExport = null;

        //this tells us when the last time was, that the anchor was synchronized
        //private DateTime lastAnchorRefresh_UTC;

        //can be local or server
        public enum AnchorOrigin
        {
            notAssigned,
            Local,
            OverrideAnchorFromServer,// get the anchor from the server anyway
            Server
        }

        private AnchorOrigin anchorOrigin = AnchorOrigin.notAssigned;


        //because another thread comes to play
        private ConcurrentQueue<Operation> opsQueue = new ConcurrentQueue<Operation>();

        private class Operation
        {
            public Operation(Action<object> call, object o)
            {
                this.call = call;
                this.o = o;
            }
            public Action<object> call = null;
            public object o = null;
        }

        //retry count for import
        private struct ImportInformation
        {
            public ImportInformation(int retryCount, byte[] anchorData, DateTime anchorTime)
            {
                this.retryCount = retryCount;
                this.anchorData = anchorData;
                this.anchorTime = anchorTime;
            }
            public int retryCount { get; set; }
            public byte[] anchorData { get; set; }
            public DateTime anchorTime { get; set; }
        }

        // in here is the anchor while importing
        private ImportInformation importInformation = default(ImportInformation);
        /// <summary>
        /// //returns the latest anchor date known
        /// does not guarantee, that this anchor exists
        /// </summary>
        private DateTime latestAnchor
        {
            get
            {
                string StoreAnchorTime_str = "";
                DateTime toReturn = default(DateTime);

                try
                {
                    if (PlayerPrefs.HasKey(this.gameObject.name))
                        StoreAnchorTime_str = PlayerPrefs.GetString(this.gameObject.name);

                    if (!string.IsNullOrEmpty(StoreAnchorTime_str))
                        toReturn = TimeMethodesForAnchors.parseTime(StoreAnchorTime_str);
                }
                catch(Exception e)
                {
                    Debug.Log(e);
                }

                
                return toReturn;
            }
            set
            {
                PlayerPrefs.SetString(this.gameObject.name, value.ToString(TimeMethodesForAnchors.timeFormat));

            }
        }


        /// <summary>
        /// this starts the process of restoring the position of the game object
        /// </summary>
        public void WorldAnchorOperations()
        {
            //we do not want to do the anchor stuff when we are in the editor:
            if (Application.isEditor)
                return;
            //plan:
            //1. wait for the mqtt to establish a connection
            //2. ask for new anchor
            StartCoroutine(startLoadAnchorFromServer(10));

            //3. get time of anchor from store



            //4. get anchor from store
            WorldAnchorManager.Instance.LoadAnchor(this.gameObject, onAnchorLoadedFromStore);

            //5. later, we decide if we want to get an anchor from mqtt (onAnchorAvailable)

        }

        private void onAnchorLoadedFromStore(bool successfull)
        {
            Debug.Log("Device " + gameObject.name + " loaded anchor from store");
            anchorOrigin = AnchorOrigin.Local;
            StartCoroutine(loadAnchorFromServerifNotLocated(20));
        }

        private IEnumerator loadAnchorFromServerifNotLocated(int seconds)
        {
            yield return new WaitForSeconds(seconds);

            var WA = GetComponent<WorldAnchor>();

            //if anchor not located within this time
            if (anchorOrigin == AnchorOrigin.Local && WA != null && !this.GetComponent<WorldAnchor>().isLocated)
            {
                Debug.Log("Device " + gameObject.name + " local anchor could not be located, now trying from server");
                anchorOrigin = AnchorOrigin.OverrideAnchorFromServer;//set to override
                StartCoroutine(startLoadAnchorFromServer(0));
            }
        }

        private IEnumerator startLoadAnchorFromServer(int waitForSeconds)
        {
            yield return new WaitForSeconds(waitForSeconds);

            if(anchorOrigin == AnchorOrigin.OverrideAnchorFromServer)
                WorldAnchorAvailableOnlineManager.Instance.unsubscribeToNewAnchors(gameObject.name, onAnchorAvailable);

            WorldAnchorAvailableOnlineManager.Instance.subscribeToNewAnchors(gameObject.name, onAnchorAvailable, true);
        }

        private void onAnchorAvailable(KeyValuePair<string, DateTime> anchor)
        {
            if(anchor.Equals(default(KeyValuePair<string, DateTime>)))
            {
                //no anchor has been found

                //fallback to any anchor from Store
                Debug.LogWarning("empty anchor given");
            }
            else
            {
                //3. decide wether to load the new anchor online or keep from store

                //if anchor.value is earlier than latest anchor
                //or the anchor from the store does not exist
                //or anchor is on override
                if(anchor.Value > latestAnchor || ! WorldAnchorManager.Instance.hasAnchorInStore(gameObject) || anchorOrigin == AnchorOrigin.OverrideAnchorFromServer)
                //get the anchor from mqtt
                //we have the name and the  time of the newest anchor
                //generate a topic and subscribe to it
                    AppDataExportManager.Instance.subscribe(
                        AppDataExportManager.Instance.worldAnchorBaseSubscriptionPath +
                        TimeMethodesForAnchors.getAnchorNameWithTimeFromKeyValuePair(anchor),
                        onAnchorDataAvailable);
            }

        }

        private void onAnchorDataAvailable(MqttNetClientAccess.mqttMessage message)
        {
            DateTime anchorTime = TimeMethodesForAnchors.parseAnchor(message.topic).Value;

            importInformation = new ImportInformation(3, message.payload, anchorTime);
            //TODO: what happens when one anchor gets available while the other is currently importing
            //then we should somehow stop the import of the other and use the new

            //we have the anchor now
            AppDataExportManager.Instance.unsubscribe(message.topic, onAnchorDataAvailable);

            WorldAnchorTransferBatch.ImportAsync(message.payload, onImportComplete);
        }

        //is called from the world anchor transfer batch
        private void onImportComplete(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
        {
            if (importInformation.Equals(default(ImportInformation)))
                return;//already finished to import

            if (completionReason != SerializationCompletionReason.Succeeded)
            {
                Debug.Log("Failed to import: " + completionReason.ToString());
                if (importInformation.retryCount > 0)
                {
                    importInformation.retryCount--;
                    WorldAnchorTransferBatch.ImportAsync(importInformation.anchorData, onImportComplete);
                }
                return;
            }

            opsQueue.Enqueue(new Operation(onImportCompleteForUnity, deserializedTransferBatch));

        }

        private void onImportCompleteForUnity(object transferBatchObject)
        {
            WorldAnchorTransferBatch transferBatch = (WorldAnchorTransferBatch)transferBatchObject;


            //before we lock this, we remove the world anchor
            detachWorldAnchor();

            //locks it in place
            WorldAnchor wa = transferBatch.LockObject(gameObject.name, this.gameObject);
            anchorOrigin = AnchorOrigin.Server;

            //saves the importet anchor in the store
            WorldAnchorManager.Instance.SaveAnchor(this.gameObject);


            //if import complete, we take the time from information as current time
            latestAnchor = importInformation.anchorTime;

            //reset
            importInformation = default(ImportInformation);
        }






        /// <summary>
        /// instantly destroys the world anchor if there is one attached to this GO
        /// </summary>
        public void detachWorldAnchor()
        {
            WorldAnchorManager.Instance.RemoveAnchorFromGO(this.gameObject);
        }

        /// <summary>
        /// adds a World Anchor if there is none attached
        /// </summary>
        public void attachWorldAnchor()
        {
            WorldAnchorManager.Instance.SaveAnchor(this.gameObject);
        }

        /// <summary>
        /// immediately adds a World Anchor if there is none attached
        /// </summary>
        public void attachWorldAnchorNow()
        {
            WorldAnchorManager.Instance.SaveAnchorNow(this.gameObject);
        }


        /// <summary>
        /// Exports a world anchor over MQTT
        /// Attaches an anchor, if there is no attached
        /// </summary>
        public void exportAnchorToWorldAnchorExportManager()
        {
            if (Application.isEditor)
            {
                //WA does not work in editor mode
                return;
            }
            //we make sure there is an anchor attached
            attachWorldAnchorNow();

            WorldAnchorTransferBatch transferBatch = new WorldAnchorTransferBatch();


            transferBatch.AddWorldAnchor(gameObject.name , GetComponent<WorldAnchor>());

            //reset export list
            toExport = null;

            WorldAnchorTransferBatch.ExportAsync(transferBatch, onExportDataAvailable, onExportCompleted);

        }


        private void onExportDataAvailable(byte[] data)
        {
            //add the
            if (toExport == null)
            {
                toExport = new List<byte>(data);
            }
            else
            {
                toExport.AddRange(data);
            }
        }

        private void onExportCompleted(SerializationCompletionReason completionReason)
        {
            if (completionReason == SerializationCompletionReason.Succeeded)
            {
                opsQueue.Enqueue(new Operation(onExportCompletedForUnity, null));

                
            }
            else if (completionReason == SerializationCompletionReason.AccessDenied)
            {
                Debug.LogError("The export of the worldanchor of the gameobject " + this.gameObject.name + " failed.\nAre you running this on the Hololens or Emulator? Have you enabled Spartial PerceptionCapability?");
            }
            else
            {
                Debug.LogError("The export of the worldanchor of the gameobject " + this.gameObject.name + " failed");
            }

            
        }

        private void onExportCompletedForUnity(object transferbatch)
        {

            //anchor exported
            Debug.Log("Anchor Exported for: " + this.gameObject.name + ". now transmitting " + toExport.Count + " bytes");
            AppDataExportManager.Instance.MQTTPublishAnchor(this.gameObject.name, toExport.ToArray());

            latestAnchor = TimeMethodesForAnchors.getCurrentTimeforAnchors();

            toExport = null;//free space
        }

        void Update()
        {
            Operation nextOp = null;
            opsQueue.TryDequeue(out nextOp);
            if(nextOp != null)
            {
                nextOp.call.Invoke(nextOp.o);
            }
        }


    }
}
