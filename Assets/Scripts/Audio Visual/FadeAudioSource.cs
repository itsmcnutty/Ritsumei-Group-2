﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]

public class FadeAudioSource : MonoBehaviour
{
    // The audio source with the loop
    public AudioSource source;

    [Header("Fading variables")]
    // How long (in seconds) to spend on fade in
    public float fadeInTime;
    // How long (in seconds) to spend on fade out
    public float fadeOutTime;

    [Header("Ducking variables")]
    // The audio will duck down if the pitch is not changing
    public bool useDucking;
    // How long the sound takes to duck down
    public float duckTime;
    // How quiet to make the sound when ducking
    public float duckVolume;

    // How much volume should be modified by each second
    private float volumeUpdatePerSecond = 0;
    // True if audio should stop reducing volume at duckVolume
    private bool isCurrentlyDucking = false;

    // Plays the sound if not already looping (Fades back in whether stopped or in middle of fade out)
    // Returns false if the sound was already playing
    public bool Play()
    {
    // Calculate how much to update volume per second (regardless of current volume)
        volumeUpdatePerSecond = 1f / (fadeInTime <= 0 ? 0.000001f : fadeInTime);
        
        if (!source.isPlaying)
        {
            source.Play();
            return true;
        }
        
        // Sound was already playing
        return false;
    }

    // Stops the sound if it is currently looping (fading in or full volume)
    // Returns false if the sound was not playing when called
    public bool Stop()
    {
        if (source.isPlaying)
        {
            // Calculate how much to update volume per second (regardless of current volume)
            volumeUpdatePerSecond = -1f / (fadeOutTime <= 0 ? 0.000001f : fadeOutTime);
            return true;
        }
        
        // Sound was not playing
        return false;
    }
    
    // Set the volume update rate so that the audio can duck as specified
    private void SetDucking()
    {
        
    }

        // Update is called once per frame. 
    void Update()
    {
        // Update volume if playing
        if (source.isPlaying)
        {
            source.volume += volumeUpdatePerSecond * Time.deltaTime;
        }

        if (source.volume >= 1)
        {
            // Max volume achieved, stop updating volume
            volumeUpdatePerSecond = 0;
        }
        else if (source.volume <= 0)
        {
            // Min volume achieved, stop updating volume and stop playing audio
            source.Stop();
            volumeUpdatePerSecond = 0;
        }
    }
}
