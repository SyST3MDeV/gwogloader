using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Reflection;
using JetBrains.Annotations;
using Unity.Services.Lobbies.Scheduler;
using System.Linq;
using Unity.VisualScripting;

class GwogLoader{
    public static string version = "0.0.1";

    public void Start(GameObject gameObject){
        Debug.Log("GwogLoader v"+version+" loaded!");
        gameObject.AddComponent<GwogLoaderGUI>();
    }
}

class GwogLoaderModSync : NetworkBehaviour{
    public Assembly assembly;

    public NetworkList<FixedString64Bytes> serverEnabledMods;

    void Awake()
    {
        serverEnabledMods = new NetworkList<FixedString64Bytes>(new FixedString64Bytes[]{}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    }

    public override void OnNetworkSpawn()
   {
        base.OnNetworkSpawn();
       if (IsHost)
       {
            Debug.Log("We netspawned as host!");
            //serverEnabledMods = new NetworkList<FixedString64Bytes>(new FixedString64Bytes[]{}, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
       }
       else{
        Debug.Log("We netspawned as client!");
       }

        serverEnabledMods.Initialize(this);
        serverEnabledMods.OnListChanged += OnModsChanged;
   }

   public int getNumberModsEnabledServer(){
    return serverEnabledMods.Count;
   }

   private void OnModsChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        Debug.Log("Mods changed!");
    }

    [ClientRpc]
    public void testthingy(){
        Debug.Log("GWOG SAYS HI");
    }
}

class GwogLoaderGUI: MonoBehaviour{
    public GwogLoaderModSync gwogLoaderModSync;

    public bool connectedToLobby = false;

    public List<string> availableModList = new List<string>();

    public static string version = "0.0.1";

    public static string GwogLoaderDomain = "https://gwogloader.dev";

    public bool menuEnabled = false;

    public bool gwogLoaderFinishedInit = false;

    public Rect windowPosition = new Rect(100, 100, 500, 750);

    public Vector2 scrollPosition = Vector2.zero;

    public GameObject consoleGameobject;

    private GameObject objectToAdd;

    IEnumerator initGwogLoader(){
        UnityWebRequest www = UnityWebRequest.Get(GwogLoaderDomain + "/manifest.json");
		yield return www.Send();
		availableModList = JsonUtility.FromJson<ModList>(www.downloadHandler.text).Mods;
        gwogLoaderFinishedInit = true;
    }

    public void Start(){
        NetworkManager.Singleton.NetworkConfig.ForceSamePrefabs = false;
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("GwogLoaderModSyncModsUpdated", modsUpdatedCallback);
        objectToAdd = new GameObject();
        
        DontDestroyOnLoad(objectToAdd);
        objectToAdd.AddComponent<GwogLoaderModSync>();
        objectToAdd.AddComponent<NetworkObject>();
        NetworkManager.Singleton.AddNetworkPrefab(objectToAdd);
        Debug.Log(objectToAdd);
        StartCoroutine(initGwogLoader());
    }

    private void modsUpdatedCallback(ulong senderClientId, FastBufferReader messagePayload)
    {
        Debug.Log("Mods updated!");
    }

    public void enableMod(string mod){
        gwogLoaderModSync.serverEnabledMods.Add(mod);
        gwogLoaderModSync.testthingy();
    }

    public void disableMod(string mod){
        gwogLoaderModSync.serverEnabledMods.Remove(mod);
    }

    public bool modIsEnabled(string mod){
        if(gwogLoaderModSync == null){
            return false;
        }

        return gwogLoaderModSync.serverEnabledMods.Contains(mod);
    }

    public void OnGUI(){
        string modsString2 = availableModList.Count == 1 ? "mod" : "mods";
        string availableModsText = gwogLoaderFinishedInit ? "\n"+availableModList.Count+" "+modsString2+" available!" : "\nUpdating manifest...";

        if(gwogLoaderModSync == null){
            GUI.Label(new Rect(10, 10, 300, 50), "GwogLoader v"+version+"\n"+availableModsText);
        }
        else{
            string modsString = gwogLoaderModSync.getNumberModsEnabledServer() == 1 ? "mod" : "mods";

            GUI.Label(new Rect(10, 10, 300, 80), "GwogLoader v"+version+"\n"+gwogLoaderModSync.getNumberModsEnabledServer()+" enabled "+modsString+"!"+availableModsText);
        }

        if(menuEnabled){
            windowPosition = GUI.Window(0, windowPosition, windowFunc, "GwogLoader Menu");
        }
    }

    public void windowFunc(int windowID){
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, 500, 600), scrollPosition, new Rect(0, 0, 500, 600));

        int i = 1;

        foreach(string mod in availableModList){
            if(GUI.Button(new Rect(0, i * 20, 500, 20), mod)){
                if(!modIsEnabled(mod)){
                    enableMod(mod);
                }
                else{
                    disableMod(mod);
                }
            }
            i++;
        }

        //GUI.Button(new Rect(0, 0, 100, 20), "Top-left");
        //GUI.Button(new Rect(120, 0, 100, 20), "Top-right");
        //GUI.Button(new Rect(0, 180, 100, 20), "Bottom-left");
        //GUI.Button(new Rect(120, 180, 100, 20), "Bottom-right");

        GUI.EndScrollView();

        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }

    public void Update(){
        if(Input.GetKeyDown(KeyCode.P)){
            menuEnabled = !menuEnabled;
        }

        GameManager gameManager = FindAnyObjectByType<GameManager>();

        if(gameManager != null){
            //Debug.Log(FindObjectsByType<GwogLoaderModSync>(FindObjectsSortMode.None).Length);
            if(gwogLoaderModSync == null){
                if(gameManager.IsHost){
                    GameObject instantiatedGameObject = Instantiate(objectToAdd, Vector3.zero, Quaternion.identity);
                    instantiatedGameObject.GetComponent<NetworkObject>().Spawn();
                    gwogLoaderModSync = instantiatedGameObject.GetComponent<GwogLoaderModSync>();
                    Debug.Log(gwogLoaderModSync);
                    Debug.Log("Instantiated a GwogLoaderModSync on the server side!");
                }
                else{
                    if(FindObjectsByType<GwogLoaderModSync>(FindObjectsSortMode.None).Length > 1){
                        gwogLoaderModSync = FindObjectsByType<GwogLoaderModSync>(FindObjectsSortMode.None)[0];
                        Debug.Log("Got a GwogLoaderModSync on the client side!");
                    }
                }
            }
        }
        else{
            if(gwogLoaderModSync != null){
                Destroy(gameObject.GetComponent<GwogLoaderModSync>());
                Destroy(gameObject.GetComponent<NetworkObject>());
                gwogLoaderModSync = null;
            }
        }
    }
}

[System.Serializable]
public struct ModList{
    public List<string> Mods;
}