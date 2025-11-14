using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

/// <summary>
/// Centralized controller for the Tag environment.
/// Manages episode timing and coordinates all agents.
/// </summary>
public class TagEnvironmentController : MonoBehaviour
{
    [Header("Episode Settings")]
    [Tooltip("Use deterministic episode length (mean value) or dynamic (sampled from normal distribution)")]
    [SerializeField] private bool useDeterministicEpisodeLength = false;

    [Tooltip("Mean episode length in steps")]
    [SerializeField] private float episodeLengthMean = 1000f;

    [Tooltip("Standard deviation for episode length (only used when dynamic mode is enabled)")]
    [SerializeField] private float episodeLengthStd = 200f;

    [Tooltip("Minimum episode length to prevent too short episodes (only used when dynamic mode is enabled)")]
    [SerializeField] private int minEpisodeLength = 500;

    [Header("Agent References")]
    [Tooltip("All predator agents in the environment")]
    [SerializeField] private List<PredatorAgent> predatorAgents = new List<PredatorAgent>();

    [Tooltip("All prey agents in the environment")]
    [SerializeField] private List<PreyAgent> preyAgents = new List<PreyAgent>();

    [Header("Spawn Area Reference")]
    [Tooltip("Reference to spawn area manager")]
    [SerializeField] private SpawnArea spawnArea;

    private int currentStep;
    private int targetEpisodeLength;

    private void Awake()
    {
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (predatorAgents == null || predatorAgents.Count == 0)
        {
            Debug.LogWarning("TagEnvironmentController: No predator agents assigned!", this);
        }

        if (preyAgents == null || preyAgents.Count == 0)
        {
            Debug.LogWarning("TagEnvironmentController: No prey agents assigned!", this);
        }

        if (spawnArea == null)
        {
            Debug.LogWarning("TagEnvironmentController: Spawn area not assigned!", this);
        }
    }

    private void Start()
    {
        ResetEnvironment();
    }

    private void FixedUpdate()
    {
        currentStep++;

        if (currentStep >= targetEpisodeLength)
        {
            EndEpisode();
        }
    }

    /// <summary>
    /// Sample episode length from normal distribution or return deterministic value.
    /// </summary>
    private int SampleEpisodeLength()
    {
        if (useDeterministicEpisodeLength)
        {
            // Use deterministic episode length (mean value)
            return Mathf.RoundToInt(episodeLengthMean);
        }
        else
        {
            // Sample from normal distribution
            // Box-Muller transform for normal distribution
            float u1 = Random.value;
            float u2 = Random.value;
            float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
            float sampledLength = episodeLengthMean + episodeLengthStd * randStdNormal;

            // Clamp to minimum episode length
            return Mathf.Max(minEpisodeLength, Mathf.RoundToInt(sampledLength));
        }
    }

    /// <summary>
    /// Reset the environment for a new episode.
    /// </summary>
    public void ResetEnvironment()
    {
        currentStep = 0;
        targetEpisodeLength = SampleEpisodeLength();

        // Reset all agents
        foreach (var predator in predatorAgents)
        {
            if (predator != null)
            {
                predator.OnEpisodeBegin();
            }
        }

        foreach (var prey in preyAgents)
        {
            if (prey != null)
            {
                prey.OnEpisodeBegin();
            }
        }
    }

    /// <summary>
    /// End the current episode for all agents due to max steps reached.
    /// Uses EpisodeInterrupted() to signal truncation rather than termination.
    /// </summary>
    public void EndEpisode()
    {
        // Interrupt episode for all agents (truncation, not termination)
        foreach (var predator in predatorAgents)
        {
            if (predator != null)
            {
                predator.EpisodeInterrupted();
            }
        }

        foreach (var prey in preyAgents)
        {
            if (prey != null)
            {
                prey.EpisodeInterrupted();
            }
        }

        // Reset for next episode
        ResetEnvironment();
    }

    /// <summary>
    /// Get the spawn area reference for agents.
    /// </summary>
    public SpawnArea GetSpawnArea()
    {
        return spawnArea;
    }

    /// <summary>
    /// Get current progress through episode (0 to 1).
    /// </summary>
    public float GetEpisodeProgress()
    {
        return (float)currentStep / targetEpisodeLength;
    }

    /// <summary>
    /// Get remaining steps in current episode.
    /// </summary>
    public int GetRemainingSteps()
    {
        return targetEpisodeLength - currentStep;
    }
}
