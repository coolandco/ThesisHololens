

using UnityEngine;

namespace ThesisHololens.utilities
{
    public static class BoundsUtils
    {

        public static Bounds getBounds(GameObject go)
        {
            Bounds bounds;
            Renderer childRender;
            bounds = getRenderBounds(go);
            if (bounds.extents.x == 0)
            {
                bounds = new Bounds(go.transform.position, Vector3.zero);
                foreach (Transform child in go.transform)
                {
                    childRender = child.GetComponent<Renderer>();
                    if (childRender)
                    {
                        bounds.Encapsulate(childRender.bounds);
                    }
                    else
                    {
                        bounds.Encapsulate(getBounds(child.gameObject));
                    }
                }
            }
            return bounds;
        }

        private static Bounds getRenderBounds(GameObject go)
        {
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            Renderer render = go.GetComponent<Renderer>();
            if (render != null)
            {
                return render.bounds;
            }
            return bounds;
        }
    }
}
