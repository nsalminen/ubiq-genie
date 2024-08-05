using RoslynCSharp;
using RoslynCSharp.Example;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Trivial.Mono.Cecil.Cil;
using Ubiq.Samples;
using Ubiq.XR;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using Microsoft.CodeAnalysis;
using System.Linq;
using RoslynCSharp.Compiler;
using Org.BouncyCastle.Bcpg;

public class TestRoslyn : MonoBehaviour
{
    //private
    private string activeCSharpSource = null;
    private ScriptProxy activeCrawlerScript = null;
    private ScriptDomain domain = null;
    public ModelGenerationFromPrimitivesManager manager;
    // Start is called before the first frame update

    /*public HandController handController; //only for debug purpose
    public Canvas canvas;
    public GameObject connectionPanel;
    public GameObject text;*/

    public AssemblyReferenceAsset[] assemblyReferences;
    private string cSharpSource;
    private static readonly Regex csharpScriptRegex = new Regex(@"`csharp\n(.*?)\n`", RegexOptions.Multiline);

    bool codevis = false;
    // Methods
    /// <summary>
    /// Called by Unity.
    /// </summary>
    public void Awake()
    {

    }

    void Start()
    {
        cSharpSource = @"
            using UnityEngine;
            using System.Collections.Generic;

            public class RedCastle : MonoBehaviour
            {
                void Start()
                {
                    GameObject redCastle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    redCastle.name = ""RedCastle"";

                    string uniqueID = GenerateUniqueID();
                    ObjectsTable.AddGameObject(uniqueID, redCastle);
                }

                void Update()
                {
                    //Destroy(this);
                }

                string GenerateUniqueID()
                {
                    List<string> existingIDs = ObjectsTable.GetAllIDs();
                    string newID = ""RedCastle_"" + Random.Range(1000, 9999).ToString();

                    while (existingIDs.Contains(newID))
                    {
                        newID = ""RedCastle_"" + Random.Range(1000, 9999).ToString();
                    }

                    return newID;
                }
            }";
            

        // Create the domain
        domain = ScriptDomain.CreateDomain("myDom", true);
        //string objectsTableAssemblyPath = ObjectsTable.GetAssemblyPath(typeof(ObjectsTable));
        //Debug.Log("ObjectsTable assembly path: " + objectsTableAssemblyPath);
        //IMetadataReferenceProvider objectsTableAssemblyReference = new AssemblyReferenceFromFile(objectsTableAssemblyPath);
        //Debug.Log("Reference created " + objectsTableAssemblyReference.ToString());

        // Add assembly references
        foreach (AssemblyReferenceAsset reference in assemblyReferences)
        {
            domain.RoslynCompilerService.ReferenceAssemblies.Add(reference);
            Debug.Log("ref " + reference.AssemblyPath);
        }
            

        Debug.Log("N of references after GUI " + domain.RoslynCompilerService.ReferenceAssemblies.Count);
        
        //domain.RoslynCompilerService.ReferenceAssemblies.Add(objectsTableAssemblyReference);
        //Debug.Log("N of references after runtime add " + domain.RoslynCompilerService.ReferenceAssemblies.Count);

        domain.InitializeCompilerService();
        Debug.Log("Roslyn Loaded OK");
        //handController.TriggerPress.AddListener(showCodePanel); //for debug
        RunCode(this.gameObject);
        Debug.Log("Roslyn run OK");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            codevis = !codevis;
            //showCodePanel(codevis);
        }
    }

    /*public void showCodePanel(bool show) //for debug
    {
        if (show)
        {
            canvas.gameObject.SetActive(show);
            connectionPanel.SetActive(!show);
        } else
        {
            connectionPanel.SetActive(!show);
            canvas.gameObject.SetActive(show);
        }
    }*/

    public static string Extract(string text)
    {
        Match match = csharpScriptRegex.Match(text);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    public void SetCodeString(string code)
    {
        cSharpSource = code;
    }

    

    public void RunCode(GameObject gameObjectTarget)
    {
        // Get the C# code from the input field
        //cSharpSource = "using UnityEngine;\r\n\r\npublic class ColorObject : MonoBehaviour\r\n{\r\n    private void Start()\r\n    {\r\n        // Create a new material with the desired color\r\n        Material material = new Material(Shader.Find(\"Standard\"));\r\n        material.color = Color.red;\r\n\r\n        // Assign the new material to the object\'s Renderer component\r\n        Renderer renderer = GetComponent<Renderer>();\r\n        if (renderer != null)\r\n        {\r\n            renderer.material = material;\r\n        }\r\n    }\r\n}\n\n";
        
        // Dont recompile the same code
        if (activeCSharpSource != cSharpSource)
        {
            //try
            {
                // Compile code
                ScriptType type = domain.CompileAndLoadMainSource(cSharpSource, ScriptSecurityMode.UseSettings, assemblyReferences);
                
                // Check for null
                if (type == null)
                {
                    if (domain.RoslynCompilerService.LastCompileResult.Success == false)
                        throw new Exception("Maze crawler code contained errors. Please fix and try again");
                    else if (domain.SecurityResult.IsSecurityVerified == false)
                        throw new Exception("Maze crawler code failed code security verification");
                    else
                        throw new Exception("Maze crawler code does not define a class. You must include one class definition of any name that inherits from 'RoslynCSharp.Example.MazeCrawler'");
                }

                ScriptProxy p = type.CreateInstance(gameObjectTarget);
                if (p != null)
                {
                    Debug.Log("Created instance");
                } else
                {
                    Debug.Log("Failed to create instance");
                }


            }
            //catch (Exception e)
            //{
            //    // Show the code editor window
            //    codeEditorWindow.SetActive(true);
            //    throw e;
            //}
        }
        else
        {
        }

        //display the code
        //text.GetComponent<UnityEngine.UI.Text>().text = cSharpSource; //debug
        manager.TakeAndSendResults();

    }

    

    
}
