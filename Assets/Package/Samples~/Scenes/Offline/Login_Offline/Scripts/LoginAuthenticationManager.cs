using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using CyberHub.Brane;
using CyberHub.Brane.Database;
using Foundry.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginAuthenticationManager : MonoBehaviour
{
    [Header("Login Fields")]
    public TMP_InputField usernameField;
    public TMP_InputField passwordField;
    
    [Header("Signup Fields")]
    public TMP_InputField signupUsernameField;
    public TMP_InputField signupEmailField;
    public TMP_InputField signupPasswordField;
    public TMP_InputField signupConfirmPasswordField;

    [Header("Common Elements")] 
    public TMP_Text messageText;

    [Header("Settings")] 
    public bool autoLogin = true;
    public string targetScene;
    
    public async void Start()
    {
        if (!autoLogin)
            return;
        var db = await DatabaseSession.GetActive();
        if (db.LoggedIn)
        {
            messageText.text = "Already logged in!";
            
            PlayerPrefs.SetString("usernameLAN", db.LocalUser.username);
            await BraneApp.GetService<ISceneNavigator>().GoToAsync(targetScene);
        }
    }
    
    public void Login()
    {
        Login(usernameField.text, passwordField.text);
    }
    
    public async Task Login(string username, string password)
    {
        messageText.text = "Logging in...";
        var db = await DatabaseSession.GetActive();
        var loginResult = await db.Login(username, password);
        if (loginResult.IsSuccess)
        {
            messageText.text = "Login successful!";
            PlayerPrefs.SetString("usernameLAN", db.LocalUser.username);
            await BraneApp.GetService<ISceneNavigator>().GoToAsync(targetScene);
        }
        else
        {
            messageText.text = loginResult.error_message;
            Debug.LogError(loginResult.error_message);   
        }
    }
    
    public void Signup()
    {
        Signup(signupUsernameField.text, signupEmailField.text, signupPasswordField.text, signupConfirmPasswordField.text);
    }
    
    public async Task Signup(string username, string email, string password, string confirmPassword)
    {
        if (password != confirmPassword)
        {
            messageText.text = "Passwords do not match!";
            return;
        }
        var db = DatabaseSession.GetActive().Result;
        var signupResult = await db.CreateAccount(username, password, email);
        if (signupResult.IsSuccess)
        {
            messageText.text = "Signup successful!";
            await Login(signupUsernameField.text, signupPasswordField.text);
        }
        else
        {
            messageText.text = signupResult.error_message;
            Debug.LogError(signupResult.error_message);
        }
    }
}
