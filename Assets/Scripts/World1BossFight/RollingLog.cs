using System;
using System.Collections;
using UnityEngine;

namespace World1BossFight
{
    public class RollingLog : MonoBehaviour
    {
        [SerializeField] private float dissolveDelay;
        [SerializeField] private Signal bossDamagedSignal;
        
        private Rigidbody2D _rigidbody2D;
        private Animator _animator;
        private BoxCollider2D _boxCollider2D;
        

        private bool _isRolling;
        private AudioSource _audioSource;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _audioSource = GetComponent<AudioSource>();
        }

        public void ThrowUpAndRoll(Vector2 direction, float speed)
        {
            //_animator.SetTrigger("ThrowUp");
            _rigidbody2D.linearVelocity = direction;
            StartCoroutine(RollRoutine(direction, speed));
        }

        private void Roll(Vector2 direction, float speed)
        {
            _rigidbody2D.linearVelocity = direction * speed;
            _isRolling = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isRolling) return;
            if (other.CompareTag("Player"))
            {
                Debug.Log("Damaged Player");
                bossDamagedSignal?.Raise();
            }
            else if (other.CompareTag("Log"))
            {
                _rigidbody2D.linearVelocity = -_rigidbody2D.linearVelocity;
            }
            else if (other.CompareTag("Void"))
            {
                _animator.SetTrigger("FallDown");
                _rigidbody2D.linearDamping = 5f;
                _boxCollider2D.enabled = false;
                StartCoroutine(DissolveRoutine());
            }
        }

        private IEnumerator RollRoutine(Vector2 direction, float speed)
        {
            yield return new WaitForSeconds(1);
            Roll(direction, speed);
            _audioSource.Play();
        }

        private IEnumerator DissolveRoutine()
        {
            _audioSource.Stop();
            yield return new WaitForSeconds(dissolveDelay);
            Destroy(gameObject);
        }
    }
}
