using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;
using MiniJSON;
using UnityEditor.PackageManager;

namespace Foundry.Core.Editor
{
    public static class PackageManagerUtil
    {
        private static IDictionary<string, object> _manifest = null;
        private static bool _unsavedChanges = false;

        // Every time the project reloads this will be called
        [InitializeOnLoadMethod]
        public static void ReadManifest()
        {
            if(_unsavedChanges)
                Debug.LogWarning("Unsaved changes to the package manifest were discarded, make sure you are calling PackageManagerUtils.Apply().");
            string jsonText = File.ReadAllText("Packages/manifest.json");
            _manifest = (IDictionary<string, object>)Json.Deserialize(jsonText);
            _unsavedChanges = false;
        }

        public static void Apply()
        {
            if (!_unsavedChanges)
                return;
            File.WriteAllText("Packages/manifest.json", Json.Serialize(_manifest));
            Client.Resolve();
            _unsavedChanges = false;
        }

        public static bool IsPackageInstalled(string name)
        {
            if(_manifest == null)
                ReadManifest();
            var dependencies = _manifest["dependencies"] as IDictionary<string, object>;
            if (dependencies == null)
                return false;

            return dependencies.ContainsKey(name);
        }

        public static string GetPackageSource(string name)
        {
            var deps = _manifest["dependencies"] as IDictionary<string, object>;
            if(!deps.ContainsKey(name))
                return "";
            return deps[name] as string;
        }
        
        public static bool ArePackagesInstalled(List<string> names)
        {
            if(_manifest == null)
                ReadManifest();
            var dependencies = _manifest["dependencies"] as IDictionary<string, object>;
            if (dependencies == null)
                return false;
            foreach (var name in names)
            {
               if (!dependencies.ContainsKey(name))
                    return false;
            }
            return true;
        }

        public class ScopedRegestry
        {
            public string name;
            public string url;
            public List<string> scopes;
        }

        public static void AddScopedRegistry(ScopedRegestry regestry)
        {
            if(_manifest == null)
                ReadManifest();
            if (!_manifest.ContainsKey("scopedRegistries"))
                _manifest["scopedRegistries"] = new List<object>();

            Dictionary<string, object> r = new();
            r["name"] = regestry.name;
            r["url"] = regestry.url;
            List<object> scopes = new();
            foreach (var s in regestry.scopes)
                scopes.Add(s);
            r["scopes"] = scopes;

            IList<object> registries = (IList<object>)_manifest["scopedRegistries"];
            registries.Add(r);
            _unsavedChanges = true;
        }
        
        /// <summary>
        /// Adds or updates a package in manifest.json
        /// </summary>
        /// <param name="packageName">Name of package: "com.example.packageName"</param>
        /// <param name="packageVersion">Either a version number or git repo: "0.0.1" or "https://github.com/etc..."</param>
        public static void AddPackage(string packageName, string packageVersion)
        {
            if(_manifest == null)
                ReadManifest();
           
            if (!_manifest.ContainsKey("dependencies"))
                _manifest["dependencies"] = new Dictionary<string, object>();
            
            IDictionary<string, object> dependencies = _manifest["dependencies"] as IDictionary<string, object>;
            dependencies[packageName] = packageVersion;
            _unsavedChanges = true;
        }
        
        public static bool IsAssemblyDefined(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name == name)
                    return true;
            }

            return false;
        }
    }
}
