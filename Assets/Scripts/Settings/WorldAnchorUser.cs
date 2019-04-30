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
    public class WorldAnchorUser : MonoBehaviour
    {
        //in here is the world anchor while exporting
        //private List<Byte> toExport = null;

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
            public Operation(Action<object,object> call, object o = null, object o2 = null)
            {
                this.call = call;
                this.o = o;
                this.o2 = o2;
            }
            public Action<object,object> call = null;
            public object o = null;
            public object o2 = null;
        }

        //retry count for import
        //internal struct ImportInformation
        //{
        //    public ImportInformation(int retryCount, byte[] anchorData, DateTime anchorTime)
        //    {
        //        this.retryCount = retryCount;
        //        this.anchorData = anchorData;
        //        this.anchorTime = anchorTime;
        //    }
        //    public int retryCount { get; set; }
        //    public byte[] anchorData { get; set; }
        //    public DateTime anchorTime { get; set; }
        //}

        //// in here is the anchor while importing
        //private ImportInformation importInformation = default(ImportInformation);

        private WorldAnchorImport CurrentImportProcess;
        private WorldAnchorImport currentImportProcess
        {
            get
            {
                return CurrentImportProcess;
            }
            set
            {
                CurrentImportProcess?.stop();
                CurrentImportProcess = value;
            }
        }

        private WorldAnchorExport CurrentExportProcess;
        private WorldAnchorExport currentExportProcess
        {
            get
            {
                return CurrentExportProcess;
            }
            set
            {
                CurrentExportProcess?.stop();
                CurrentExportProcess = value;
            }
        }

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
                    if (PlayerPrefs.HasKey(tmpName))
                        StoreAnchorTime_str = PlayerPrefs.GetString(tmpName);

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
                PlayerPrefs.SetString(tmpName, value.ToString(TimeMethodesForAnchors.timeFormat));

            }
        }
        private string tmpName;


        /// <summary>
        /// this starts the process of restoring the position of the game object
        /// </summary>
        public void WorldAnchorOperations()
        {
            //we do not want to do the anchor stuff when we are in the editor:
            if (Application.isEditor)
                return;

            //save the name vor odd behaviour:
            tmpName = gameObject.name;

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
            if (successfull)
            {
                Debug.Log("Device " + gameObject.name + " loaded anchor from store successfully");
                anchorOrigin = AnchorOrigin.Local;
                StartCoroutine(loadAnchorFromServerifNotLocated(20));
            }

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

#region Anchor Import
        private void onAnchorDataAvailable(MqttNetClientAccess.mqttMessage message)
        {
            DateTime anchorTime = TimeMethodesForAnchors.parseAnchor(message.topic).Value;

            //we have the anchor now
            AppDataExportManager.Instance.unsubscribe(message.topic, onAnchorDataAvailable);

            //importInformation = new ImportInformation(3, message.payload, anchorTime);
            //TODO: what happens when one anchor gets available while the other is currently importing
            //then we should somehow stop the import of the other and use the new

            //this will start a new import process and kills the old if there is one
            currentImportProcess = new WorldAnchorImport(message.payload, importetAnchorFromWorldAnchorImport, anchorTime, 3);

        }

        private void importetAnchorFromWorldAnchorImport(WorldAnchorTransferBatch deserializedTransferBatch, DateTime anchorTime)
        {
            opsQueue.Enqueue(new Operation(onImportCompleteForUnity, deserializedTransferBatch, anchorTime));
        }

        private void onImportCompleteForUnity(object transferBatchObject, object anchorTimeObject)
        {
            WorldAnchorTransferBatch transferBatch = (WorldAnchorTransferBatch)transferBatchObject;
            DateTime anchorTime = (DateTime)anchorTimeObject;


            //before we lock this, we remove the world anchor
            detachWorldAnchor();

            //locks it in place
            WorldAnchor wa = transferBatch.LockObject(gameObject.name, this.gameObject);
            anchorOrigin = AnchorOrigin.Server;


            //if (!wa.isLocated)
            //    wa.OnTrackingChanged += saveWhenLocated;
            //else
            //    //saves the importet anchor in the store
            WorldAnchorManager.Instance.SaveAnchor(this.gameObject);
            //transferBatch.Dispose();

            //if import complete, we take the time from information as current time
            latestAnchor = anchorTime;

            //reset
            //importInformation = default(ImportInformation);
        }

        //private void saveWhenLocated(WorldAnchor self, bool located)
        //{
        //    WorldAnchorManager.Instance.SaveAnchor(this.gameObject);
        //    self.OnTrackingChanged -= saveWhenLocated;
        //}



        #endregion




        /// <summary>
        /// instantly destroys the world anchor if there is one attached to this GO
        /// </summary>
        public void detachWorldAnchor()
        {
            if (Application.isEditor)
                return;
            WorldAnchorManager.Instance.RemoveAnchorFromGO(this.gameObject);
        }

        /// <summary>
        /// adds a World Anchor if there is none attached
        /// </summary>
        public void attachWorldAnchor()
        {
            if (Application.isEditor)
                return;

            WorldAnchorManager.Instance.SaveAnchor(this.gameObject);
        }

        /// <summary>
        /// immediately adds a World Anchor if there is none attached
        /// </summary>
        public void attachWorldAnchorNow()
        {
            if (Application.isEditor)
                return;

            WorldAnchorManager.Instance.SaveAnchorNow(this.gameObject);
        }

        #region anchor export
        /// <summary>
        /// Exports a world anchor over MQTT
        /// Attaches (creates) an anchor, if there is non attached
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
            //toExport = null;

            currentExportProcess = new WorldAnchorExport(transferBatch,onExportProcessComplete);


        }

        private void onExportProcessComplete(List<byte> anchorData)
        {
            opsQueue.Enqueue(new Operation(onExportCompletedForUnity, anchorData));
        }


        

        private void onExportCompletedForUnity(object anchorDataObject, object o2)
        {

            var anchorData = (List<byte>)anchorDataObject;

            //anchor exported
            Debug.Log("Anchor Exported for: " + this.gameObject.name + ". now transmitting " + anchorData.Count + " bytes");
            AppDataExportManager.Instance.MQTTPublishAnchor(this.gameObject.name, anchorData.ToArray());

            latestAnchor = TimeMethodesForAnchors.getCurrentTimeforAnchors();

            //toExport = null;//free space
        }

        #endregion

        void Update()
        {
            Operation nextOp = null;
            opsQueue.TryDequeue(out nextOp);
            if(nextOp != null)
            {
                nextOp.call.Invoke(nextOp.o, nextOp.o2);
            }
        }


    }
}
