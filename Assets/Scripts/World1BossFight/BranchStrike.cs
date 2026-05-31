using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World1BossFight
{
    public class BranchStrike : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private GameObject warningObject;
        [SerializeField] private SpriteRenderer branchBody;
        [SerializeField] private Transform branchEnd;
        [SerializeField] private GameObject disableOnDown;

        [Header("Collision")]
        [SerializeField] private BoxCollider2D damageTrigger;

        [Header("Settings")]
        [SerializeField] private float dissolveDelay;

        private BranchStrikeData _branchStrikeData;

        private Animator _animator;
        private float _tileSize;
        private Vector2 _direction;
        private AudioSource _audioSource;

        [Header("Damage")]
        [SerializeField] private Signal bossDamagedSignal;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
        }

        public void Strike(float delay, float speed, float duration)
        {
            _branchStrikeData = PlatformManager.Instance.FindAndReserveBranchStrikePositions(true);

            if (!_branchStrikeData.PlatformData.IsValid)
            {
                Destroy(gameObject);
                return;
            }

            _direction = _branchStrikeData.Direction;
            _tileSize = _branchStrikeData.PlatformData.TileSize;

            if (_direction == Vector2.down) disableOnDown.SetActive(false);

            transform.position = _branchStrikeData.PlatformData.StartPosition + transform.localScale * 0.5f;

            RotateToDirection();

            SetupWarning();

            StartCoroutine(StrikeRoutine(delay, speed, duration));
        }

        private void RotateToDirection()
        {
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            disableOnDown.transform.localRotation = Quaternion.Euler(0, 0, -angle);
        }

        private void SetupWarning()
        {
            warningObject.transform.localPosition =
                Vector3.right * (_branchStrikeData.Distance * _tileSize * 0.5f + 0.5f);

            warningObject.transform.localScale =
                new Vector3(_branchStrikeData.Distance * _tileSize, _tileSize, 1);
        }

        private IEnumerator StrikeRoutine(float delay, float speed, float duration)
        {

            yield return new WaitForSeconds(delay);

            _audioSource.Play();
            float targetLength = _branchStrikeData.Distance * _tileSize;

            float length = 0;

            while (length < targetLength)
            {
                length += speed * Time.deltaTime;

                UpdateBranch(length);

                yield return null;
            }

            UpdateBranch(targetLength);

            yield return new WaitForSeconds(duration);

            PlatformManager.Instance.UnreservePositions(_branchStrikeData.PlatformData.Positions);

            while (length > 1)
            {
                length -= speed * Time.deltaTime;

                UpdateBranch(length);

                yield return null;
            }

            UpdateBranch(1);

            _animator.SetTrigger("Hide");
            yield return new WaitForSeconds(dissolveDelay);

            Destroy(gameObject);
        }



        private void UpdateBranch(float length)
        {
            float bodyLength = Mathf.Max(0, length - _tileSize);

            branchBody.size = new Vector2(bodyLength, _tileSize);
            branchBody.transform.localPosition = new Vector2(length * 0.5f, 0);

            branchEnd.localPosition = new Vector3(length, 0, 0);

            damageTrigger.size = new Vector2(length, _tileSize);
            damageTrigger.offset = new Vector2(length * 0.5f, 0);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;

            Debug.Log("Damaged Player");
            bossDamagedSignal?.Raise();

        }
    }
}
