using System.Collections.Generic;
using System.Linq;
using Foundry.Core.Editor.UIUtils;
using Foundry.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foundry.Core.Editor
{
    public class NetworkStateDebugger : EditorWindow
    {
        [MenuItem("Foundry/Debugging/Network State Debugger")]
        static void OpenDebugWindow()
        {
            var window = GetWindow<NetworkStateDebugger>("Network State Debugger");
            window.Show();
            window.position = new Rect(100, 100, 0, 0);
            window.minSize = new Vector2(400, 200);
        }

        private void OnInspectorUpdate()
        {
            scrollView ??= new ScrollView();
            ConstructState(scrollView, NetworkManager.State);
            Repaint();
        }

        void ConstructState(VisualElement root, NetworkState state)
        {
            if (state == null)
                return;
            
            ++cacheVersion;
            foreach (var node in state.Objects)
                ConstructStateNode(root, node);
            
            var toRemove = nodeElementCache.Where(node=>!node.Key ||  node.Value.version != cacheVersion).ToList();
            foreach (var node in toRemove)
            {
                nodeElementCache.Remove(node.Key);
                if(node.Value.node.parent == root)
                    root.Remove(node.Value.node);
            }
        }

        class NodeContext
        {
            public Foldout node = new();
            public bool objectLinked;
            public NetworkId cachedId;
            public int cachedOwner = -1;
            public Button selectButton;
            public bool propertiesDrawn;
            public VisualElement propPlaceholder;
            public ulong version;
        }
        
        private ulong cacheVersion = 0;
        
        private Dictionary<NetworkObjectState, NodeContext> nodeElementCache = new();
        void ConstructStateNode(VisualElement root, NetworkObjectState node)
        {
            NodeContext ctx;

            if (nodeElementCache.TryGetValue(node, out var value))
            {
                ctx = value;
            }
            else
            {
                ctx = new();
                ctx.node.text = node.Id.ToString();
                
                ctx.selectButton = new Button();
                ctx.selectButton.text = "Select";
                ctx.selectButton.style.width = 80f;
                ctx.selectButton.SetEnabled(false);
                ctx.node.Add(ctx.selectButton);
                
                nodeElementCache.Add(node, ctx);
                root.Add(ctx.node);
            }
            
            ctx.version = cacheVersion;
            
            EditorUIUtils.SetMargin(ctx.node, 7f);
            EditorUIUtils.SetBorderRadius(ctx.node, 5f);
            EditorUIUtils.SetBorderWidth(ctx.node, 1f);
            EditorUIUtils.SetBorderColor(ctx.node, Color.black);

            if (ctx.cachedId != node.Id || ctx.cachedOwner != node.owner)
            {
                ctx.node.text = $"{(node.AssociatedObject ? node.AssociatedObject.gameObject.name : "")}, id: {node.Id}, owner {node.owner}";
                ctx.cachedId = node.Id;
                ctx.cachedOwner = node.owner;
            }

            if (!ctx.objectLinked && node.AssociatedObject != null)
            {
                ctx.objectLinked = true;
                ctx.node.text =  $"{(node.AssociatedObject ? node.AssociatedObject.gameObject.name : "")}, id: {node.Id}, owner {node.owner}";
                ctx.selectButton.SetEnabled(true);
                ctx.selectButton.clicked += () =>
                {
                    Selection.activeGameObject = node.AssociatedObject.gameObject;
                };
            }
            
            if (!ctx.propertiesDrawn && node.Properties != null)
            {
                ctx.propertiesDrawn = true;
                if(ctx.propPlaceholder != null)
                    ctx.node.Remove(ctx.propPlaceholder);
                foreach (var p in node.Properties)
                {
                    var propData = new Label(p.ToString());
                    p.OnChanged += () =>
                    {
                        if(propData != null)
                            propData.text = p.ToString();
                    };
                    ctx.node.Add(propData);
                }
            }
            else if (!ctx.propertiesDrawn && ctx.propPlaceholder == null)
            {
                ctx.propPlaceholder = new Label("Properties not registered.");
                ctx.node.Add(ctx.propPlaceholder);
            }
        }

        private ScrollView scrollView;

        void CreateGUI()
        {
            rootVisualElement.Clear();
            var title = new Label("Foundry Network State Debugger");
            title.style.fontSize = 20;
            rootVisualElement.Add(title);
            if (!Application.isPlaying)
            {
                rootVisualElement.Add(new Label("No graph available. Start the game to see the graph."));
                return;
            }
            
            var networkProvider = FoundryApp.GetService<INetworkProvider>();
            
            if(networkProvider == null)
            {
                rootVisualElement.Add(new Label("No network provider found. Waiting for it to start..."));
                return;
            }
            
            networkProvider.SessionConnected += () =>
            {
                scrollView.Clear();
                CreateGUI();
                Repaint();
            };
            
            networkProvider.SessionDisconnected += (s) =>
            {
                scrollView?.Clear();
                CreateGUI();
                Repaint();
            };
            
            if (!networkProvider.IsSessionConnected || NetworkManager.State == null)
            {
                rootVisualElement.Add(new Label("No active network session. Waiting one to start..."));
                return;
            }

            NetworkManager.State.OnStateStructureChanged += graph =>
            {
                scrollView ??= new ScrollView();
                ConstructState(scrollView, graph);
                Repaint();
            };

            
            scrollView ??= new ScrollView();
            
            ConstructState(scrollView, NetworkManager.State);
            rootVisualElement.Add(scrollView);
        }
    }
}
