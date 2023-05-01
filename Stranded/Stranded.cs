using UnityEngine;

namespace Stranded
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class Stranded : MonoBehaviour
    {
        private GameObject sphere;

        public void Start()
        {
            sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(SpaceCenter.Instance.SpaceCenterTransform);
            sphere.transform.localScale = 100.0f * Vector3.one;
            // sphere.SetLayerRecursive(2);

            /*for (int i = 0; i < 32; ++i)
            {
                Debug.Log("Layer " + i + ": " + LayerMask.LayerToName(i));
            }*/

            /*Scene activeScene = SceneManager.GetActiveScene();
            
            foreach (GameObject obj in activeScene.GetRootGameObjects())
            {
                Debug.Log("Parent: " + obj.name);
                Debug.Log("Children:");
                foreach (Transform child in obj.transform)
                {
                    Debug.Log(child.name);
                }
            }*/
        }

        public void Update()
        {
            // Debug.Log("Hello world! " + Time.realtimeSinceStartup);
            RaycastHit hit;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            /*Debug.DrawRay(ray.origin, ray.direction * 10, Color.yellow);
            Debug.Log("Mouse.screenPos: " + Mouse.screenPos);
            Debug.Log("Input.mousePosition: " + Input.mousePosition);
            Debug.Log("Casting ray " + ray);*/
            bool isHit = Physics.Raycast(ray, out hit, 10000.0f, 1 << 15);
            if (!isHit)
            {
                //Debug.Log("No hit");
            }
            else
            {
                //Debug.Log("Hit! Collider " + hit.collider.name + " transform " + hit.transform.name + " at " +
                //          hit.point + ", layer " + hit.transform.gameObject.layer + ", distance " + hit.distance);
                sphere.transform.position = hit.point;
            }
        }
    }
}