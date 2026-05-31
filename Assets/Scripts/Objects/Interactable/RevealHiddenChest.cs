using UnityEngine;

public class RevealHiddenChest : MonoBehaviour
{

    [SerializeField] private GameObject QuestGiverObject;
    private QuestGiver _questGiver;
    private SignalListener _listener;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _questGiver = QuestGiverObject.GetComponent<QuestGiver>();
        _listener = new SignalListener();
        _listener.response.AddListener(RevealChest);
        _questGiver.questComplete.RegisterListener(_listener);

        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void RevealChest()
    {
        gameObject.SetActive(true);
    }
}
