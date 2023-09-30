using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Unity.Netcode;

class GwogLoader{
    public static string version = "0.0.1";

    public void Start(GameObject gameObject){
        Debug.Log("GwogLoader v"+version+" loaded!");
        GwogLoaderGUI gui = gameObject.AddComponent<GwogLoaderGUI>();
    }
}

class GwogLoaderGUI: MonoBehaviour{
    public NetworkVariable<List<string>> serverEnabledMods = new NetworkVariable<List<string>>(new List<string>(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public List<string> enabledModList = new List<string>();

    public List<string> availableModList = new List<string>();

    public static string version = "0.0.1";

    public static string GwogLoaderDomain = "http://localhost:5500";

    public bool menuEnabled = false;

    public bool gwogLoaderFinishedInit = false;

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
        string modsString = enabledModList.Count == 1 ? "mod" : "mods";
        string modsString2 = availableModList.Count == 1 ? "mod" : "mods";
        string availableModsText = gwogLoaderFinishedInit ? "\n"+availableModList.Count+" "+modsString2+" available!" : "\nUpdating manifest...";

        GUI.Label(new Rect(10, 10, 300, 80), "GwogLoader v"+version+"\n"+enabledModList.Count+" enabled "+modsString+"!"+availableModsText);

        if(menuEnabled){
        }
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