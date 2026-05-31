using UnityEngine;
using UnityEngine.Events;

public class TotemMinigame : MonoBehaviour
{
    [SerializeField] private GameObject TopPiece;
    [SerializeField] private GameObject TopPieceSpot;
    [SerializeField] private GameObject MiddlePiece;
    [SerializeField] private GameObject MiddlePieceSpot;
    [SerializeField] private GameObject BottomPiece;
    [SerializeField] private GameObject BottomPieceSpot;

    private QuestGiver totemQuest;
    private SignalListener checkCorrect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        totemQuest = GetComponent<QuestGiver>();

        //set this codes signal listener to listen for the quest giver's check signal
        checkCorrect = new SignalListener();
        checkCorrect.response.AddListener(OnCorrectnessChecked); 
        totemQuest.checkQuestCompletion.RegisterListener(checkCorrect);

        
    }

    // Update is called once per frame
    void Update()
    {
        //debug method to check if the puzzle is complete. need to make it so interacting with the Chief triggers this code instead.
        // also make it do more than outputting to the console

    }

    void OnCorrectnessChecked()
    {
        if (CheckTotemPieces())
        {
            totemQuest.MarkQuestDone();
        }
        totemQuest.SetIsChecking(false);
    }

    //Is this the most effective way to do this? Probably not. Does it work? It should.
    public bool CheckTotemPieces()
    {
        //check if the totem pieces are in their corresponding spots
        Debug.Log("Checking pieces...");
        if (!TopPiece.GetComponent<Rigidbody2D>().IsTouching(TopPieceSpot.GetComponent<BoxCollider2D>()))
        {
            return false;
        }

        if (!MiddlePiece.GetComponent<Rigidbody2D>().IsTouching(MiddlePieceSpot.GetComponent<BoxCollider2D>()))
        {
            return false;
        }

        if (!BottomPiece.GetComponent<Rigidbody2D>().IsTouching(BottomPieceSpot.GetComponent<BoxCollider2D>()))
        {
            return false;
        }
        Debug.Log("Pieces correct!");
        return true;
    }
}
