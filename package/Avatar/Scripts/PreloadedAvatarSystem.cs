using System.Collections.Generic;
using Foundry.Networking;
using UnityEngine;
using Random = UnityEngine.Random;

public class PreloadedAvatarSystem : NetworkComponent
{
    [Header("Preloaded Avatar Models")] 
    public GameObject[] avatars;

    //The transform our avatars spawn under
    [Tooltip("Where your avatars get instanced too")] public Transform avatarHolder;

    //Networked
    private NetworkProperty<int> selectedAvatar = new(0);

    public override void RegisterProperties(List<INetworkProperty>  props)
    {
        props.Add(selectedAvatar);
        selectedAvatar.OnValueChanged +=InitializeAvatar;
    }
    
    public override void OnConnected()
    {
        if(PlayerPrefs.GetInt("lanMode") != 1) return;
        
        //Select A Random Avatar From The Saved List
        selectedAvatar.Value = Random.Range(0, avatars.Length - 1);
        
        Debug.Log("selected random avatar " + selectedAvatar);
    }
    
    void InitializeAvatar(int changed)
    {
        SpawnSelectedAvatar(changed);
    }

    void SpawnSelectedAvatar(int selected)
    {
        //Spawn That Avatar Under Our Holder
        GameObject avatarInstance = Instantiate(avatars[selected], avatarHolder);
        //Apply Needed Scripts To Configure Avatar
        avatarInstance.AddComponent<AvatarReskin>();
        
        //DEBUG
        Debug.Log("Spawned Selected Avatar " + selected);
    }
}
