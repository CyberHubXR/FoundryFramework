using Fusion;

using UnityEngine;
using Random = UnityEngine.Random;

public class PreloadedAvatarSystem : NetworkBehaviour
{
    [Header("Preloaded Avatar Models")] 
    public GameObject[] avatars;

    //The transform our avatars spawn under
    [Tooltip("Where your avatars get instanced too")] public Transform avatarHolder;

    //Networked
    [Networked(OnChanged = nameof(InitializeAvatar))] private int selectedAvatar { get; set; }

    public override void Spawned()
    {
        if(PlayerPrefs.GetInt("lanMode") != 1) return;
        
        //Select A Random Avatar From The Saved List
        selectedAvatar = Random.Range(0, avatars.Length - 1);
        
        Debug.Log("selected random avatar " + selectedAvatar);
    }
    
    static void InitializeAvatar(Changed<PreloadedAvatarSystem> changed)
    {
        Debug.Log("changed " + changed.Behaviour);
        changed.Behaviour.SpawnSelectedAvatar(changed.Behaviour.selectedAvatar);
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
