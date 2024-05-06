using CyberHub.Brane;
using Foundry;
using Foundry.Services;
using UnityEngine;

namespace Foundry
{
    public class CoreAuthentication : MonoBehaviour
    {
        public void Authenticate(string scenename) 
        {
            BraneApp.GetService<ISceneNavigator>().GoToAsync(scenename);
            PlayerPrefs.SetString("usernameLAN", "user");
        }
    }
}
