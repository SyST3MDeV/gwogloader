using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Unity.Netcode;

class GwogLoader{
    public static string version = "0.0.1";

    public void Start(GameObject gameObject){
        Debug.Log("GwogLoader v"+version+" loaded!");
        gameObject.AddComponent<NetworkObject>();
        GwogLoaderGUI gui = gameObject.AddComponent<GwogLoaderGUI>();
    }
}

class GwogLoaderGUI: NetworkBehaviour{
    public NetworkVariable<List<string>> serverEnabledMods = new NetworkVariable<List<string>>(new List<string>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public List<string> availableModList = new List<string>();

    public static string version = "0.0.1";

    public static string GwogLoaderDomain = "http://localhost:5500";

    public bool menuEnabled = false;

    public bool gwogLoaderFinishedInit = false;

    public Rect windowPosition = new Rect(100, 100, 500, 750);

    public Vector2 scrollPosition = Vector2.zero;

    IEnumerator initGwogLoader(){
        UnityWebRequest www = UnityWebRequest.Get(GwogLoaderDomain + "/manifest.json");
		yield return www.Send();
		availableModList = JsonUtility.FromJson<ModList>(www.downloadHandler.text).Mods;
        gwogLoaderFinishedInit = true;
    }

    public void Start(){
        StartCoroutine(initGwogLoader());
    }

    public void OnGUI(){
        string modsString = serverEnabledMods.Value.Count == 1 ? "mod" : "mods";
        string modsString2 = availableModList.Count == 1 ? "mod" : "mods";
        string availableModsText = gwogLoaderFinishedInit ? "\n"+availableModList.Count+" "+modsString2+" available!" : "\nUpdating manifest...";

        GUI.Label(new Rect(10, 10, 300, 80), "GwogLoader v"+version+"\n"+serverEnabledMods.Value.Count+" enabled "+modsString+"!"+availableModsText);

        if(menuEnabled){
            windowPosition = GUI.Window(0, windowPosition, windowFunc, "GwogLoader Menu");
        }
    }

    public void windowFunc(int windowID){
        scrollPosition = GUI.BeginScrollView(new Rect(0, 0, 500, 600), scrollPosition, new Rect(0, 0, 500, 600));

        int i = 1;

        foreach(string mod in availableModList){
            if(GUI.Toggle(new Rect(0, i * 20, 500, 20), serverEnabledMods.Value.Contains(mod), mod)){
                if(!serverEnabledMods.Value.Contains(mod)){
                    serverEnabledMods.Value.Add(mod);
                }
                else{
                    serverEnabledMods.Value.Remove(mod);
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
    }
}

[System.Serializable]
public struct ModList{
    public List<string> Mods;
}