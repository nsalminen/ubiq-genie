using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class ObjectsTable : MonoBehaviour
{
    // Start is called before the first frame update
    public static Dictionary<string, GameObject> gameObjectDict;
    void Start()
    {
        gameObjectDict = new Dictionary<string, GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Method to add a GameObject to the dictionary
    public static void AddGameObject(string id, GameObject obj)
    {
        if (!gameObjectDict.ContainsKey(id))
        {
            gameObjectDict.Add(id, obj);
        }
        else
        {
            Debug.LogWarning("ID already exists in the dictionary: " + id);
        }
    }

    public static GameObject GetGameObject(string id)
    {
        if (gameObjectDict.TryGetValue(id, out GameObject obj))
        {
            return obj;
        }
        else
        {
            Debug.LogWarning("ID not found in the dictionary: " + id);
            return null;
        }
    }

    // Method to get all present IDs in the dictionary as a list
    public static List<string> GetAllIDs()
    {
        List<string> ids = new List<string>(gameObjectDict.Keys);
        return ids;
    }

    public static string GetAssemblyPath(Type type)
    {
        return type.Assembly.Location;
    }
}
