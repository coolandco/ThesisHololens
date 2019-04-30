using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ThesisHololens.Communication;
using ThesisHololens.utilities;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Sharing;

namespace ThesisHololens.Settings
{
    /// <summary>
    /// main Purpose of this class is, to tidy up the WorldAnchorUser and refine the Export Process so it can be interrupted
    /// </summary>
    class WorldAnchorImport
    {
        private bool stopImport = false;

        UnityAction<WorldAnchorTransferBatch, DateTime> callbackWhenFinished;
        DateTime anchorTime;
        int retryCount;
        byte[] anchorData;

        internal WorldAnchorImport(byte[] anchor, UnityAction<WorldAnchorTransferBatch,DateTime> callbackWhenFinished, DateTime anchorTime, int retryCount = 3)
        {
            this.retryCount = retryCount;
            this.callbackWhenFinished = callbackWhenFinished;
            this.anchorTime = anchorTime;
            this.anchorData = anchor;

            WorldAnchorTransferBatch.ImportAsync(anchor, onImportComplete);
        }


        //is called from the world anchor transfer batch
        private void onImportComplete(SerializationCompletionReason completionReason, WorldAnchorTransferBatch deserializedTransferBatch)
        {
            if (stopImport)
            {
                exit();
                return;
            }


            if (completionReason != SerializationCompletionReason.Succeeded)
            {
                Debug.Log("Failed to import: " + completionReason.ToString());
                if (retryCount > 0)
                {
                    retryCount--;
                    WorldAnchorTransferBatch.ImportAsync(anchorData, onImportComplete);
                }
                return;
            }

            callbackWhenFinished?.Invoke(deserializedTransferBatch, anchorTime);
        }

        private void exit()
        {
            Debug.Log("Different Anchor will be Importet");
            callbackWhenFinished = null;
        }

        internal void stop()
        {
            stopImport = true;
        }
    }
}
