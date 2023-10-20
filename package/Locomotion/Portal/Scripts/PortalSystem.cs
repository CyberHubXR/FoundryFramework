using Foundry;
using Foundry.Services;
using UnityEngine;
using Fusion;
using System.Threading.Tasks;

public class PortalSystem : MonoBehaviour
{
    [System.Serializable]
    public enum PortalType
    {
        Teleport,
        SceneChange,
        GoBack,
        GoForward
    }

    [Header("Portal Type")]
    public PortalType portalType;

    [Header("TeleportSystem")]
    public Transform teleportPoint;
    public PortalSystem linkedPortal;

    [Header("SceneChange")]
    public string sceneName;

    private NetworkRunner runner;
    

    private void Awake()
    {
        if (runner == null) runner = FindObjectOfType<NetworkRunner>();
    }
    
    private async void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Player player) && player.GetComponent<NetworkObject>().HasStateAuthority)
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
                    // Stop the net runner
                    await runner.Shutdown(true);

                    // Go to new scene
                    Debug.Log("Loading new scene " + sceneName);
                    await navigator.GoToAsync(sceneName);
                }
                else if (portalType == PortalType.GoBack)
                {
                    // If we can't go back, warn
                    if (!navigator.CanGoBack)
                    {
                        Debug.LogError("Cannot navigate back. Ignoring teleport request.");
                        return;
                    }

                    // Stop the net runner
                    await runner.Shutdown(true);

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

                    // Stop the net runner
                    await runner.Shutdown(true);

                    // Go back
                    Debug.Log("Going forward");
                    await navigator.GoForwardAsync();
                }
            }
        }
    }
}