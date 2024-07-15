using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using System.IO;

public class SelectionUnderstandingManager : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    private NetworkId networkId = new NetworkId(97);
    public XRRayInteractor rayInteractor;
    public ActionBasedController actionBasedController;
    private string lastSelection = "";
    private string currentSelection = "";
    private bool triggerHeld = false;

    private int screenshotLayer = 8; // Example layer for screenshot (ensure this layer is set in Unity)

    void Start()
    {
        context = NetworkScene.Register(this, networkId);
        roomClient = RoomClient.Find(this);
    }

    void Update()
    {
        if (actionBasedController.activateAction.action.ReadValue<float>() > 0.1f)
        {
            if (!triggerHeld)
            {
                triggerHeld = true;
                if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    GameObject selectedObject = hit.collider.gameObject;
                    currentSelection = selectedObject.name;
                    StartCoroutine(TakePicture(selectedObject));
                }
                
                //context.SendJson(new SelectionMessage { selection = currentSelection, peer = roomClient.Me.uuid, triggerHeld = triggerHeld });
            }
        }
        else
        {
            if (triggerHeld)
            {
                triggerHeld = false;
                
                if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
                {
                    GameObject selectedObject = hit.collider.gameObject;
                    lastSelection = selectedObject.name;
                }

                if (!string.IsNullOrEmpty(lastSelection))
                {
                    //context.SendJson(new SelectionMessage { selection = lastSelection, peer = roomClient.Me.uuid, triggerHeld = triggerHeld });
                }

                currentSelection = ""; // Reset the current selection as the trigger has been released
            }
        }
    }

    struct SelectionUnderstandingMessage
    {
        public string selection;
        public string peer;
        public bool triggerHeld;
        public byte[] image;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
    {
    }

    private IEnumerator TakePicture(GameObject targetObject)
    {
        int originalLayer = targetObject.layer;

        // Create a new Camera GameObject
        GameObject cameraObject = new GameObject("TempCamera");
        Camera camera = cameraObject.AddComponent<Camera>();

        // Set the camera to only render the screenshot layer
        camera.cullingMask = 1 << screenshotLayer;

        // Position the camera in front and above the target object
        Vector3 offset = new Vector3(0, 1, -3); // Adjust as needed
        cameraObject.transform.position = targetObject.transform.position + offset;
        cameraObject.transform.LookAt(targetObject.transform);

        // Change the target object's layer to the screenshot layer
        SetLayerRecursively(targetObject, screenshotLayer);

        // Wait for end of frame to ensure everything is rendered
        yield return new WaitForEndOfFrame();

        // Take a screenshot
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = renderTexture;
        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        camera.Render();
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        // Clean up
        camera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(cameraObject);

        // Restore the target object's original layer
        SetLayerRecursively(targetObject, originalLayer);

        // Save screenshot to file for debugging
        byte[] screenshotBytes = screenshot.EncodeToPNG();
        string filePath = Path.Combine(Application.persistentDataPath, "screenshot.png");
        File.WriteAllBytes(filePath, screenshotBytes);

        Debug.Log($"Screenshot taken and saved to {filePath}");
        
        context.SendJson(new SelectionUnderstandingMessage { selection = currentSelection, peer = roomClient.Me.uuid, triggerHeld = triggerHeld, image = screenshotBytes});
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null)
        {
            return;
        }

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}
