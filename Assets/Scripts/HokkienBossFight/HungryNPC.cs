using System;
using System.Collections;
using UnityEngine;

namespace HokkienBossFight
{
    [Serializable]
    public class HungerRequest
    {
        public string requestInHokkien;
        public Sprite requestSprite;
        public string requestInEnglish;

        public string itemId;
    }
    
    public class HungryNPC : MonoBehaviour, IInteractable
    {
        [SerializeField] private RequestDialogue requestDialogue;
        [SerializeField] private float cooldownDuration;
        
        private HungerRequest _currentHungerRequest;
        private bool _isOnCooldown;

        private void Start()
        {
            SelectRandomRequest();
        }

        private void SelectRandomRequest()
        {
            _currentHungerRequest = RequestManaqer.Instance.GetRandomHungerRequest();
            requestDialogue.Initialize(_currentHungerRequest);
        }

        public void Interact()
        {
            if (RequestManaqer.Instance.ItemId == _currentHungerRequest.itemId)
            {
                Debug.Log("Completed Request");
                requestDialogue.Complete(cooldownDuration);
                StartCoroutine(CooldownRoutine());
                return;
            }
            
            requestDialogue.ShowRequest();
            Debug.Log("Interacted");
        }

        public bool CanInteract()
        {
            return !_isOnCooldown;
        }

        private IEnumerator CooldownRoutine()
        {
            _isOnCooldown = true;
            yield return new WaitForSeconds(cooldownDuration);
            SelectRandomRequest();
            _isOnCooldown = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            if (CanInteract()) Interact();
        }
    }
}
