using System.Collections;
using System.Collections.Generic;
using Ubiq.Networking;
using UnityEngine;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using Ubiq.Logging.Utf8Json;
using Ubiq.Rooms;
using System;
using System.Text;
using Ubiq.Samples;
using Ubiq.Voip;
using Ubiq.Voip.Implementations;
//using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;

public class ModelGenerationFromPrimitivesManager : MonoBehaviour
{
    private class AssistantSpeechUnit
    {
        public float startTime;
        public int samples;
        public string speechTargetName;

        public float endTime { get { return startTime + samples/(float)AudioSettings.outputSampleRate; } }
    }
    
    public XRRayInteractor rayInteractor;
    private NetworkId networkId = new NetworkId(94);
    private NetworkContext context;
    private RoomClient roomClient;

    public InjectableAudioSource audioSource;
    public VirtualAssistantController assistantController;
    public AudioSourceVolume volume;
    private GameObject selected4Query;

    private string speechTargetName;

    private List<AssistantSpeechUnit> speechUnits = new List<AssistantSpeechUnit>();
    public TestRoslyn testRoslyn;

    public GameObject targetObject;
    public GameObject sceneController;

    private string lastSelection = "";
    private string currentSelection = "";


    private int screenshotLayer = 8; // Example layer for screenshot (ensure this layer is set in Unity)

    [Serializable]
    private struct Message
    {
        public string type;
        public string peer;
        public string data;
    }

    struct SelectionUnderstandingMessage
    {
        public string selection;
        public string peer;
        public byte[] image;
    };



    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this,networkId);
        roomClient = RoomClient.Find(this);
    }

    // Update is called once per frame
    void Update()
    {
        /*while(speechUnits.Count > 0)
        {
            if (Time.time > speechUnits[0].endTime)
            {
                speechUnits.RemoveAt(0);
            }
            else
            {
                break;
            }
        }

        if (assistantController)
        {
            var speechTarget = null as string;
            if (speechUnits.Count > 0)
            {
                speechTarget = speechUnits[0].speechTargetName;
            }

            assistantController.UpdateAssistantSpeechStatus(speechTarget,volume.volume);
        }

        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            targetObject = hit.collider.gameObject;
        }
        else
        {
            targetObject = null;
        }*/
    }

    /*public void SendDescription(string description, string objectname, GameObject objectSelection)
    {
        targetObject = objectSelection;
        context.SendJson(new CodeGenerationQuery
        {
            objectname = objectname,
            data = description,
        });
    }*/

    public void TakeAndSendResults()
    {
        StartCoroutine(TakePicture(targetObject));
    }

    

    public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
    {
        //the ration is , component is attached to the target object, the code is run, the component self destructs
        Message message = data.FromJson<Message>();
        Debug.Log("Res: " + message.data.ToString());
        testRoslyn.SetCodeString(message.data.ToString());
        testRoslyn.RunCode(targetObject); // the component created need to self destruct as first instruction of the Update, while at the start it
                                          // does its job.
        //maybe here a control to destroy the component is needed as safe guard
    }

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
