using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Unity.Netcode;
using Unity.Collections;
using System;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json;
using HarmonyLib;
using Unity.VisualScripting;

class GwogLoader{
    public static string version = "0.0.1";

    public void Start(GameObject gameObject){
        if(gameObject.GetComponent<GwogLoaderGUI>() == null){
            Debug.Log("GwogLoader v"+version+" loaded!");
            gameObject.AddComponent<GwogLoaderGUI>();
        }
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

    public List<string> enabledModIDs = new List<string>();

    public List<Mod> availableModList = new List<Mod>();

    public static string version = "0.0.1";

    public static string GwogLoaderDomain = "https://gwogloader.dev";

    public bool menuEnabled = false;

    public bool gwogLoaderFinishedInit = false;

    public Rect windowPosition = new Rect(100, 100, 500, 750);

    public Vector2 scrollPosition = Vector2.zero;

    public GwogLoaderState gwogLoaderState = GwogLoaderState.FetchingManifest;

    public bool fatalError = false;

    public bool modsAreEnabledClient = false;

    List<ulong> moddedConnectionIDs = new List<ulong>();

    private bool customCallbacksRegistered = false;

    private ulong hostConnectionID;

    private Dictionary<string, Assembly> loadedAssemblies = new Dictionary<string, Assembly>();

    List<ulong> clientsWhoHaveLoadedAllMods = new List<ulong>();

    IEnumerator initGwogLoader(){
        UnityWebRequest www = UnityWebRequest.Get(GwogLoaderDomain + "/manifest.json");
		yield return www.Send();
        try{
            ModList modList = JsonConvert.DeserializeObject<ModList>(www.downloadHandler.text);
            availableModList.AddRange(modList.mods);
		    //availableModList = JsonUtility.FromJson<ModList>(www.downloadHandler.text).Mods;
            gwogLoaderFinishedInit = true;
        }
        catch(Exception e){
            Debug.Log(e);
            fatalError = true;
            gwogLoaderState = GwogLoaderState.FetchFailed;
        }
    }

    public void Start(){
        if(FindObjectsByType<GwogLoaderGUI>(FindObjectsSortMode.None).Count<GwogLoaderGUI>() > 1){
            Destroy(this.gameObject);
            return;
        }
        Harmony harmony = new Harmony("dev.gwogloader.gwogloader");
        harmony.PatchAll();
        StartCoroutine(initGwogLoader());
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
        else if(gwogLoaderState == GwogLoaderState.Ready){
            currentStateText = "Ready to join a lobby!";
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

        string modsEnabledString = enabledModIDs.Count == 1 ? " mod enabled!" : " mods enabled!";

        modsEnabledString = enabledModIDs.Count + modsEnabledString;

        string modsAvailableString = "";

        if(gwogLoaderState != GwogLoaderState.FetchingManifest && gwogLoaderState != GwogLoaderState.FetchFailed){
            modsAvailableString = availableModList.Count == 1 ? " mod available!" : " mods available!";
            modsAvailableString = availableModList.Count + modsAvailableString;
        }

        string statusText = currentStateText+"\n"+modsEnabledString+"\n"+modsAvailableString;

        GUIStyle style = new GUIStyle();

        style.normal.textColor = Color.white;

        GUI.Label(new Rect(5, 20, 295, 50), statusText, style);

        if(menuCanBeEnabled()){
            if(GUI.Button(new Rect(2, 70, 296, 20), "Toggle Menu [P]")){
                menuEnabled = !menuEnabled;
            }
        }
    }

    public bool menuCanBeEnabled(){
        return gwogLoaderState == GwogLoaderState.InModdableLobbyAsHost;
    }

    IEnumerator loadAllActiveMods(){
        gwogLoaderState = GwogLoaderState.LoadingMods;

        foreach(string modID in enabledModIDs){
            if(!loadedAssemblies.ContainsKey(modID)){
                UnityWebRequest www = UnityWebRequest.Get(GwogLoaderDomain + "/"+modID+".dll");
		        yield return www.Send();
                loadedAssemblies.Add(modID, Assembly.Load(www.downloadHandler.data));
            }

            Type type = loadedAssemblies[modID].GetType("EntryPoint");
            foreach (MemberInfo memberInfo in type.GetMembers())
		    {
		    }
            type.InvokeMember("Load", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, new object[]{
                gameObject
            });
        }


        if(!NetworkManager.Singleton.IsHost){
            FastBufferWriter buffer = new FastBufferWriter(0, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("sendToServerModsLoaded", hostConnectionID, buffer);
            buffer.Dispose();
        }
        else{
            clientsWhoHaveLoadedAllMods.Clear();
            clientsWhoHaveLoadedAllMods.Add(NetworkManager.Singleton.LocalClientId);
            FastBufferWriter buffer = new FastBufferWriter(0, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("sendToClientLoadModsNow", buffer);
            buffer.Dispose();
        }

        gwogLoaderState = GwogLoaderState.WaitingForOtherPlayersToLoadMods;
    }

    private void unloadAllActiveMods(){
        foreach(string modID in enabledModIDs){
            Type type = loadedAssemblies[modID].GetType("EntryPoint");
            foreach (MemberInfo memberInfo in type.GetMembers())
		    {
                //Debug.Log(memberInfo);
		    }
            type.InvokeMember("UnLoad", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, new object[]{
                gameObject
            });
        }

        enabledModIDs.Clear();
    }

    private void synchronizeEnabledMods(){
        if(NetworkManager.Singleton.IsHost){
            string json = JsonConvert.SerializeObject(enabledModIDs);
            byte[] bytes = System.Text.UTF8Encoding.UTF8.GetBytes(json);
            FastBufferWriter buffer = new FastBufferWriter(sizeof(int) + bytes.Length, Allocator.Temp);
            buffer.WriteValue<int>(bytes.Length);
            buffer.WriteBytes(bytes);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("sendToClientSetEnabledMods", buffer);
            buffer.Dispose();
        }
    }

    private void ensureCustomCallbacksRegistered(){
        if(!customCallbacksRegistered){
            customCallbacksRegistered = true;
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToClientCheckIfModded", sendToClientCheckIfModded);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToServerIsModded", sendToServerIsModded);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToClientSetModdedState", sendToClientSetModdedState);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToClientSetEnabledMods", sendToClientSetEnabledMods);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToClientLoadModsNow", sendToClientLoadModsNow);
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("sendToServerModsLoaded", sendToServerModsLoaded);
            NetworkManager.Singleton.OnClientConnectedCallback += onClientConnectedCallback;
        }
    }

    private void sendToServerModsLoaded(ulong senderClientId, FastBufferReader messagePayload)
    {
        if(NetworkManager.Singleton.IsHost){
            clientsWhoHaveLoadedAllMods.Add(senderClientId);
        }
    }

    private void sendToClientLoadModsNow(ulong senderClientId, FastBufferReader messagePayload)
    {
        if(!NetworkManager.Singleton.IsHost){
            if(senderClientId == hostConnectionID){
                StartCoroutine("loadAllActiveMods");
            }
        }
    }

    private void sendToClientSetEnabledMods(ulong senderClientId, FastBufferReader messagePayload)
    {
        if(!NetworkManager.Singleton.IsHost){
            messagePayload.ReadValue<int>(out int Length);

            byte[] bytes = new byte[Length];

            messagePayload.ReadBytes(ref bytes, Length);

            string json = System.Text.UTF8Encoding.UTF8.GetString(bytes, 0, Length);
            
            enabledModIDs = JsonConvert.DeserializeObject<List<string>>(json);
        }
    }

    private void updateModsStateForClients(){
        FastBufferWriter buffer = new FastBufferWriter(1, Allocator.Temp);

        buffer.WriteValue<bool>(shouldModsBeEnabledHost());

        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll("sendToClientSetModdedState", buffer);

        if(shouldModsBeEnabledHost()){
            synchronizeEnabledMods();
        }
    }

    private void onClientConnectedCallback(ulong obj)
    {
        Debug.Log("Client connected! ID of " + obj);
        if(NetworkManager.Singleton.IsHost){
            FastBufferWriter buffer = new FastBufferWriter(0, Allocator.Temp);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("sendToClientCheckIfModded", obj, buffer);
            buffer.Dispose();
            updateModsStateForClients();
        }
    }

    private void sendToClientSetModdedState(ulong senderClientId, FastBufferReader messagePayload)
    {
        if(!NetworkManager.Singleton.IsHost){
            messagePayload.ReadValue<bool>(out bool modsEnabled);
            Debug.Log("Modded state update recieved: "+modsEnabled);

            modsAreEnabledClient = modsEnabled;
        }
    }

    private void sendToServerIsModded(ulong senderClientId, FastBufferReader messagePayload)
    {
        Debug.Log("ID "+senderClientId+" just told us they were modded!");
        if(!moddedConnectionIDs.Contains(senderClientId)){
            moddedConnectionIDs.Add(senderClientId);
        }
        updateModsStateForClients();
    }

    private void sendToClientCheckIfModded(ulong senderClientId, FastBufferReader messagePayload)
    {
        Debug.Log("modCheck from server ID "+senderClientId);
        hostConnectionID = senderClientId;
        FastBufferWriter buffer = new FastBufferWriter(0, Allocator.Temp);
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("sendToServerIsModded", hostConnectionID, buffer);
        buffer.Dispose();
    }

    private void toggleModState(string modID){
        if(NetworkManager.Singleton.IsHost){
            if(!enabledModIDs.Contains(modID)){
                enabledModIDs.Add(modID);
            }
            else{
                enabledModIDs.Remove(modID);
            }

            synchronizeEnabledMods();
        }
    }

    public void windowFunc(int windowID){
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, 500, 600), scrollPosition, new Rect(0, 0, 500, 600));

        int i = 1;

        foreach(Mod mod in availableModList){
            if(GUI.Button(new Rect(0, i * 20, 500, 20), mod.name)){
                toggleModState(mod.id);
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

    private bool shouldModsBeEnabledHost(){
        return NetworkManager.Singleton.ConnectedClientsIds.All(moddedConnectionIDs.Contains);
    }

    public void Update(){
        if(menuCanBeEnabled()){
            if(Input.GetKeyDown(KeyCode.P)){
                menuEnabled = !menuEnabled;
            }
        }
        else{
            menuEnabled = false;
        }
        
        GameManager gameManager = FindAnyObjectByType<GameManager>();

        if(!fatalError){
            if(gameManager == null){
                moddedConnectionIDs.Clear();
                modsAreEnabledClient = false;
                customCallbacksRegistered = false;

                if(gwogLoaderFinishedInit){
                    gwogLoaderState = GwogLoaderState.Ready;
                }
                else{
                    gwogLoaderState = GwogLoaderState.FetchingManifest;
                }

                if(enabledModIDs.Count > 0){
                    unloadAllActiveMods();
                }
            }
            else{
                ensureCustomCallbacksRegistered();

                if(!moddedConnectionIDs.Contains(NetworkManager.Singleton.LocalClientId)){
                    moddedConnectionIDs.Add(NetworkManager.Singleton.LocalClientId);
                }
                
                if(gameManager.gameState == GameManager.GameState.pre){
                    if(FindObjectOfType<DummyLoadMods>() != null && gwogLoaderState != GwogLoaderState.LoadingMods && gwogLoaderState != GwogLoaderState.WaitingForOtherPlayersToLoadMods){
                        StartCoroutine("loadAllActiveMods");
                    }

                    if(gwogLoaderState == GwogLoaderState.LoadingMods || gwogLoaderState == GwogLoaderState.WaitingForOtherPlayersToLoadMods){
                        if(NetworkManager.Singleton.IsHost){
                            if(moddedConnectionIDs.All(clientsWhoHaveLoadedAllMods.Contains)){
                                FindObjectOfType<LobbyManager>().AttemptToStartGame();
                            }
                        }
                    }
                    else{
                        if(NetworkManager.Singleton.IsHost){
                            if(shouldModsBeEnabledHost()){
                                gwogLoaderState = GwogLoaderState.InModdableLobbyAsHost;
                            }
                            else{
                                gwogLoaderState = GwogLoaderState.InUnmoddableLobbyAsHost;

                                enabledModIDs.Clear();
                            }
                        }
                        else{
                            if(modsAreEnabledClient){
                                gwogLoaderState = GwogLoaderState.InModdableLobbyAsClient;
                            }
                            else{
                                gwogLoaderState = GwogLoaderState.InUnmoddableLobbyAsClient;

                                enabledModIDs.Clear();
                            }
                        }
                    }
                }
                else if(gameManager.gameState == GameManager.GameState.playing){
                    if(enabledModIDs.Count <= 0){
                        gwogLoaderState = GwogLoaderState.InModsDisabledGame;
                    }
                    else{
                        gwogLoaderState = GwogLoaderState.InModsEnabledGame;
                    }
                }
                
            }
        }
    }
}

[System.Serializable]
public struct ModList{
    public List<Mod> mods;
}

[System.Serializable]
public struct Mod{
    public Mod(string name, string description, string id){
        this.name = name;
        this.description = description;
        this.id = id;
    }

    public string name;
    public string description;
    public string id;
}

[HarmonyPatch(typeof(LobbyManager))]
[HarmonyPatch("AttemptToStartGame")]
class StartGameLoadModsPatch
{
    static bool Prefix(LobbyManager __instance)
    {
        bool actuallyStarting = !(bool)typeof(LobbyManager).InvokeMember("CheckForTeamSizeIssues", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod, null, __instance, new object[]{});
        if(actuallyStarting){
                if(__instance.GetComponent<DummyLoadMods>() == null){
                __instance.AddComponent<DummyLoadMods>();
                return false;
            }
            else{
                __instance.GetComponent<DummyLoadMods>().nuke();
                return true;
            }
        }
        else{
            return true;
        }
    }
}

class DummyLoadMods: MonoBehaviour{
    public void nuke(){
        Destroy(this);
    }
}