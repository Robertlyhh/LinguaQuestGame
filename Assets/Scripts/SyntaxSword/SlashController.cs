// using System.Collections.Generic;
// using UnityEngine;

// [RequireComponent(typeof(CircleCollider2D), typeof(TrailRenderer))]
// public class SlashController : MonoBehaviour
// {
//     [Header("Motion")]
//     [SerializeField] private Camera cam;
//     [SerializeField] private float activateSpeed = 6.0f; // world units/sec threshold
//     [SerializeField] private float maxDistancePerStep = 2.0f; // anti-teleport clamp

//     [Header("Hit")]
//     [SerializeField] private LayerMask wordLayer; // set to the Word layer
//     [SerializeField] private float rehitCooldown = 0.07f;

//     private CircleCollider2D _col;
//     private TrailRenderer _trail;

//     private Vector3 _prevPos;
//     private float _speed;
//     private bool _active; // whether slash is "sharp" this frame

//     // small cache so we don't multi-slice the same block in same frame sweep
//     private readonly Dictionary<WordBlock, float> _recentHits = new();

//     void Awake()
//     {
//         _col = GetComponent<CircleCollider2D>();
//         _trail = GetComponent<TrailRenderer>();
//         if (!cam) cam = Camera.main;
//         _col.isTrigger = true;
//         _col.enabled = true;
//     }

//     void OnEnable()
//     {
//         Vector3 start = WorldMouse();
//         transform.position = start;
//         _prevPos = start;
//         _trail.Clear();
//     }

//     void Update()
//     {
//         // follow mouse in world space
//         var target = WorldMouse();
//         var step = target - _prevPos;

//         // clamp huge jumps (window focus, etc.)
//         if (step.sqrMagnitude > maxDistancePerStep * maxDistancePerStep)
//             step = step.normalized * maxDistancePerStep;

//         transform.position = _prevPos + step;

//         // compute speed
//         float dt = Mathf.Max(Time.deltaTime, 1e-5f);
//         _speed = step.magnitude / dt;

//         // slash is "active" only when moving fast enough (feels like a swipe)
//         _active = _speed >= activateSpeed;

//         // fade trail when inactive
//         _trail.emitting = _active;

//         // decay recent-hits map
//         DecayRecentHits(dt);

//         _prevPos = transform.position;
//     }

//     private Vector3 WorldMouse()
//     {
//         var m = Input.mousePosition;
//         m.z = Mathf.Abs(cam.transform.position.z); // ortho: any positive z works
//         return cam.ScreenToWorldPoint(m);
//     }

//     private void OnTriggerEnter2D(Collider2D other)
//     {
//         TrySlice(other);
//     }

//     private void OnTriggerStay2D(Collider2D other)
//     {
//         TrySlice(other);
//     }

//     private void TrySlice(Collider2D other)
//     {
//         if (!_active) return;
//         if (((1 << other.gameObject.layer) & wordLayer) == 0) return;

//         var wb = other.GetComponentInParent<WordBlock>() ?? other.GetComponent<WordBlock>();
//         if (!wb || !wb.gameObject.activeInHierarchy) return;

//         // prevent rapid multi-hits on same object
//         if (_recentHits.TryGetValue(wb, out float t) && t > 0f) return;

//         int delta = wb.Slice();

//         // record hit cooldown
//         _recentHits[wb] = rehitCooldown;

//         // update score (optional singleton; add your own manager if you prefer events)
//         SyntaxSwordGameManager.TryAddScore(delta);
//     }

//     private void DecayRecentHits(float dt)
//     {
//         if (_recentHits.Count == 0) return;
//         var keys = new List<WordBlock>(_recentHits.Keys);
//         foreach (var k in keys)
//         {
//             _recentHits[k] -= dt;
//             if (_recentHits[k] <= 0f) _recentHits.Remove(k);
//         }
//     }
// }
