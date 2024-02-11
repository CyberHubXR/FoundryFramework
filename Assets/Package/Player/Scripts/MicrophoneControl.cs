using UnityEngine;
using UnityEngine.UI;
using Foundry;
using Foundry.Networking;
using System.Collections.Generic;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Foundry
{
    public class MicrophoneControl : NetworkComponent
    {
        public AudioSource speakerSource;
        public Image micIcon;

        public Sprite mutedIcon;
        public Sprite unmutedIcon;

        
        public NetworkProperty<bool> muted = new(false);

        public UnityEvent mute = new();
        public UnityEvent unmute = new();

        void Start()
        {
            mute.AddListener(MuteMicrophone);
            unmute.AddListener(UnmuteMicrophone);
            muted.OnValueChanged += v =>
            {
                if(v)
                    mute.Invoke();
                else
                    unmute.Invoke();
            };
        }

        public override void OnConnected()
        {
            if (IsOwner)
                return;
            if (muted.Value)
                mute.Invoke();
            else
                unmute.Invoke();
        }

        public void ClickButton() 
        {
            if(!IsOwner) return;
            
            muted.Value = !muted.Value;
        }

        public void MuteMicrophone() 
        {
            micIcon.sprite = mutedIcon;
            speakerSource.volume = 0;
        }

        public void UnmuteMicrophone() 
        {
            micIcon.sprite = unmutedIcon;
            speakerSource.volume = 1;
        }

        public override void RegisterProperties(List<INetworkProperty> props)
        {
            props.Add(muted);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MicrophoneControl))]
    public class MicrophoneControlEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Unity is kind of silly, make sure you have the game window focused in order to use the UI", MessageType.Warning);

            MicrophoneControl target = (MicrophoneControl)base.target;
            if(target.speakerSource == null)
            {
                EditorGUILayout.HelpBox("Speaker Source is null!, this component wont work. Please set the audio source to the audiosource on the speaker object in the player prefab", MessageType.Error);
            }

            DrawDefaultInspector();
        }
    }
#endif
}
