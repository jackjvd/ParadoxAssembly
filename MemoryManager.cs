using UnityEngine;
using System.Collections.Generic;
using System;

public class MemoryManager : MonoBehaviour
{
    public static MemoryManager Instance { get; private set; }

    [Header("Memory Settings")]
    public int maxMemorySlots = 5;
    public float memoryEntropyReduction = 2f; // Entropy reduction for remembered cards
    public bool allowDuplicateMemories = false;

    [Header("Runtime Properties")]
    private List<MemorySlot> memorySlots = new List<MemorySlot>();
    private Dictionary<string, int> cardPlayHistory = new Dictionary<string, int>();

    public event Action<Card> OnCardMemorized;
    public event Action<Card> OnCardForgotten;
    public event Action<MemorySlot> OnMemorySlotUpdated;

    [System.Serializable]
    public class MemorySlot
    {
        public Card card;
        public int turnsRemembered;
        public bool isPermanent;
        public float entropyModifier = 1f;
        public Dictionary<string, object> memoryData = new Dictionary<string, object>();
    }

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

    public bool CanMemorizeCard(Card card)
    {
        if (!allowDuplicateMemories && IsCardMemorized(card))
        {
            return false;
        }

        return memorySlots.Count < maxMemorySlots;
    }

    public bool MemorizeCard(Card card, bool isPermanent = false)
    {
        if (!CanMemorizeCard(card))
        {
            return false;
        }

        var slot = new MemorySlot
        {
            card = card,
            turnsRemembered = 0,
            isPermanent = isPermanent,
            entropyModifier = 1f
        };

        // Store additional data based on card type
        if (card is ParadoxCard paradoxCard)
        {
            slot.memoryData["quantumState"] = paradoxCard.isObserved;
            slot.entropyModifier = 1.5f; // Paradox cards are more entropy-intensive to remember
        }
        else if (card is LawCard lawCard)
        {
            slot.memoryData["lawPriority"] = lawCard.lawPriority;
            slot.entropyModifier = 0.8f; // Laws are easier to remember
        }

        memorySlots.Add(slot);
        OnCardMemorized?.Invoke(card);
        OnMemorySlotUpdated?.Invoke(slot);

        // Track card play history
        if (!cardPlayHistory.ContainsKey(card.cardName))
        {
            cardPlayHistory[card.cardName] = 0;
        }
        cardPlayHistory[card.cardName]++;

        return true;
    }

    public void ForgetCard(Card card)
    {
        var slot = memorySlots.Find(s => s.card == card && !s.isPermanent);
        if (slot != null)
        {
            memorySlots.Remove(slot);
            OnCardForgotten?.Invoke(card);
        }
    }

    public void UpdateMemories()
    {
        for (int i = memorySlots.Count - 1; i >= 0; i--)
        {
            var slot = memorySlots[i];
            if (!slot.isPermanent)
            {
                slot.turnsRemembered++;
                OnMemorySlotUpdated?.Invoke(slot);

                // Check if memory should fade
                if (ShouldMemoryFade(slot))
                {
                    ForgetCard(slot.card);
                }
            }
        }
    }

    private bool ShouldMemoryFade(MemorySlot slot)
    {
        // Implement memory fade logic based on turns remembered and card type
        if (slot.isPermanent) return false;

        // Base fade chance increases with turns
        float fadeChance = slot.turnsRemembered * 0.1f;

        // Modify based on card type and play history
        if (slot.card is ParadoxCard)
        {
            fadeChance *= 1.5f; // Paradoxes are harder to remember
        }
        else if (slot.card is LawCard)
        {
            fadeChance *= 0.7f; // Laws are easier to remember
        }

        // Frequently played cards are easier to remember
        if (cardPlayHistory.TryGetValue(slot.card.cardName, out int playCount))
        {
            fadeChance *= Mathf.Max(0.5f, 1f - (playCount * 0.1f));
        }

        return UnityEngine.Random.value < fadeChance;
    }

    public bool IsCardMemorized(Card card)
    {
        return memorySlots.Exists(s => s.card == card);
    }

    public MemorySlot GetMemorySlot(Card card)
    {
        return memorySlots.Find(s => s.card == card);
    }

    public List<Card> GetMemorizedCards()
    {
        List<Card> cards = new List<Card>();
        foreach (var slot in memorySlots)
        {
            cards.Add(slot.card);
        }
        return cards;
    }

    public void OnChronoLoopStart()
    {
        // Update memory slots for the new loop
        foreach (var slot in memorySlots)
        {
            if (!slot.isPermanent)
            {
                // Reduce entropy cost for remembered cards
                var gameState = FindObjectOfType<GameStateSnapshot>();
                if (gameState != null)
                {
                    gameState.entropyMeterValue -= memoryEntropyReduction * slot.entropyModifier;
                }
            }
        }
    }

    public void OnChronoLoopEnd()
    {
        // Clean up temporary memories
        for (int i = memorySlots.Count - 1; i >= 0; i--)
        {
            var slot = memorySlots[i];
            if (!slot.isPermanent && !ShouldPreserveMemory(slot))
            {
                ForgetCard(slot.card);
            }
        }
    }

    private bool ShouldPreserveMemory(MemorySlot slot)
    {
        if (slot.isPermanent) return true;

        // Check card-specific preservation rules
        if (slot.card is ParadoxCard paradoxCard)
        {
            return paradoxCard.persistsThroughLoop;
        }
        else if (slot.card is LawCard lawCard)
        {
            // Laws with higher priority are more likely to be preserved
            return lawCard.lawPriority > 0 && UnityEngine.Random.value < (0.5f + lawCard.lawPriority * 0.1f);
        }

        return false;
    }

    public string GetMemoryStatus()
    {
        string status = $"Memory Slots: {memorySlots.Count}/{maxMemorySlots}\n";
        foreach (var slot in memorySlots)
        {
            status += $"- {slot.card.cardName} (Turns: {slot.turnsRemembered}, {(slot.isPermanent ? "Permanent" : "Temporary")})\n";
        }
        return status;
    }

    public float GetTotalEntropyModification()
    {
        float totalMod = 0f;
        foreach (var slot in memorySlots)
        {
            totalMod -= memoryEntropyReduction * slot.entropyModifier;
        }
        return totalMod;
    }
} 