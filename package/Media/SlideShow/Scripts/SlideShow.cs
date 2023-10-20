using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class SlideShow : NetworkBehaviour
{
    public Texture2D[] slides;

    public float slideChangeSpeed;
    private float timer;

    [Networked (OnChanged = nameof(UpdateSlideValue))] public int currentSlide { get; set; }

    private Material renderImage;

    public override void Spawned()
    {
        TryGetComponent(out Renderer imageComponent);
        renderImage = imageComponent.sharedMaterial;
    }

    public override void FixedUpdateNetwork()
    {
        CycleSlide();
    }

    void CycleSlide()
    {
        timer += Runner.DeltaTime;
        
        if (timer > slideChangeSpeed)
        {
            if(HasStateAuthority)
                currentSlide++;
            
            currentSlide %= slides.Length;
            
            timer = 0;
        }
    }

    static void UpdateSlideValue(Changed<SlideShow> changed)
    {
        changed.Behaviour.UpdateSlide();
    }


    public void UpdateSlide()
    {
        renderImage.mainTexture = slides[currentSlide];
    }
}
