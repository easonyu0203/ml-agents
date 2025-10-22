using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class Ball3DAgent : Agent
{
    [Header("Specific to Ball3D")]
    public GameObject ball;
    [Tooltip("Whether to use vector observation. This option should be checked " +
        "in 3DBall scene, and unchecked in Visual3DBall scene. ")]
    public bool useVecObs;

    [Header("Discrete Action Settings")]
    [Tooltip("Rotation step size for discrete actions")]
    public float rotationStep = 1f;

    Rigidbody m_BallRb;
    EnvironmentParameters m_ResetParams;

    public override void Initialize()
    {
        m_BallRb = ball.GetComponent<Rigidbody>();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        SetResetParameters();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        if (useVecObs)
        {
            sensor.AddObservation(gameObject.transform.rotation.z);
            sensor.AddObservation(gameObject.transform.rotation.x);
            sensor.AddObservation(ball.transform.position - gameObject.transform.position);
            sensor.AddObservation(m_BallRb.linearVelocity);
        }
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete actions:
        // Branch 0 (Z-axis rotation): 0 = rotate left, 1 = no rotation, 2 = rotate right
        // Branch 1 (X-axis rotation): 0 = rotate backward, 1 = no rotation, 2 = rotate forward

        var discreteActions = actionBuffers.DiscreteActions;

        // Z-axis rotation (left/right tilt)
        int actionZ = discreteActions[0];
        float rotationZ = 0f;
        if (actionZ == 0)
            rotationZ = -rotationStep;  // Rotate left
        else if (actionZ == 2)
            rotationZ = rotationStep;   // Rotate right
        // actionZ == 1 means no rotation

        // X-axis rotation (forward/backward tilt)
        int actionX = discreteActions[1];
        float rotationX = 0f;
        if (actionX == 0)
            rotationX = -rotationStep;  // Rotate backward
        else if (actionX == 2)
            rotationX = rotationStep;   // Rotate forward
        // actionX == 1 means no rotation

        // Apply rotations with bounds checking
        if ((gameObject.transform.rotation.z < 0.25f && rotationZ > 0f) ||
            (gameObject.transform.rotation.z > -0.25f && rotationZ < 0f))
        {
            gameObject.transform.Rotate(new Vector3(0, 0, 1), rotationZ);
        }

        if ((gameObject.transform.rotation.x < 0.25f && rotationX > 0f) ||
            (gameObject.transform.rotation.x > -0.25f && rotationX < 0f))
        {
            gameObject.transform.Rotate(new Vector3(1, 0, 0), rotationX);
        }

        // Check if ball fell off or went too far
        if ((ball.transform.position.y - gameObject.transform.position.y) < -2f ||
            Mathf.Abs(ball.transform.position.x - gameObject.transform.position.x) > 3f ||
            Mathf.Abs(ball.transform.position.z - gameObject.transform.position.z) > 3f)
        {
            SetReward(-1f);
            EndEpisode();
        }
        else
        {
            SetReward(0.1f);
        }
    }

    public override void OnEpisodeBegin()
    {
        gameObject.transform.rotation = new Quaternion(0f, 0f, 0f, 0f);
        gameObject.transform.Rotate(new Vector3(1, 0, 0), Random.Range(-10f, 10f));
        gameObject.transform.Rotate(new Vector3(0, 0, 1), Random.Range(-10f, 10f));
        m_BallRb.linearVelocity = new Vector3(0f, 0f, 0f);
        ball.transform.position = new Vector3(Random.Range(-1.5f, 1.5f), 4f, Random.Range(-1.5f, 1.5f))
            + gameObject.transform.position;
        //Reset the parameters when the Agent is reset.
        SetResetParameters();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Z-axis (Horizontal input)
        float horizontal = Input.GetAxis("Horizontal");
        if (horizontal < -0.1f)
            discreteActionsOut[0] = 0;  // Rotate left
        else if (horizontal > 0.1f)
            discreteActionsOut[0] = 2;  // Rotate right
        else
            discreteActionsOut[0] = 1;  // No rotation

        // X-axis (Vertical input)
        float vertical = Input.GetAxis("Vertical");
        if (vertical < -0.1f)
            discreteActionsOut[1] = 0;  // Rotate backward
        else if (vertical > 0.1f)
            discreteActionsOut[1] = 2;  // Rotate forward
        else
            discreteActionsOut[1] = 1;  // No rotation
    }

    public void SetBall()
    {
        //Set the attributes of the ball by fetching the information from the academy
        m_BallRb.mass = m_ResetParams.GetWithDefault("mass", 1.0f);
        var scale = m_ResetParams.GetWithDefault("scale", 1.0f);
        ball.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void SetResetParameters()
    {
        SetBall();
    }
}
