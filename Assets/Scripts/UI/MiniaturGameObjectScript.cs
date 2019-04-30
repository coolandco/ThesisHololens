using HoloToolkit.Examples.InteractiveElements;
using System.Collections.Generic;
using ThesisHololens.utilities;
using UnityEngine;
using UnityEngine.UI;

namespace ThesisHololens.UI.EditorMenu
{
    public class MiniaturGameObjectScript : MonoBehaviour
    {


        [Tooltip("Prefab of a GameObject that will be displayed in the ListButton")]
        [SerializeField]
        private GameObject ToShow_PREFAB = null; //this is only for inspector use

        private GameObject toShowCurrentInstance = null;

        /// <summary>
        /// during runtime, you may only use ready set up gameObjects
        /// They will be modified and destroyed
        /// </summary>
        public GameObject toShowInstance
        {
            get
            {
                return toShowCurrentInstance;

            }
            set
            {
                if(toShowCurrentInstance != null)
                    //destroy old instance
                    Destroy(toShowCurrentInstance);

                value.transform.parent = this.transform; // we are parent

                toShowCurrentInstance = value;

                showGameObjectPreview();
            }
        }

        private const string nameEnding = "_littleVersion";

        [Tooltip("Scales the object, so it doesnt exeed this value")]
        public float targetSizeHeight = 0.07f;

        [Tooltip("Scales the object, so it doesnt exeed this value")]
        public float targetSizeLenght= 0.15f;

        [Tooltip("Th offset of the miniature placement")]
        public Vector3 offset = new Vector3(-0.07f, 0f, 0.02f);


        /// <summary>
        /// calculates the given prefab (inspector) to a miniature version without any function
        /// because it instantiates the given Game object, awake on the scripts is called
        /// This can the prefab can only be changed in inspector
        /// </summary>
        public void showGameObjectPreviewFromPrefab()
        {
            if(ToShow_PREFAB == null)
            {
                Debug.LogWarning("no GameObject has been assinged");
                return;
            }

            foreach (Transform child in transform)
            {
                if (child.gameObject.name.EndsWith(nameEnding))
                    DestroyImmediate(child.gameObject);
            }

            //TODO: awake is called during Instantiate. Is that ok?
            GameObject toShowInstance_temp = Instantiate(ToShow_PREFAB, this.transform);
            toShowInstance_temp.name = ToShow_PREFAB.name + nameEnding;

            //sets and triggers
            toShowInstance = toShowInstance_temp;

        }


        private void showGameObjectPreview()
        {
            if (toShowInstance == null)
                return;

            //all comonents
            List<Component> toShow_components = new List<Component>(toShowInstance.GetComponentsInChildren<Component>());

            //removes all components but meshes
            foreach (Component comp in toShow_components)
            {
                //Debug.Log(comp.GetType());
                //if comp is not a mesh or a transform component or interactive component
                if (!comp.GetType().Equals(typeof(Mesh)) &&
                    !comp.GetType().Equals(typeof(Renderer)) &&
                    !comp.GetType().Equals(typeof(MeshRenderer)) &&
                    !comp.GetType().Equals(typeof(TextMesh)) &&
                    !comp.GetType().Equals(typeof(MeshFilter)) &&
                    !comp.GetType().Equals(typeof(Transform)) &&
                    !comp.GetType().Equals(typeof(Vector3InteractiveTheme)) &&
                    !comp.GetType().Equals(typeof(ColorInteractiveTheme)) &&
                    !comp.GetType().Equals(typeof(StringInteractiveTheme)) &&
                    !comp.GetType().Equals(typeof(TextureInteractiveTheme)) &&
                    !comp.GetType().Equals(typeof(ButtonFocusShowHideWidget)) &&
                    !comp.GetType().Equals(typeof(ButtonThemeWidget)) &&
                    !comp.GetType().Equals(typeof(ButtonThemeWidgetLabel)) &&
                    !comp.GetType().Equals(typeof(ButtonThemeWidgetOutline)) &&
                    !comp.GetType().Equals(typeof(ElementSelectedActiveWidget)) &&
                    !comp.GetType().Equals(typeof(LabelTheme)) &&
                    !comp.GetType().Equals(typeof(LoadingAnimation)) &&
                    !comp.GetType().Equals(typeof(SliderGestureControl)) &&
                    !comp.GetType().Equals(typeof(RectTransform)) &&//canvas
                    !comp.GetType().Equals(typeof(Canvas)) &&
                    !comp.GetType().Equals(typeof(CanvasScaler)) &&
                    !comp.GetType().Equals(typeof(GraphicRaycaster)) &&
                    !comp.GetType().Equals(typeof(Text)) &&
                    !comp.GetType().Equals(typeof(CanvasRenderer)))



                    if (Application.isPlaying) //Application.isPlaying
                    {//if called from inspector, use destroyimmediate
                        Destroy(comp);
                        try
                        {
                            //MonoBehaviour b = (MonoBehaviour)comp;
                            //b.enabled = false;
                            //Destroy(b);
                        }
                        catch { }
                    }
                    else
                    {
                        DestroyImmediate(comp);
                    }
                       
            }


            //bounds of original obj
            Bounds toShow_Instance_Bounds = BoundsUtils.getBounds(toShowInstance);


            Vector3 newScale = toShowInstance.transform.localScale;
            //use the smallest resize
            if (targetSizeHeight / toShow_Instance_Bounds.size.y < targetSizeLenght / toShow_Instance_Bounds.size.x)
            {
                //calc a new scale based on targetSizeHeight
                newScale.y = targetSizeHeight * newScale.y / toShow_Instance_Bounds.size.y;
                newScale.x = targetSizeHeight * newScale.x / toShow_Instance_Bounds.size.y;
                newScale.z = targetSizeHeight * newScale.z / toShow_Instance_Bounds.size.y;
            }
            else
            {
                //calc a new scale based on targetSizeLenght
                newScale.y = targetSizeLenght * newScale.y / toShow_Instance_Bounds.size.x;
                newScale.x = targetSizeLenght * newScale.x / toShow_Instance_Bounds.size.x;
                newScale.z = targetSizeLenght * newScale.z / toShow_Instance_Bounds.size.x;
            }

            //set new scale
            toShowInstance.transform.localScale = newScale;


            //applies the scale resize to the go
            // toShow_Instance.transform.localScale = toShow_Instance.transform.lossyScale / scaleFactor;

            //apply the offset
            toShowInstance.transform.localPosition = offset;
            toShowInstance.transform.localEulerAngles = Vector3.zero;
        }


        


    }
}
