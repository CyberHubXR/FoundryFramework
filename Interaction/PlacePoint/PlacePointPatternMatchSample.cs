using Foundry;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PlacePointPatternMatchSample : MonoBehaviour
{
    public PlacePoint[] placePoint;

    public GameObject[] enableOnPlace;

    public UnityEvent OnPatternComplete;

    bool[] pattern;
    bool patternComplete;

    private void Start() {
        if(placePoint.Length != enableOnPlace.Length) {
            Debug.LogError("Place Point Pattern Match: placePoint and enableOnPlace must be the same length");
        }

        pattern = new bool[placePoint.Length];
        RandomizePattern();
    }

    public void RandomizePattern() {
        for(int i = 0; i < pattern.Length; i++) {
            pattern[i] = Random.Range(0, 1) == 0;
            if(pattern[i]) {
                enableOnPlace[i].SetActive(true);
            }
            else {
                enableOnPlace[i].SetActive(false);
            }
        }
        patternComplete = false;
        CheckComplete();
    }

    public void OnPlacePointPatternMatch() {
        for (int i = 0;i < pattern.Length;i++) {
            if(!pattern[i] && placePoint[i].placedObject != null) {
                pattern[i] = true;
                CheckComplete();
                return;
            }
        }
    }

    public void CheckComplete() {
        if(patternComplete)
            return;

        for(int i = 0; i < pattern.Length; i++) {
            if(pattern[i] && placePoint[i].placedObject == null) {
                return;
            }
        }
        OnPatternComplete.Invoke();
        patternComplete = true;
        Debug.Log("Pattern Complete!");
    }
}
