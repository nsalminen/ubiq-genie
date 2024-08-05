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

public class SelectionModelGenerationManager : MonoBehaviour
{
    private NetworkContext context;
    private RoomClient roomClient;
    private NetworkId networkId = new NetworkId(99);
    public XRRayInteractor rayInteractor;
    public ActionBasedController actionBasedController;
    private string lastSelection = "";
    private string currentSelection = "";
    private bool triggerHeld = false;
    
    
    public GameObject menuPrefab;
    public GameObject funcPrefab;
    public ModelGenerationFromPrimitivesManager codegenManager;


    struct SelectionMessage
    {
        public string type;
        public string peer;
        public string data;
    };


    void Start()
    {
        context = NetworkScene.Register(this, networkId);
        roomClient = RoomClient.Find(this);
    }

    void Update()
    {
        
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

            Debug.Log("@@@@@Something arrived..." + type);
            Debug.Log("@@@@@ Object table n elements" + ObjectsTable.gameObjectDict.Count);

            //here the YES / NO that repeat or no the cycle
            foreach (KeyValuePair<string, GameObject> entry in ObjectsTable.gameObjectDict)
            {
                Debug.Log("Key: " + entry.Key + " Value: " + entry.Value);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Not handled");
        }
    }
    
    

    
}
