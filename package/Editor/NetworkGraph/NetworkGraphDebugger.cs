using System.Collections.Generic;
using System.Linq;
using Foundry.Core.Editor.UIUtils;
using Foundry.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Foundry.Core.Editor
{
    public class NetworkGraphDebugger : EditorWindow
    {
        [MenuItem("Foundry/Debugging/Network Graph Debugger")]
        static void OpenDebugWindow()
        {
            var window = GetWindow<NetworkGraphDebugger>("Network Graph Debugger");
            window.position = new Rect(100, 100, 0, 0);
            window.minSize = new Vector2(400, 200);
            window.Show();
        }

        private void OnInspectorUpdate()
        {
            if (!Application.isPlaying)
                return;
            var networkProvider = FoundryApp.GetService<INetworkProvider>();
            if(networkProvider != null && scrollView != null)
                ConstructGraph(scrollView, networkProvider.Graph);
            Repaint();
        }

        void ConstructGraph(VisualElement root, NetworkGraph graph)
        {
            if (graph == null)
                return;
            
            ++cacheVersion;
            foreach (var node in graph.RootNodes)
                ConstructGraphNode(root, node);
            
            var toRemove = nodeElementCache.Where(node=>node.Value.version != cacheVersion).ToList();
            foreach (var node in toRemove)
            {
                Debug.Log("removing node " + node.Key + " from cache");
                nodeElementCache.Remove(node.Key);
                node.Value.node.parent?.Remove(node.Value.node);
            }
        }

        class NodeContext
        {
            public Foldout node = new();
            public bool objectLinked;
            public NetworkId cachedId;
            public Button selectButton;
            public bool propertiesDrawn;
            public VisualElement propPlaceholder;
            public ulong version;
        }
        
        private ulong cacheVersion = 0;
        
        private Dictionary<NetworkId, NodeContext> nodeElementCache = new();
        void ConstructGraphNode(VisualElement root, NetworkGraphNode node)
        {
            NodeContext ctx;

            if (nodeElementCache.TryGetValue(node.ID, out var value))
                ctx = value;
            else
            {
                ctx = new();
                ctx.node.text = node.ID.ToString();
                
                ctx.selectButton = new Button();
                ctx.selectButton.text = "Select";
                ctx.selectButton.style.width = 80f;
                ctx.selectButton.SetEnabled(false);
                ctx.node.Add(ctx.selectButton);
                
                nodeElementCache.Add(node.ID, ctx);
                root.Add(ctx.node);
            }
            
            ctx.version = cacheVersion;
            
            EditorUIUtils.SetMargin(ctx.node, 7f);
            EditorUIUtils.SetBorderRadius(ctx.node, 5f);
            EditorUIUtils.SetBorderWidth(ctx.node, 1f);
            EditorUIUtils.SetBorderColor(ctx.node, Color.black);

            if (ctx.cachedId != node.ID)
            {
                ctx.node.text = (node.AssociatedObject?.gameObject.name ?? "") + node.ID;
                ctx.cachedId = node.ID;
            }

            if (!ctx.objectLinked && node.AssociatedObject != null)
            {
                ctx.objectLinked = true;
                ctx.node.text = node.AssociatedObject.gameObject.name + node.ID;
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
            

            foreach (var child in node.Children)
                ConstructGraphNode(ctx.node, child);
        }

        private ScrollView scrollView;

        void CreateGUI()
        {
            rootVisualElement.Clear();
            var title = new Label("Foundry Network Graph Debugger");
            title.style.fontSize = 20;
            rootVisualElement.Add(title);
            if (!Application.isPlaying)
            {
                rootVisualElement.Add(new Label("No graph available. Start the game to see the graph."));
                nodeElementCache.Clear();
                return;
            }
            
            var networkProvider = FoundryApp.GetService<INetworkProvider>();
            
            if(networkProvider == null)
            {
                rootVisualElement.Add(new Label("No network provider found. Waiting for it to start..."));
                nodeElementCache.Clear();
                return;
            }
            
            networkProvider.SessionConnected += () =>
            {
                rootVisualElement.Clear();
                CreateGUI();
            };
            
            networkProvider.SessionDisconnected += (s) =>
            {
                rootVisualElement.Clear();
                CreateGUI();
            };
            
            if (!networkProvider.IsSessionConnected)
            {
                rootVisualElement.Add(new Label("No active network session. Waiting one to start..."));
                nodeElementCache.Clear();
                return;
            }

            networkProvider.Graph.OnGraphChanged += graph =>
            {
                nodeElementCache.Clear();
                scrollView ??= new ScrollView();
                ConstructGraph(scrollView, graph);
            };

            
            scrollView ??= new ScrollView();
            
            ConstructGraph(scrollView, networkProvider.Graph);
            rootVisualElement.Add(scrollView);
        }
    }
}
