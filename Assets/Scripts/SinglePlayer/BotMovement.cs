using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class BotMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;

    [Header("Bot Settings")]
    public float movementSpeed = 3.5f;
    public float guardRadius = 5f;
    public float timelineBufferTime = 3f;
    private bool trackingPlayer = false;
    private bool guardingPearl = false;
    private Vector3 guardPosition;
    private bool isPatrolling = false;
    public float separationRadius = 3f;
    public float pathDiversionTime = 4f;
    private float proximityTimer = 0f;
    private bool isFrozen = false; // New flag to track freeze state

    [Header("Invisibility Settings")]
    private InvisibilityOffline invisibilityScript;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = movementSpeed;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogWarning("Player not found. Ensure there is a GameObject tagged 'Player' in the scene.");
        }

        // Access the InvisibilityOffline script
        invisibilityScript = GetComponent<InvisibilityOffline>();
        if (invisibilityScript == null)
        {
            Debug.LogError("InvisibilityOffline script not found on the bot.");
        }

        StartCoroutine(TimelineBuffer(timelineBufferTime));
    }

    private void Update()
    {
        // Skip updates if the bot is frozen
        if (isFrozen)
        {
            return;
        }

        if (guardingPearl)
        {
            if (!isPatrolling)
            {
                StartCoroutine(PatrolAroundPearl());
            }
            else if (trackingPlayer && player != null)
            {
                if (Random.value < 0.3f)
                {
                    agent.SetDestination(player.position);
                }
            }

            // Check for proximity with other antagonists
            CheckAndDiversifyPath();

            // Trigger invisibility if guarding a pearl and not on cooldown
            if (invisibilityScript != null)
            {
                invisibilityScript.ActivateInvisibility();
            }
        }
        else if (trackingPlayer && player != null)
        {
            agent.SetDestination(player.position);
        }
    }

    private IEnumerator TimelineBuffer(float bufferDuration)
    {
        agent.isStopped = true;
        yield return new WaitForSeconds(bufferDuration);
        agent.isStopped = false;
        trackingPlayer = true;
    }

    public void StartGuarding(Vector3 pearlPosition)
    {
        guardingPearl = true;
        guardPosition = pearlPosition;
        isPatrolling = false;
    }

    private IEnumerator PatrolAroundPearl()
    {
        isPatrolling = true;

        while (guardingPearl)
        {
            // Pause if the bot is frozen
            if (isFrozen)
            {
                yield return null;
                continue;
            }

            Vector3 patrolPoint = guardPosition + Random.insideUnitSphere * guardRadius;
            patrolPoint.y = guardPosition.y;
            agent.SetDestination(patrolPoint);

            yield return new WaitForSeconds(Random.Range(3f, 5f));
        }

        isPatrolling = false;
    }

    private void CheckAndDiversifyPath()
    {
        Collider[] nearbyAntagonists = Physics.OverlapSphere(transform.position, separationRadius);

        foreach (Collider col in nearbyAntagonists)
        {
            BotMovement otherBot = col.GetComponent<BotMovement>();
            if (otherBot != null && otherBot != this && otherBot.guardingPearl)
            {
                proximityTimer += Time.deltaTime;
                if (proximityTimer > pathDiversionTime)
                {
                    Vector3 newPatrolPoint = guardPosition + Random.insideUnitSphere * guardRadius * 1.5f;
                    newPatrolPoint.y = guardPosition.y;
                    agent.SetDestination(newPatrolPoint);
                    proximityTimer = 0f;
                }
                return;
            }
        }
        proximityTimer = 0f;
    }

    // Method to freeze the bot
    public void FreezeBot()
    {
        if (agent != null)
        {
            agent.isStopped = true; // Stop the agent's pathfinding
            agent.velocity = Vector3.zero; // Ensure the agent's current movement stops immediately
        }
        isFrozen = true;

        // Debug message to confirm FreezeBot is called
        Debug.Log("Bot frozen: " + gameObject.name);
    }

    // Method to unfreeze the bot
    public void UnfreezeBot()
    {
        if (agent != null)
        {
            agent.isStopped = false; // Resume the agent's pathfinding
        }
        isFrozen = false;

        // Debug message to confirm UnfreezeBot is called
        Debug.Log("Bot unfrozen: " + gameObject.name);
    }
}