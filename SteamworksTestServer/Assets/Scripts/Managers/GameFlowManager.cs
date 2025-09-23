using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Will control the flow of switching scenes and making sure essential objects get
/// moved and preserve refs correctly.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public Scene curScene;

    [Header("Scene Names")]
    public string MainMenuScene;
    public string GameScene;
    public string BetweenGamesScene;

    [Header("Scene Transitions")]
    public Canvas TransitionCanvas;
    public Image fadeImage;
    public const float defaultFadeDuration = 0.2f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
            DontDestroyOnLoad(TransitionCanvas.gameObject);
        }
        else DestroyImmediate(gameObject);
    }

    public void LoadMainMenuScene() => StartCoroutine(FadeAndChangeScene(MainMenuScene));

    public void LoadBetweenGamesScene() => StartCoroutine(FadeAndChangeScene(BetweenGamesScene));

    public void LoadGameScene() => StartCoroutine(FadeAndChangeScene(GameScene));


    /// <summary>
    /// Fades in to black, changes the scene, then fades back out
    /// </summary>
    public IEnumerator FadeAndChangeScene(string sceneName, float fadeDuration = defaultFadeDuration)
    {
        // Fade to black
        yield return StartCoroutine(FadeToBlack(fadeDuration));
        
        // Actually change the scene
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        curScene = SceneManager.GetActiveScene();

        // Fade back to clear
        yield return StartCoroutine(FadeToClear(fadeDuration));
    }


    /// <summary>
    /// Fades current image overlay screen to black 
    /// </summary>
    public IEnumerator FadeToBlack(float fadeDuration)
    {
        float elapsedTime = 0f;
        
        Color startColor = fadeImage.color;
        Color endColor = Color.black;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);

            yield return null;
        }
    }


    /// <summary>
    /// Fades current image overlay screen to clear 
    /// </summary>
    public IEnumerator FadeToClear(float fadeDuration)
    {
        float elapsedTime = 0f;
        
        Color startColor = fadeImage.color;
        Color endColor = Color.clear;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;

            fadeImage.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);

            yield return null;
        }
    }


    private void FixedUpdate()
    {
        // Manually call the pre-physics fixed update of the other  child managers

        // PRE-PHYSICS TICK
        GameManager.Instance?.RunPlayerFixedUpdate();

        // RUN PHYSICS

        // IN GAME
        if (curScene.name == GameScene)
        {
            // Client
            GameManager.Instance?.RunClientPhysics();

            // Server

        }

        // POST-PYSICS TICK


        // UPDATE
        SteamManager.Instance?.PostPhysUpdate();
    }
}
