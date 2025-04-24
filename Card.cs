using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Card", menuName = "Paradox Assembly/Card")]
public abstract class Card : ScriptableObject
{
    [Header("Basic Card Info")]
    public string cardName;
    public string description;
    public int manaCost;
    public Sprite cardArt;
    
    [Header("Card Properties")]
    public CardType cardType;
    public CardRarity rarity;
    public bool isPlayable = true;

    [Header("Runtime Properties")]
    [HideInInspector] public bool isObserved = false; // For quantum effects
    [HideInInspector] public int turnPlayed = -1;
    [HideInInspector] public int uniqueInstanceId; // For tracking specific card instances

    public enum CardType
    {
        Law,
        Paradox,
        Action
    }

    public enum CardRarity
    {
        Common,
        Uncommon,
        Rare,
        Mythic,
        Paradoxical
    }

    // Virtual methods to be overridden by specific card types
    public virtual void OnPlay(GameStateSnapshot gameState)
    {
        turnPlayed = TurnManager.Instance.CurrentTurn;
        Debug.Log($"Playing card: {cardName}");
    }

    public virtual void OnDiscard(GameStateSnapshot gameState)
    {
        Debug.Log($"Discarding card: {cardName}");
    }

    public virtual void OnDraw(GameStateSnapshot gameState)
    {
        Debug.Log($"Drawing card: {cardName}");
    }

    public virtual bool CanBePlayed(GameStateSnapshot gameState)
    {
        return isPlayable;
    }

    // Deep copy method for creating card instances
    public virtual Card CreateInstance()
    {
        Card copy = Instantiate(this);
        copy.uniqueInstanceId = Guid.NewGuid().GetHashCode();
        return copy;
    }

    // Method to get modified card text based on active laws
    public virtual string GetModifiedDescription(GameStateSnapshot gameState)
    {
        return description;
    }

    // Utility method to check if card is affected by a specific law
    public virtual bool IsAffectedByLaw(LawCard law)
    {
        return true; // By default, all cards are affected by laws
    }
} 