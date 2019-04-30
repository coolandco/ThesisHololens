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
    class WorldAnchorExport
    {
        private bool stopexport = false;
        //in here is the world anchor while exporting
        private List<byte> toExport = null;

        WorldAnchorTransferBatch transferBatch;
        UnityAction<List<byte>> callbackWhenFinished;

        public WorldAnchorExport(WorldAnchorTransferBatch transferBatch, UnityAction<List<byte>> callbackWhenFinished)
        {
            this.transferBatch = transferBatch;
            toExport = new List<byte>();
            this.callbackWhenFinished = callbackWhenFinished;

            WorldAnchorTransferBatch.ExportAsync(transferBatch, onExportDataAvailable, onExportCompleted);
        }


        private void onExportDataAvailable(byte[] data)
        {
            if (stopexport)
            {
                exit();
                return;
            }

            //add the data
            toExport.AddRange(data);

        }

        private void onExportCompleted(SerializationCompletionReason completionReason)
        {
            if (stopexport)
            {
                exit();
                return;
            }

            if (completionReason == SerializationCompletionReason.Succeeded)
            {
                callbackWhenFinished?.Invoke(toExport);

            }
            else if (completionReason == SerializationCompletionReason.AccessDenied)
            {
                Debug.LogError("The export of the worldanchor of the gameobject failed.\nAre you running this on the Hololens or Emulator? Have you enabled Spartial PerceptionCapability?");
            }
            else
            {
                Debug.LogError("The export of the worldanchor of the gameobject failed");
            }


        }


        private void exit()
        {
            transferBatch?.Dispose();
            Debug.Log(" Anchor export stop");
            callbackWhenFinished = null;
        }

        internal void stop()
        {
            stopexport = true;
        }

    }
}
