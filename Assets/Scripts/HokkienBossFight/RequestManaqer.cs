using System;
using UnityEngine;

namespace HokkienBossFight
{
    public class RequestManaqer : MonoBehaviour
    {
        public static RequestManaqer Instance;
        
        [SerializeField] private HungerRequest[] hungerRequests;

        public string ItemId { get; set; }

        private void OnEnable()
        {
            if (!Instance) Instance = this;
        }

        private void Awake()
        {
            ItemId = string.Empty;
        }

        public HungerRequest GetRandomHungerRequest()
        {
            var index = UnityEngine.Random.Range(0, hungerRequests.Length);
            return hungerRequests[index];
        }
    }
}
