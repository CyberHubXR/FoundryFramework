
using System;
using System.Collections.Generic;
using Foundry.Networking;
using UnityEngine;

public class SlideShow : NetworkComponent
{
    public Texture2D[] slides;

    public float slideChangeSpeed;
    private float timer;

    public NetworkProperty<int> currentSlide = new(0);

    private Material renderImage;

    public override void RegisterProperties(List<INetworkProperty> props, List<INetworkEvent> events)
    {
        props.Add(currentSlide);
    }

    public override void OnConnected()
    {
        TryGetComponent(out Renderer imageComponent);
        renderImage = imageComponent.sharedMaterial;
    }

    private void Start()
    {
        currentSlide.OnChanged += UpdateSlide;
    }

    public void Update()
    {
        CycleSlide();
    }

    void CycleSlide()
    {
        timer += Time.deltaTime;
        
        if (timer > slideChangeSpeed)
        {
            if(IsOwner)
                currentSlide.Value++;
            
            currentSlide.Value %= slides.Length;
            
            timer = 0;
        }
    }

    public void UpdateSlide()
    {
        renderImage.mainTexture = slides[currentSlide.Value % slides.Length];
    }
}
