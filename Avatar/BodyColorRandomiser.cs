using Foundry.Networking;
using System.Collections.Generic;
using UnityEngine;

namespace Foundry
{
    public class BodyColorRandomiser : NetworkComponent
    {
        public SkinnedMeshRenderer bodyColorRenderer;

        public NetworkProperty<Color> _bodyColor = new(Color.white);
         
        public Color bodyColor { 
            get => _bodyColor.Value;
            set => _bodyColor.Value = value;
        }

        public override void OnConnected()
        {
            if (IsOwner)
            {
                Color randomColor = Random.ColorHSV();
                randomColor.a = 1f;
                bodyColor = randomColor;
            }
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            _bodyColor.OnValueChanged += c => { this.bodyColorRenderer.sharedMaterial.SetColor("_BodyColor", c); };

            props.Add(_bodyColor);
        }
    }
}
