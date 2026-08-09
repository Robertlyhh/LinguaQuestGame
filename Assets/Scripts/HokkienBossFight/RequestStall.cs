using System;
using UnityEngine;

namespace HokkienBossFight
{
    public class RequestStall : MonoBehaviour
    {
        [SerializeField] private string itemId;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            RequestManaqer.Instance.ItemId = itemId;
        }
    }
}
