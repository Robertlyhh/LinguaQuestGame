using TMPro;
using UnityEngine;

namespace World1BossFight
{
    public class QuestionBubble : MonoBehaviour
    {
        public Canvas bubbleCanvas;
        public TextMeshProUGUI bubbleText;

        private float _duration;
        private float _timer;
        private bool _showing;

        private void Update()
        {
            if (!_showing) return;
            _timer += Time.deltaTime;
            if (_timer > _duration)
            {
                HideBubble();
            }
        }

        public void ShowMessage(string message, float duration)
        {
            bubbleText.text = message;
            bubbleCanvas.gameObject.SetActive(true);
            _duration = duration;
            _timer = 0f;
            _showing = true;
        }

        private void HideBubble()
        {
            bubbleCanvas.gameObject.SetActive(false);
            _showing = false;
        }
    }
}
