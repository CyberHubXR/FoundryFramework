using CyberHub.Foundry;
using Foundry;
using Foundry.Services;
using UnityEngine;
public class LanAuthentication : MonoBehaviour
{
    [Header("Usernames")] 
    public string[] usernames;

    [Header("Scene")] 
    public int onlineIndex;

    public int maxAvatarCount = 4;

    public void Start() {
        //StartGame();
    }

    public async void StartGame()
    {
        // Select Our Username From The List
        string username = usernames[Random.Range(0, usernames.Length - 1)];
        // Save That Username To PlayerPrefs So We Can Access It
        PlayerPrefs.SetString("usernameLAN", username);
        // Select and save an avatar
        int avatarSelected = Random.Range(0, maxAvatarCount);
        PlayerPrefs.SetInt("AvatarSelected", avatarSelected);
        // Tell The Player Account System To Ignore Beamable
        PlayerPrefs.SetInt("lanMode", 1);
        
        // Get the navigator service
        var navigator = FoundryApp.GetService<ISceneNavigator>();
        // Go To The Online Scene
        await navigator.GoToAsync(onlineIndex);
    }
}
