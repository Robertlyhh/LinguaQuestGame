using UnityEngine;
using UnityEngine.SceneManagement;

namespace World1BossFight
{
    public class World1BossFightResultManager : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject winPage;
        [SerializeField] private GameObject losePage;

        [Header("Audio")]
        [SerializeField] private AudioClip winSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioSource audioSource;

        private bool _fightEnded;
        private bool _lastOutcomeWin;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (winPage != null)
            {
                winPage.SetActive(false);
            }

            if (losePage != null)
            {
                losePage.SetActive(false);
            }
        }

        private void Update()
        {
            if (!_fightEnded) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                ReturnToWorld();
            }
            else if (Input.GetKeyDown(KeyCode.R))
            {
                RestartFight();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                QuitGame();
            }
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        public void HandleWin()
        {
            if (_fightEnded) return;

            _fightEnded = true;
            _lastOutcomeWin = true;
            ShowResultPage(winPage, losePage, winSound);
        }

        public void HandleLose()
        {
            if (_fightEnded) return;

            _fightEnded = true;
            _lastOutcomeWin = false;
            ShowResultPage(losePage, winPage, loseSound);
        }

        private void ShowResultPage(GameObject pageToShow, GameObject pageToHide, AudioClip resultSound)
        {
            if (pageToHide != null)
            {
                pageToHide.SetActive(false);
            }

            if (pageToShow != null)
            {
                pageToShow.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[World1BossFightResultManager] Result page is not assigned.");
            }

            if (resultSound != null)
            {
                if (audioSource != null)
                {
                    audioSource.PlayOneShot(resultSound);
                }
                else if (Camera.main != null)
                {
                    AudioSource.PlayClipAtPoint(resultSound, Camera.main.transform.position);
                }
            }

            Time.timeScale = 0f;
        }

        private void ReturnToWorld()
        {
            Time.timeScale = 1f;

            if (SceneTracker.Instance != null)
            {
                SceneTracker.Instance.ReturnToPreviousScene(_lastOutcomeWin);
            }
            else
            {
                Debug.LogWarning("[World1BossFightResultManager] SceneTracker.Instance not found.");
            }
        }

        private void RestartFight()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void QuitGame()
        {
            Time.timeScale = 1f;
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
