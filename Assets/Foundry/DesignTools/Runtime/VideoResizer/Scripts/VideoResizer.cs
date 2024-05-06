using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoResizer : MonoBehaviour
{
    public void ResizeScreen()
    {
        //Grab the video player
        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        VideoClip videoClip = videoPlayer.clip;

        //Grab the width / height ratio
        float width = (float)videoClip.height;
        float height = (float)videoClip.width;

        //Scale the screen to match
        videoPlayer.targetMaterialRenderer.transform.localScale = new Vector3(width / 1000, height / 1000, 1);
    }
}
