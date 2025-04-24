using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Law Card", menuName = "Paradox Assembly/Law Card")]
public class LawCard : Card
{
    [Header("Law Properties")]
    public LawType lawType;
    public int lawPriority = 0; // Higher priority laws take precedence
    public bool isGlobalEffect = true; // Affects all cards/players
    public float entropyContribution = 1f; // How much this law adds to entropy

    public enum LawType
    {
        ValueModifier,    // Changes numerical values
        RuleRewrite,      // Changes game rules
        StateTransform,   // Changes game state
        ParadoxModifier   // Affects how paradoxes work
    }

    [Header("Law Effects")]
    public List<LawEffect> lawEffects = new List<LawEffect>();

    [System.Serializable]
    public class LawEffect
    {
        public string effectName;
        public string effectDescription;
        public EffectTarget target;
        public EffectType effectType;
        public float modifierValue;

        public enum EffectTarget
        {
            AllCards,
            ActionCards,
            ParadoxCards,
            OtherLaws,
            PlayerStats,
            EnemyStats,
            GameRules
        }

        public enum EffectType
        {
            Multiply,
            Add,
            Set,
            Invert,
            Transform
        }
    }

    public override void OnPlay(GameStateSnapshot gameState)
    {
        base.OnPlay(gameState);
        
        // Register law with the LawManager
        LawManager.Instance.RegisterLaw(this);
        
        // Update entropy meter
        if (gameState != null)
        {
            gameState.entropyMeterValue += entropyContribution;
        }

        ApplyLawEffects(gameState);
    }

    private void ApplyLawEffects(GameStateSnapshot gameState)
    {
        if (gameState == null) return;

        foreach (var effect in lawEffects)
        {
            // Apply each law effect based on its type and target
            switch (effect.effectType)
            {
                case LawEffect.EffectType.Multiply:
                    ApplyMultiplyEffect(effect, gameState);
                    break;
                case LawEffect.EffectType.Invert:
                    ApplyInvertEffect(effect, gameState);
                    break;
                // Add more effect type implementations as needed
            }
        }
    }

    private void ApplyMultiplyEffect(LawEffect effect, GameStateSnapshot gameState)
    {
        // Implementation for multiply effects
        Debug.Log($"Applying multiply effect: {effect.effectName}");
    }

    private void ApplyInvertEffect(LawEffect effect, GameStateSnapshot gameState)
    {
        // Implementation for invert effects
        Debug.Log($"Applying invert effect: {effect.effectName}");
    }

    public override bool CanBePlayed(GameStateSnapshot gameState)
    {
        // Check if this law would create an invalid game state
        if (gameState == null) return false;

        // Check for conflicting laws
        foreach (var activeLaw in gameState.activeLaws)
        {
            if (WouldConflictWith(activeLaw))
            {
                return false;
            }
        }

        return base.CanBePlayed(gameState);
    }

    private bool WouldConflictWith(LawCard otherLaw)
    {
        // Implement law conflict detection logic
        // For now, just prevent duplicate laws
        return this.cardName == otherLaw.cardName;
    }

    // Method to check if this law affects a specific card
    public bool ModifiesCard(Card card)
    {
        foreach (var effect in lawEffects)
        {
            if (effect.target == LawEffect.EffectTarget.AllCards) return true;
            if (effect.target == LawEffect.EffectTarget.ActionCards && card is ActionCard) return true;
            if (effect.target == LawEffect.EffectTarget.ParadoxCards && card is ParadoxCard) return true;
        }
        return false;
    }

    // Get the modified value after applying this law's effects
    public float GetModifiedValue(float originalValue, LawEffect.EffectTarget target)
    {
        float modifiedValue = originalValue;
        foreach (var effect in lawEffects)
        {
            if (effect.target == target)
            {
                switch (effect.effectType)
                {
                    case LawEffect.EffectType.Multiply:
                        modifiedValue *= effect.modifierValue;
                        break;
                    case LawEffect.EffectType.Add:
                        modifiedValue += effect.modifierValue;
                        break;
                    case LawEffect.EffectType.Set:
                        modifiedValue = effect.modifierValue;
                        break;
                    case LawEffect.EffectType.Invert:
                        modifiedValue = -modifiedValue;
                        break;
                }
            }
        }
        return modifiedValue;
    }
} 