using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class FinderAgent : Agent
{
    public Room OwnerRoom; // Room containing the agent
    public Transform Goal; // Transform of the goal object that agent has to reach
    public Transform Obstacle; // Transform of the obstacle object corresponding to the agent

    public float MovementSpeed = 2f; // Movement speed of the agent
    public float RotationSpeed = 90f; // Rotation speed of the agent (in degrees)

    private Rigidbody _rigidbody; // Rigidbody of the agent
    private float _prevTime; // time of the previous update to correctly calculate time penalty (should be excessive, as agents are updated during the FixedUpdate)
    private float _collisionTime; // field to contain start time of the collision timer (to set periodic penalties for touching the obstacle)

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void AgentReset()
    {
        // reset the velocities of the agent and randomize its position and rotation within the room
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        transform.position = OwnerRoom.transform.position + OwnerRoom.RandomPosition(0f);
        transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // randomize the position of the goal while avoiding colliding starting positions with respect to the agent
        do
        {
            Goal.position = OwnerRoom.transform.position + OwnerRoom.RandomPosition(0f);
        }
        while (Vector3.Distance(transform.position, Goal.position) < 1.1f);

        // random choice between two obstacle generation options (for better obstacle avoidance training):
        // 1. if the distance between the goal and the agent allows it and if this option is chosen by random variable,
        // we position the obstacle somewhere between the agent and the goal to facilitate learning obstacle avoidance while still pursuing goal
        if (Vector3.Distance(transform.position, Goal.position) > 4.9f && Random.value < 0.5f)
        {
            Obstacle.position = OwnerRoom.transform.position + Vector3.Lerp(Vector3.MoveTowards(transform.position, Goal.position, 2.4f),
                Vector3.MoveTowards(Goal.position, transform.position, 2.4f), Random.value);
            Obstacle.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }
        else // 2. randomize the position of the obstacle while avoiding colliding starting positions with respect to the agent and the goal
        {
            do
            {
                Obstacle.position = OwnerRoom.transform.position + OwnerRoom.RandomPosition(0f);
                Obstacle.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }
            while (Vector3.Distance(transform.position, Obstacle.position) <= 2f && Vector3.Distance(Goal.position, Obstacle.position) <= 2f);

        }
    }

    // action selector
    // NOTE: here we don't use observation assignment as we rely purely on the visual observations
    public override void AgentAction(float[] vectorAction, string textAction)
    {
        // here we use discrete actions, 2 branches of 3 values.
        // forward-backward movement
        switch(vectorAction[0])
        {
            case 0f:
                _rigidbody.velocity = new Vector3(0f, 0f, 0f);
                break;
            case 1f:
                _rigidbody.velocity = transform.forward * MovementSpeed;
                break;
            case 2f:
                _rigidbody.velocity = -transform.forward * MovementSpeed;
                break;
            default:
                break;
        }

        // rotation
        switch (vectorAction[1])
        {
            case 0f:
                _rigidbody.angularVelocity = new Vector3(0f, 0f, 0f);
                break;
            case 1f:
                _rigidbody.angularVelocity = new Vector3(0f, -RotationSpeed * Mathf.Deg2Rad, 0f);
                break;
            case 2f:
                _rigidbody.angularVelocity = new Vector3(0f, RotationSpeed * Mathf.Deg2Rad, 0f);
                break;
            default:
                break;
        }

        float delta = Time.time - _prevTime;
        _prevTime = Time.time;  // remember the time of the previous step

        AddReward(-delta * 0.1f); // time penalty

        // small discount to the time penalty, depending on the distance to the goal
        AddReward((OwnerRoom.MaxDistance - Vector3.Distance(transform.position, Goal.position) + 1f) / OwnerRoom.MaxDistance * delta * 0.1f);
    }

    // function that reacts to new collisions
    private void OnCollisionEnter(Collision col)
    {
        // agent might still get a collision update when already marked as Done
        // in this case we should ignore the collision or we can get multiple big penalties for reaching goal or colliding with the deathzone
        if (IsDone())
        {
            return;
        }

        if (col.gameObject.tag == "Obstacle") // if the agent collided with the obstacle
        {
            AddReward(-0.1f); // by tuning this variable we can set how inclined the agent will be to avoid touching the obstacle if it gets it faster to the goal
            _collisionTime = Time.time; // remember when the collision tick started to assign periodic penalties for touching the obstacle
        }
        else if (col.gameObject.tag == "Deathzone") // if the agent fell off from the platform
        {
            AddReward(-10f); 
            Done(); // if we fell off, we need to reset
        }
        else if (col.gameObject.tag == "Goal") // if the agent reached the goal
        {
            AddReward(10f);
            Done(); // if the agent reached its goal, we need to reset
        }
    }

    // if the agent continues to touch some collider
    private void OnCollisionStay(Collision col)
    {
        // if the agent touches the obstacle, we penalize it once per second
        if (col.gameObject.tag == "Obstacle" && Time.time - _collisionTime >= 1f)
        {
            AddReward(-0.1f); // also tune this to propagate obstacle avoidance
            _collisionTime = Time.time;
        }
    }
}
