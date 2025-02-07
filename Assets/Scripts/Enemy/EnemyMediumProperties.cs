﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyMediumProperties : EnemyProperties
{
	[Header("Melee Variables")]
	// Time between melee attacks (seconds)
    public float MELEE_DELAY = 2f;
    // Radius for melee attacking
    public float MELEE_RADIUS;
    
    [Header("Shooting Variables")]
    // Time between ranged attacks (seconds)
    public float RANGED_DELAY = 3f;
    public float PROJECTILE_VEL_X = 15f; // the projectile Initial velocity in X-axis for projectile
    // todo get the agentHead
    [NonSerialized] public Vector3 agentHead; // this is where the ray cast originates, determines if enemy can see player

    [Header("Strafing Variables")]
    // Speed of navmesh agent when strafing 
    public float MAX_STRAFE_SPEED;
    // Minimum distance before using ranged attacks on player
    public float RANGED_RADIUS = 23f;
    // distance that the enemy will start strafing around player
    public float STRAFE_DIST = 15f; 
    // every time a point around the strafing circle is reached, next point will be close to the center of circle by this radius
    public float RADIUS_REDUCTION = 5f;
    // allows to have a random radius reduction for each enemy, given the specified radius
    public bool randomRadiusReduction = true;
    public float minRangeRadiusReduction = 3;
    public float maxRangeRadiusReduction = 7;
    [NonSerialized] public bool isStrafing = false; // bool indicating if agent is in strafing state
    [NonSerialized] public int lastPointIndex; // last point index value in the pointsAroundTarget array
    [NonSerialized] public bool isClockwise = false; // walk in a clockwise direction when strafying
    
    // Climbing Variables
    [NonSerialized] public float climbCounter = 0; // counter that keeps track of how many times the agent has climbed

    // Allowed space around attack radius that enemies can attack from
    [NonSerialized] public float ATTACK_MARGIN = 1f;
    
    // Getup animation speed
    [Header("Animation")]
    public float getUpStateTimeOut;

    
    // All states
    [NonSerialized] public RunState runState;
    [NonSerialized] public StrafeState strafeState;
    [NonSerialized] public AdvanceState advanceState;
    [NonSerialized] public MeleeState meleeState;
    [NonSerialized] public RetreatState retreatState;
    [NonSerialized] public SwingState swingState;
    [NonSerialized] public RagdollState ragdollState;
    [NonSerialized] public ClimbingState climbingState;
    [NonSerialized] public AboveWallState aboveWallState;
    [NonSerialized] public GetUpState getUpState;

    // Start is called before the first frame update
    new void Start()
    {
        base.Start();
        
        isStrafing = false;
        lastPointIndex = 0; // just initialization
        // assign randomly if enemy will strafe clockwise or counter clockwise
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
	        isClockwise = true;
        }
        
        if (randomRadiusReduction)
        {
	        RADIUS_REDUCTION = UnityEngine.Random.Range(minRangeRadiusReduction, maxRangeRadiusReduction);
        }
        
        // Instantiate states with the properties above
        runState = new RunState(this);
        strafeState = new StrafeState(this);
        advanceState = new AdvanceState(this);
        meleeState = new MeleeState(this);
        retreatState = new RetreatState(this);
        swingState = new SwingState(this);
        ragdollState = new RagdollState(this);
        climbingState = new ClimbingState(this);
        aboveWallState = new AboveWallState(this);
        getUpState = new GetUpState(this);
		
        // Initialize states within these state objects
        runState.InitializeStates(this);
        strafeState.InitializeStates(this);
        advanceState.InitializeStates(this);
        meleeState.InitializeStates(this);
        retreatState.InitializeStates(this);
        swingState.InitializeStates(this);
        ragdollState.InitializeStates(this);
        climbingState.InitializeStates(this);
        aboveWallState.InitializeStates(this);
        getUpState.InitializeStates(this);

        // Give FSM an initial state
        stateMachine.ChangeState(runState);
    }

    public void IncreaseClimbCount()
    {
	    climbCounter++;
    }

    public override float GetCurrentMaxSpeed()
    {
	    switch (stateMachine.GetCurrentState())
	    {
		    case "Run":
			    return MAX_RUN_SPEED;
		    case "Strafe":
			    return MAX_STRAFE_SPEED;
		    case "Advance":
			    return MAX_STRAFE_SPEED;
		    case "Retreat":
			    return MAX_STRAFE_SPEED;
		    default:
			    return 0;
	    }
    }

    public override void PlayFootstepSound()
    {
	    if (agent.enabled)
	    {
		    if (stateMachine.GetCurrentState().Equals(runState.ToString()))
		    {
			    if (agent.speed < MAX_RUN_SPEED)
			    {
				    quicksandFootstep.PlayRandom();
			    }
			    else
			    {
				    groundFootstep.PlayRandom();
			    }
		    }
		    else
		    {
			    if (agent.speed < MAX_STRAFE_SPEED)
			    {
				    quicksandFootstep.PlayRandom();
			    }
			    else
			    {
				    groundFootstep.PlayRandom();
			    }
		    }
	    }
    }
}
