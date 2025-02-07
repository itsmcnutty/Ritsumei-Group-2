﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RunState : IState
{
	// Radius for ranged attacks
	private float rangedRadius;
    
	// This is the agent to move around by NavMesh
	private NavMeshAgent agent;
	// The enemy's Animator component
	private Animator animator;
	// The enemy's ragdoll controller
	private RagdollController ragdollController;
	// The NavMeshObstacle used to block enemies pathfinding when not moving
	private NavMeshObstacle obstacle;
	// Speed of navmesh agent in this state
	private float maxRunSpeed;
	// Speed of navmesh agent when in strafe state
	private float maxStrafeSpeed;
	// Doesn't walk if true (for debugging)
	private bool debugNoWalk;
    
	// Squared ranged radius (for optimized calculations)
//	private float sqrRangedRadius;
    
	// Player GameObject
	private GameObject player;
	// Player's head's world position
	private Vector3 playerPos;
	// This enemy GameObject
	private GameObject gameObj;
	// The enemy properties component
	private EnemyProperties enemyProps;

	private bool firstRun;
	
	// States to transition to
	private StrafeState strafeState;
	private RagdollState ragdollState;
	private ClimbingState climbingState;

	public RunState(EnemyMediumProperties enemyProps)
	{
		rangedRadius = enemyProps.RANGED_RADIUS;
		agent = enemyProps.agent;
		animator = enemyProps.animator;
		ragdollController = enemyProps.ragdollController;
		obstacle = enemyProps.obstacle;
		maxRunSpeed = enemyProps.MAX_RUN_SPEED;
		maxStrafeSpeed = enemyProps.MAX_STRAFE_SPEED;
		debugNoWalk = enemyProps.debugNoWalk;
		player = enemyProps.player;
		playerPos = enemyProps.playerPos;
		gameObj = enemyProps.gameObject;
		this.enemyProps = enemyProps;
	}
	
	public RunState(EnemyLightProperties enemyProps)
	{
		rangedRadius = enemyProps.RANGED_RADIUS;
		agent = enemyProps.agent;
		animator = enemyProps.animator;
		ragdollController = enemyProps.ragdollController;
		obstacle = enemyProps.obstacle;
		maxRunSpeed = enemyProps.MAX_RUN_SPEED;
		maxStrafeSpeed = enemyProps.MAX_STRAFE_SPEED;
		debugNoWalk = enemyProps.debugNoWalk;
		player = enemyProps.player;
		playerPos = enemyProps.playerPos;
		gameObj = enemyProps.gameObject;
		this.enemyProps = enemyProps;
	}
	
	// Initializes the IState instance fields. This occurs after the enemy properties class has constructed all of the
	// necessary states for the machine
	public void InitializeStates(EnemyMediumProperties enemyProps)
	{
		strafeState = enemyProps.strafeState;
		ragdollState = enemyProps.ragdollState;
		climbingState = enemyProps.climbingState;
	}
	
	// Initializes the IState instance fields. This occurs after the enemy properties class has constructed all of the
	// necessary states for the machine
	public void InitializeStates(EnemyLightProperties enemyProps)
	{
		strafeState = enemyProps.strafeState;
		ragdollState = enemyProps.ragdollState;
	}

	// Called upon entering this state from anywhere
	public void Enter()
	{
		// No longer obstacle
		obstacle.enabled = false;
		enemyProps.EnablePathfind();
		
		// first time running
		firstRun = true;
		
		// Settings for agent
		agent.stoppingDistance = rangedRadius;
		agent.speed = maxRunSpeed;
		agent.angularSpeed = 8000f;
	}

	// Called upon exiting this state
	public void Exit() {}

	// Called during Update while currently in this state
	public void Action()
	{
		// Store transform variables for player
		playerPos = player.transform.position;
        
		// Pass speed to animation controller
		float moveSpeed = agent.velocity.magnitude;
		animator.SetFloat("RunSpeed", moveSpeed);

		// Move to player if outside attack range, otherwise transition
		if (agent.enabled && !debugNoWalk)
		{
			// Too far, walk closer
			if (firstRun)
			{
				firstRun = false;
				agent.SetDestination(playerPos);
			}
			
//			if (agent.isPathStale)
//			{
//				Debug.Log("Path became stale, recalculate again");
////				Vector3 backupPos = new Vector3(-3.6f,0.8f,3.9f);
//				Vector3 backupPos = new Vector3(0f,0f,0f);
//				agent.SetDestination(backupPos);
//				if (!agent.pathPending)
//				{
//					Debug.Log("paht is not calculating anymore, therefore go to agent");
//					agent.SetDestination(playerPos);
//				}
//			}
//
//			if (agent.pathPending)
//			{
//				Debug.Log("Agent is calculating navmesh");
//			}

			// Stopping distance at which we want the agent to slow down to strafe speed from its current movement speed
			agent.stoppingDistance = rangedRadius +
			                         ((maxStrafeSpeed * maxStrafeSpeed - moveSpeed * moveSpeed )/ (2 * agent.acceleration));
		}
	}

	// Called immediately after Action. Returns an IState if it can transition to that state, and null if no transition
	// is possible
	public IState Transition()
	{
		// Transition to ragdoll state if ragdolling
		if (ragdollController.IsRagdolling())
		{
			animator.SetTrigger("Ragdoll");
			return ragdollState;
		}
		
//		// Transition to climbing state if climbing
		if (agent.isOnOffMeshLink)
		{
			// todo do something with animator
			return climbingState;
		}
		
		// Get enemy position
		Vector3 gameObjPos = gameObj.transform.position;
		
		// Calculate enemy distance
		float distanceToPlayer = enemyProps.calculateSqrDist(playerPos, gameObjPos);

		// If within ranged attack range, transition to strafe state
		if (distanceToPlayer < rangedRadius * rangedRadius)
		{
			animator.SetTrigger("Strafe");
			return strafeState;
		}
		
		// Otherwise, don't transition
		return null;
	}

	public override string ToString()
	{
		return "Run";
	}
}
