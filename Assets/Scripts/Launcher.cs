using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;  // Import UI for button handling

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public Button singlePlayerButton;  // Reference to the Single Player button
    public Button multiplayerButton;   // Reference to the Multiplayer button
    public PanelShake panelShake;      // Reference to the PanelShake script
    public Image loadingBar;           // Reference to the UI Image for the progress bar
    public GameObject loadingScreen;   // Reference to the loading screen panel (optional)

    private void Start()
    {
        // Assign button listeners
        if (singlePlayerButton != null)
        {
            singlePlayerButton.onClick.AddListener(StartSinglePlayer);
        }

        if (multiplayerButton != null)
        {
            multiplayerButton.onClick.AddListener(StartMultiplayer);
        }

        // Ensure any UI setup if needed
        if (panelShake == null)
        {
            panelShake = FindObjectOfType<PanelShake>(); // Find the PanelShake script in the scene if not assigned
        }
    }

    // Method to handle Single Player button click
    private void StartSinglePlayer()
    {
        Debug.Log("Starting Single Player...");
        // You can load your single-player scene here
        StartCoroutine(LoadSceneAsync("SinglePlayerScene")); // Replace with your single-player scene name
    }

    // Method to handle Multiplayer button click
    private void StartMultiplayer()
    {
        Debug.Log("Starting Multiplayer...");

        // Generate a random player name
        string randomPlayerName = "Player" + Random.Range(100, 1000).ToString();
        PhotonNetwork.NickName = randomPlayerName;

        // Connect to the Photon Master Server only when multiplayer is selected
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Master Server");
        PhotonNetwork.JoinLobby();  // Join the lobby after connecting to the master server
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        StartCoroutine(LoadSceneAsync("CreateRoom"));  // Replace with your multiplayer room scene
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("Disconnected from Photon: " + cause.ToString());

        // Trigger panel shake only when there is an error
        if (panelShake != null)
        {
            panelShake.TriggerShake();
        }
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Enable the loading screen (if you have one)
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Start loading the scene asynchronously
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        // Make sure the scene doesn't activate immediately
        operation.allowSceneActivation = false;

        // While the scene is loading, update the progress bar
        while (!operation.isDone)
        {
            // Get the loading progress (ranges from 0 to 1)
            float progress = Mathf.Clamp01(operation.progress / 0.9f); // Adjust for Unity's weird loading range (0 to 0.9)

            // Update the loading bar fill amount based on the progress
            if (loadingBar != null)
            {
                loadingBar.fillAmount = progress;
            }

            // If the loading is complete (progress reaches ~0.9), allow scene activation
            if (progress >= 1f)
            {
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
