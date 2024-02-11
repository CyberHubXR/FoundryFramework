# Creating a Custom Network Component

## Overview
Create a new script that inherits from NetworkComponent. 
Then override the RegisterProperties method and add your properties to the list.
You can also override the OnSpawned method to do any initialization that requires the network to be ready.

## Example

```cs
using System.Collections;
using System.Collections.Generic;
using Foundry.Networking;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ColorSync : NetworkComponent
{
    // Create an instance of NetworkedProperty<Color> with a default value of Color.white.
    private NetworkProperty<Color> _color = new(Color.white);

    public Color color
    {
        get => _color.Value;
        set => _color.Value = value;
    }

    private Renderer renderer;

    public void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    public void Update()
    {
        if (IsOwner)
        {
            color = new Color(Mathf.Sin(Time.time), Mathf.Cos(Time.time), 0.5f);
        }
    }

    /* RegisterProperties is called once when the component is added to the networked object on Awake, 
     * this is where we connect up all our properties.*/
    public override void RegisterProperties(List<INetworkProperty> props)
    {
        // This callback is called both when the value is set locally and when it is set remotely.
        _color.OnValueChanged += c=>
        {
            renderer.material.color = c;
        };
        props.Add(_color);
    }
    
    // OnConnected is called when all our networked properties have been set and we're connected to the network.
    public override void OnConnected()
    {
    
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(ColorSync))]
public class ColorSyncEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var colorSync = (ColorSync) target;
        var color = colorSync.color;
        color = EditorGUILayout.ColorField("Color", color);
        colorSync.color = color;
    }
}
#endif
```