using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/// <summary>
/// Predator agent in a predator-prey environment.
/// Slower but aims to catch prey for rewards.
/// </summary>
public class PredatorAgent : Agent
{
    [Header("Movement Settings")]
    [Tooltip("Speed of agent rotation")]
    public float turnSpeed = 150f;

    [Tooltip("Force applied for movement")]
    public float moveSpeed = 1.5f;

    [Header("References")]
    [Tooltip("Reference to the environment controller")]
    public TagEnvironmentController environmentController;

    private Rigidbody m_AgentRb;
    private SpawnArea m_SpawnArea;

    /// <summary>
    /// Initialize the agent.
    /// </summary>
    public override void Initialize()
    {
        m_AgentRb = GetComponent<Rigidbody>();

        if (environmentController == null)
        {
            Debug.LogError("PredatorAgent: Environment controller reference not set!", this);
        }
        else
        {
            m_SpawnArea = environmentController.GetSpawnArea();
            if (m_SpawnArea == null)
            {
                Debug.LogError("PredatorAgent: Spawn area not found in environment controller!", this);
            }
        }
    }

    /// <summary>
    /// Collect vector observations for the agent.
    /// Provides local velocity information and agent type.
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Add local velocity (helps agent understand its current movement)
        var localVelocity = transform.InverseTransformDirection(m_AgentRb.linearVelocity);
        sensor.AddObservation(localVelocity.x);
        sensor.AddObservation(localVelocity.z);

        // Add agent type: 1.0 = predator, 0.0 = prey
        sensor.AddObservation(1f);
    }

    /// <summary>
    /// Execute actions from the neural network.
    /// Uses discrete action space: [forward/backward, turn left/right]
    /// </summary>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;

        var discreteActions = actionBuffers.DiscreteActions;

        // Action 0: Forward (0=nothing, 1=forward, 2=backward)
        var forwardAction = discreteActions[0];
        if (forwardAction == 1)
        {
            dirToGo = transform.forward;
        }
        else if (forwardAction == 2)
        {
            dirToGo = -transform.forward;
        }

        // Action 1: Rotation (0=nothing, 1=turn left, 2=turn right)
        var rotateAction = discreteActions[1];
        if (rotateAction == 1)
        {
            rotateDir = -transform.up; // Turn left
        }
        else if (rotateAction == 2)
        {
            rotateDir = transform.up; // Turn right
        }

        // Apply movement
        m_AgentRb.AddForce(dirToGo * moveSpeed, ForceMode.VelocityChange);
        transform.Rotate(rotateDir, Time.fixedDeltaTime * turnSpeed);
    }

    /// <summary>
    /// Heuristic for manual control (WASD keys).
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Forward/Backward
        if (Input.GetKey(KeyCode.W))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            discreteActionsOut[0] = 2;
        }
        else
        {
            discreteActionsOut[0] = 0;
        }

        // Turn Left/Right
        if (Input.GetKey(KeyCode.A))
        {
            discreteActionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            discreteActionsOut[1] = 2;
        }
        else
        {
            discreteActionsOut[1] = 0;
        }
    }

    /// <summary>
    /// Reset agent at the start of each episode.
    /// Called by TagEnvironmentController.
    /// </summary>
    public override void OnEpisodeBegin()
    {
        // Reset physics
        m_AgentRb.linearVelocity = Vector3.zero;
        m_AgentRb.angularVelocity = Vector3.zero;

        // Spawn at random position
        if (m_SpawnArea != null)
        {
            m_SpawnArea.PlacePredator(gameObject);
        }
    }

    /// <summary>
    /// Handle collisions with prey.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("prey"))
        {
            // Reward for catching prey
            AddReward(1f);

            // Notify prey that it was caught
            var preyAgent = collision.gameObject.GetComponent<PreyAgent>();
            if (preyAgent != null)
            {
                preyAgent.OnCaughtByPredator();
            }
        }
    }
}
