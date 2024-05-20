using UnityEngine;
using System.Collections;
using Foundry.Account;
using Foundry.Networking;
using System.Collections.Generic;

namespace Foundry.Account
{
    public class CorePlayerAccount : NetworkComponent
    {
        //network instances of variables
        private NetworkProperty<string> _username = new(string.Empty);
        private NetworkProperty<int> _avatarIndex = new(0);

        public bool spawnBaseAvatar = true;

        public string username
        {
            get => _username.Value;
            set => _username.Value = value;
        }

        public int avatarIndex
        {
            get => _avatarIndex.Value;
            set => _avatarIndex.Value = value;
        }

        [Tooltip("Avatar Instance Location In Hierarchy")] public Transform avatarHolder;

        [Header("Preloaded Avatar Models")]
        [SerializeField] GameObject[] avatars;

        AccountDisplay accountDisplay;

        private void Awake()
        {
            TryGetComponent(out accountDisplay);
        }

        public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
        {
            _username.OnValueChanged += n => { accountDisplay.SetText(n); };
            _avatarIndex.OnValueChanged += i => { SpawnSelectedAvatar(i); };
            
            props.Add(_username);
            props.Add(_avatarIndex);
        }

        /// <summary>
        /// Simple overrideable method to initialize account contains username and avatar
        /// </summary>
        public virtual void InitializeAccount() 
        {
            int avatarID = PlayerPrefs.GetInt("AvatarSelected");

            if (avatarID > avatars.Length - 1)
            {
                Debug.LogWarning($"Value is higher than max count of avatars switching to avatar 0");
                avatarID = 0;
            }

            username = PlayerPrefs.GetString("usernameLAN");
            avatarIndex = avatarID;
        }

        void SpawnSelectedAvatar(int selected)
        {
            if (spawnBaseAvatar) return;

            //Spawn That Avatar Under Our Holder
            GameObject avatarInstance = Instantiate(avatars[selected], avatarHolder);
            avatarInstance.transform.localPosition = Vector3.zero;
            //Apply Needed Scripts To Configure Avatar
            avatarInstance.AddComponent<AvatarReskin>();
        }

        public override void OnConnected()
        {
            if (IsOwner)
            {
                InitializeAccount();
            }
        }
    }
}
