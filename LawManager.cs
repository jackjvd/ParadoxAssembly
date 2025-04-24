using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LawManager : MonoBehaviour
{
    public static LawManager Instance { get; private set; }

    [Header("Law Settings")]
    public int maxActiveLaws = 5;
    public float maxEntropyThreshold = 100f;
    public float lawStackingPenalty = 1.2f; // Entropy multiplier for each active law

    [Header("Runtime Properties")]
    public List<LawCard> activeLaws = new List<LawCard>();
    private Dictionary<string, float> lawModifiers = new Dictionary<string, float>();
    private List<string> conflictingLawPairs = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool RegisterLaw(LawCard law)
    {
        if (activeLaws.Count >= maxActiveLaws)
        {
            Debug.LogWarning("Maximum number of active laws reached");
            return false;
        }

        if (WouldCauseParadox(law))
        {
            Debug.LogWarning("Law would cause unstable paradox");
            return false;
        }

        activeLaws.Add(law);
        UpdateLawModifiers();
        CheckForConflicts();

        // Notify all relevant systems of the new law
        NotifyLawChange(law);

        return true;
    }

    public void RemoveLaw(LawCard law)
    {
        if (activeLaws.Contains(law))
        {
            activeLaws.Remove(law);
            UpdateLawModifiers();
            CheckForConflicts();
            NotifyLawChange(law, true);
        }
    }

    private bool WouldCauseParadox(LawCard newLaw)
    {
        // Check for direct contradictions
        foreach (var activeLaw in activeLaws)
        {
            if (AreDirectlyContradictory(newLaw, activeLaw))
            {
                return true;
            }
        }

        // Check for entropy overflow
        float projectedEntropy = CalculateProjectedEntropy(newLaw);
        return projectedEntropy > maxEntropyThreshold;
    }

    private bool AreDirectlyContradictory(LawCard law1, LawCard law2)
    {
        // Implementation for checking direct contradictions
        // This would need to be expanded based on specific law interactions
        string conflictKey = $"{law1.cardName}_{law2.cardName}";
        return conflictingLawPairs.Contains(conflictKey);
    }

    private float CalculateProjectedEntropy(LawCard newLaw)
    {
        float currentEntropy = 0f;
        foreach (var law in activeLaws)
        {
            currentEntropy += law.entropyContribution;
        }

        // Apply stacking penalty
        float stackingMultiplier = Mathf.Pow(lawStackingPenalty, activeLaws.Count);
        return currentEntropy * stackingMultiplier + newLaw.entropyContribution;
    }

    private void UpdateLawModifiers()
    {
        lawModifiers.Clear();

        // Sort laws by priority
        var sortedLaws = activeLaws.OrderBy(l => l.lawPriority).ToList();

        foreach (var law in sortedLaws)
        {
            foreach (var effect in law.lawEffects)
            {
                string effectKey = $"{effect.effectName}_{effect.target}";
                if (!lawModifiers.ContainsKey(effectKey))
                {
                    lawModifiers[effectKey] = 1f;
                }

                // Apply law effect modifiers
                switch (effect.effectType)
                {
                    case LawCard.LawEffect.EffectType.Multiply:
                        lawModifiers[effectKey] *= effect.modifierValue;
                        break;
                    case LawCard.LawEffect.EffectType.Add:
                        lawModifiers[effectKey] += effect.modifierValue;
                        break;
                    case LawCard.LawEffect.EffectType.Set:
                        lawModifiers[effectKey] = effect.modifierValue;
                        break;
                    case LawCard.LawEffect.EffectType.Invert:
                        lawModifiers[effectKey] *= -1;
                        break;
                }
            }
        }
    }

    private void CheckForConflicts()
    {
        // Reset conflicts
        conflictingLawPairs.Clear();

        // Check each law pair for conflicts
        for (int i = 0; i < activeLaws.Count; i++)
        {
            for (int j = i + 1; j < activeLaws.Count; j++)
            {
                if (AreDirectlyContradictory(activeLaws[i], activeLaws[j]))
                {
                    string conflictKey = $"{activeLaws[i].cardName}_{activeLaws[j].cardName}";
                    conflictingLawPairs.Add(conflictKey);
                }
            }
        }
    }

    private void NotifyLawChange(LawCard law, bool isRemoval = false)
    {
        // Notify all relevant game systems about the law change
        var gameState = FindObjectOfType<GameStateSnapshot>();
        if (gameState != null)
        {
            // Update game state
            if (!isRemoval)
            {
                gameState.entropyMeterValue += law.entropyContribution;
            }
        }

        // Trigger any paradox effects that react to law changes
        foreach (var activeLaw in activeLaws)
        {
            foreach (var effect in activeLaw.lawEffects)
            {
                if (effect.target == LawCard.LawEffect.EffectTarget.OtherLaws)
                {
                    // Apply law interaction effects
                }
            }
        }
    }

    // Get the current modifier for a specific effect type and target
    public float GetModifier(string effectName, LawCard.LawEffect.EffectTarget target)
    {
        string key = $"{effectName}_{target}";
        return lawModifiers.ContainsKey(key) ? lawModifiers[key] : 1f;
    }

    // Check if the current law configuration is stable
    public bool IsLawConfigurationStable()
    {
        return conflictingLawPairs.Count == 0 && 
               CalculateTotalEntropy() <= maxEntropyThreshold;
    }

    private float CalculateTotalEntropy()
    {
        float totalEntropy = 0f;
        float stackingMultiplier = 1f;

        foreach (var law in activeLaws)
        {
            totalEntropy += law.entropyContribution * stackingMultiplier;
            stackingMultiplier *= lawStackingPenalty;
        }

        return totalEntropy;
    }

    // Get a summary of all active laws and their effects
    public string GetLawSummary()
    {
        string summary = "Active Laws:\n";
        foreach (var law in activeLaws)
        {
            summary += $"- {law.cardName}: {law.description}\n";
        }
        summary += $"\nTotal Entropy: {CalculateTotalEntropy():F1}/{maxEntropyThreshold}";
        return summary;
    }
} 