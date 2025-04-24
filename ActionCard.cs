using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Action Card", menuName = "Paradox Assembly/Action Card")]
public class ActionCard : Card
{
    [Header("Action Properties")]
    public ActionType actionType;
    public List<CardEffect> effects = new List<CardEffect>();
    public bool isReactable = true; // Can be modified by laws/paradoxes
    public float baseEntropyGeneration = 0.5f;

    public enum ActionType
    {
        Attack,
        Defense,
        Utility,
        Combo,
        Meta // Affects other cards/game rules
    }

    [System.Serializable]
    public class CardEffect
    {
        public string effectName;
        public EffectType type;
        public float baseValue;
        public TargetType target;
        public bool isModifiableByLaws = true;

        public enum EffectType
        {
            Damage,
            Block,
            Heal,
            Draw,
            Energy,
            Entropy,
            Special
        }

        public enum TargetType
        {
            Self,
            Enemy,
            AllEnemies,
            All,
            Random
        }
    }

    public override void OnPlay(GameStateSnapshot gameState)
    {
        base.OnPlay(gameState);

        if (gameState != null)
        {
            gameState.entropyMeterValue += CalculateEntropyGeneration(gameState);
        }

        ApplyEffects(gameState);
    }

    private float CalculateEntropyGeneration(GameStateSnapshot gameState)
    {
        float entropy = baseEntropyGeneration;

        // Modify entropy based on active laws
        foreach (var law in gameState.activeLaws)
        {
            if (law.ModifiesCard(this))
            {
                entropy = law.GetModifiedValue(entropy, LawCard.LawEffect.EffectTarget.ActionCards);
            }
        }

        return entropy;
    }

    private void ApplyEffects(GameStateSnapshot gameState)
    {
        if (gameState == null) return;

        foreach (var effect in effects)
        {
            float finalValue = CalculateEffectValue(effect, gameState);
            ApplyEffect(effect, finalValue, gameState);
        }
    }

    private float CalculateEffectValue(CardEffect effect, GameStateSnapshot gameState)
    {
        float modifiedValue = effect.baseValue;

        if (effect.isModifiableByLaws)
        {
            // Apply law modifications
            foreach (var law in gameState.activeLaws)
            {
                if (law.ModifiesCard(this))
                {
                    modifiedValue = law.GetModifiedValue(modifiedValue, LawCard.LawEffect.EffectTarget.ActionCards);
                }
            }
        }

        return modifiedValue;
    }

    private void ApplyEffect(CardEffect effect, float value, GameStateSnapshot gameState)
    {
        switch (effect.type)
        {
            case CardEffect.EffectType.Damage:
                ApplyDamage(value, effect.target, gameState);
                break;
            case CardEffect.EffectType.Block:
                ApplyBlock(value, effect.target, gameState);
                break;
            case CardEffect.EffectType.Heal:
                ApplyHeal(value, effect.target, gameState);
                break;
            case CardEffect.EffectType.Draw:
                ApplyDraw(Mathf.RoundToInt(value), gameState);
                break;
            case CardEffect.EffectType.Energy:
                ApplyEnergyGain(Mathf.RoundToInt(value), gameState);
                break;
            case CardEffect.EffectType.Entropy:
                gameState.entropyMeterValue += value;
                break;
            case CardEffect.EffectType.Special:
                HandleSpecialEffect(effect, value, gameState);
                break;
        }
    }

    private void ApplyDamage(float amount, CardEffect.TargetType target, GameStateSnapshot gameState)
    {
        switch (target)
        {
            case CardEffect.TargetType.Enemy:
                gameState.enemyHealth -= Mathf.RoundToInt(amount);
                Debug.Log($"Dealing {amount} damage to enemy");
                break;
            case CardEffect.TargetType.All:
                // Implementation for AOE damage
                break;
            // Implement other target types
        }
    }

    private void ApplyBlock(float amount, CardEffect.TargetType target, GameStateSnapshot gameState)
    {
        // Implementation for block effect
        Debug.Log($"Applying {amount} block");
    }

    private void ApplyHeal(float amount, CardEffect.TargetType target, GameStateSnapshot gameState)
    {
        switch (target)
        {
            case CardEffect.TargetType.Self:
                gameState.playerHealth += Mathf.RoundToInt(amount);
                Debug.Log($"Healing {amount} to player");
                break;
            // Implement other target types
        }
    }

    private void ApplyDraw(int amount, GameStateSnapshot gameState)
    {
        // Implementation for card draw
        Debug.Log($"Drawing {amount} cards");
    }

    private void ApplyEnergyGain(int amount, GameStateSnapshot gameState)
    {
        gameState.playerMana += amount;
        Debug.Log($"Gaining {amount} energy");
    }

    private void HandleSpecialEffect(CardEffect effect, float value, GameStateSnapshot gameState)
    {
        // Implementation for special/unique effects
        Debug.Log($"Applying special effect: {effect.effectName}");
    }

    public override bool CanBePlayed(GameStateSnapshot gameState)
    {
        if (!base.CanBePlayed(gameState)) return false;

        // Check if player has enough mana
        if (gameState != null && gameState.playerMana < manaCost)
        {
            return false;
        }

        return true;
    }

    // Get a preview of the card's effects considering current laws
    public string GetEffectPreview(GameStateSnapshot gameState)
    {
        string preview = "";
        foreach (var effect in effects)
        {
            float modifiedValue = CalculateEffectValue(effect, gameState);
            preview += $"{effect.effectName}: {modifiedValue}\n";
        }
        return preview;
    }
} 