using UnityEngine;
using System.Collections.Generic;
using System;

[Serializable]
public class GameStateSnapshot
{
    public int currentTurn;
    public int playerHealth;
    public int enemyHealth;
    public int playerMana;
    public int entropyLevel;
    public float entropyMeterValue;
    
    // Active effects and states
    public List<LawCard> activeLaws = new List<LawCard>();
    public List<Card> playerHand = new List<Card>();
    public List<Card> playerDeck = new List<Card>();
    public List<Card> playerDiscard = new List<Card>();
    public List<Card> memoryZone = new List<Card>();
    
    // Enemy state
    public List<Card> enemyIntentions = new List<Card>();
    public Dictionary<string, int> enemyStatus = new Dictionary<string, int>();
    
    // Paradox tracking
    public Dictionary<string, bool> paradoxFlags = new Dictionary<string, bool>();
    public List<string> activeEffects = new List<string>();

    // Create a deep copy of the current state
    public GameStateSnapshot Clone()
    {
        GameStateSnapshot clone = new GameStateSnapshot
        {
            currentTurn = this.currentTurn,
            playerHealth = this.playerHealth,
            enemyHealth = this.enemyHealth,
            playerMana = this.playerMana,
            entropyLevel = this.entropyLevel,
            entropyMeterValue = this.entropyMeterValue
        };

        // Deep copy lists
        clone.activeLaws = new List<LawCard>(this.activeLaws);
        clone.playerHand = new List<Card>(this.playerHand);
        clone.playerDeck = new List<Card>(this.playerDeck);
        clone.playerDiscard = new List<Card>(this.playerDiscard);
        clone.memoryZone = new List<Card>(this.memoryZone);
        clone.enemyIntentions = new List<Card>(this.enemyIntentions);
        clone.activeEffects = new List<string>(this.activeEffects);

        // Deep copy dictionaries
        clone.enemyStatus = new Dictionary<string, int>(this.enemyStatus);
        clone.paradoxFlags = new Dictionary<string, bool>(this.paradoxFlags);

        return clone;
    }

    // Compare two states for equality (useful for paradox detection)
    public bool IsEquivalentTo(GameStateSnapshot other)
    {
        if (other == null) return false;

        return currentTurn == other.currentTurn &&
               playerHealth == other.playerHealth &&
               enemyHealth == other.enemyHealth &&
               playerMana == other.playerMana &&
               entropyLevel == other.entropyLevel &&
               activeLaws.Count == other.activeLaws.Count;
    }

    // Check if the current state would cause a paradox
    public bool WouldCauseParadox(Card cardToPlay)
    {
        // Implement paradox detection logic here
        return false;
    }

    // Utility method to get all active effects that modify a specific card
    public List<string> GetActiveModifiersForCard(Card card)
    {
        List<string> modifiers = new List<string>();
        foreach (var law in activeLaws)
        {
            if (law.IsAffectedByLaw(card))
            {
                modifiers.Add(law.cardName);
            }
        }
        return modifiers;
    }
} 