using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World1BossFight
{
    public class MapleLeafSlam : MonoBehaviour
    {
        [SerializeField] private Vector2Int bounds;
        [SerializeField] private float dissolveDelay;
        [SerializeField] private Signal bossDamagedSignal;
        
        private Animator _animator;
        private PlatformData _platformData;
        private AudioSource _audioSource;
        

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
        }

        public void Slam(float delay)
        {
            _platformData = PlatformManager.Instance.FindAndReservePositions(bounds);
            if (!_platformData.IsValid)
            {
                Destroy(gameObject);
                return; 
            }
            transform.position = _platformData.StartPosition + (transform.localScale / 2f);
            StartCoroutine(SlamRoutine(delay));
        }
        
        public void SlamAtPosition(float delay)
        {
            _platformData = new PlatformData
            {
                IsValid = false,
            };

            StartCoroutine(SlamRoutine(delay));
        }

        private IEnumerator SlamRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _animator.SetTrigger("Slam");
            _audioSource.Play();
            yield return new WaitForSeconds(1.5f);
            _animator.SetTrigger("Hide");
            yield return new WaitForSeconds(dissolveDelay);
            if (_platformData.IsValid) PlatformManager.Instance.UnreservePositions(_platformData.Positions);
            Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                Debug.Log("Damaged Player");
                bossDamagedSignal?.Raise();
            }
        }
    }
}
