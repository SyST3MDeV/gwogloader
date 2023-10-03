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

enum GwogLoaderState{
    FetchingManifest,
    FetchFailed,
    Ready,
    InModdableLobbyAsHost,
    InModdableLobbyAsClient,
    InUnmoddableLobbyAsHost,
    InUnmoddableLobbyAsClient,
    LoadingMods,
    WaitingForOtherPlayersToLoadMods,
    InModsEnabledGame,
    InModsDisabledGame,
    UnknownError
}

class GwogLoaderGUI: MonoBehaviour{
    public bool connectedToLobby = false;

    public List<FixedString64Bytes> enabledModList = new List<FixedString64Bytes>();

    public List<string> availableModList = new List<string>();

    public static string version = "0.0.1";

    public static string GwogLoaderDomain = "https://gwogloader.dev";

    public bool menuEnabled = false;

    public bool gwogLoaderFinishedInit = false;

    public Rect windowPosition = new Rect(100, 100, 500, 750);

    public Vector2 scrollPosition = Vector2.zero;

    public GwogLoaderState gwogLoaderState = GwogLoaderState.FetchingManifest;

    public bool fatalError = false;

    public bool recievedHostModCheck = false;

    IEnumerator initGwogLoader(){
        UnityWebRequest www = UnityWebRequest.Get(GwogLoaderDomain + "/manifest.json");
		yield return www.Send();
		availableModList = JsonUtility.FromJson<ModList>(www.downloadHandler.text).Mods;
        gwogLoaderFinishedInit = true;
    }

    public void Start(){
        StartCoroutine(initGwogLoader());
    }

    private void modsUpdatedCallback(ulong senderClientId, FastBufferReader messagePayload)
    {
        Debug.Log("Mods updated!");
    }

    public void OnGUI(){
        GUI.Window(1, new Rect(10, 15, 300, 92), statusWindowFunc, "GwogLoader v"+version);

        if(menuEnabled){
            windowPosition = GUI.Window(0, windowPosition, windowFunc, "GwogLoader Menu");
        }
    }

    public void statusWindowFunc(int windowID){
        string currentStateText = "";

        if(gwogLoaderState == GwogLoaderState.FetchingManifest){
            currentStateText = "Fetching mod manifest...";
        }
        else if(gwogLoaderState == GwogLoaderState.FetchFailed){
            currentStateText = "FATAL ERROR: Unable to fetch mod manifest";
        }
        else if(gwogLoaderState == GwogLoaderState.InModdableLobbyAsClient){
            currentStateText = "In moddable lobby, host will enable mods!";
        }
        else if(gwogLoaderState == GwogLoaderState.InModdableLobbyAsHost){
            currentStateText = "In moddable lobby, enable mods using the menu!";
        }
        else if(gwogLoaderState == GwogLoaderState.InUnmoddableLobbyAsClient){
            currentStateText = "One of the clients/host is not modded, mods are disabled!";
        }
        else if(gwogLoaderState == GwogLoaderState.InUnmoddableLobbyAsHost){
            currentStateText = "One of the clients is not modded, mods are disabled!";
        }
        else if(gwogLoaderState == GwogLoaderState.LoadingMods){
            currentStateText = "Loading mods...";
        }
        else if(gwogLoaderState == GwogLoaderState.WaitingForOtherPlayersToLoadMods){
            currentStateText = "Waiting for all clients to load mods...";
        }
        else if(gwogLoaderState == GwogLoaderState.InModsEnabledGame){
            currentStateText = "In Game - Mods Enabled!";
        }
        else if(gwogLoaderState == GwogLoaderState.InModsDisabledGame){
            currentStateText = "In Game - Mods Disabled :(";
        }
        else{
            currentStateText = "FATAL ERROR: Unknown";
        }

        string statusText = currentStateText+"\nTesting\nTesting";

        GUIStyle style = new GUIStyle();

        style.normal.textColor = Color.white;

        GUI.Label(new Rect(0, 20, 300, 50), statusText, style);

        if(GUI.Button(new Rect(2, 70, 296, 20), "Toggle Menu [P]")){
            menuEnabled = !menuEnabled;
        }
    }

    public void windowFunc(int windowID){
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, 500, 600), scrollPosition, new Rect(0, 0, 500, 600));

        int i = 1;

        foreach(string mod in availableModList){
            if(GUI.Button(new Rect(0, i * 20, 500, 20), mod)){
                /*
                if(!modIsEnabled(mod)){
                    enableMod(mod);
                }
                else{
                    disableMod(mod);
                }
                */
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

        if(!fatalError){
            if(gameManager == null){
            if(gwogLoaderFinishedInit){
                
            }
        }
        }

        /*

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
        */
    }
}

[System.Serializable]
public struct ModList{
    public List<string> Mods;
}