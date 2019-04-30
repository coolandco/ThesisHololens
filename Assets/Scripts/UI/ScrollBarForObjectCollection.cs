using HoloToolkit.Unity.Collections;
using HoloToolkit.Unity.InputModule;
using UnityEngine;

namespace ThesisHololens.UI.EditorMenu
{
    /// <summary>
    /// based on https://docs.microsoft.com/en-us/windows/mixed-reality/holograms-211
    /// 
    /// it only works on surface type "sphere"
    /// </summary>
    /// 
    [RequireComponent(typeof(ObjectCollection))]
    public class ScrollBarForObjectCollection : MonoBehaviour, INavigationHandler
    {
        [Tooltip("Rotation max speed controls amount of rotation.")]
        [SerializeField]
        private float NavigationSensitivity = 5f;

        ObjectCollection objectCollection;


        [Tooltip("a Number in m, that will be shown")]
        public float showSmallerThanThis = 2.4f;


        [Tooltip("a Number in m, that will be shown")]
        public float showBiggerThanThis = 0.4f;



        void Start()
        {
            objectCollection = GetComponent<ObjectCollection>();

            //enshure surface type
            if(objectCollection.SurfaceType != SurfaceTypeEnum.Plane)
            {
                objectCollection.SurfaceType = SurfaceTypeEnum.Plane;
            }

            if (showSmallerThanThis < showBiggerThanThis)
            {
                var tmp = showBiggerThanThis;
                showBiggerThanThis = showSmallerThanThis;
                showSmallerThanThis = tmp;
            }
        }


        void Update()
        {
            //so it is withhin range
            //little bit les, because of tolerances
            //node 0 is highest element
            //last node is lowest element
            if(objectCollection.NodeList.Count > 1 && objectCollection.NodeList[objectCollection.NodeList.Count - 1]?.transform?.position.y - 0.00001f > showSmallerThanThis)//for the top of the list
            {

                objectCollection.transform.position += new Vector3(0, showSmallerThanThis - objectCollection.NodeList[objectCollection.NodeList.Count - 1].transform.position.y - 0.00001f, 0);

            }
            else if(objectCollection.NodeList.Count > 1 && objectCollection.NodeList[0]?.transform?.position.y < showBiggerThanThis)//for the bottom
            {

                //manage, so that, the object collection is again within the showable range
                objectCollection.transform.position -= new Vector3(0, objectCollection.NodeList[0].transform.position.y - showBiggerThanThis, 0);
            }
                

            foreach (CollectionNode node in objectCollection.NodeList)
            {
                if (node.transform == null)
                {
                    Debug.LogWarning("node Is null, this should not occur more than once");
                    continue;
                }

                //we assume, that the heigt of the ceiling is 2,2m
                //we assume that the floor is at 0
                
                //We want to show stuff between 0.4 and 2.4 meters
                //we want to make stuff transparent between 0.4 and 0.6 al well 2.2 and 2.6m

                if(node.transform.position.y < showBiggerThanThis || node.transform.position.y > showSmallerThanThis)
                {
                    node.transform.gameObject.SetActive(false);

                } 
                else
                {
                    node.transform.gameObject.SetActive(true);
                }

            }
        }



        public void OnNavigationStarted(NavigationEventData eventData)
        {
            InputManager.Instance.PushModalInputHandler(gameObject);
        }

        public void OnNavigationUpdated(NavigationEventData eventData)
        {
            //move up or down
            float movingFactor = eventData.NormalizedOffset.y * NavigationSensitivity;

            transform.position = new Vector3(transform.position.x, transform.position.y + (1 * movingFactor), transform.position.z);

        }

        public void OnNavigationCanceled(NavigationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }


        public void OnNavigationCompleted(NavigationEventData eventData)
        {
            InputManager.Instance.PopModalInputHandler();
        }
    }
}
