using System;
using Foundry;
using TMPro;
using UnityEngine;

public class WristMenu : MonoBehaviour
{
    public GameObject menuContainer;

    public TMP_InputField usernameField;
    public TMP_Dropdown locomotionDropdown;

    [Header("Settings")]
    public Player locomotionOptions;

    bool menuActive = false;

    [Header("Console")]
    public TMP_Text consoleText;

    void Start()
    {
        usernameField.text = PlayerPrefs.GetString("usernameLAN", "guest");
        //usernameField.onEndEdit.AddListener(UpdateUsername);
    }

    public void UpdateUsername()
    {
        PlayerPrefs.SetString("customDisplayName", usernameField.text);
        PlayerPrefs.SetString("usernameLAN", usernameField.text);
        
        consoleText.text = "Username updated to: " + usernameField.text;
    }

    public void LoadUsername ()
    {
        usernameField.text = PlayerPrefs.GetString("customDisplayName", PlayerPrefs.GetString("usernameLAN", "guest"));
    }

    public void ToggleMenu()
    {
        menuActive = !menuActive;
        menuContainer.SetActive(menuActive);
    }

    public void LeaveExperience ()
    {
        Application.Quit();
    }

    public void LoadLocomotion ()
    {
        locomotionDropdown.value = PlayerPrefs.GetInt("locomotion", 1);
    }

    public void SwitchLocomotion ()
    {
        // switch statement for locomotion dropdown

        switch (locomotionDropdown.value)
        {
            case 0:
                locomotionOptions.movementEnabled = false;
                break;
            case 1:
                locomotionOptions.movementEnabled = true;
                break;
        }

        // save the locomotion value
        PlayerPrefs.SetInt("locomotion", locomotionDropdown.value);

        Debug.Log($"Saved to pp, {locomotionDropdown.value}");
        
        consoleText.text = $"Locomotion switched {locomotionDropdown.options[locomotionDropdown.value].text}";
    }
}
