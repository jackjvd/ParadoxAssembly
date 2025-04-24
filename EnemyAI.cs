using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float decisionDelay = 0.5f;
    public bool respectsLaws = true;
    public float aggressiveness = 0.6f; // 0-1, higher means more offensive
    public int maxActionsPerTurn = 3;

    [Header("Enemy Stats")]
    public int maxHealth = 100;
    public int currentHealth;
    public int baseEnergy = 3;
    public int currentEnergy;

    [Header("Enemy Deck")]
    public List<Card> deck = new List<Card>();
    public List<Card> hand = new List<Card>();
    public List<Card> discardPile = new List<Card>();
    public int handSize = 5;

    private Queue<Card> plannedActions = new Queue<Card>();
    private Dictionary<string, float> threatAssessment = new Dictionary<string, float>();
    private float entropyThreshold = 75f; // Threshold for considering entropy in decisions

    private void Start()
    {
        currentHealth = maxHealth;
        currentEnergy = baseEnergy;
        InitializeDeck();
    }

    public void TakeTurn()
    {
        // Reset for new turn
        currentEnergy = baseEnergy;
        DrawCards();

        // Plan actions
        PlanActions();

        // Execute planned actions
        ExecutePlannedActions();
    }

    private void InitializeDeck()
    {
        // Implementation: Initialize enemy deck with cards
        // This would be customized based on enemy type
    }

    private void DrawCards()
    {
        while (hand.Count < handSize && (deck.Count > 0 || discardPile.Count > 0))
        {
            if (deck.Count == 0)
            {
                ShuffleDiscardIntoDeck();
            }

            if (deck.Count > 0)
            {
                Card drawnCard = deck[0];
                deck.RemoveAt(0);
                hand.Add(drawnCard);
            }
        }
    }

    private void ShuffleDiscardIntoDeck()
    {
        deck.AddRange(discardPile);
        discardPile.Clear();
        
        // Fisher-Yates shuffle
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    private void PlanActions()
    {
        plannedActions.Clear();
        UpdateThreatAssessment();

        var gameState = FindObjectOfType<GameStateSnapshot>();
        if (gameState == null) return;

        // Sort cards by priority
        var prioritizedCards = hand.OrderByDescending(card => EvaluateCardPriority(card, gameState)).ToList();

        int plannedEnergy = currentEnergy;
        int actionsPlanned = 0;

        foreach (var card in prioritizedCards)
        {
            if (actionsPlanned >= maxActionsPerTurn) break;
            if (card.manaCost > plannedEnergy) continue;

            if (ShouldPlayCard(card, gameState))
            {
                plannedActions.Enqueue(card);
                plannedEnergy -= card.manaCost;
                actionsPlanned++;
            }
        }
    }

    private float EvaluateCardPriority(Card card, GameStateSnapshot gameState)
    {
        float priority = 0f;

        // Base priority by card type
        switch (card.cardType)
        {
            case Card.CardType.Law:
                priority = EvaluateLawPriority(card as LawCard, gameState);
                break;
            case Card.CardType.Paradox:
                priority = EvaluateParadoxPriority(card as ParadoxCard, gameState);
                break;
            case Card.CardType.Action:
                priority = EvaluateActionPriority(card as ActionCard, gameState);
                break;
        }

        // Adjust for current game state
        if (gameState.entropyMeterValue > entropyThreshold)
        {
            priority *= 0.5f; // Reduce priority when entropy is high
        }

        // Consider energy cost
        priority /= (card.manaCost + 1);

        return priority;
    }

    private float EvaluateLawPriority(LawCard law, GameStateSnapshot gameState)
    {
        if (law == null) return 0f;

        float priority = 10f; // Base priority for laws

        // Check if law would benefit current strategy
        if (law.lawType == LawCard.LawType.ValueModifier && aggressiveness > 0.7f)
        {
            priority *= 1.5f;
        }

        // Reduce priority if similar law is active
        foreach (var activeLaw in gameState.activeLaws)
        {
            if (activeLaw.lawType == law.lawType)
            {
                priority *= 0.5f;
            }
        }

        return priority;
    }

    private float EvaluateParadoxPriority(ParadoxCard paradox, GameStateSnapshot gameState)
    {
        if (paradox == null) return 0f;

        float priority = 15f; // Base priority for paradoxes

        // Adjust based on current entropy
        if (gameState.entropyMeterValue < entropyThreshold)
        {
            priority *= 1.5f;
        }
        else
        {
            priority *= 0.3f;
        }

        return priority;
    }

    private float EvaluateActionPriority(ActionCard action, GameStateSnapshot gameState)
    {
        if (action == null) return 0f;

        float priority = 5f; // Base priority for actions

        // Adjust based on action type and current strategy
        switch (action.actionType)
        {
            case ActionCard.ActionType.Attack:
                priority *= aggressiveness;
                break;
            case ActionCard.ActionType.Defense:
                priority *= (1f - aggressiveness);
                break;
        }

        return priority;
    }

    private bool ShouldPlayCard(Card card, GameStateSnapshot gameState)
    {
        // Check if card can be legally played
        if (!card.CanBePlayed(gameState)) return false;

        // Check entropy risk
        if (card is ParadoxCard && gameState.entropyMeterValue > entropyThreshold)
        {
            return Random.value > 0.8f; // Only 20% chance to risk high entropy
        }

        // Check if card would conflict with strategy
        if (card is LawCard law)
        {
            return !WouldConflictWithStrategy(law);
        }

        return true;
    }

    private bool WouldConflictWithStrategy(LawCard law)
    {
        // Check if law would hinder current strategy
        if (aggressiveness > 0.7f && law.lawType == LawCard.LawType.ValueModifier)
        {
            // Don't play laws that might reduce damage output when aggressive
            return true;
        }

        return false;
    }

    private void UpdateThreatAssessment()
    {
        threatAssessment.Clear();
        var gameState = FindObjectOfType<GameStateSnapshot>();
        if (gameState == null) return;

        // Assess various threat factors
        float healthThreat = 1f - (currentHealth / (float)maxHealth);
        float entropyThreat = gameState.entropyMeterValue / 100f;
        float lawThreat = gameState.activeLaws.Count / 5f;

        threatAssessment["Health"] = healthThreat;
        threatAssessment["Entropy"] = entropyThreat;
        threatAssessment["Laws"] = lawThreat;

        // Adjust strategy based on threats
        if (healthThreat > 0.7f)
        {
            aggressiveness = Mathf.Max(0.3f, aggressiveness - 0.2f);
        }
        else if (healthThreat < 0.3f)
        {
            aggressiveness = Mathf.Min(0.9f, aggressiveness + 0.1f);
        }
    }

    private void ExecutePlannedActions()
    {
        while (plannedActions.Count > 0)
        {
            Card action = plannedActions.Dequeue();
            if (currentEnergy >= action.manaCost)
            {
                PlayCard(action);
                currentEnergy -= action.manaCost;
            }
        }
    }

    private void PlayCard(Card card)
    {
        var gameState = FindObjectOfType<GameStateSnapshot>();
        if (gameState == null) return;

        // Execute card effect
        card.OnPlay(gameState);

        // Move card to discard
        hand.Remove(card);
        discardPile.Add(card);

        Debug.Log($"Enemy plays {card.cardName}");
    }

    public void TakeDamage(int amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (currentHealth == 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Implementation: Handle enemy death
        Debug.Log("Enemy defeated!");
        gameObject.SetActive(false);
    }

    public string GetCurrentIntention()
    {
        if (plannedActions.Count == 0) return "Planning...";

        string intention = "Next actions:\n";
        foreach (var action in plannedActions)
        {
            intention += $"- {action.cardName}\n";
        }
        return intention;
    }
} 