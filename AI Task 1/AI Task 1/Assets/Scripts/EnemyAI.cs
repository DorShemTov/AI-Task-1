using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Search, Die }

    public State currentState = State.Patrol;
    public float sightRange = 10f;
    public float sightAngle = 90f;
    public float hearingRange = 5f;
    public float attackRange = 2f;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float searchDuration = 10f;
    public float health = 100f;

    private NavMeshAgent agent;
    private Transform player;
    private Vector3 lastKnownPosition;
    private float searchStartTime;

    public float attackCooldown = 1f;
    private float lastAttackTime = -Mathf.Infinity;

    public GameObject[] patrolPointObjects;
    private int currentPatrolIndex = 0;


    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(StateMachine());

        // Set initial destination
        if (patrolPointObjects.Length > 0)
        {
            agent.SetDestination(patrolPointObjects[0].transform.position);
        }
        else
        {
            Debug.LogWarning("No patrol points set for " + gameObject.name);
        }
    }

    IEnumerator StateMachine()
    {
        while (true)
        {
            yield return StartCoroutine(currentState.ToString());
        }
    }

    IEnumerator Patrol()
    {
        if (patrolPointObjects == null || patrolPointObjects.Length == 0)
        {
            Debug.LogWarning("No patrol points set for " + gameObject.name);
            yield break;
        }

        agent.speed = patrolSpeed;

        while (currentState == State.Patrol)
        {
            if (agent.remainingDistance < 0.1f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPointObjects.Length;
                agent.SetDestination(patrolPointObjects[currentPatrolIndex].transform.position);
            }

            if (CanSeePlayer() || CanHearPlayer())
            {
                currentState = State.Chase;
            }

            yield return null;
        }
    }

    IEnumerator Chase()
    {
        agent.speed = chaseSpeed;
        while (currentState == State.Chase)
        {
            agent.SetDestination(player.position);
            lastKnownPosition = player.position;

            if (Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                currentState = State.Attack;
            }
            else if (!CanSeePlayer() && !CanHearPlayer())
            {
                currentState = State.Search;
                searchStartTime = Time.time;
            }

            yield return null;
        }
    }


    IEnumerator Attack()
    {
        while (currentState == State.Attack)
        {
            agent.SetDestination(transform.position);

            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // Perform attack
                Debug.Log("Enemy attacks player!");
                lastAttackTime = Time.time;
            }

            if (Vector3.Distance(transform.position, player.position) > attackRange)
            {
                currentState = State.Chase;
            }

            yield return null;
        }
    }

    IEnumerator Search()
    {
        agent.SetDestination(lastKnownPosition);

        while (currentState == State.Search)
        {
            if (agent.remainingDistance < 0.1f)
            {
                yield return StartCoroutine(LookAround());
            }

            if (CanSeePlayer() || CanHearPlayer())
            {
                currentState = State.Chase;
            }
            else if (Time.time - searchStartTime > searchDuration)
            {
                currentState = State.Patrol;
            }

            yield return null;
        }
    }

    IEnumerator LookAround()
    {
        for (int i = 0; i < 4; i++)
        {
            transform.Rotate(0, 90, 0);
            yield return new WaitForSeconds(1);
            if (CanSeePlayer()) break;
        }
    }

    IEnumerator Die()
    {
        agent.isStopped = true;
        // Play death animation
        Debug.Log("Enemy dies!");
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            currentState = State.Die;
        }
    }

    bool CanSeePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) <= sightRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToPlayer);

            if (angle <= sightAngle / 2)
            {
                RaycastHit hit;
                if (Physics.Raycast(transform.position, directionToPlayer, out hit, sightRange))
                {
                    if (hit.transform == player)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    bool CanHearPlayer()
    {
        return Vector3.Distance(transform.position, player.position) <= hearingRange;
    }
}