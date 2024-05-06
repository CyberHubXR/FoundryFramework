using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;


public class UpdateFoundryPackage : MonoBehaviour
{
    [MenuItem("Foundry/Update Package")]
    public static void UpdatePackage()
    {
        Debug.Log("Updating Foundry Asset Package");
        
        var installedPackageVersion = Resources.Load<FoundryVersion>("Version/FoundryInstalledVersion");
        var packageJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.cyberhub.foundry.core/package.json");

        // In our local dev environment, the package.json file is not in the expected location
        if (!packageJsonFile)
            packageJsonFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Package/package.json");
            
        var packageJson = JsonConvert.DeserializeObject<JObject>(packageJsonFile.text);
        var packageVersion = packageJson["version"].ToString();

        installedPackageVersion.version = packageVersion;
        EditorUtility.SetDirty(installedPackageVersion);
        
        AssetDatabase.ExportPackage("Assets/Foundry", "Assets/Package/FoundryAssets.unitypackage", ExportPackageOptions.Recurse);
    }
}
