using TMPro;
using UnityEngine;

public class Keyboard : MonoBehaviour
{
    [Tooltip("External input fields to set text on and subscribe to for focus events.")]
    public TMP_InputField[] targets;
    public TMP_InputField currentTarget;

    public bool multiLine = false;
    public GameObject lowercaseHolder;
    public GameObject uppercaseHolder;
    bool shiftPressed;

    public void Start()
    {
        lowercaseHolder.SetActive(true);
        uppercaseHolder.SetActive(false);
        
        foreach (var target in targets)
            TargetManager.Listen(this, target);
    }
    
    public void ToggleShift() 
    {
        shiftPressed = !shiftPressed;
        
        uppercaseHolder.SetActive(shiftPressed);
        lowercaseHolder.SetActive(!shiftPressed);
    }

    class TargetManager
    {
        Keyboard keyboard;
        TMP_InputField target;
        public static void Listen(Keyboard keyboard, TMP_InputField target)
        {
            var manager = new TargetManager()
            {
                keyboard = keyboard,
                target = target
            };
            target.onSelect.AddListener(manager.OnTargetActivated);
            target.onDeselect.AddListener(manager.OnTargetDeactivated);
        }
        
        private void OnTargetDeactivated(string value)
        {
            // This gets called when the input field loses focus, which happens when we click keyboard buttons >:[
            //Debug.Assert(keyboard.currentTarget == target);
            //keyboard.currentTarget = null;
        }

        private void OnTargetActivated(string value)
        {
            //Debug.Assert(keyboard.currentTarget == null);
            keyboard.currentTarget = target;
        }
    }

    /// <summary>
    /// Use this to deselect the current target, and thus make button presses do nothing
    /// </summary>
    public void Deselect()
    {
        currentTarget = null;
    }
    
    public void KeyPress(string key) 
    {
        if (!currentTarget)
            return;
        if(key == "backspace") 
        {
            if(currentTarget.text.Length > 0) 
            {
                currentTarget.text = currentTarget.text.Substring(0, currentTarget.text.Length - 1);
            }
        } 
        else if(key == "enter")
        {
            if(!multiLine)
                currentTarget.onEndEdit.Invoke(currentTarget.text);
            else
                currentTarget.SetTextWithoutNotify(currentTarget.text += "\n");
        } 
        else
        {
            currentTarget.SetTextWithoutNotify(currentTarget.text += shiftPressed ? key.ToUpper() : key.ToLower());
        }
    }
    
    
}
