using UnityEngine;
using System.Collections.Generic;
using System;

public class ChronoLoopManager : MonoBehaviour
{
    public static ChronoLoopManager Instance { get; private set; }

    [Header("Chrono Loop Settings")]
    public int maxMemoryCards = 3;
    public int maxLoopsPerGame = 3;
    public float entropyPenaltyPerLoop = 10f;
    public bool preservePlayerHealth = false;
    public bool preserveEnergy = false;

    [Header("Runtime Properties")]
    public int remainingLoops;
    public bool isInLoop = false;
    private int loopStartTurn;
    private GameStateSnapshot preLoopState;
    private List<Card> memorizedCards = new List<Card>();

    public event Action<int> OnLoopStart;
    public event Action<int> OnLoopEnd;
    public event Action<Card> OnCardMemorized;

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

        remainingLoops = maxLoopsPerGame;
    }

    public bool CanInitiateLoop()
    {
        return remainingLoops > 0 && !isInLoop && TurnManager.Instance.CurrentTurn > 1;
    }

    public bool InitiateChronoLoop()
    {
        if (!CanInitiateLoop())
        {
            Debug.LogWarning("Cannot initiate Chrono Loop!");
            return false;
        }

        // Store current state before loop
        preLoopState = FindObjectOfType<GameStateSnapshot>().Clone();
        loopStartTurn = TurnManager.Instance.CurrentTurn;
        isInLoop = true;
        remainingLoops--;

        // Notify subscribers
        OnLoopStart?.Invoke(loopStartTurn);

        // Apply entropy penalty
        preLoopState.entropyMeterValue += entropyPenaltyPerLoop;

        // Rewind to start of turn
        RewindToTurn(loopStartTurn);

        return true;
    }

    public void EndChronoLoop()
    {
        if (!isInLoop) return;

        isInLoop = false;
        OnLoopEnd?.Invoke(loopStartTurn);

        // Clear memorized cards that weren't preserved
        memorizedCards.RemoveAll(card => !ShouldPreserveCard(card));
    }

    public bool MemorizeCard(Card card)
    {
        if (memorizedCards.Count >= maxMemoryCards)
        {
            Debug.LogWarning("Maximum memory cards reached!");
            return false;
        }

        memorizedCards.Add(card);
        OnCardMemorized?.Invoke(card);
        return true;
    }

    public void ForgetCard(Card card)
    {
        memorizedCards.Remove(card);
    }

    private void RewindToTurn(int targetTurn)
    {
        var turnManager = TurnManager.Instance;
        var targetState = turnManager.GetTurnSnapshot(targetTurn);

        if (targetState == null)
        {
            Debug.LogError($"Could not find state for turn {targetTurn}");
            return;
        }

        // Preserve certain aspects if configured
        if (preservePlayerHealth)
        {
            targetState.playerHealth = preLoopState.playerHealth;
        }
        if (preserveEnergy)
        {
            targetState.playerMana = preLoopState.playerMana;
        }

        // Apply memorized cards
        foreach (var card in memorizedCards)
        {
            if (ShouldPreserveCard(card))
            {
                targetState.memoryZone.Add(card);
            }
        }

        // Update game state
        ApplyGameState(targetState);

        // Restart turn
        turnManager.StartNewTurn();
    }

    private bool ShouldPreserveCard(Card card)
    {
        // Check if card should persist through loop
        if (card is ParadoxCard paradoxCard)
        {
            return paradoxCard.persistsThroughLoop;
        }
        return true;
    }

    private void ApplyGameState(GameStateSnapshot newState)
    {
        var currentState = FindObjectOfType<GameStateSnapshot>();
        if (currentState != null)
        {
            // Copy state properties
            currentState.playerHealth = newState.playerHealth;
            currentState.enemyHealth = newState.enemyHealth;
            currentState.playerMana = newState.playerMana;
            currentState.entropyLevel = newState.entropyLevel;
            currentState.entropyMeterValue = newState.entropyMeterValue;

            // Clear and copy lists
            currentState.activeLaws.Clear();
            currentState.activeLaws.AddRange(newState.activeLaws);

            currentState.playerHand.Clear();
            currentState.playerHand.AddRange(newState.playerHand);

            currentState.playerDeck.Clear();
            currentState.playerDeck.AddRange(newState.playerDeck);

            currentState.playerDiscard.Clear();
            currentState.playerDiscard.AddRange(newState.playerDiscard);

            currentState.memoryZone.Clear();
            currentState.memoryZone.AddRange(newState.memoryZone);

            // Handle paradox-specific state
            currentState.paradoxFlags.Clear();
            foreach (var kvp in newState.paradoxFlags)
            {
                currentState.paradoxFlags[kvp.Key] = kvp.Value;
            }

            // Update enemy state
            currentState.enemyIntentions.Clear();
            currentState.enemyIntentions.AddRange(newState.enemyIntentions);

            currentState.enemyStatus.Clear();
            foreach (var kvp in newState.enemyStatus)
            {
                currentState.enemyStatus[kvp.Key] = kvp.Value;
            }
        }
    }

    public List<Card> GetMemorizedCards()
    {
        return new List<Card>(memorizedCards);
    }

    public bool IsCardMemorized(Card card)
    {
        return memorizedCards.Contains(card);
    }

    public float GetLoopEntropyPenalty()
    {
        return entropyPenaltyPerLoop * (maxLoopsPerGame - remainingLoops);
    }

    // Get a preview of what will be preserved in the loop
    public string GetLoopPreview()
    {
        string preview = "Chrono Loop Preview:\n";
        preview += $"Remaining Loops: {remainingLoops}/{maxLoopsPerGame}\n";
        preview += $"Entropy Penalty: {entropyPenaltyPerLoop}\n";
        preview += "\nMemorized Cards:\n";
        
        foreach (var card in memorizedCards)
        {
            preview += $"- {card.cardName} ({(ShouldPreserveCard(card) ? "Will Persist" : "Will Be Lost")})\n";
        }

        return preview;
    }
} 