using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HokkienBossFight
{
    public class RequestDialogue : MonoBehaviour
    {
        private static readonly int IsRevealedHash = Animator.StringToHash("IsRevealed");
        private static readonly int CompleteHash = Animator.StringToHash("Complete");

        [SerializeField] private TextMeshProUGUI textContainer;
        [SerializeField] private Image image;
        [SerializeField] private float revealDuration;

        private HungerRequest _currentHungerRequest;
        private Animator _animator;
        private int _stage;
        private bool _isRevealed;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void Initialize(HungerRequest hungerRequest)
        {
            _currentHungerRequest = hungerRequest;
            _stage = 0;
        }

        public void Complete(float cooldown)
        {
            StartCoroutine(CompleteRoutine(cooldown));
        }

        public void ShowRequest()
        {
            if (_isRevealed) return;
            
            switch (_stage)
            {
                case 0:
                    textContainer.gameObject.SetActive(true);
                    image.gameObject.SetActive(false);
                    textContainer.text = _currentHungerRequest.requestInHokkien;
                    break;
                case 1:
                    image.gameObject.SetActive(true);
                    textContainer.gameObject.SetActive(false);
                    image.sprite = _currentHungerRequest.requestSprite;
                    break;
                case 2:
                    textContainer.gameObject.SetActive(true);
                    image.gameObject.SetActive(false);
                    textContainer.text = _currentHungerRequest.requestInEnglish;
                    break;
            }

            StartCoroutine(RevealRoutine());
            _stage++;
        }

        private IEnumerator RevealRoutine()
        {
            _isRevealed = true;
            _animator.SetBool(IsRevealedHash, _isRevealed);
            yield return new WaitForSeconds(revealDuration);
            _isRevealed = false;
            _animator.SetBool(IsRevealedHash, _isRevealed);
        }
        
        private IEnumerator CompleteRoutine(float cooldown)
        {
            textContainer.gameObject.SetActive(false);
            image.gameObject.SetActive(false);
            _isRevealed = true;
            _animator.SetBool(IsRevealedHash, _isRevealed);
            _animator.SetTrigger(CompleteHash);
            yield return new WaitForSeconds(cooldown);
            _isRevealed = false;
            _animator.SetBool(IsRevealedHash, _isRevealed);
        }
    }
}
