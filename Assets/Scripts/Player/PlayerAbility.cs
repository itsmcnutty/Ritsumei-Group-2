using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class PlayerAbility : MonoBehaviour
{
    public SteamVR_Input_Sources handType;
    public SteamVR_Behaviour_Pose controllerPose;
    public SteamVR_Action_Boolean grabAction;
    public SteamVR_Action_Boolean gripAction;
    public SteamVR_Action_Boolean drawAction;
    public Hand otherHand;
    public float rockStartSize;
    public float energyCost;
    public float baseSpikeRadius = 0.5f;

    [Header ("Prefabs")]
    public GameObject actionPlaceholderPrefab;
    public GameObject wallOutlinePrefab;
    public GameObject spikePrefab;
    public GameObject quicksandPrefab;
    public GameObject wallPrefab;

    [Header ("Outline Materials")]
    public Material validOutlineMat;
    public Material invalidOutlineMat;

    private Hand hand;
    private PlayerEnergy playerEnergy;
    private ControllerArc arc;
    private ControllerArc otherArc;
    private float rockSize = 0;
    private GameObject player;
    private GameObject rock;
    private GameObject spikeQuicksandOutline;
    private Rigidbody rockRigidbody;
    private float startingSpikeWidth;
    private Vector3 spikeEndPosition;

    private static GameObject wallOutline;
    private static GameObject wall;
    private static Hand firstHandHeld;
    private static float lastAngle;
    private static float startingHandHeight;
    private static float currentWallHeight;

    private static List<Vector2> spikeLocations;
    private HashSet<Vector3> allSpikes;

    private const float ROCK_CREATE_DIST = 3f;
    private const float ROCK_SIZE_INCREASE_RATE = 0.01f;
    private const float SPIKE_SPEED_REDUCTION = 10f;
    private const float SPIKE_BASE_SPEED = .05f;
    private const float WALL_SIZE_MULTIPLIER = 200f;

    public NavMeshSurface surface;

    private void Awake ()
    {
        player = GameObject.FindWithTag ("MainCamera");
        if (player != null)
        {
            playerEnergy = player.GetComponent<PlayerEnergy> ();
        }
    }

    // Start is called before the first frame update
    void Start ()
    {
        rockSize = rockStartSize;

        arc = GetComponentInChildren<ControllerArc> ();
        otherArc = otherHand.GetComponentInChildren<ControllerArc> ();
        hand = GetComponent<Hand> ();

        allSpikes = new HashSet<Vector3> ();

        spikeLocations = new List<Vector2> ();
        spikeLocations.Add (new Vector2 (2, 0));
        spikeLocations.Add (new Vector2 (1, 1));
        spikeLocations.Add (new Vector2 (-1, 1));
        spikeLocations.Add (new Vector2 (-2, 0));
        spikeLocations.Add (new Vector2 (-1, -1));
        spikeLocations.Add (new Vector2 (1, -1));
    }

    // Update is called once per frame
    void Update ()
    {
        if (GripPress ())
        {
            CancelAbility ();
        }

        if (GrabPress ())
        {
            TriggerNewAbility ();
        }
        else if (GrabHold ())
        {
            UpdateAbility ();
        }
        else if (GrabRelease ())
        {
            EndAbility ();
        }

        if (DrawPress () && playerEnergy.EnergyAboveThreshold (100f))
        {
            EnterDrawMode ();
        }
        else if (DrawHold ())
        {
            playerEnergy.UpdateAbilityUseTime ();
            if (WallOutlineIsActive () && !WallIsActive ())
            {
                SetWallLocation ();
                SetOutlineMaterial (wallOutline, WallIsValid ());
            }
        }
        else if (DrawRelease ())
        {
            if (WallOutlineIsActive () && !WallIsActive ())
            {
                CancelAbility ();
            }
        }
    }

    public bool GrabHold ()
    {
        return grabAction.GetState (handType);
    }

    private bool GrabPress ()
    {
        return grabAction.GetStateDown (handType);
    }

    private bool GrabRelease ()
    {
        return grabAction.GetStateUp (handType);
    }

    public bool DrawHold ()
    {
        return drawAction.GetState (handType);
    }

    private bool DrawPress ()
    {
        return drawAction.GetStateDown (handType);
    }

    private bool DrawRelease ()
    {
        return drawAction.GetStateUp (handType);
    }

    private bool GripPress ()
    {
        return gripAction.GetStateDown (handType);
    }

    private void TriggerNewAbility ()
    {
        if (WallOutlineIsActive ())
        {
            if (firstHandHeld != null && firstHandHeld != hand)
            {
                OutlineProperties properties = wallOutline.GetComponentInChildren<OutlineProperties> ();
                if (WallIsValid ())
                {
                    playerEnergy.AddHandToActive (firstHandHeld);
                    wall = Instantiate (wallPrefab) as GameObject;
                    wall.transform.position = wallOutline.transform.position;
                    wall.transform.localScale = wallOutline.transform.localScale;
                    wall.transform.rotation = wallOutline.transform.rotation;
                    startingHandHeight = Math.Min (hand.transform.position.y, otherHand.transform.position.y);
                    Destroy (wallOutline);
                }
                else
                {
                    playerEnergy.CancelEnergyUsage (firstHandHeld);
                    Destroy (wallOutline);
                    ResetWallInfo ();
                }

            }
            else
            {
                firstHandHeld = hand;
            }
        }
        else if (!WallIsActive () && arc.CanUseAbility ())
        {
            firstHandHeld = null;
            playerEnergy.AddHandToActive (hand);
            if (arc.GetDistanceFromPlayer () <= ROCK_CREATE_DIST)
            {
                GetComponent<SpawnAndAttachToHand> ().SpawnAndAttach (null);
                GameObject[] allObjects = GameObject.FindGameObjectsWithTag ("Rock");
                rock = allObjects[allObjects.Length - 1];
                rockRigidbody = rock.GetComponent<Rigidbody> ();
                hand.TriggerHapticPulse (800);
                hand.SetAllowResize (true);
            }
            else if (playerEnergy.EnergyAboveThreshold (100f))
            {
                spikeQuicksandOutline = Instantiate (actionPlaceholderPrefab) as GameObject;
                spikeQuicksandOutline.transform.position = arc.GetEndPosition ();
                startingSpikeWidth = hand.transform.position.y;
            }
        }
    }

    private void UpdateAbility ()
    {

        if (RockIsActive ())
        {
            if (playerEnergy.EnergyIsNotZero ())
            {
                rockSize = (float) Math.Pow (Math.Floor (rock.transform.localScale.x * rock.transform.localScale.y * rock.transform.localScale.z), 3);
                rockRigidbody.mass = 3200f * (float) Math.Pow (rockSize / 2.0, 3.0);
                playerEnergy.SetTempEnergy (hand, rockSize);
                hand.TriggerHapticPulse (800);
            }
            else
            {
                hand.SetAllowResize (false);
            }
        }
        else if (SpikeQuicksandIsActive ())
        {
            allSpikes.Clear ();
            float size = (float) Math.Pow ((Math.Abs (hand.transform.position.y - startingSpikeWidth)) + (baseSpikeRadius * 2), 3);
            spikeQuicksandOutline.transform.localScale = new Vector3 (size, 1f, size);
            float energyCost = (float) Math.Round (Math.Pow (spikeQuicksandOutline.transform.localScale.x, 2), 2) * 50f;
            playerEnergy.SetTempEnergy (hand, energyCost);
            SetOutlineMaterial (spikeQuicksandOutline, SpikeQuicksandIsValid ());
        }
        else if (WallIsActive () && playerEnergy.EnergyIsNotZero ())
        {
            hand.TriggerHapticPulse (1500);
            float newHandHeight = (Math.Min (hand.transform.position.y, otherHand.transform.position.y) - startingHandHeight) * 2f;
            if (newHandHeight < 1 && currentWallHeight < newHandHeight)
            {
                currentWallHeight = newHandHeight;
                Vector3 newPos = new Vector3 (wall.transform.position.x, wall.transform.localScale.y * newHandHeight, wall.transform.position.z);
                wall.transform.position = Vector3.MoveTowards (wall.transform.position, newPos, 1f);
                float area = (float) Math.Round (wall.transform.localScale.x * wall.transform.localScale.y * newHandHeight, 2) * WALL_SIZE_MULTIPLIER;
                playerEnergy.SetTempEnergy (firstHandHeld, area);
                surface.BuildNavMesh ();
            }
        }
    }

    private void EndAbility ()
    {
        if (RockIsActive ())
        {
            playerEnergy.UseEnergy (hand);
            RemoveRockFromHand ();
        }
        else if (SpikeQuicksandIsActive ())
        {
            float controllerVelocity = controllerPose.GetVelocity ().y;
            float handPos = (hand.transform.position.y - startingSpikeWidth);
            if (handPos < 0 && SpikeQuicksandIsValid ())
            {
                GameObject quicksand = Instantiate (quicksandPrefab) as GameObject;
                quicksand.transform.position = spikeQuicksandOutline.transform.position;
                quicksand.transform.localScale = new Vector3 (spikeQuicksandOutline.transform.localScale.x, .01f, spikeQuicksandOutline.transform.localScale.z);
                quicksand.AddComponent<QuicksandProperties> ();
                Destroy (spikeQuicksandOutline);
                playerEnergy.UseEnergy (hand);
            }
            else if (handPos > 0 && controllerVelocity > 0 && SpikeQuicksandIsValid ())
            {
                float height = (float) Math.Sqrt (3) * baseSpikeRadius;
                float size = spikeQuicksandOutline.transform.localScale.x / 2;
                float finalSpikeRadius = GenerateSpikes (spikeQuicksandOutline.transform.position, spikeQuicksandOutline.transform.position, height, baseSpikeRadius, size);
                float radiusIncrease = finalSpikeRadius - baseSpikeRadius;

                finalSpikeRadius *= 2;
                Vector3 centerLoc = spikeQuicksandOutline.transform.position;

                Destroy (spikeQuicksandOutline);
                playerEnergy.UseEnergy (hand);

                foreach (Vector3 spikePos in allSpikes)
                {
                    GameObject spike = Instantiate (spikePrefab) as GameObject;
                    Vector3 spikeCorrection = (spikePos - centerLoc) / 2;
                    Vector3 radiusCorrection = new Vector3 (Math.Sign (spikeCorrection.x) * radiusIncrease, 0, Math.Sign (spikeCorrection.z) * radiusIncrease);
                    spike.transform.position = (spikePos - spikeCorrection) + radiusCorrection;
                    spike.transform.localScale = new Vector3 (finalSpikeRadius, finalSpikeRadius, finalSpikeRadius);

                    float spikeVelocity = (controllerVelocity / SPIKE_SPEED_REDUCTION) + SPIKE_BASE_SPEED;
                    spike.GetComponent<SpikeMovement> ().SetSpeed (spikeVelocity);

                    spikeEndPosition = spike.transform.position;
                    spikeEndPosition.y += spikePos.y + (2f * finalSpikeRadius);
                    spike.GetComponent<SpikeMovement> ().SetEndPosition (spikeEndPosition);
                }
                surface.BuildNavMesh ();
            }
            else
            {
                Destroy (spikeQuicksandOutline);
                playerEnergy.CancelEnergyUsage (hand);
            }
        }
        else if (WallIsActive ())
        {
            wall.AddComponent<WallProperties> ();
            playerEnergy.UseEnergy (firstHandHeld);
            ResetWallInfo ();
        }
    }

    private float GenerateSpikes (Vector3 position, Vector3 centerLoc, float height, float spikeRadius, float areaRadius)
    {
        float radius = areaRadius;
        allSpikes.Add (position);
        foreach (Vector2 locationOffset in spikeLocations)
        {
            float newX = position.x + (spikeRadius * locationOffset.x);
            float newZ = position.z + (height * locationOffset.y);
            float newY = position.y; // TODO implement height checks
            Vector3 newPos = new Vector3 (newX, newY, newZ);
            if (!allSpikes.Contains (newPos))
            {
                float currentDistance = Vector3.Distance (newPos, centerLoc) + spikeRadius;
                if (currentDistance > areaRadius)
                {
                    float layerNum = (float) Math.Floor ((areaRadius - spikeRadius) / (spikeRadius * 2));
                    return (layerNum != 0) ? (areaRadius - spikeRadius) / (2 * layerNum) : areaRadius;
                }
                else
                {
                    radius = GenerateSpikes (newPos, centerLoc, height, spikeRadius, areaRadius);
                }
            }
        }
        return radius;
    }

    private void CancelAbility ()
    {
        if (WallIsActive ())
        {
            playerEnergy.UseEnergy (firstHandHeld);
            ResetWallInfo ();
        }
        else
        {
            playerEnergy.CancelEnergyUsage (hand);
            if (RockIsActive ())
            {
                RemoveRockFromHand ();
            }
            else if (SpikeQuicksandIsActive ())
            {
                Destroy (spikeQuicksandOutline);
                spikeQuicksandOutline = null;
            }
            else if (WallOutlineIsActive ())
            {
                Destroy (wallOutline);
                ResetWallInfo ();
            }
        }
    }

    private void EnterDrawMode ()
    {
        if (firstHandHeld != null && firstHandHeld != hand)
        {
            wallOutline = Instantiate (wallOutlinePrefab) as GameObject;
            SetWallLocation ();
            firstHandHeld = null;
        }
        else
        {
            firstHandHeld = hand;
        }
    }

    private void RemoveRockFromHand ()
    {
        GetComponent<SpawnAndAttachToHand> ().hand.DetachObject (rock);
        rockSize = rockStartSize;
        rock = null;
    }

    private void ResetWallInfo ()
    {
        firstHandHeld = null;
        wallOutline = null;
        wall = null;
        lastAngle = 0;
        currentWallHeight = 0;
    }

    private Vector3 GetWallPosition ()
    {
        Vector3 thisArcPos = arc.GetEndPosition ();
        Vector3 otherArcPos = otherArc.GetEndPosition ();
        float wallPosX = (thisArcPos.x + otherArcPos.x) / 2;
        float wallPosY = Math.Min (thisArcPos.y, otherArcPos.y);
        float wallPosZ = (thisArcPos.z + otherArcPos.z) / 2;
        return new Vector3 (wallPosX, wallPosY, wallPosZ);
    }

    private void SetWallLocation ()
    {
        Vector3 wallPosition = GetWallPosition ();
        wallOutline.transform.position = new Vector3 (wallPosition.x, wallPosition.y, wallPosition.z);

        float remainingEnergy = playerEnergy.GetRemainingEnergy ();
        float maxHeight = remainingEnergy / (arc.GetEndPointsDistance (otherArc) * WALL_SIZE_MULTIPLIER);
        float area = arc.GetEndPointsDistance (otherArc) * maxHeight;
        area = (float) Math.Round (area, 2) * WALL_SIZE_MULTIPLIER;
        if (maxHeight <= 1f)
        {
            wallOutline.transform.localScale = new Vector3 (remainingEnergy / WALL_SIZE_MULTIPLIER, 1f, 0.1f);
        }
        else if (arc.GetEndPointsDistance (otherArc) <= 1f)
        {
            wallOutline.transform.localScale = new Vector3 (1f, remainingEnergy / WALL_SIZE_MULTIPLIER, 0.1f);
        }
        else
        {
            wallOutline.transform.localScale = new Vector3 (arc.GetEndPointsDistance (otherArc), maxHeight, 0.1f);
        }

        float angle = Vector3.SignedAngle (arc.GetEndPosition () - otherArc.GetEndPosition (), wallOutline.transform.position, new Vector3 (0, -1, 0));
        angle += Vector3.SignedAngle (wallOutline.transform.position, new Vector3 (1, 0, 0), new Vector3 (0, -1, 0));
        float newAngle = angle;
        angle -= lastAngle;
        if (Math.Abs (angle) >= 0.5f)
        {
            lastAngle = newAngle;
            wallOutline.transform.Rotate (0, angle, 0, Space.Self);
        }
    }

    private bool WallIsValid ()
    {
        OutlineProperties properties = wallOutline.GetComponentInChildren<OutlineProperties> ();
        return (arc.CanUseAbility () &&
            otherArc.CanUseAbility () &&
            !properties.CollisionDetected () &&
            Vector3.Distance (player.transform.position, wallOutline.transform.position) >= ROCK_CREATE_DIST);
    }

    private bool SpikeQuicksandIsValid ()
    {
        OutlineProperties properties = spikeQuicksandOutline.GetComponentInChildren<OutlineProperties> ();
        return (arc.CanUseAbility () &&
            !properties.CollisionDetected ());
    }

    private bool RockIsActive ()
    {
        return rock != null;
    }

    private bool SpikeQuicksandIsActive ()
    {
        return spikeQuicksandOutline != null;
    }

    private bool WallIsActive ()
    {
        return wall != null;
    }

    private bool WallOutlineIsActive ()
    {
        return wallOutline != null;
    }

    private void SetOutlineMaterial (GameObject outlineObject, bool valid)
    {
        if (valid)
        {
            outlineObject.GetComponentInChildren<SkinnedMeshRenderer> ().material = validOutlineMat;
        }
        else
        {
            outlineObject.GetComponentInChildren<SkinnedMeshRenderer> ().material = invalidOutlineMat;
        }
    }
}