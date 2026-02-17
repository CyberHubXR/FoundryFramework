using CyberHub.Foundry;
using Foundry;
using Foundry.Networking;
using Foundry.Services;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PortalSystem : FoundryScript
{
    [System.Serializable]
    public enum PortalType
    {
        Teleport,
        SceneChange,
        GoBack,
        GoForward
    }
    
    [System.Serializable]
    public enum SceneAddressType
    {
        Name,
        Addressable
    }

    [Header("Portal Type")]
    public PortalType portalType;

    [Header("TeleportSystem")]
    public Transform teleportPoint;
    public PortalSystem linkedPortal;

    [Header("SceneChange")]
    public SceneAddressType sceneAddressType;
    public string sceneName;
    public AssetReference addressableScene;
    
    private async void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player) && player.GetComponent<NetworkObject>().IsOwner)
        {
            if (portalType == PortalType.Teleport)
            {
                player.TeleportLook(linkedPortal.teleportPoint.position, linkedPortal.teleportPoint.forward, linkedPortal.teleportPoint.up);
                Debug.Log("Teleporting player to " + linkedPortal.teleportPoint.position + " from " +
                          teleportPoint.position);
            }
            else
            {
                // Get the navigator service
                var navigator = FoundryApp.GetService<ISceneNavigator>();

                if (portalType == PortalType.SceneChange)
                {

                    // Go to new scene
                    switch (sceneAddressType)
                    {
                        case SceneAddressType.Name:
                            Debug.Log("Loading new scene " + sceneName);
                            await navigator.GoToAsync(sceneName);
                            break;
                        case SceneAddressType.Addressable:
                            Debug.Log("Loading new scene " + addressableScene);
                            await navigator.GoToAsync(addressableScene);
                            break;
                    }
                }
                else if (portalType == PortalType.GoBack)
                {
                    // If we can't go back, warn
                    if (!navigator.CanGoBack)
                    {
                        Debug.LogError("Cannot navigate back. Ignoring teleport request.");
                        return;
                    }

                    // Go back
                    Debug.Log("Going back");
                    await navigator.GoBackAsync();
                }
                else if (portalType == PortalType.GoForward)
                {
                    // If we can't go forward, warn
                    if (!navigator.CanGoForward)
                    {
                        Debug.LogError("Cannot navigate forward. Ignoring teleport request.");
                        return;
                    }

                    // Go back
                    Debug.Log("Going forward");
                    await navigator.GoForwardAsync();
                }
            }
        }
    }
}