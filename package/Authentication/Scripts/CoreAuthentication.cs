using Foundry;
using Foundry.Services;
using UnityEngine;

namespace Foundry
{
    public class CoreAuthentication : MonoBehaviour
    {
        public void Authenticate(string scenename) 
        {
            FoundryApp.GetService<ISceneNavigator>().GoToAsync(scenename);
        }
    }
}
