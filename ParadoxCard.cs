using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Paradox Card", menuName = "Paradox Assembly/Paradox Card")]
public class ParadoxCard : Card
{
    [Header("Paradox Properties")]
    public ParadoxType paradoxType;
    public float paradoxPower = 1f; // Base power level of the paradox
    public float entropyGeneration = 2f; // How much entropy this generates
    public bool requiresObservation = false; // For quantum paradoxes
    public bool persistsThroughLoop = false; // Survives time loops

    public enum ParadoxType
    {
        Quantum,     // State is undefined until observed
        Temporal,    // Affects time/turns
        Logical,     // Creates rule contradictions
        Spatial,     // Affects card positions/zones
        Causal      // Affects cause/effect relationships
    }

    [Header("Paradox Effects")]
    public List<ParadoxEffect> paradoxEffects = new List<ParadoxEffect>();
    public List<ParadoxTrigger> paradoxTriggers = new List<ParadoxTrigger>();

    [System.Serializable]
    public class ParadoxEffect
    {
        public string effectName;
        public EffectType type;
        public float baseValue;
        public bool appliesBeforeObservation;
        public bool appliesAfterObservation;

        public enum EffectType
        {
            Damage,
            Healing,
            Draw,
            Discard,
            EntropyModifier,
            StateChange
        }
    }

    [System.Serializable]
    public class ParadoxTrigger
    {
        public TriggerType type;
        public string triggerCondition;
        public float triggerChance = 1f;

        public enum TriggerType
        {
            OnDraw,
            OnPlay,
            OnDiscard,
            OnObservation,
            OnLawChange,
            OnParadoxResolution
        }
    }

    private bool isQuantumCollapsed = false;

    public override void OnPlay(GameStateSnapshot gameState)
    {
        base.OnPlay(gameState);

        // Generate entropy
        if (gameState != null)
        {
            gameState.entropyMeterValue += entropyGeneration;
        }

        // Handle quantum effects
        if (requiresObservation && !isQuantumCollapsed)
        {
            HandleQuantumState(gameState);
        }
        else
        {
            ApplyParadoxEffects(gameState);
        }

        // Check for paradox triggers
        CheckTriggers(TriggerType.OnPlay, gameState);
    }

    private void HandleQuantumState(GameStateSnapshot gameState)
    {
        if (isObserved)
        {
            isQuantumCollapsed = true;
            Debug.Log($"Quantum state collapsed for {cardName}");
            ApplyParadoxEffects(gameState);
        }
        else
        {
            // Apply pre-observation effects only
            ApplyPreObservationEffects(gameState);
        }
    }

    private void ApplyParadoxEffects(GameStateSnapshot gameState)
    {
        if (gameState == null) return;

        foreach (var effect in paradoxEffects)
        {
            if ((!requiresObservation || isQuantumCollapsed) && effect.appliesAfterObservation)
            {
                ApplyEffect(effect, gameState);
            }
        }
    }

    private void ApplyPreObservationEffects(GameStateSnapshot gameState)
    {
        if (gameState == null) return;

        foreach (var effect in paradoxEffects)
        {
            if (effect.appliesBeforeObservation)
            {
                ApplyEffect(effect, gameState);
            }
        }
    }

    private void ApplyEffect(ParadoxEffect effect, GameStateSnapshot gameState)
    {
        float modifiedValue = effect.baseValue;

        // Apply law modifications
        foreach (var law in gameState.activeLaws)
        {
            if (law.ModifiesCard(this))
            {
                modifiedValue = law.GetModifiedValue(modifiedValue, LawCard.LawEffect.EffectTarget.ParadoxCards);
            }
        }

        // Apply the effect based on type
        switch (effect.type)
        {
            case ParadoxEffect.EffectType.Damage:
                // Implementation for damage effect
                Debug.Log($"Applying {modifiedValue} paradox damage");
                break;
            case ParadoxEffect.EffectType.EntropyModifier:
                gameState.entropyMeterValue += modifiedValue;
                break;
            // Implement other effect types
        }
    }

    private void CheckTriggers(TriggerType triggerType, GameStateSnapshot gameState)
    {
        foreach (var trigger in paradoxTriggers)
        {
            if (trigger.type == triggerType && Random.value <= trigger.triggerChance)
            {
                HandleTrigger(trigger, gameState);
            }
        }
    }

    private void HandleTrigger(ParadoxTrigger trigger, GameStateSnapshot gameState)
    {
        Debug.Log($"Paradox trigger activated: {trigger.triggerCondition}");
        // Implement trigger-specific logic
    }

    public override void OnDraw(GameStateSnapshot gameState)
    {
        base.OnDraw(gameState);
        CheckTriggers(TriggerType.OnDraw, gameState);
    }

    public override void OnDiscard(GameStateSnapshot gameState)
    {
        base.OnDiscard(gameState);
        CheckTriggers(TriggerType.OnDiscard, gameState);
    }

    public override bool CanBePlayed(GameStateSnapshot gameState)
    {
        if (!base.CanBePlayed(gameState)) return false;

        // Check entropy threshold
        if (gameState != null && gameState.entropyMeterValue >= 100f)
        {
            return false; // Too much entropy to safely play paradox cards
        }

        return true;
    }
} 