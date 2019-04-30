using HoloToolkit.Unity;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.WSA;
using UnityEngine.XR.WSA.Persistence;

namespace ThesisHololens.Settings
{
    public class WorldAnchorManager : Singleton<WorldAnchorManager>
    {


        /// <summary>
        /// To prevent initializing too many anchors at once
        /// and to allow for the WorldAnchorStore to load asynchronously
        /// without callers handling the case where the store isn't loaded yet
        /// we'll setup a queue of anchor attachment operations.  
        /// The AnchorAttachmentInfo struct has the data needed to do this.
        /// </summary>
        protected struct AnchorAttachmentInfo
        {
            public GameObject AnchoredGameObject { get; set; }
            public string AnchorName { get; set; }
            public AnchorOperation Operation { get; set; }
            public UnityAction<bool> onComplete { get; set; }
        }


        /// <summary>
        /// The data structure for anchor operations.
        /// </summary>
        protected enum AnchorOperation
        {
            /// <summary>
            /// Save anchor to anchor store.  Creates anchor if none exists.
            /// </summary>
            Save,
            /// <summary>
            /// Deletes anchor from anchor store.
            /// </summary>
            Delete,
            /// <summary>
            /// tries to load an anchor from the store
            /// </summary>
            Load
        }


        /// <summary>
        /// The queue for local device anchor operations.
        /// </summary>
        protected Queue<AnchorAttachmentInfo> LocalAnchorOperations = new Queue<AnchorAttachmentInfo>();


        /// <summary>
        /// The WorldAnchorStore for the current application.
        /// Can be null when the application starts.
        /// </summary>
        public WorldAnchorStore AnchorStore { get; protected set; }


        protected override void Awake()
        {
            base.Awake();
            AnchorStore = null;
        }

        protected virtual void Start()
        {
            WorldAnchorStore.GetAsync(AnchorStoreReady);
        }

        protected virtual void Update()
        {
            if (AnchorStore == null) { return; }

            if (LocalAnchorOperations.Count > 0)
            {
                DoAnchorOperation(LocalAnchorOperations.Dequeue());
            }
        }

        /// <summary>
        /// Callback function that contains the WorldAnchorStore object.
        /// </summary>
        /// <param name="anchorStore">The WorldAnchorStore to cache.</param>
        protected virtual void AnchorStoreReady(WorldAnchorStore anchorStore)
        {
            AnchorStore = anchorStore;

        }



        /// <summary>
        /// Attaches an anchor to the GameObject if it has none.
        /// Saves the anchor in the anchor manager
        /// </summary>
        /// <param name="gameObjectToAnchor">The GameObject to attach the anchor to.</param>
        /// <param name="anchorName">Name of the anchor. </returns>
        public void SaveAnchor(GameObject gameObjectToAnchor)
        {

            if (gameObjectToAnchor == null)
            {
                Debug.LogError("[WorldAnchorManager] Must pass in a valid gameObject");
                return;
            }


            LocalAnchorOperations.Enqueue(
                new AnchorAttachmentInfo
                {
                    AnchoredGameObject = gameObjectToAnchor,
                    AnchorName = gameObjectToAnchor.name,
                    Operation = AnchorOperation.Save
                }
            );
        }

        /// <summary>
        /// Attaches an anchor to the GameObject if it has none immediately.
        /// Saves the anchor in the anchor manager
        /// </summary>
        /// <param name="gameObjectToAnchor">The GameObject to attach the anchor to.</param>
        /// <param name="anchorName">Name of the anchor. </returns>
        public void SaveAnchorNow(GameObject gameObjectToAnchor)
        {

            if (gameObjectToAnchor == null)
            {
                Debug.LogError("[WorldAnchorManager] Must pass in a valid gameObject");
                return;
            }


            DoAnchorOperation(
                new AnchorAttachmentInfo
                {
                    AnchoredGameObject = gameObjectToAnchor,
                    AnchorName = gameObjectToAnchor.name,
                    Operation = AnchorOperation.Save
                }
            );
        }


        /// <summary>
        /// Removes the anchor component from the GameObject and from store
        /// happens immediadely
        /// </summary>
        public void removeAnchor(GameObject gameObjectToRemove)
        {

            if (gameObjectToRemove == null)
            {
                Debug.LogError("[WorldAnchorManager] Must pass in a valid gameObject");
                return;
            }

            if (AnchorStore == null)
            {
                Debug.LogWarning("[WorldAnchorManager] AttachAnchor called before anchor store is ready.");
            }

            //removes anchor from GO
            RemoveAnchorFromGO(gameObjectToRemove);

            //immediately
            AnchorStore.Delete(gameObjectToRemove.name);

        }

        /// <summary>
        /// Removes the anchor from store
        /// happens immediadely
        /// </summary>
        public void removeAnchor(string Anchorname)
        {

            if (Anchorname == null)
            {
                Debug.LogError("[WorldAnchorManager] Must pass in a valid Anchor name");
                return;
            }

            if (AnchorStore == null)
            {
                Debug.LogWarning("[WorldAnchorManager] AttachAnchor called before anchor store is ready.");
            }

            //immediately
            AnchorStore.Delete(Anchorname);

        }


        /// <summary>
        /// Removes the anchor component from the GameObject but not from store
        /// happens immediadely
        /// </summary>
        /// <param name="gameObjectToUnanchor">The GameObject reference with valid anchor</param>
        public void RemoveAnchorFromGO(GameObject gameObjectToUnanchor)
        {
            if (gameObjectToUnanchor == null)
            {
                Debug.LogError("[WorldAnchorManager] Invalid GameObject!");

                return;
            }

            //do imediadely
            WorldAnchor wa = gameObjectToUnanchor.GetComponent<WorldAnchor>();
            if (wa != null)
            {
                DestroyImmediate(wa);
            }


        }

        /// <summary>
        /// loads an anchor from the store, if there is one saved
        /// </summary>
        /// <param name="toLoad"></param>
        public void LoadAnchor(GameObject toLoad, UnityAction<bool> onComplete)
        {
            if (toLoad == null)
            {
                Debug.LogError("[WorldAnchorManager] Invalid GameObject!");

                return;
            }

            LocalAnchorOperations.Enqueue(
                new AnchorAttachmentInfo
                {
                    AnchoredGameObject = toLoad,
                    AnchorName = toLoad.name,
                    Operation = AnchorOperation.Load,
                    onComplete = onComplete
                }
            );


        }

        //TODO: exception or not?
        public bool hasAnchorInStore(GameObject toCheck)
        {
            if (AnchorStore == null || toCheck == null)
                return false;

            return new List<string>(AnchorStore.GetAllIds()).Contains(toCheck.name);
        }

        /// <summary>
        /// Executes the anchor operations from the localAnchorOperations queue.
        /// </summary>
        /// <param name="anchorAttachmentInfo">Parameters for attaching the anchor.</param>
        private void DoAnchorOperation(AnchorAttachmentInfo anchorAttachmentInfo)
        {
            GameObject anchoredGameObject = anchorAttachmentInfo.AnchoredGameObject;


            if (AnchorStore == null || anchoredGameObject == null || Application.isEditor)//no Wa if in editor
                return;



            switch (anchorAttachmentInfo.Operation)
            {
                case AnchorOperation.Save:

                    //before we save, we delete the old anchor from the store
                    if (new List<string>(AnchorStore.GetAllIds()).Contains(anchoredGameObject.name))
                        AnchorStore.Delete(anchoredGameObject.name);

                    //gets the anchor to save from the GO, creates one, if there is none
                    WorldAnchor anchorToSave = anchoredGameObject.GetComponent<WorldAnchor>();
                    if (anchorToSave == null)
                        anchorToSave = anchoredGameObject.AddComponent<WorldAnchor>();
                    //saves the anchor in the store
                    AnchorStore.Save(anchoredGameObject.name, anchorToSave);



                    break;
               
                case AnchorOperation.Load:
                    //if there is an anchor for the go, then load it and attache it
                    if (new List<string>(AnchorStore.GetAllIds()).Contains(anchoredGameObject.name))
                    {
                        AnchorStore.Load(anchoredGameObject.name, anchoredGameObject);
                        anchorAttachmentInfo.onComplete?.Invoke(true);
                    }
                    else
                    {
                        anchorAttachmentInfo.onComplete?.Invoke(false);
                    }

                    
                    break;
                case AnchorOperation.Delete:
                    //should not habben, because we do it directly in the methode
                    removeAnchor(anchoredGameObject);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }

        }
        

    }
}
