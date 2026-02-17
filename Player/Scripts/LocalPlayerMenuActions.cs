using UnityEngine;
using UnityEngine.InputSystem;
using Foundry;
using TMPro;
using UnityEngine.UI;


namespace Foundry 
{
    public class LocalPlayerMenuActions : MonoBehaviour
    {
        // References
        public TMP_Text consoleText;
        public TMP_Dropdown locomotionDropdown;
        public Toggle flightToggle;
        public Slider volumeSlider;
        public TMP_InputField usernameField;
        
        private Foundry.Player player;
        private SimpleAvatarVisualController visualController;

        /// <summary>
        /// Called immediately after instantiating the menu
        /// </summary>
        
        public void Initialize(Player localPlayer)
        {
            player = localPlayer;

            // Safe to bind UI now
            if (flightToggle != null)
            {
                flightToggle.isOn =
                    player.movementMode == Foundry.Player.MovementMode.Flying;

                flightToggle.onValueChanged.AddListener(OnFlightToggleChanged);
            }

            // Find avatar visual controller
            visualController = localPlayer.GetComponentInChildren<SimpleAvatarVisualController>();

            if (!visualController)
                Debug.LogWarning("[Menu] No SimpleAvatarVisualController found on player");
        }

        void OnDestroy()
        {
            if (flightToggle != null)
                flightToggle.onValueChanged.RemoveListener(OnFlightToggleChanged);
        }

        void Start()
        {
            LoadUsername();
            LoadLocomotion();
            consoleText.text = "";
        }

        // Player Settings Module
        private void ApplyLocomotionSetting(int value)
        {
            if (player == null) return;

            switch (value)
            {
                case 0: // Teleport only
                    player.movementEnabled = false;
                    break;

                case 1: // Smooth locomotion
                    player.movementEnabled = true;
                    break;
            }
        }
        public void LoadLocomotion ()
        {
            int value = PlayerPrefs.GetInt("locomotion", 1);
            locomotionDropdown.value = value;
            ApplyLocomotionSetting(value);
            Debug.Log($"Loaded from pp, {value}");
        }
        public void SwitchLocomotion ()
        {
            int value = locomotionDropdown.value;
            ApplyLocomotionSetting(value); 


            // save the locomotion value
            PlayerPrefs.SetInt("locomotion", value);

            Debug.Log($"Saved to pp, {value}");
            
            consoleText.text = $"Locomotion switched to {locomotionDropdown.options[value].text}";
        }

        private void OnFlightToggleChanged(bool isOn)
        {
            if (player == null) return;
            player.SetFlyingMode(isOn);
        }
        public void AdjustVolume ()
        {
            AudioListener.volume = volumeSlider.value;
            consoleText.text = "Volume set to: " + Mathf.RoundToInt(volumeSlider.value * 100) + "%";
        }

        // User Settings Module
        public void LoadUsername ()
        {
            usernameField.text = PlayerPrefs.GetString("customDisplayName", PlayerPrefs.GetString("usernameLAN", "guest"));
        }

        public void UpdateUsername()
        {
            PlayerPrefs.SetString("customDisplayName", usernameField.text);
            PlayerPrefs.SetString("usernameLAN", usernameField.text);
            consoleText.text = "Username updated to: " + usernameField.text;
        }
        
        // Leave Module
        public void LeaveExperience ()
        {
            Application.Quit();
        }

        // Avatar Customization Module

        public void OnNextHairPressed()
        {
            visualController?.NextHair();
        }

        public void OnPreviousHairPressed()
        {
            visualController?.PreviousHair();
        }

        public void OnNextColorPressed()
        {
            visualController?.NextColor();
        }
        public void OnPreviousColorPressed()
        {
            visualController?.PreviousColor();
        }

    }
}
