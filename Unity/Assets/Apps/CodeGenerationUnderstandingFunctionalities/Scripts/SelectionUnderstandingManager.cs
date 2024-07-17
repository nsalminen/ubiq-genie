using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TinyJson;
using Unity.VisualScripting;

public class SelectionUnderstandingManager : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    private NetworkId networkId = new NetworkId(94);
    public XRRayInteractor rayInteractor;
    public ActionBasedController actionBasedController;
    private string lastSelection = "";
    private string currentSelection = "";
    private bool triggerHeld = false;
    
    private GameObject selected4Query;
    public GameObject menuPrefab;
    public GameObject funcPrefab;
    public CodeGenerationUnderstandingFunctionalitiesManager codegenManager;
    
    private int screenshotLayer = 8; // Example layer for screenshot (ensure this layer is set in Unity)
    
    public static Transform FindTransform(Transform parent, string name)
    {
        if (parent.name.Equals(name)) return parent;
        foreach (Transform child in parent)
        {
            Transform result = FindTransform(child, name);
            if (result != null) return result;
        }
        return null;
    }
    
    // Function to create and attach the menu
    public void CreateAndAttachMenu(string objectName, Dictionary<string, string> functionalities, GameObject selectedObject)
    {
        GameObject menuInstance = Instantiate(menuPrefab);
        menuInstance.transform.SetParent(selectedObject.transform, false);
        
        //where to add
        Transform func_root = FindTransform(menuInstance.transform, "Content");
        Transform name = FindTransform(menuInstance.transform, "ObjectName");
        name.GetComponent<UnityEngine.UI.Text>().text = objectName;
        
        // Populate the functionalities in the UI
        foreach (KeyValuePair<string, string> functionality in functionalities)
        {
            GameObject funcInstance = Instantiate(funcPrefab);
            funcInstance.transform.SetParent(func_root, false);
            UnityEngine.UI.Text buttonText = FindTransform(funcInstance.transform, "Text").gameObject.GetComponent<UnityEngine.UI.Text>();
            buttonText.text = functionality.Key;
            FunctionalityDescription item = funcInstance.GetComponent<FunctionalityDescription>();
            item.go = selectedObject;
            item.objectName = objectName;
            item.functionalityName = functionality.Key;
            item.functionalityDescription = functionality.Value;
            
            //here@@ add a structure
            
            // Add OnClick listener
            Button button = funcInstance.GetComponent<Button>();
            button.onClick.AddListener(() => OnFunctionalityButtonClicked(objectName, functionality.Key, functionality.Value, selectedObject));
        }
    }
    
    // Method to handle the button click event
    private void OnFunctionalityButtonClicked(string objectname, string functionalityKey, string functionalityDescription, GameObject selection)
    {
        Debug.Log($"A {objectname} !");
        Debug.Log($"B {functionalityKey} !");
        Debug.Log($"C {functionalityDescription} !");
        Debug.Log($"D {selection.name} !");
        // Implement additional logic here
        codegenManager.SendDescription(functionalityDescription, objectname, selection);
    }

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
    };
    
    struct SelectionMessage
    {
        public string type;
        public string peer;
        public string data;
    };
    
    public class Functionality
    {
        public string objectName { get; set; }
        public Dictionary<string, string> functionalities { get; set; }
    }
    
    static string RemoveTags(string input, string initialTag, string endingTag)
    {
        // Find the index of the initial tag
        int initialTagIndex = input.IndexOf(initialTag);
        if (initialTagIndex == -1)
        {
            throw new ArgumentException("Initial tag not found.");
        }

        // Find the index of the ending tag
        int endingTagIndex = input.LastIndexOf(endingTag);
        if (endingTagIndex == -1)
        {
            throw new ArgumentException("Ending tag not found.");
        }

        // Calculate the start index of the JSON content
        int jsonStartIndex = initialTagIndex + initialTag.Length;

        // Calculate the length of the JSON content
        int jsonLength = endingTagIndex - jsonStartIndex;

        // Extract the JSON content
        string json = input.Substring(jsonStartIndex, jsonLength).Trim();

        return json;
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
    {
        // Try to parse the data as a message, if it fails, then we have received the audio data
        SelectionMessage message;
        try
        {
            message = data.FromJson<SelectionMessage>();
            string type = message.type;
            string peer = message.peer;
            string data_in = message.data;
            
            string initialTag = "```json";
            string endingTag = "```";

            data_in = RemoveTags(data_in, initialTag, endingTag);
            var res = data_in.FromJson<Functionality>();

            // Extract the object name
            string objectName = res.objectName;

            // Extract the functionalities into a dictionary
            Dictionary<string, string> functionalities = res.functionalities;
            
            
            Debug.Log(objectName);
            foreach (var functionality in functionalities)
            {
                Debug.Log($"{functionality.Key}: {functionality.Value}");
            }
            
            CreateAndAttachMenu(objectName, functionalities, selected4Query);
            return;
        }
        catch (Exception e)
        {
            Debug.Log("Not handled");
        }
    }
    
    private IEnumerator TakePicture(GameObject targetObject)
    {
        int originalLayer = targetObject.layer;
        selected4Query = targetObject;

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

        // Create a RenderTexture
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        camera.targetTexture = renderTexture;

        // Wait for end of frame to ensure everything is rendered
        yield return new WaitForEndOfFrame();

        // Render the camera's view to the RenderTexture
        RenderTexture.active = renderTexture;
        camera.Render();

        // Create a Texture2D to hold the screenshot
        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        // Reset the RenderTexture and clean up
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

        // Send screenshot data to the context
        context.SendJson(new SelectionUnderstandingMessage
        {
            selection = currentSelection,
            peer = roomClient.Me.uuid,
            triggerHeld = triggerHeld,
            image = screenshotBytes
        });
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}
