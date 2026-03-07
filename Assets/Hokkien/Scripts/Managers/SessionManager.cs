using UnityEngine;
using System.Linq;
using System.Collections.Generic;

using NUnit.Framework;
using DG.Tweening;
public class SessionManager : MonoBehaviour   
{
    [Header("File Storage Config")]
    [SerializeField] private string fileName;

    private GameData gameData;
    private List<ISessionData> sessionDataObjects;
    private FileDataHandler fileDataHandler;

    public static SessionManager Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("Found more than one instance of Game Data Manager in the scene.");
        }
        Instance = this;
    }

    public void Start()
    {
        this.fileDataHandler = new FileDataHandler(Application.persistentDataPath, fileName);
        this.sessionDataObjects = FindAllSessionDataObjects();
        LoadGame();
    }

    public void NewGame()
    {
        this.gameData = new GameData();
    }

    public void LoadGame()
    {
        this.gameData = this.fileDataHandler.Load();
        if (this.gameData == null)
        {
            Debug.Log("No data was found. Initializing new game data.");
            NewGame();
        }

        foreach(ISessionData sessionDataObject in this.sessionDataObjects)
        {
            sessionDataObject.LoadData(this.gameData);
        }
    }

    public void SaveGame()
    {
        foreach (ISessionData sessionDataObject in this.sessionDataObjects)
        {
            sessionDataObject.SaveData(ref this.gameData);
        }
        this.fileDataHandler.Save(this.gameData);
    }

    public void OnApplicationQuit()
    {
        SaveGame();
    }

    private List<ISessionData> FindAllSessionDataObjects()
    {
        IEnumerable<ISessionData> sessionDataObjects = 
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .OfType<ISessionData>();

        return new List<ISessionData>(sessionDataObjects);
    }
}
