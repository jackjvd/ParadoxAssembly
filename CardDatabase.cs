using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "CardDatabase", menuName = "Paradox Assembly/Card Database")]
public class CardDatabase : ScriptableObject
{
    public static CardDatabase Instance { get; private set; }

    [Header("Card Collections")]
    public List<LawCard> lawCards = new List<LawCard>();
    public List<ParadoxCard> paradoxCards = new List<ParadoxCard>();
    public List<ActionCard> actionCards = new List<ActionCard>();

    [Header("Card Sets")]
    public List<CardSet> cardSets = new List<CardSet>();

    [System.Serializable]
    public class CardSet
    {
        public string setName;
        public List<Card> cards = new List<Card>();
        public Card.CardRarity minimumRarity;
        public bool isUnlocked = true;
    }

    private Dictionary<string, Card> cardsByName = new Dictionary<string, Card>();
    private Dictionary<string, List<Card>> cardsByTag = new Dictionary<string, List<Card>>();

    public void Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Build lookup dictionaries
        BuildCardLookup();
    }

    private void BuildCardLookup()
    {
        cardsByName.Clear();
        cardsByTag.Clear();

        // Index all cards
        foreach (var card in lawCards)
        {
            IndexCard(card);
        }
        foreach (var card in paradoxCards)
        {
            IndexCard(card);
        }
        foreach (var card in actionCards)
        {
            IndexCard(card);
        }
    }

    private void IndexCard(Card card)
    {
        if (card == null) return;

        // Add to name lookup
        if (!cardsByName.ContainsKey(card.cardName))
        {
            cardsByName[card.cardName] = card;
        }

        // Add to tag lookup (if we implement tags later)
        // This is a placeholder for future tag system
    }

    public Card GetCardByName(string cardName)
    {
        if (cardsByName.TryGetValue(cardName, out Card card))
        {
            return card.CreateInstance();
        }
        return null;
    }

    public List<Card> GetCardsByType(Card.CardType type)
    {
        switch (type)
        {
            case Card.CardType.Law:
                return lawCards.Cast<Card>().ToList();
            case Card.CardType.Paradox:
                return paradoxCards.Cast<Card>().ToList();
            case Card.CardType.Action:
                return actionCards.Cast<Card>().ToList();
            default:
                return new List<Card>();
        }
    }

    public List<Card> GetCardsByRarity(Card.CardRarity rarity)
    {
        var cards = new List<Card>();
        cards.AddRange(lawCards.Where(c => c.rarity == rarity));
        cards.AddRange(paradoxCards.Where(c => c.rarity == rarity));
        cards.AddRange(actionCards.Where(c => c.rarity == rarity));
        return cards;
    }

    public CardSet GetCardSet(string setName)
    {
        return cardSets.Find(set => set.setName == setName);
    }

    public List<Card> GenerateRandomDeck(int size, float lawRatio = 0.2f, float paradoxRatio = 0.3f)
    {
        List<Card> deck = new List<Card>();
        
        int lawCount = Mathf.RoundToInt(size * lawRatio);
        int paradoxCount = Mathf.RoundToInt(size * paradoxRatio);
        int actionCount = size - lawCount - paradoxCount;

        // Add Laws
        deck.AddRange(GetRandomCards(lawCards, lawCount));

        // Add Paradoxes
        deck.AddRange(GetRandomCards(paradoxCards, paradoxCount));

        // Add Actions
        deck.AddRange(GetRandomCards(actionCards, actionCount));

        // Shuffle the deck
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }

        return deck;
    }

    private List<Card> GetRandomCards<T>(List<T> sourceCards, int count) where T : Card
    {
        var cards = new List<Card>();
        var availableCards = new List<T>(sourceCards);

        while (cards.Count < count && availableCards.Count > 0)
        {
            int index = Random.Range(0, availableCards.Count);
            cards.Add(availableCards[index].CreateInstance());
            availableCards.RemoveAt(index);
        }

        return cards;
    }

    public List<Card> GenerateEnemyDeck(string enemyType, int deckSize)
    {
        // Customize deck based on enemy type
        switch (enemyType.ToLower())
        {
            case "lawkeeper":
                return GenerateRandomDeck(deckSize, 0.4f, 0.1f); // More laws
            case "chaosagent":
                return GenerateRandomDeck(deckSize, 0.1f, 0.5f); // More paradoxes
            case "balanced":
                return GenerateRandomDeck(deckSize, 0.2f, 0.3f); // Standard distribution
            default:
                return GenerateRandomDeck(deckSize);
        }
    }

    // Utility method to create example cards for testing
    public void CreateExampleCards()
    {
        // Create Law Cards
        var damageInversion = CreateLawCard(
            "All Damage is Healing",
            "Inverts all damage effects",
            3,
            LawCard.LawType.ValueModifier
        );
        lawCards.Add(damageInversion);

        // Create Paradox Cards
        var schrodinger = CreateParadoxCard(
            "Schrödinger's Touch",
            "Deal 10 damage and heal 10 until observed",
            2,
            ParadoxCard.ParadoxType.Quantum
        );
        paradoxCards.Add(schrodinger);

        // Create Action Cards
        var echoBlast = CreateActionCard(
            "Echo Blast",
            "Deal 6 damage. If a Law was played this turn, double it.",
            2,
            ActionCard.ActionType.Attack
        );
        actionCards.Add(echoBlast);

        BuildCardLookup();
    }

    private LawCard CreateLawCard(string name, string description, int cost, LawCard.LawType type)
    {
        var card = CreateInstance<LawCard>();
        card.cardName = name;
        card.description = description;
        card.manaCost = cost;
        card.lawType = type;
        return card;
    }

    private ParadoxCard CreateParadoxCard(string name, string description, int cost, ParadoxCard.ParadoxType type)
    {
        var card = CreateInstance<ParadoxCard>();
        card.cardName = name;
        card.description = description;
        card.manaCost = cost;
        card.paradoxType = type;
        return card;
    }

    private ActionCard CreateActionCard(string name, string description, int cost, ActionCard.ActionType type)
    {
        var card = CreateInstance<ActionCard>();
        card.cardName = name;
        card.description = description;
        card.manaCost = cost;
        card.actionType = type;
        return card;
    }

    // Validation method to check card database integrity
    public bool ValidateDatabase()
    {
        bool isValid = true;

        // Check for duplicate names
        var allCards = new List<Card>();
        allCards.AddRange(lawCards);
        allCards.AddRange(paradoxCards);
        allCards.AddRange(actionCards);

        var duplicateNames = allCards
            .GroupBy(c => c.cardName)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateNames.Count > 0)
        {
            Debug.LogError($"Found duplicate card names: {string.Join(", ", duplicateNames)}");
            isValid = false;
        }

        // Check for null references
        if (lawCards.Any(c => c == null) || 
            paradoxCards.Any(c => c == null) || 
            actionCards.Any(c => c == null))
        {
            Debug.LogError("Found null card references in database");
            isValid = false;
        }

        // Check card sets
        foreach (var set in cardSets)
        {
            if (string.IsNullOrEmpty(set.setName))
            {
                Debug.LogError("Found card set with empty name");
                isValid = false;
            }

            if (set.cards.Any(c => c == null))
            {
                Debug.LogError($"Found null card reference in set: {set.setName}");
                isValid = false;
            }
        }

        return isValid;
    }
} 