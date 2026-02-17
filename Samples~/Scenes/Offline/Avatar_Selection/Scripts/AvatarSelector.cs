using System;
using System.Collections;
using System.Collections.Generic;
using CyberHub.Foundry;
using CyberHub.Foundry.Database;
using Foundry.Services;
using UnityEngine;
using UnityEngine.UI;

public class AvatarSelector : MonoBehaviour
{
    [Tooltip("The buttons that will be used to select the avatar. Their position in this list will be the index of the avatar selected.")]
    public Button[] options;
    public Color selectedColor;
    public Button confirmButton;
    public string nextScene;

    private int selectedAvatar = -1;
    private Button previousButton;
    private Color previousColor;
    
    async void Start()
    {
        for (int i = 0; i < options.Length; i++)
        {
            options[i].interactable = false;
        }  
        
        var session = await DatabaseSession.GetActive();
        try
        {
            
            var previousSelection = await session.GetUserProperty<Int64>("avatar");
            if (previousSelection.IsSuccess)
                SelectAvatar((int)previousSelection.data);
            else
                Debug.LogWarning("No previous avatar selection found: " + previousSelection.error_message);
        }
        catch(Exception e)
        {
            Debug.LogWarning("No previous avatar selection found: " + e.Message);
        }
            
        
        if(selectedAvatar == -1)
            confirmButton.interactable = false;
        confirmButton.onClick.AddListener(Confirm);
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            options[i].interactable = true;
            options[i].onClick.AddListener(() => SelectAvatar(index));
        }    
    }
    
    void SelectAvatar(int index)
    {
        Debug.Log($"Selected avatar {index}");
        if (selectedAvatar != -1)
        {
            previousButton = options[selectedAvatar];
            previousButton.image.color = previousColor;
        }
        
        selectedAvatar = index;
        previousColor = options[selectedAvatar].image.color;
        options[selectedAvatar].image.color = selectedColor;
        PlayerPrefs.SetInt("AvatarSelected", selectedAvatar);
        
        confirmButton.interactable = true;
    }
    
    async void Confirm()
    {
        Debug.Log($"Confirmed avatar {selectedAvatar}");
        var session = await DatabaseSession.GetActive();
        await session.SetUserProperty("avatar", selectedAvatar);
        
        await FoundryApp.GetService<ISceneNavigator>().GoToAsync(nextScene);
    }
}
