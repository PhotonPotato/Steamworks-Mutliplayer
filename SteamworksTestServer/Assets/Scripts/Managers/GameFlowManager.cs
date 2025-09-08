using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Will control the flow of switching scenes and making sure essential objects get
/// moved and preserve refs correctly.
/// </summary>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public string MainMenuScene;
    public string GameScene;
    public string BetweenGamesScene;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else DestroyImmediate(gameObject);
    }

    public void LoadMainMenuScene() => SceneManager.LoadScene(MainMenuScene, LoadSceneMode.Single);

    public void LoadBetweenGamesScene()
    {
        SceneManager.LoadScene(BetweenGamesScene, LoadSceneMode.Single);
    }

    public void LoadGameScene()
    {
        SceneManager.LoadScene(GameScene, LoadSceneMode.Single);
    }
}
