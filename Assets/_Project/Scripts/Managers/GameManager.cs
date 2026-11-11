using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Containers / Documents (Gameplay Overlays)")]
    [SerializeField] private GameObject gameOverUIContainer; 
    [SerializeField] private GameObject gameWonUIContainer;  
    [SerializeField] private GameObject pauseMenuUIContainer;

    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene"; // Exact name of your Main Menu scene

    [Header("Scripts to Enable/Disable")]
    [SerializeField] private MonoBehaviour playerControllerScript; 

    [Header("Lives Settings")]
    [SerializeField] private int maxLives = 3;
    private int currentLives;

    public int CurrentLives => currentLives;
    public event Action<int> OnLivesChanged;

    private bool isPaused = false;
    private bool isLevelWonOrLost = false;
    private bool isSpawning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Initialize lives on fresh boot
        currentLives = maxLives;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isLevelWonOrLost = false;

        // 1. FIND UI containers FIRST so GameManager has the references immediately
        FindGameplayUIContainers();

        // 2. THEN force hide and completely reset all UI containers
        HideAndResetAllUIOverlays();

        // 3. If we loaded into a gameplay scene, start spawn routine & initialize lives
        if (scene.name != mainMenuSceneName)
        {
            // If lives dropped to 0 or uninitialized, ensure they are reset to max when starting fresh
            if (currentLives <= 0)
            {
                ResetLives();
            }
            else
            {
                OnLivesChanged?.Invoke(currentLives);
            }

            SoulMassSystem massSystem = FindAnyObjectByType<SoulMassSystem>();
            if (massSystem != null)
            {
                massSystem.ResetToBaseline();
            }

            GameStartPortalManager portalManager = FindAnyObjectByType<GameStartPortalManager>();
            if (portalManager != null)
            {
                portalManager.TriggerStartSequence();
            }

            StartCoroutine(GameplaySpawnSequenceRoutine());
        }
        else
        {
            // When returning to Main Menu, always reset lives back to 3 so the next run starts fresh!
            ResetLives();
            isSpawning = false;
        }
    }

    private void HideAndResetAllUIOverlays()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverUIContainer != null) 
        {
            var doc = gameOverUIContainer.GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.style.display = DisplayStyle.None;
            }
            gameOverUIContainer.SetActive(false);
        }

        if (gameWonUIContainer != null) 
        {
            var doc = gameWonUIContainer.GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.style.display = DisplayStyle.None;
            }
            gameWonUIContainer.SetActive(false);
        }

        if (pauseMenuUIContainer != null) 
        {
            var doc = pauseMenuUIContainer.GetComponent<UIDocument>();
            if (doc != null && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.style.display = DisplayStyle.None;
            }
            pauseMenuUIContainer.SetActive(false);
        }
    }

    private void FindGameplayUIContainers()
    {
        if (gameOverUIContainer == null)
        {
            GameObject go = GameObject.Find("GameOverContainer");
            if (go != null) gameOverUIContainer = go;
        }
        if (gameWonUIContainer == null)
        {
            GameObject go = GameObject.Find("GameWonContainer");
            if (go != null) gameWonUIContainer = go;
        }
        if (pauseMenuUIContainer == null)
        {
            GameObject go = GameObject.Find("PauseMenuContainer");
            if (go != null) pauseMenuUIContainer = go;
        }
    }

    private void BindPauseMenuButtons()
    {
        if (pauseMenuUIContainer == null) return;
        var doc = pauseMenuUIContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var resumeBtn = doc.rootVisualElement.Q<Button>("ResumeButton");
            var restartBtn = doc.rootVisualElement.Q<Button>("RestartButton");
            var menuBtn = doc.rootVisualElement.Q<Button>("MenuButton");

            if (resumeBtn != null) 
            { 
                resumeBtn.clicked -= ResumeGame; 
                resumeBtn.clicked += ResumeGame; 
                resumeBtn.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
                resumeBtn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (restartBtn != null) 
            { 
                restartBtn.clicked -= RestartLevel; 
                restartBtn.clicked += RestartLevel; 
                restartBtn.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
                restartBtn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
            if (menuBtn != null) 
            { 
                menuBtn.clicked -= GoToMainMenu; 
                menuBtn.clicked += GoToMainMenu; 
                menuBtn.UnregisterCallback<PointerEnterEvent>(OnButtonHover);
                menuBtn.RegisterCallback<PointerEnterEvent>(OnButtonHover);
            }
        }
    }

    private void OnButtonHover(PointerEnterEvent evt)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonHoverSound();
        }
    }

    private void BindGameWonButtons()
    {
        if (gameWonUIContainer == null) return;
        var doc = gameWonUIContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var playAgainBtn = doc.rootVisualElement.Q<Button>("GameWonRestartButton");
            if (playAgainBtn != null) 
            { 
                playAgainBtn.clicked -= GoToMainMenu; 
                playAgainBtn.clicked += GoToMainMenu; 
            }

            var nextBtn = doc.rootVisualElement.Q<Button>("NextLevelButton");
            if (nextBtn != null) 
            { 
                nextBtn.clicked -= LoadNextLevel; 
                nextBtn.clicked += LoadNextLevel; 
            }
        }
    }

    private void BindGameOverButtons()
    {
        if (gameOverUIContainer == null) return;
        var doc = gameOverUIContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var restartBtn = doc.rootVisualElement.Q<Button>("GameOverRestartButton");
            if (restartBtn != null) 
            { 
                restartBtn.clicked -= RestartLevel; 
                restartBtn.clicked += RestartLevel; 
            }
        }
    }

    private IEnumerator GameplaySpawnSequenceRoutine()
    {
        isSpawning = true;

        GameObject portal = GameObject.Find("StartPortal");
        if (portal == null)
        {
            try { portal = GameObject.FindWithTag("StartPortal"); } catch {}
        }

        GameObject ghost = playerControllerScript != null ? playerControllerScript.gameObject : null;

        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = false;
        }

        if (portal != null)
        {
            portal.SetActive(true);
            portal.transform.localScale = Vector3.zero;
            
            float portalDuration = 0.6f;
            float elapsed = 0f;
            while (elapsed < portalDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / portalDuration);
                portal.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
                yield return null;
            }
            portal.transform.localScale = Vector3.one;
        }

        if (ghost != null)
        {
            Vector3 targetPos = ghost.transform.position;
            float riseHeight = 1.5f;
            Vector3 startPos = targetPos - new Vector3(0f, riseHeight, 0f);

            ghost.transform.position = startPos;
            ghost.SetActive(true);

            float ghostDuration = 0.5f;
            float elapsed = 0f;
            while (elapsed < ghostDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / ghostDuration);
                ghost.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }

            ghost.transform.position = targetPos;
        }

        if (playerControllerScript != null)
        {
            playerControllerScript.enabled = true;
        }

        yield return new WaitForSeconds(0.2f);
        isSpawning = false;
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName) return;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuUIContainer != null) 
        {
            pauseMenuUIContainer.SetActive(true);
            BindPauseMenuButtons();
        }
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuUIContainer != null) pauseMenuUIContainer.SetActive(false);
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (isSpawning || isLevelWonOrLost) return;
        isLevelWonOrLost = true;

        if (gameOverUIContainer != null) 
        {
            gameOverUIContainer.SetActive(true);
            BindGameOverButtons();
        }
        Time.timeScale = 0f;
    }

    public void TriggerGameWon()
    {
        if (isSpawning || isLevelWonOrLost) return;
        isLevelWonOrLost = true;

        if (gameWonUIContainer != null) 
        {
            gameWonUIContainer.SetActive(true);
            BindGameWonButtons();
        }
        Time.timeScale = 0f; 
    }

    public void RestartLevel()
    {
        HideAndResetAllUIOverlays();
        SoulMassSystem massSystem = FindAnyObjectByType<SoulMassSystem>();
        if (massSystem != null)
        {
            massSystem.ResetToBaseline();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        ResetLives(); // Always reset lives when returning to main menu
        HideAndResetAllUIOverlays();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadNextLevel()
    {
        HideAndResetAllUIOverlays();
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            GoToMainMenu();
        }
    }

    public void StartNewGame()
    {
        ResetLives();
    }

    public void ResetLives()
    {
        currentLives = maxLives;
        OnLivesChanged?.Invoke(currentLives);
        Debug.Log($"[GameManager] Lives successfully reset to max: {currentLives}");
    }

    public void LoseLife()
    {
        currentLives--;
        currentLives = Mathf.Max(0, currentLives);
        OnLivesChanged?.Invoke(currentLives);

        Debug.Log($"[GameManager] Life lost! Remaining lives: {currentLives}");

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
    }
}