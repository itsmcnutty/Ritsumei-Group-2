﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class PlayerEnergy : MonoBehaviour
{
    public Slider energyBarBefore;
    public Slider energyBarAfter;
    public Text energyBarText;
    public float maxEnergy;
    public float regenEnergyRate;
    public float regenDelayInSec;

    private float currentEnergy;
    private float lastAbilityUsedTime;
    private Dictionary<Hand, float> activeAbilityEnergyCost;

    // Start is called before the first frame update
    void Start ()
    {
        activeAbilityEnergyCost = new Dictionary<Hand, float> ();
        currentEnergy = maxEnergy;
        energyBarBefore.maxValue = maxEnergy;
        energyBarBefore.value = maxEnergy;
        energyBarAfter.maxValue = maxEnergy;
        energyBarAfter.value = maxEnergy;
    }

    // Update is called once per frame
    void Update ()
    {
        RegenEnergy ();
        SetEnergyBarText ();
    }

    public void DrainTempEnergy (Hand activeHand, float energy)
    {
        if (EnergyIsNotZero ())
        {
            activeAbilityEnergyCost[activeHand] += energy;
            float afterAbilityEnergy = GetTotalEnergyUsage ();
            if (afterAbilityEnergy > currentEnergy)
            {
                afterAbilityEnergy = currentEnergy;
                activeAbilityEnergyCost[activeHand] -= (afterAbilityEnergy - currentEnergy);
            }
            energyBarAfter.value = currentEnergy - afterAbilityEnergy;
        }
        UpdateAbilityUseTime ();
    }

    public void SetTempEnergy (Hand activeHand, float energy)
    {
        activeAbilityEnergyCost[activeHand] = energy;
        float afterAbilityEnergy = GetTotalEnergyUsage ();
        if (afterAbilityEnergy > currentEnergy)
        {
            activeAbilityEnergyCost[activeHand] -= (afterAbilityEnergy - currentEnergy);
            afterAbilityEnergy = currentEnergy;
        }
        energyBarAfter.value = currentEnergy - afterAbilityEnergy;
        UpdateAbilityUseTime ();
    }

    public void DrainRealEnergy (float energy)
    {
        if (currentEnergy > 0)
        {
            currentEnergy -= energy;
            if (currentEnergy < 0)
            {
                currentEnergy = 0;
            }
            energyBarBefore.value = currentEnergy;
            energyBarAfter.value = currentEnergy;
        }
        UpdateAbilityUseTime ();
    }

    public void UseEnergy (Hand activeHand)
    {
        currentEnergy -= activeAbilityEnergyCost[activeHand];
        energyBarBefore.value = currentEnergy;
        activeAbilityEnergyCost[activeHand] = 0;
        RemoveHandFromActive (activeHand);
    }

    public void CancelEnergyUsage (Hand activeHand)
    {
        energyBarAfter.value = currentEnergy;
        activeAbilityEnergyCost[activeHand] = 0;
        RemoveHandFromActive (activeHand);
    }

    public void RegenEnergy ()
    {
        if ((Time.time - lastAbilityUsedTime) > regenDelayInSec && currentEnergy < maxEnergy)
        {
            currentEnergy += regenEnergyRate;
            if (currentEnergy > maxEnergy)
            {
                currentEnergy = maxEnergy;
            }
            energyBarBefore.value = currentEnergy;
            energyBarAfter.value = currentEnergy;
        }
    }

    public bool EnergyIsNotZero ()
    {
        return (currentEnergy - GetTotalEnergyUsage ()) > 0;
    }

    public bool EnergyAboveThreshold(float threshold)
    {
        return (currentEnergy - GetTotalEnergyUsage ()) > threshold;
    }

    private void SetEnergyBarText ()
    {
        energyBarText.text = currentEnergy - GetTotalEnergyUsage () + " / " + maxEnergy;
    }

    public void UpdateAbilityUseTime ()
    {
        lastAbilityUsedTime = Time.time;
    }

    public void AddHandToActive (Hand activeHand)
    {
        if(!activeAbilityEnergyCost.ContainsKey(activeHand))
        {
            activeAbilityEnergyCost.Add (activeHand, 0);
        }
        else
        {
            activeAbilityEnergyCost[activeHand] = 0;
        }
    }

    public float GetRemainingEnergy ()
    {
        return currentEnergy - GetTotalEnergyUsage ();
    }

    private void RemoveHandFromActive (Hand activeHand)
    {
        float entry;
        if (activeAbilityEnergyCost.TryGetValue (activeHand, out entry))
        {
            activeAbilityEnergyCost.Remove (activeHand);
        }
    }

    private float GetTotalEnergyUsage ()
    {
        float totalEnergy = 0;
        foreach (float abilityEnergyCost in activeAbilityEnergyCost.Values)
        {
            totalEnergy += abilityEnergyCost;
        }
        return totalEnergy;
    }

}