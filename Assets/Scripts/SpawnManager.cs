using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class SpawnManager : MonoBehaviourPunCallbacks
{
    public Transform protagonistSpawnPoint;
    public Material freezeEffectMaterial;

    // Add multiple spawner points for antagonists
    public Transform[] antagonistSpawnPoints; // Array of antagonist spawners
    public Button powerButton;
    private string currentPowerUp = null;

    public GameObject protagonistPrefab;
    public GameObject antagonistPrefab;
    public GameObject bulletPrefab;

    public float bufferTime = 3.0f;
    private bool isCooldown = false;

    public Image powerUpThumbnail;
    public Sprite freezeSprite;
    public Sprite bulletSprite;
    public Sprite speedBoostSprite;

    public GameObject protagonistPanel;
    public GameObject antagonistInvisibilityPanel;
    public GameObject antagonistDashPanel;
    public GameObject antagonistTrapPanel;

    public GameObject controlUI;
    public GameObject loadingScreen;

    public GameObject timerObject;

    private ShaderManager shaderManager;

    private int spawnedGhostsCount = 0;  // Keep track of ghosts spawned

    private void Start()
    {
        // Find ShaderManager in the scene and assign it
        shaderManager = FindObjectOfType<ShaderManager>();

        // Check if ShaderManager was found
        if (shaderManager == null)
        {
            Debug.LogError("ShaderManager not found in the scene!");
        }

        // If connected to Photon, in a room, and the current client is the Master
        if (PhotonNetwork.IsConnected && PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            // Call the RPC to show the loading screen across all clients
            photonView.RPC("ShowLoadingScreenRPC", RpcTarget.All);

            // Start the coroutine to assign roles
            StartCoroutine(WaitAndAssignRoles());
        }
    }

    private IEnumerator WaitAndAssignRoles()
    {
        yield return new WaitForSeconds(bufferTime);
        photonView.RPC("HideLoadingScreenRPC", RpcTarget.All);

        photonView.RPC("HideAllRolePanelsRPC", RpcTarget.All);

        AssignRoles();

        yield return new WaitForSeconds(3f);
        photonView.RPC("HideAllRolePanelsRPC", RpcTarget.All);

        photonView.RPC("ShowControlUI", RpcTarget.All);
        yield return new WaitForSeconds(2f);
        photonView.RPC("StartTimer", RpcTarget.All);
    }

    private void AssignRoles()
    {
        List<Player> players = new List<Player>(PhotonNetwork.PlayerList);
        int protagonistIndex = Random.Range(0, players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            if (i == protagonistIndex)
            {
                photonView.RPC("SetProtagonist", players[i]);
            }
            else
            {
                photonView.RPC("SetAntagonist", players[i]);
            }
        }
    }

    [PunRPC]
    private void SetProtagonist()
    {
        Debug.Log("Protagonist Role Assigned");

        GameObject protagonist = PhotonNetwork.Instantiate(protagonistPrefab.name, protagonistSpawnPoint.position, protagonistSpawnPoint.rotation);
        AssignCamera(protagonist);

        if (protagonist.GetComponent<PhotonView>().IsMine)
        {
            ShowPanel(protagonistPanel);
        }
    }

    [PunRPC]
    private void SetAntagonist()
    {
        Debug.Log("Antagonist Role Assigned");

        // Check the number of ghosts spawned so far
        if (spawnedGhostsCount < antagonistSpawnPoints.Length)
        {
            Transform spawnPoint = antagonistSpawnPoints[spawnedGhostsCount]; // Use a different spawn point
            GameObject antagonist = PhotonNetwork.Instantiate(antagonistPrefab.name, spawnPoint.position, spawnPoint.rotation);
            AssignCamera(antagonist);

            if (antagonist.GetComponent<PhotonView>().IsMine)
            {
                AssignAbilities(antagonist);
            }

            // Only increase the count for the first 2 spawners
            if (spawnedGhostsCount < 2)
            {
                spawnedGhostsCount++; // Increase spawned ghosts count
            }
        }
    }

    private void AssignCamera(GameObject player)
    {
        if (player.GetComponent<PhotonView>().IsMine)
        {
            Camera mainCamera = Camera.main;

            if (mainCamera != null)
            {
                TopDownCameraFollow cameraFollowScript = mainCamera.GetComponent<TopDownCameraFollow>();

                if (cameraFollowScript == null)
                {
                    cameraFollowScript = mainCamera.gameObject.AddComponent<TopDownCameraFollow>();
                }

                cameraFollowScript.target = player.transform;
            }
        }
    }

    private void AssignAbilities(GameObject player)
    {
        if (player.GetComponent<PhotonView>().IsMine)
        {
            powerButton.interactable = true;
            powerButton.onClick.RemoveAllListeners();

            Invisibility invisibility = player.GetComponent<Invisibility>();
            Dash dash = player.GetComponent<Dash>();
            Trap trap = player.GetComponent<Trap>();

            if (invisibility != null)
            {
                powerButton.onClick.AddListener(() => ActivatePower(invisibility.ActivateInvisibility, invisibility.cooldownTime));
                ShowPanel(antagonistInvisibilityPanel);
            }
            if (dash != null)
            {
                powerButton.onClick.AddListener(() => ActivatePower(dash.ActivateDash, dash.cooldownTime));
                ShowPanel(antagonistDashPanel);
            }
            if (trap != null)
            {
                powerButton.onClick.AddListener(() => ActivatePower(trap.PlaceTrap, trap.cooldownTime));
                ShowPanel(antagonistTrapPanel);
            }
        }
    }

    private void ActivatePower(System.Action powerAction, float cooldown)
    {
        if (!isCooldown)
        {
            powerAction.Invoke();
            StartCoroutine(CooldownRoutine(cooldown));
        }
    }

    private IEnumerator CooldownRoutine(float cooldown)
    {
        isCooldown = true;
        powerButton.interactable = false;
        yield return new WaitForSeconds(cooldown);
        powerButton.interactable = true;
        isCooldown = false;
    }

    public void UpdateInventory(string powerUpName)
    {
        currentPowerUp = powerUpName;
        Debug.Log("Power-Up added to inventory: " + powerUpName);

        photonView.RPC("ShowPowerUpThumbnailRPC", RpcTarget.All, powerUpName);
    }

    private void ActivateFreezePowerUp()
    {
        photonView.RPC("FreezeGhostsAcrossNetwork", RpcTarget.AllBuffered);
        Debug.Log("Yesssss");
    }

    [PunRPC]
    private void FreezeGhostsAcrossNetwork()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Ghost");
        foreach (GameObject enemy in enemies)
        {
            PacMan3DMovement enemyMovement = enemy.GetComponent<PacMan3DMovement>();
            if (enemyMovement != null)
            {
                Debug.Log("Disabling movement for ghost: " + enemy.name);
                enemyMovement.enabled = false;

                Rigidbody ghostRigidbody = enemy.GetComponent<Rigidbody>();
                if (ghostRigidbody != null)
                {
                    ghostRigidbody.velocity = Vector3.zero;
                    ghostRigidbody.angularVelocity = Vector3.zero;
                }

                Animator ghostAnimator = enemy.GetComponent<Animator>();
                if (ghostAnimator != null)
                {
                    ghostAnimator.enabled = false;
                }

                // Ensure ShaderManager is assigned before using it
                if (shaderManager != null)
                {
                    // Set freeze effect to visible
                    shaderManager.SetTilingMultiplier(shaderManager.freezeEffectMaterial, shaderManager.visibleValue);
                }

                StartCoroutine(ReEnableMovement(enemyMovement, 5f, enemy.name));
            }
        }
    }

    private IEnumerator ReEnableMovement(PacMan3DMovement enemyMovement, float delay, string enemyName)
    {
        yield return new WaitForSeconds(delay);

        if (enemyMovement != null)
        {
            Debug.Log("Re-enabling movement for ghost: " + enemyName);
            enemyMovement.enabled = true;
        }

        Animator ghostAnimator = enemyMovement.GetComponent<Animator>();
        if (ghostAnimator != null)
        {
            ghostAnimator.enabled = true;
        }

        // Ensure ShaderManager is assigned before using it
        if (shaderManager != null)
        {
            // Reset freeze effect to invisible
            shaderManager.SetTilingMultiplier(shaderManager.freezeEffectMaterial, shaderManager.invisibleValue);
        }
    }

    private void ActivateBulletPowerUp()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && player.GetComponent<PhotonView>().IsMine)
        {
            // Pass the player's transform to FireBullet
            FireBullet(player.transform);
        }
    }

    void FireBullet(Transform playerTransform)
    {
        if (bulletPrefab != null)
        {
            // Instantiate the bullet at the player's position
            GameObject bullet = PhotonNetwork.Instantiate(bulletPrefab.name, playerTransform.position, playerTransform.rotation, 0);

            // Get the PhotonView of the bullet
            PhotonView bulletPhotonView = bullet.GetComponent<PhotonView>();

            // Transfer ownership of the bullet to the player who fired it
            if (bulletPhotonView != null && bulletPhotonView.Owner != PhotonNetwork.LocalPlayer)
            {
                bulletPhotonView.TransferOwnership(PhotonNetwork.LocalPlayer);
            }

            // Get the player's movement script to determine the direction
            PacMan3DMovement playerMovement = playerTransform.GetComponent<PacMan3DMovement>();
            if (playerMovement != null)
            {
                // Set bullet direction based on player's facing direction
                bullet.GetComponent<Rigidbody>().velocity = playerMovement.transform.forward * 10f; // Adjust speed as needed
            }
        }
    }

    [PunRPC]
    public void ShowPowerUpThumbnailRPC(string powerUpName)
    {
        switch (powerUpName)
        {
            case "Freeze":
                powerUpThumbnail.sprite = freezeSprite;
                break;
            case "Bullet":
                powerUpThumbnail.sprite = bulletSprite;
                break;
            case "SpeedBoost":
                powerUpThumbnail.sprite = speedBoostSprite;
                break;
            default:
                powerUpThumbnail.sprite = null;
                break;
        }
    }

    [PunRPC]
    public void ShowLoadingScreenRPC()
    {
        loadingScreen.SetActive(true);
    }

    [PunRPC]
    public void HideLoadingScreenRPC()
    {
        loadingScreen.SetActive(false);
    }

    [PunRPC]
    private void ShowControlUI()
    {
        controlUI.SetActive(true);
    }

    [PunRPC]
    private void HideAllRolePanelsRPC()
    {
        protagonistPanel.SetActive(false);
        antagonistInvisibilityPanel.SetActive(false);
        antagonistDashPanel.SetActive(false);
        antagonistTrapPanel.SetActive(false);
    }

    [PunRPC]
    public void StartTimer()
    {
        // Implement timer functionality
        timerObject.SetActive(true);
        // Start your timer logic here
    }

    private void ShowPanel(GameObject panel)
    {
        panel.SetActive(true);
    }
}
