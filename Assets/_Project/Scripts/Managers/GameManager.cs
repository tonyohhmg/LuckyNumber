using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Document Containers (GameObjects)")]
    [SerializeField] private GameObject mainMenuContainer;
    [SerializeField] private GameObject pauseMenuContainer;
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private GameObject gameWonContainer;

    [Header("Gameplay Scripts to Toggle")]
    [SerializeField] private MonoBehaviour playerControllerScript;

    private bool isPaused = false;
    private bool isInMainMenu = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        InitializeGameState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InitializeGameState();
    }

    private void InitializeGameState()
    {
        HideAllUIOverlays();

        if (mainMenuContainer != null)
        {
            mainMenuContainer.SetActive(true);
            isInMainMenu = true;
            Time.timeScale = 0f;
            if (playerControllerScript != null) playerControllerScript.enabled = false;
            
            BindMainMenuButtons();
            EnableUIContainerInput(mainMenuContainer, true);
        }
        else
        {
            isInMainMenu = false;
            Time.timeScale = 1f;
            if (playerControllerScript != null) playerControllerScript.enabled = true;
        }

        isPaused = false;
    }

    private void Update()
    {
        if (isInMainMenu) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- UI Helpers ---
    private void HideAllUIOverlays()
    {
        if (mainMenuContainer != null) { mainMenuContainer.SetActive(false); EnableUIContainerInput(mainMenuContainer, false); }
        if (pauseMenuContainer != null) { pauseMenuContainer.SetActive(false); EnableUIContainerInput(pauseMenuContainer, false); }
        if (gameOverContainer != null) { gameOverContainer.SetActive(false); EnableUIContainerInput(gameOverContainer, false); }
        if (gameWonContainer != null) { gameWonContainer.SetActive(false); EnableUIContainerInput(gameWonContainer, false); }
    }

    private void EnableUIContainerInput(GameObject container, bool enable)
    {
        var doc = container.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            doc.rootVisualElement.pickingMode = enable ? PickingMode.Position : PickingMode.Ignore;
        }
    }

    // --- Button Binding Methods ---
    private void BindMainMenuButtons()
    {
        if (mainMenuContainer == null) return;
        var doc = mainMenuContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var playBtn = doc.rootVisualElement.Q<Button>("PlayButton");
            if (playBtn != null)
            {
                playBtn.clicked -= StartGame;
                playBtn.clicked += StartGame;
            }
        }
    }

    private void BindPauseMenuButtons()
    {
        if (pauseMenuContainer == null) return;
        var doc = pauseMenuContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var resumeBtn = doc.rootVisualElement.Q<Button>("ResumeButton");
            var restartBtn = doc.rootVisualElement.Q<Button>("RestartButton");
            var menuBtn = doc.rootVisualElement.Q<Button>("MenuButton");

            if (resumeBtn != null) { resumeBtn.clicked -= ResumeGame; resumeBtn.clicked += ResumeGame; }
            if (restartBtn != null) { restartBtn.clicked -= RestartLevel; restartBtn.clicked += RestartLevel; }
            if (menuBtn != null) { menuBtn.clicked -= GoToMainMenu; menuBtn.clicked += GoToMainMenu; }
        }
    }

    private void BindGameOverButtons()
    {
        if (gameOverContainer == null) return;
        var doc = gameOverContainer.GetComponent<UIDocument>();
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

    private void BindGameWonButtons()
    {
        if (gameWonContainer == null) return;
        var doc = gameWonContainer.GetComponent<UIDocument>();
        if (doc != null && doc.rootVisualElement != null)
        {
            var restartBtn = doc.rootVisualElement.Q<Button>("GameWonRestartButton");
            if (restartBtn != null) 
            { 
                restartBtn.clicked -= RestartLevel; 
                restartBtn.clicked += RestartLevel; 
            }

            var nextBtn = doc.rootVisualElement.Q<Button>("NextLevelButton");
            if (nextBtn != null) 
            { 
                nextBtn.clicked -= LoadNextLevel; 
                nextBtn.clicked += LoadNextLevel; 
            }
        }
    }

    // --- Game State Actions ---
    public void StartGame()
    {
        isInMainMenu = false;
        if (mainMenuContainer != null) 
        {
            mainMenuContainer.SetActive(false);
            EnableUIContainerInput(mainMenuContainer, false);
        }

        Time.timeScale = 1f;
        if (playerControllerScript != null) playerControllerScript.enabled = true;
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pauseMenuContainer != null)
        {
            pauseMenuContainer.SetActive(true);
            BindPauseMenuButtons();
            EnableUIContainerInput(pauseMenuContainer, true);
        }
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pauseMenuContainer != null) 
        {
            pauseMenuContainer.SetActive(false);
            EnableUIContainerInput(pauseMenuContainer, false);
        }
        Time.timeScale = 1f;
    }

    public void TriggerGameOver()
    {
        if (gameOverContainer != null)
        {
            gameOverContainer.SetActive(true);
            BindGameOverButtons();
            EnableUIContainerInput(gameOverContainer, true);
        }
        Time.timeScale = 0f;
    }

    public void CompleteLevel()
    {
        if (gameWonContainer != null)
        {
            gameWonContainer.SetActive(true);
            BindGameWonButtons();
            EnableUIContainerInput(gameWonContainer, true);
        }
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        Debug.Log("[GameManager] Executing Soft Reset (Restart Level)...");

        // 1. Unfreeze time and clear pause/menu flags immediately
        Time.timeScale = 1f;
        isPaused = false;
        isInMainMenu = false;

        // 2. Force-hide all UI overlays and disable their click inputs
        HideAllUIOverlays();

        // 3. Reset the player's soul mass back to baseline (1.0g)
        SoulMassSystem massSystem = FindAnyObjectByType<SoulMassSystem>();
        if (massSystem != null)
        {
            massSystem.ResetToBaseline();
        }

        // 4. Soft Reset: Teleport player back to the "Respawn" point instead of reloading the scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Find the spawn point object tagged "Respawn"
            GameObject spawnPoint = GameObject.FindWithTag("Respawn");
            Vector3 resetPos = spawnPoint != null ? spawnPoint.transform.position : new Vector3(0f, 1f, 0f);

            // Safely move the player (handles CharacterController if attached)
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            player.transform.position = resetPos;

            if (cc != null) cc.enabled = true;

            // Ensure movement script is active
            if (playerControllerScript != null) playerControllerScript.enabled = true;

            Debug.Log($"[GameManager] Player successfully soft-reset to position: {resetPos}");
        }
        else
        {
            Debug.LogWarning("[GameManager] 'Player' tag not found on player object! Falling back to scene reload.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        isInMainMenu = true;

        HideAllUIOverlays();

        if (mainMenuContainer != null) 
        {
            mainMenuContainer.SetActive(true);
            BindMainMenuButtons();
            EnableUIContainerInput(mainMenuContainer, true);
        }
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        HideAllUIOverlays();

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            GoToMainMenu();
        }
    }
}