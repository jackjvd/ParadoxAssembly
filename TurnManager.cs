using UnityEngine;
using System.Collections.Generic;
using System;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    public int startingHandSize = 5;
    public int maxHandSize = 10;
    public int cardsPerTurn = 1;
    public int startingEnergy = 3;
    public float turnTimeLimit = 60f; // 0 for no limit

    [Header("Runtime Properties")]
    public int CurrentTurn { get; private set; }
    public TurnPhase CurrentPhase { get; private set; }
    public bool IsPlayerTurn { get; private set; }
    private float turnTimer;
    private bool isTurnActive;

    public event Action<int> OnTurnStart;
    public event Action<int> OnTurnEnd;
    public event Action<TurnPhase> OnPhaseChange;

    private Queue<GameAction> actionQueue = new Queue<GameAction>();
    private List<GameStateSnapshot> turnHistory = new List<GameStateSnapshot>();

    public enum TurnPhase
    {
        TurnStart,
        DrawPhase,
        MainPhase,
        EndPhase,
        EnemyTurn,
        Cleanup
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

        CurrentTurn = 0;
        IsPlayerTurn = true;
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (isTurnActive && turnTimeLimit > 0)
        {
            turnTimer -= Time.deltaTime;
            if (turnTimer <= 0)
            {
                EndCurrentPhase();
            }
        }

        // Process queued actions
        while (actionQueue.Count > 0 && !IsWaitingForAction())
        {
            ProcessNextAction();
        }
    }

    public void StartGame()
    {
        CurrentTurn = 1;
        IsPlayerTurn = true;
        StartNewTurn();
    }

    public void StartNewTurn()
    {
        CurrentPhase = TurnPhase.TurnStart;
        isTurnActive = true;
        turnTimer = turnTimeLimit;

        // Create turn start snapshot
        SaveGameState();

        // Notify subscribers
        OnTurnStart?.Invoke(CurrentTurn);

        // Queue turn start actions
        QueueAction(new GameAction
        {
            actionType = GameAction.ActionType.PhaseTransition,
            phase = TurnPhase.DrawPhase
        });
    }

    public void EndCurrentPhase()
    {
        switch (CurrentPhase)
        {
            case TurnPhase.TurnStart:
                TransitionToPhase(TurnPhase.DrawPhase);
                break;
            case TurnPhase.DrawPhase:
                TransitionToPhase(TurnPhase.MainPhase);
                break;
            case TurnPhase.MainPhase:
                TransitionToPhase(TurnPhase.EndPhase);
                break;
            case TurnPhase.EndPhase:
                if (IsPlayerTurn)
                {
                    TransitionToPhase(TurnPhase.EnemyTurn);
                }
                else
                {
                    TransitionToPhase(TurnPhase.Cleanup);
                }
                break;
            case TurnPhase.EnemyTurn:
                TransitionToPhase(TurnPhase.Cleanup);
                break;
            case TurnPhase.Cleanup:
                EndTurn();
                break;
        }
    }

    private void TransitionToPhase(TurnPhase newPhase)
    {
        CurrentPhase = newPhase;
        OnPhaseChange?.Invoke(newPhase);

        switch (newPhase)
        {
            case TurnPhase.DrawPhase:
                HandleDrawPhase();
                break;
            case TurnPhase.MainPhase:
                HandleMainPhase();
                break;
            case TurnPhase.EndPhase:
                HandleEndPhase();
                break;
            case TurnPhase.EnemyTurn:
                HandleEnemyTurn();
                break;
            case TurnPhase.Cleanup:
                HandleCleanup();
                break;
        }
    }

    private void HandleDrawPhase()
    {
        // Queue draw actions
        for (int i = 0; i < cardsPerTurn; i++)
        {
            QueueAction(new GameAction
            {
                actionType = GameAction.ActionType.DrawCard
            });
        }

        // Automatically proceed to main phase after drawing
        QueueAction(new GameAction
        {
            actionType = GameAction.ActionType.PhaseTransition,
            phase = TurnPhase.MainPhase
        });
    }

    private void HandleMainPhase()
    {
        // Reset energy for the turn
        var gameState = FindObjectOfType<GameStateSnapshot>();
        if (gameState != null)
        {
            gameState.playerMana = startingEnergy;
        }
    }

    private void HandleEndPhase()
    {
        // Handle end of turn effects
        SaveGameState();
    }

    private void HandleEnemyTurn()
    {
        IsPlayerTurn = false;
        // Queue enemy AI actions
        var enemyAI = FindObjectOfType<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.TakeTurn();
        }
    }

    private void HandleCleanup()
    {
        // Clean up any temporary effects
        // Prepare for next turn
        QueueAction(new GameAction
        {
            actionType = GameAction.ActionType.EndTurn
        });
    }

    private void EndTurn()
    {
        isTurnActive = false;
        SaveGameState();
        OnTurnEnd?.Invoke(CurrentTurn);

        if (!IsPlayerTurn)
        {
            CurrentTurn++;
            IsPlayerTurn = true;
            StartNewTurn();
        }
    }

    public void QueueAction(GameAction action)
    {
        actionQueue.Enqueue(action);
    }

    private void ProcessNextAction()
    {
        if (actionQueue.Count == 0) return;

        var action = actionQueue.Dequeue();
        ExecuteAction(action);
    }

    private void ExecuteAction(GameAction action)
    {
        switch (action.actionType)
        {
            case GameAction.ActionType.PlayCard:
                // Handle card play
                break;
            case GameAction.ActionType.DrawCard:
                // Handle card draw
                break;
            case GameAction.ActionType.PhaseTransition:
                TransitionToPhase(action.phase);
                break;
            case GameAction.ActionType.EndTurn:
                EndTurn();
                break;
        }
    }

    private bool IsWaitingForAction()
    {
        // Check if we're waiting for any animations or player input
        return false;
    }

    private void SaveGameState()
    {
        var currentState = FindObjectOfType<GameStateSnapshot>();
        if (currentState != null)
        {
            turnHistory.Add(currentState.Clone());
        }
    }

    public GameStateSnapshot GetTurnSnapshot(int turn)
    {
        return turnHistory.Find(s => s.currentTurn == turn);
    }

    // Inner class to represent game actions
    public class GameAction
    {
        public ActionType actionType;
        public Card targetCard;
        public TurnPhase phase;
        public object additionalData;

        public enum ActionType
        {
            PlayCard,
            DrawCard,
            PhaseTransition,
            EndTurn
        }
    }
} 