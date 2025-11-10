using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class PartyManager : MonoBehaviour
{
    public static PartyManager Instance { get; private set; }
    public List<Character> Party = new List<Character>();
    public List<GameObject> PartyObjects = new List<GameObject>();

    [Header("Follow Settings")]
    public float followDistance = 2f;

    [HideInInspector] public PlayerExploring playerMovement;
    [HideInInspector] public Transform player;

    void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() => SceneManager.activeSceneChanged += Initialize;
    void OnDisable() => SceneManager.activeSceneChanged -= Initialize;

    // Called each time a scene changes
    void Initialize(Scene oldScene, Scene newScene)
    {
        playerMovement = FindObjectOfType<PlayerExploring>();

        if(playerMovement != null)
        {
            player = playerMovement.transform;
        }
        else
        {
            Debug.LogError("No PlayerExploring script found in scene " + newScene.name);
            return;
        }

        if(Party.Count == 0)
        {
            CharacterCreator creator = player.GetComponent<CharacterCreator>();
            if (creator != null)
                UpdateParty(creator.character);
        }
    }

    public void UpdateParty(Character character)
    {
        if (character == null)
        {
            Debug.LogError("No character found to update party. (character was null)");
            return;
        }

        if (character.characterPrefab != null)
        {
            Party.Add(character);
            PartyObjects.Add(character.characterPrefab);
        }
        else
        {
            Debug.LogError("No character object found on character. (characterPrefab was null, did you make a prefab for this character?)");
            return;
        }
    }

    public void AddToParty(GameObject follower)
    {
        if(player == null)
        {
            Debug.LogError("No player in the game to follow");
            return;
        }

        CharacterCreator creator = follower.GetComponent<CharacterCreator>();
        UpdateParty(creator.character);
        StartCoroutine(FollowCoroutine(follower));
    }

    private IEnumerator FollowCoroutine(GameObject follower)
    {
        Transform followerTransform = follower.transform;
    
        while(true)
        {
            if(player == null) yield break;
    
            if(playerMovement.isMoving)
            {
                float distance = Vector3.Distance(player.position, followerTransform.position);
                if(distance > followDistance)
                {
                    followerTransform.position = Vector3.Lerp(
                        followerTransform.position,
                        player.position,
                        Time.deltaTime * playerMovement.speed
                    );
                }
            }
    
            yield return null;
        }
    }
}
