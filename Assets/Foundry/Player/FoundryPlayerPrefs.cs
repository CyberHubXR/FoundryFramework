using System.Collections;
using System.Collections.Generic;
using Foundry;
using UnityEngine;

public class FoundryPlayerPrefs : MonoBehaviour
{
    private Player player;
    
    public void SavePrefs(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
    }

    void Start()
    {
        player = GetComponent<Player>();
        
        LoadLocomotion();
    }

    void LoadLocomotion ()
    {
        int locomotion = PlayerPrefs.GetInt("locomotion", 0);
        
        // if locomotion is 0, teleport is enabled and movement is disabled
        // if locomotion is 1, teleport is disabled and movement is enabled
        player.movementEnabled = locomotion == 1;
    }

    // do a save on application quit
    void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }
}
