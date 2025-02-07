﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverProperties : MonoBehaviour
{
    
    private InteractLaserButton laserPointer;

    // Start is called before the first frame update
    void Start()
    {
        laserPointer = GameObject.FindWithTag("UIController").GetComponent<InteractLaserButton>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        // active abilities and ability laser
        laserPointer.toggleLaser();
        if (TutorialController.Instance.TutorialWaveInProgress())
        {
            TutorialController.Instance.ToggleTutorialAbilities();
        }
        else
        {
            PlayerAbility.ToggleSpikeAbility();
            PlayerAbility.ToggleWallAbility();
            PlayerAbility.ToggleQuicksandAbility();
            PlayerAbility.ToggleRockAbility();
        }
        Time.timeScale = 1;
    }
}