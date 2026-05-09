using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class which manages the game.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    [Tooltip("The player gameobject")]
    public GameObject player = null;

    [Header("Scores")]
    [Tooltip("The player's score")]
    [SerializeField] private int gameManagerScore = 0;

    public static int score
    {
        get
        {
            return instance.gameManagerScore;
        }
        set
        {
            instance.gameManagerScore = value;
        }
    }

    [Tooltip("The highest score achieved on this device")]
    public int highScore = 0;

    [Header("Game Progress / Victory Settings")]
    [Tooltip("Whether the game is winnable or not \nDefault: true")]
    public bool gameIsWinnable = true;

    [Tooltip("The number of enemies that must be defeated to win the game")]
    public int enemiesToDefeat = 10;

    private int enemiesDefeated = 0;

    public int EnemiesDefeated
    {
        get
        {
            return enemiesDefeated;
        }
    }

    public int EnemiesRemainingToWin
    {
        get
        {
            return Mathf.Max(0, enemiesToDefeat - enemiesDefeated);
        }
    }

    public int EnemiesToDefeatCount
    {
        get
        {
            return enemiesToDefeat;
        }
    }

    [Tooltip("Whether or not to print debug statements about the level's winnable status at start up")]
    public bool printDebugOfWinnableStatus = true;

    [Tooltip("Page index in the UIManager to go to on winning the game")]
    public int gameVictoryPageIndex = 0;

    [Tooltip("The effect to create upon winning the game")]
    public GameObject victoryEffect = null;

    [Tooltip("The sound to play when the level is won")]
    public AudioClip victorySound = null;

    [Tooltip("The volume of the victory sound")]
    [Range(0f, 1f)] public float victorySoundVolume = 1f;

    private int numberOfEnemiesFoundAtStart = 0;

    [Header("Game Over Settings")]
    [Tooltip("The index in the UI manager of the game over page")]
    public int gameOverPageIndex = 0;

    [Tooltip("The game over effect to create when the game is lost")]
    public GameObject gameOverEffect = null;

    [Tooltip("The sound to play when the player loses")]
    public AudioClip gameOverSound = null;

    [Tooltip("The volume of the game over sound")]
    [Range(0f, 1f)] public float gameOverSoundVolume = 1f;

    [HideInInspector] public bool gameIsOver = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            DestroyImmediate(this);
        }

        if (player == null && FindObjectOfType<Controller>() != null)
        {
            player = FindObjectOfType<Controller>().gameObject;
        }
        else if (player == null && SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.Log("Player is not set and cannot find it in the scene. This is not a problem in non-playable scenes, such as the Main Menu.");
        }
    }

    private void Start()
    {
        HandleStartUp();
    }

    private void HandleStartUp()
    {
        gameIsOver = false;
        enemiesDefeated = 0;

        if (PlayerPrefs.HasKey("highscore"))
        {
            highScore = PlayerPrefs.GetInt("highscore");
        }

        score = 0;
        UpdateUIElements();

        if (printDebugOfWinnableStatus)
        {
            FigureOutHowManyEnemiesExist();
        }
    }

    private void FigureOutHowManyEnemiesExist()
    {
        List<EnemySpawner> enemySpawners = FindObjectsOfType<EnemySpawner>().ToList();
        List<Enemy> staticEnemies = FindObjectsOfType<Enemy>().ToList();

        int numberOfInfiniteSpawners = 0;
        int enemiesFromSpawners = 0;
        int enemiesFromStatic = staticEnemies.Count;

        foreach (EnemySpawner enemySpawner in enemySpawners)
        {
            if (enemySpawner.spawnInfinite)
            {
                numberOfInfiniteSpawners += 1;
            }
            else
            {
                enemiesFromSpawners += enemySpawner.maxSpawn;
            }
        }

        numberOfEnemiesFoundAtStart = enemiesFromSpawners + enemiesFromStatic;

        if (!gameIsWinnable)
        {
            return;
        }

        if (numberOfInfiniteSpawners > 0)
        {
            Debug.Log("There are " + numberOfInfiniteSpawners + " infinite spawners so the level will always be winnable,\nhowever you should still playtest for timely completion");
        }
        else if (enemiesToDefeat > numberOfEnemiesFoundAtStart)
        {
            Debug.LogWarning("There are " + enemiesToDefeat + " enemies to defeat but only " + numberOfEnemiesFoundAtStart + " enemies found at start.\nThe level cannot be completed!");
        }
        else
        {
            Debug.Log("There are " + enemiesToDefeat + " enemies to defeat and " + numberOfEnemiesFoundAtStart + " enemies found at start.\nThe level can be completed.");
        }
    }

    public void IncrementEnemiesDefeated()
    {
        enemiesDefeated++;
        UpdateUIElements();

        if (enemiesDefeated >= enemiesToDefeat && gameIsWinnable)
        {
            LevelCleared();
        }
    }

    private void OnApplicationQuit()
    {
        SaveHighScore();
        ResetScore();
    }

    public static void AddScore(int scoreAmount)
    {
        score += scoreAmount;

        if (score > instance.highScore)
        {
            SaveHighScore();
        }

        UpdateUIElements();
    }

    public static void ResetScore()
    {
        PlayerPrefs.SetInt("score", 0);
        score = 0;
    }

    public static void SaveHighScore()
    {
        if (score > instance.highScore)
        {
            PlayerPrefs.SetInt("highscore", score);
            instance.highScore = score;
        }

        UpdateUIElements();
    }

    public static void ResetHighScore()
    {
        PlayerPrefs.SetInt("highscore", 0);

        if (instance != null)
        {
            instance.highScore = 0;
        }

        UpdateUIElements();
    }

    public static void UpdateUIElements()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateUI();
        }
    }

    public void LevelCleared()
    {
        PlayerPrefs.SetInt("score", score);
        PlayClip(victorySound, victorySoundVolume);

        if (UIManager.instance != null)
        {
            if (player != null)
            {
                player.SetActive(false);
            }

            UIManager.instance.allowPause = false;
            UIManager.instance.GoToPage(gameVictoryPageIndex);

            if (victoryEffect != null)
            {
                Instantiate(victoryEffect, transform.position, transform.rotation, null);
            }
        }
    }

    public void GameOver()
    {
        gameIsOver = true;
        PlayClip(gameOverSound, gameOverSoundVolume);

        if (gameOverEffect != null)
        {
            Instantiate(gameOverEffect, transform.position, transform.rotation, null);
        }

        if (UIManager.instance != null)
        {
            UIManager.instance.allowPause = false;
            UIManager.instance.GoToPage(gameOverPageIndex);
        }
    }

    private void PlayClip(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        Vector3 soundPosition = transform.position;
        if (Camera.main != null)
        {
            soundPosition = Camera.main.transform.position;
        }

        AudioSource.PlayClipAtPoint(clip, soundPosition, volume);
    }
}
