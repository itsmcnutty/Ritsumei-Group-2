﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : CallParentCollision
{
	// Height at which to kill falling enemy
	private static float DEATH_Y = -10.0f;

	// How much HP is represented by 1m (world space) of UI health bar
	private static float HP_PER_METER = 5000;

	// How fast the enemy health bar fades in
	private static float FADE_SPEED = 0.03f;

	// The enemy's max health
	public float MAX_HEALTH;

	// How much damage a hit must deal to pierce armor reduction completely
	public float ARMOR_CUTOFF;

	// How effective armor is when hit by something below cutoff
	public float ARMOR_PROFICIENCY;

	// How much damage a hit must deal to send the enemy ragdolling
	public float RAGDOLL_DMG_THRESHOLD;

	// The percentage of the total mass that would result in dealing 100% of damage
	public float FULL_MASS_DAMAGE_PERC = 0.15f;

	// Show health value for debugging
	public bool debugShowHealthText = false;

	// UI canvas containing healthbar elements
	public Canvas healthBarCanvas;

	// Text that displays the enemy's health
	public Text healthBarText;

	// Shape that forms background of health bar
	public RectTransform healthBarBackground;

	// Slider which reflects actual health of enemy
	public Slider healthBarActual;

	// Slider which highlights how much damage the enemy took recently
	public Slider healthBarBefore;

	[Header("Audio")]
	public AudioMultiClipSource enemyHurt;
	public AudioSource enemyDeath;
	public float maxDmgForSound;

// Canvas renderers for all of the above for fade in effect
	private CanvasRenderer canvasRendererBackground;
	private CanvasRenderer canvasRendererBefore;
	private CanvasRenderer canvasRendererActual;

	// How long to show the damage that was dealt in the "before" health bar (seconds)
	private float SHOW_DAMAGE_DURATION = 1f;
	// How long it takes to remove the damage that was dealt from the "before" health bar (seconds)
	private float REMOVE_DAMAGE_DURATION = 0.8f;

	// Seconds since last time enemy took damage
	private float timeSinceDamage;
	// HP before enemy took damage;
	private float healthBeforeDamage;
	// Rate at which ghost damage decreases
	private float healthBarBeforeDecRate;
	// Enemy's health points
	private float health;
	// Flag that's true when the enemy has been damaged by some amount
	private bool isDamaged;
	// Enemy's ragdoll controller
	private RagdollController ragdollController;
	// Player camera
	private GameObject player;
	// Enemy hip bone
	private GameObject hips;

	private bool isDead; // flag to keep prevent constant collision from spawning more enemies
	private float totalEnemyMass = 0;

	// Start is called before the first frame update
	void Start()
	{
		// Instantiating stuff
		health = MAX_HEALTH;
		ragdollController = GetComponent<RagdollController>();
		player = GameObject.FindGameObjectWithTag("MainCamera");
		hips = GetComponentInChildren<Rigidbody>().gameObject;
		canvasRendererBackground = healthBarBackground.GetComponent<CanvasRenderer>();
		canvasRendererBefore = healthBarBefore.GetComponentInChildren<CanvasRenderer>();
		canvasRendererActual = healthBarActual.GetComponentInChildren<CanvasRenderer>();

		// Setup health bars

		// Resize background and make health bar invisible
		float healthBarWidth = MAX_HEALTH / HP_PER_METER * 100;
		healthBarBackground.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, healthBarWidth);
		canvasRendererBackground.SetAlpha(0);
		canvasRendererBefore.SetAlpha(0);
		canvasRendererActual.SetAlpha(0);

		// Green health bar
		healthBarActual.minValue = 0f;
		healthBarActual.maxValue = MAX_HEALTH;
		healthBarActual.SetValueWithoutNotify(MAX_HEALTH);

		// Red health bar
		healthBarBefore.minValue = 0f;
		healthBarBefore.maxValue = MAX_HEALTH;
		healthBarBefore.SetValueWithoutNotify(MAX_HEALTH);

		// Calculate the total mass of the enemy
		Rigidbody[] enemyRigidBodies = gameObject.GetComponentsInChildren<Rigidbody>();
		foreach (Rigidbody enemyRigidBody in enemyRigidBodies)
		{
			totalEnemyMass += enemyRigidBody.mass;
		}

		UpdateHealthString();

		isDead = false;
	}

	// Called by child when receiving a collision event or a collision from its child (used
	// for receiving damage)
	protected override void OnCollisionEnterChild(GameObject child, Collision other)
	{
		// Do nothing if enemy hit was not hit with a weapon
		if (!IsRockWeapon(other.gameObject))
		{
			return;
		}

		// Set HP before damage to actual health if health bar is finished animating
		if (timeSinceDamage >= SHOW_DAMAGE_DURATION + REMOVE_DAMAGE_DURATION)
		{
			healthBeforeDamage = health;
		}
		// Otherwise set HP before damage to be what the ghost health currently shows
		else
		{
			healthBeforeDamage = healthBarBefore.value;
		}

		// Reset damage timer
		timeSinceDamage = 0f;

		// Calculate damage received
		float damage = CalculateDamage(child, other);

		// Increment player power-up counter based on the damage done by the given ability
		string tag = other.gameObject.tag;
		switch (tag)
		{
			case "Rock":
				PowerupController.IncrementRockClusterCounter(damage);
				break;
			case "Spike":
				PowerupController.IncrementSpikeChainCounter(damage);
				break;
		}

		// Update health and health bar
		health -= damage;
		healthBarActual.SetValueWithoutNotify(health);
		enemyHurt.PlayFromParam(damage / maxDmgForSound);
		UpdateHealthString();
		isDamaged = true;

		// Get linear rate of decrease for ghost damage
		healthBarBeforeDecRate = (health - healthBeforeDamage) / REMOVE_DAMAGE_DURATION;

		// Begin ragdolling if taken enough damage and not already ragdolling
		if (damage >= RAGDOLL_DMG_THRESHOLD && !ragdollController.IsRagdolling())
		{
			ragdollController.StartRagdoll();
		}

		if (health <= 0f && !isDead)
		{
			isDead = true;
			enemyDeath.Play();
			ragdollController.StartRagdoll();
		}
	}

	private float CalculateDamage(GameObject child, Collision other)
	{
		float momentum = other.gameObject.GetComponent<RockCollide>().GetMomentum();

		// Raw incoming damage
		float damage = momentum;

		// Scale damage by weapon type
		string tag = other.gameObject.tag;
		switch (tag)
		{
			case "Rock":
				damage *= GetRockDamageScalar(child, other.gameObject, damage);
				break;
			case "Wall":
				damage *= 0.1f;
				break;
			case "Spike":
				damage *= GetSpikeDamageScalar(other.gameObject);
				break;
			default:
				damage *= 0;
				break;
		}

		// Determine whether armor will reduce damage
		if (damage < ARMOR_CUTOFF)
		{
			// Armor damage reduction
			// actual_dmg = (1 / (cutoff ^ (prof - 1))) * raw_dmg ^ prof
			return (1f / (float) Math.Pow(ARMOR_CUTOFF, ARMOR_PROFICIENCY - 1f)) *
				(float) Math.Pow(damage, ARMOR_PROFICIENCY);
		}

		return damage;
	}

	private float GetRockDamageScalar(GameObject child, GameObject rock, float damage)
	{
		RockProperties properties = rock.GetComponent<RockProperties>();
		if (!properties.EnemyWasHit(gameObject.GetInstanceID()))
		{
			float damageScalar = 1f;
			if (rock.transform.parent && rock.transform.parent.gameObject.layer == 10)
			{
				if ((damage * damageScalar) < 750)
				{
					return 0;
				}
				else
				{
					StartCoroutine(properties.TempAddEnemy(gameObject.GetInstanceID()));
					ragdollController.StartRagdoll();
				}
			}
			else
			{
				StartCoroutine(properties.TempAddEnemy(gameObject.GetInstanceID()));
				if (child.name.Contains("Head"))
				{
					damageScalar *= 1.5f;
				}
			}
			return damageScalar;
		}
		return 0;
	}

	private float GetSpikeDamageScalar(GameObject spike)
	{
		SpikeMovement properties = spike.GetComponent<SpikeMovement>();
		if (!properties.EnemyWasHit(gameObject.GetInstanceID()))
		{
			float damageScalar = 0.8f;
			properties.NewEnemyHit(gameObject.GetInstanceID());
			return damageScalar;
		}
		return 0;
	}

	// Returns true if the specified GameObject can damage the enemy
	private bool IsRockWeapon(GameObject obj)
	{
		return obj.GetComponent<RockCollide>() != null;
	}

	// Lerps alpha value of all of the slider elements in the health bar UI to create a fade in effect when called repeatedly
	private void lerpHealthBarAlpha(float alpha)
	{
		// Get next alpha value
		float nextAlpha = Mathf.Lerp(canvasRendererBackground.GetAlpha(), alpha, FADE_SPEED);

		// Update alpha values
		canvasRendererBackground.SetAlpha(nextAlpha);
		canvasRendererBefore.SetAlpha(nextAlpha);
		canvasRendererActual.SetAlpha(nextAlpha);
	}

	// Updates the text above the health bar based on the enemy's current health
	private void UpdateHealthString()
	{
		if (debugShowHealthText)
		{
			healthBarText.text = String.Format("{0} / {1}", Math.Ceiling(health), Math.Ceiling(MAX_HEALTH));
		}
		else if (gameObject.name == "Target Dummy")
		{
			healthBarText.text = "Target Dummy";
		}
		else
		{
			healthBarText.text = "";
		}
	}

	// Checks for death plane and controls the animation of the "before" health bar
	private void Update()
	{
		// Check for death plane
		if (hips.transform.position.y <= DEATH_Y)
		{
			// Indicate the Game Controller that an enemy was destroyed
			GameController.Instance.EnemyGotDestroyed(gameObject);
			Destroy(gameObject);
		}

		timeSinceDamage += Time.deltaTime;

		// Animation complete
		if (timeSinceDamage >= REMOVE_DAMAGE_DURATION + SHOW_DAMAGE_DURATION)
		{
			healthBarBefore.SetValueWithoutNotify(health);
		}
		// Animate the decreasing ghost health
		else if (timeSinceDamage >= SHOW_DAMAGE_DURATION)
		{
			float decTime = timeSinceDamage - SHOW_DAMAGE_DURATION;
			healthBarBefore.SetValueWithoutNotify(healthBeforeDamage + healthBarBeforeDecRate * decTime);
		}

		// Fade in health bar if damaged
		if (isDamaged && canvasRendererBackground.GetAlpha() < 0.9999)
		{
			lerpHealthBarAlpha(1.0f);
		}

		// Rotate health bar to face player
		Vector3 toPlayerVector = player.transform.position - healthBarBefore.transform.position;
		healthBarCanvas.transform.rotation = Quaternion.LookRotation(toPlayerVector);
	}
}