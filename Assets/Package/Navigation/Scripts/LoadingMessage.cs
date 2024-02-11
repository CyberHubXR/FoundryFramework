using System;
using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class LoadingMessage : MonoBehaviour
{
    public float animationSpeed = 1;
    private TMP_Text text;

    private IEnumerator animation;
    
    IEnumerator AnimateText()
    {
        int numDots = 0;
        while (enabled && animationSpeed != 0)
        {
            text.text = "Loading";
            for(int i = 0; i < numDots; i++)
                text.text += ".";
            numDots = (numDots + 1) % 4;
            yield return new WaitForSeconds(1.0f / animationSpeed);
        }
    }

    private void OnEnable()
    {
        text = GetComponent<TMP_Text>();
        animation = AnimateText();
        StartCoroutine(animation);
    }

    private void OnDisable()
    {
        StopCoroutine(animation);
    }
}
