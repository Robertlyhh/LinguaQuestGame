using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World1BossFight
{
    [Serializable]
    public class BossQuestion
    {
        public string question;
        public string[] answers = new string[4];
        public int correctIndex;
    }
    
    public class MapleTreeBoss : MonoBehaviour
    {
        [Header("Health & Staging")]
        [SerializeField] private int maxHealth;
        [Range(0,1)] [SerializeField] private float mediumStageUpperPercentage;
        [Range(0,1)] [SerializeField] private float hardStageUpperPercentage;
        [Space]
        [SerializeField] private GameObject bossHeartPrefab;
        [Space]
        [SerializeField] private Color stage0Color;
        [SerializeField] private Color stage1Color;
        [SerializeField] private Color stage2Color;
        [SerializeField] private Color stage3Color;
        
        [Header("Extra")]
        [SerializeField] private Animator bridgeAnimator;
        [SerializeField] private GameObject enableOnFightStart;
        [SerializeField] private GameObject disableOnFightStart;
        [SerializeField] private Signal bossDefeatedSignal;
        
        [Header("Questions")]
        [SerializeField] private QuestionBubble questionBubble;
        [SerializeField] private BossQuestion[] bossQuestions;
        [SerializeField] private Vector3Int questionPhaseCounts = new Vector3Int(2, 2, 4);
        [SerializeField] private TextMeshProUGUI[] questions;
        [SerializeField] private Transform[] answerPositions;
        [SerializeField] private GameObject questionMapleLeafSlamPrefab;
        
        [Header("Attacks")]
        [SerializeField] private int attacksUntilQuestion;
        [SerializeField] private float attackCooldown;
        
        [Header("Rolling Log Attack")]
        [SerializeField] private GameObject rollingLogPrefab;
        [SerializeField] private Vector3Int rollingLogStageCount;
        [SerializeField] private Vector3 rollingLogStageSpeed;
        [SerializeField] private Vector3 rollingLogStageAttackSpeed;
        [SerializeField] private int maxRollingLogAttacksPerQuestionCycle = 1;
        [SerializeField] private int rollingLogCountReduction = 1;
        [SerializeField] private float rollingLogAttackSpacingMultiplier = 0.85f;
        [Space]
        [SerializeField] private Transform leftRollingLogSpawnPoint;
        [SerializeField] private Transform rightRollingLogSpawnPoint;
        [SerializeField] private float rollingLogSpawnPointHeight;
        
        [Header("Branch Strike Attack")]
        [SerializeField] private GameObject branchStrikePrefab;
        [SerializeField] private Vector3Int branchStrikeStageCount;
        [SerializeField] private Vector3 branchStrikeStageDelay;
        [SerializeField] private Vector3 branchStrikeStageSpeed;
        [SerializeField] private Vector3 branchStrikeStageDuration;
        [SerializeField] private Vector3 branchStrikeStageAttackSpeed;
        
        [Header("Maple Leaf Slam Attack")]
        [SerializeField] private GameObject mapleLeafSlamPrefab;
        [SerializeField] private Vector3Int mapleLeafSlamStageCount;
        [SerializeField] private Vector3 mapleLeafSlamStageDelay;
        [SerializeField] private Vector3 mapleLeafSlamStageAttackSpeed;
        
        [Header("Hedge Split Attack")]
        [SerializeField] private GameObject hedgeSplitPrefab;
        [SerializeField] private Vector3 hedgeSplitStageDelay;
        [SerializeField] private Vector3 hedgeSplitStageQuestionDuration;

        [Header("Key System")]
        [SerializeField] private Inventory inventory;


        [Header("Zero Keys — Undefeatable")]
        [SerializeField] private float zeroKey_AttackCooldown = 100;
        [SerializeField] private int zeroKey_AttacksUntilQuestion = 125;
        [SerializeField] private Vector3 zeroKey_RollingLogSpeed = new Vector3(80f, 100f, 120f);
        [SerializeField] private Vector3Int zeroKey_RollingLogCount = new Vector3Int(500,750, 1000);
        [SerializeField] private Vector3 zeroKey_RollingLogAttackSpeed = new Vector3(7f, 2f, 1f);

        [Header("2-4 Keys — Hard")]
        [SerializeField] private float twoKey_AttackCooldown = 4;
        [SerializeField] private int twoKey_AttacksUntilQuestion = 6;
        [SerializeField] private Vector3 twoKey_RollingLogSpeed = new Vector3(5f, 7f, 9f);
        [SerializeField] private Vector3Int twoKey_RollingLogCount = new Vector3Int(2, 3, 4);
        [SerializeField] private Vector3 twoKey_RollingLogAttackSpeed = new Vector3(0.6f, 0.5f, 0.4f);

        [Header("5 Keys — Defeatable")]
        [SerializeField] private float fiveKey_AttackCooldown = 2;
        [SerializeField] private int fiveKey_AttacksUntilQuestion = 3;
        [SerializeField] private Vector3 fiveKey_RollingLogSpeed = new Vector3(3f, 4f, 5f);
        [SerializeField] private Vector3Int fiveKey_RollingLogCount = new Vector3Int(1, 2, 3);
        [SerializeField] private Vector3 fiveKey_RollingLogAttackSpeed = new Vector3(1f, 0.8f, 0.6f);

        private int _health;
        private int _attacksCount;
        private SpriteRenderer _spriteRenderer;
        private BoxCollider2D _boxCollider2D;
        private Animator _animator;
        private AudioSource _audioSource;
        private int _rollingLogAttacksThisCycle;
        private int _currentPhaseIndex;
        private readonly List<int>[] _phaseQuestionIndexes = new List<int>[3];
        private readonly List<int>[] _phaseQuestionQueue = new List<int>[3];
        private readonly int[] _lastBossQuestionIndexByPhase = { -1, -1, -1 };

        private void Start()
        {
            int keyCount = CountKeys();
            Debug.Log("Boss fight starting with " + keyCount + " keys");
            ApplyDifficulty(keyCount);
        }

        private int CountKeys()
        {
            if (inventory == null)
            {
                Debug.LogError("Boss inventory is NULL!");
                return 0;
            }

            int count = 0;
            foreach (var entry in inventory.items)
            {
                Debug.Log("Checking item: " + entry.item.itemName);
                if (entry.item.itemName.Contains("Key") ||
                    entry.item.itemName.Contains("key") ||
                    entry.item.itemName == "KeyPiece")
                {
                    count += entry.quantity;
                    Debug.Log("Found key: " + entry.item.itemName + " quantity: " + entry.quantity);
                }
            }

            Debug.Log("Total keys found: " + count);
            return count;
        }

        private void ApplyDifficulty(int keys)
        {
            if (keys >= 5)
            {
                // ?? DEFEATABLE ??
                attackCooldown = fiveKey_AttackCooldown;
                attacksUntilQuestion = fiveKey_AttacksUntilQuestion;

                // Rolling Log
                rollingLogStageSpeed = new Vector3(3f, 4f, 5f);
                rollingLogStageCount = new Vector3Int(1, 2, 2);
                rollingLogStageAttackSpeed = new Vector3(1f, 0.8f, 0.6f);
                maxRollingLogAttacksPerQuestionCycle = 1;
                rollingLogCountReduction = 1;
                rollingLogAttackSpacingMultiplier = 1.2f;

                // Branch Strike
                branchStrikeStageCount = new Vector3Int(1, 2, 2);
                branchStrikeStageDelay = new Vector3(1.5f, 1.2f, 1f);
                branchStrikeStageSpeed = new Vector3(2f, 3f, 4f);
                branchStrikeStageDuration = new Vector3(2f, 1.8f, 1.5f);
                branchStrikeStageAttackSpeed = new Vector3(1.5f, 1.2f, 1f);

                // Maple Leaf Slam
                mapleLeafSlamStageCount = new Vector3Int(1, 2, 2);
                mapleLeafSlamStageDelay = new Vector3(1.5f, 1.2f, 1f);
                mapleLeafSlamStageAttackSpeed = new Vector3(1.2f, 1f, 0.8f);

                // Hedge Split (question phase)
                hedgeSplitStageDelay = new Vector3(2f, 1.8f, 1.5f);
                hedgeSplitStageQuestionDuration = new Vector3(8f, 7f, 6f); // more time to answer

                // Question phase counts — more questions means more damage chances
                questionPhaseCounts = new Vector3Int(3, 3, 4);

                Debug.Log("Difficulty: Defeatable (5 keys)");
            }
            else if (keys >= 2)
            {
                // ?? HARD ??
                attackCooldown = twoKey_AttackCooldown;
                attacksUntilQuestion = twoKey_AttacksUntilQuestion;

                // Rolling Log
                rollingLogStageSpeed = new Vector3(5f, 7f, 9f);
                rollingLogStageCount = new Vector3Int(2, 3, 4);
                rollingLogStageAttackSpeed = new Vector3(0.6f, 0.5f, 0.4f);
                maxRollingLogAttacksPerQuestionCycle = 2;
                rollingLogCountReduction = 0;
                rollingLogAttackSpacingMultiplier = 0.95f;

                // Branch Strike
                branchStrikeStageCount = new Vector3Int(2, 3, 4);
                branchStrikeStageDelay = new Vector3(1f, 0.8f, 0.6f);
                branchStrikeStageSpeed = new Vector3(4f, 6f, 8f);
                branchStrikeStageDuration = new Vector3(1.5f, 1.2f, 1f);
                branchStrikeStageAttackSpeed = new Vector3(0.8f, 0.6f, 0.5f);

                // Maple Leaf Slam
                mapleLeafSlamStageCount = new Vector3Int(2, 3, 4);
                mapleLeafSlamStageDelay = new Vector3(1f, 0.8f, 0.6f);
                mapleLeafSlamStageAttackSpeed = new Vector3(0.8f, 0.6f, 0.5f);

                // Hedge Split
                hedgeSplitStageDelay = new Vector3(1.2f, 1f, 0.8f);
                hedgeSplitStageQuestionDuration = new Vector3(5f, 4f, 3f); // less time

                // Fewer question phases — less damage chances
                questionPhaseCounts = new Vector3Int(2, 2, 3);

                Debug.Log("Difficulty: Hard (2-4 keys)");
            }
            else
            {
                // ?? UNDEFEATABLE ??
                attackCooldown = zeroKey_AttackCooldown;
                attacksUntilQuestion = zeroKey_AttacksUntilQuestion;

                // Rolling Log — insane
                rollingLogStageSpeed = new Vector3(10f, 13f, 16f);
                rollingLogStageCount = new Vector3Int(5, 6, 8);
                rollingLogStageAttackSpeed = new Vector3(0.2f, 0.15f, 0.1f);
                maxRollingLogAttacksPerQuestionCycle = 4;
                rollingLogCountReduction = 0;
                rollingLogAttackSpacingMultiplier = 0.7f;

                // Branch Strike — rapid fire
                branchStrikeStageCount = new Vector3Int(4, 5, 6);
                branchStrikeStageDelay = new Vector3(0.4f, 0.3f, 0.2f);
                branchStrikeStageSpeed = new Vector3(8f, 10f, 12f);
                branchStrikeStageDuration = new Vector3(0.8f, 0.6f, 0.5f);
                branchStrikeStageAttackSpeed = new Vector3(0.3f, 0.2f, 0.15f);

                // Maple Leaf Slam — overwhelming
                mapleLeafSlamStageCount = new Vector3Int(5, 6, 8);
                mapleLeafSlamStageDelay = new Vector3(0.3f, 0.2f, 0.15f);
                mapleLeafSlamStageAttackSpeed = new Vector3(0.3f, 0.2f, 0.15f);

                // Hedge Split — barely any time to answer
                hedgeSplitStageDelay = new Vector3(0.5f, 0.4f, 0.3f);
                hedgeSplitStageQuestionDuration = new Vector3(2f, 1.5f, 1f); // almost impossible

                // Minimal question phases — almost no damage chances
                questionPhaseCounts = new Vector3Int(1, 1, 2);

                Debug.Log("Difficulty: Undefeatable (0-1 keys)");
            }
        }
        private void Awake()
        {
            _health = maxHealth;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.color = stage0Color;
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
            _currentPhaseIndex = 0;
            InitializeQuestionPhases();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                StartCoroutine(StartBattleRoutine());
            }
        }

        private IEnumerator StartBattleRoutine()
        {
            _boxCollider2D.enabled = false;

            // Show key warning
            int keys = CountKeys();
            if (keys >= 5)
                Debug.Log("You have all 5 keys — the boss is vulnerable!");
            else if (keys >= 2)
                Debug.Log("You have " + keys + " keys — the boss will be tough!");
            else
                Debug.Log("You have no keys — the boss is undefeatable!");

            if (enableOnFightStart) enableOnFightStart.SetActive(true);
            if (disableOnFightStart) disableOnFightStart.SetActive(false);
            yield return new WaitForSeconds(1);
            bridgeAnimator.SetTrigger("Break");
            StartCoroutine(ChangeStateRoutine());
            yield return new WaitForSeconds(3);
            _animator.SetTrigger("Idle");
            _audioSource.Play();
        }

        public BossQuestion GetRandomBossQuestion()
        {
            if (bossQuestions == null || bossQuestions.Length == 0) return null;

            int phaseIndex = Mathf.Clamp(_currentPhaseIndex, 0, _phaseQuestionQueue.Length - 1);
            if (_phaseQuestionQueue[phaseIndex] == null || _phaseQuestionQueue[phaseIndex].Count == 0)
            {
                RefillPhaseQuestionQueue(phaseIndex);
            }

            var phaseQuestions = _phaseQuestionQueue[phaseIndex];
            if (phaseQuestions == null || phaseQuestions.Count == 0) return null;

            int selectedIndex = 0;
            if (phaseQuestions.Count > 1 && phaseQuestions[0] == _lastBossQuestionIndexByPhase[phaseIndex])
            {
                selectedIndex = 1;
            }

            int questionIndex = phaseQuestions[selectedIndex];
            phaseQuestions.RemoveAt(selectedIndex);
            _lastBossQuestionIndexByPhase[phaseIndex] = questionIndex;
            return bossQuestions[questionIndex];
        }

        public void PerformAttack()
        {
            if (_attacksCount >= attacksUntilQuestion)
            {
                _attacksCount = 0;
                _rollingLogAttacksThisCycle = 0;
                PerformHedgeSplitAttack();
                return;
            }
            
            //PerformMapleLeafSlamAttack();
            //return;
            
            var availableAttacks = new List<int> { 0, 1, 2 };
            if (_rollingLogAttacksThisCycle >= maxRollingLogAttacksPerQuestionCycle)
            {
                availableAttacks.Remove(0);
            }

            var rand = availableAttacks[Random.Range(0, availableAttacks.Count)];
            switch (rand)
            {
                case 0:
                    PerformRollingLogAttack();
                    break;
                case 1:
                    PerformBranchStrikeAttack();
                    break;
                case 2:
                    PerformMapleLeafSlamAttack();
                    break;
            }
            _attacksCount++;
        }

        private float GetStageValue(Vector3 value)
        {
            switch (Mathf.Clamp(_currentPhaseIndex, 0, 2))
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        public void PerformRollingLogAttack()
        {
            _rollingLogAttacksThisCycle++;
            StartCoroutine(RollingLogAttackRoutine());
        }

        private IEnumerator RollingLogAttackRoutine()
        {
            var count = Mathf.Max(1, Mathf.RoundToInt(GetStageValue(rollingLogStageCount)) - rollingLogCountReduction);
            var speed = GetStageValue(rollingLogStageSpeed);
            var attackSpeed = Mathf.Max(0.1f, GetStageValue(rollingLogStageAttackSpeed) * rollingLogAttackSpacingMultiplier);
            for (var i = 0; i < count; i++)
            {
                var spawnLeft = Random.Range(0, 2) == 1;
                var spawnTransform = spawnLeft ? leftRollingLogSpawnPoint : rightRollingLogSpawnPoint;
                var spawnOffset = (int)Random.Range(-rollingLogSpawnPointHeight, rollingLogSpawnPointHeight);
                var direction = spawnLeft ? Vector3.right : Vector3.left;
                var spawnPosition = spawnTransform.position + Vector3.up * spawnOffset;
                
                var rollingLogGameObject = Instantiate(rollingLogPrefab, spawnPosition, Quaternion.identity);
                var rollingLog = rollingLogGameObject.GetComponent<RollingLog>();
                rollingLog.ThrowUpAndRoll(direction, speed);
                yield return new WaitForSeconds(attackSpeed);
            }
            yield return new WaitForSeconds(attackCooldown);
            PerformAttack();
        }

        public void PerformBranchStrikeAttack()
        {
            StartCoroutine(BranchStrikeAttackRoutine());
        }
        
        private IEnumerator BranchStrikeAttackRoutine()
        {
            var count = GetStageValue(branchStrikeStageCount);
            var delay = GetStageValue(branchStrikeStageDelay);
            var speed = GetStageValue(branchStrikeStageSpeed);
            var duration = GetStageValue(branchStrikeStageDuration);
            var attackSpeed = GetStageValue(branchStrikeStageAttackSpeed);
            for (var i = 0; i < count; i++)
            {
                var branchStrikeGameObject = Instantiate(branchStrikePrefab);
                var branchStrike = branchStrikeGameObject.GetComponent<BranchStrike>();
                branchStrike.Strike(delay, speed, duration);
                yield return new WaitForSeconds(attackSpeed);
            }
            yield return new WaitForSeconds(delay + duration + attackCooldown);
            PerformAttack();
        }

        public void PerformMapleLeafSlamAttack()
        {
            StartCoroutine(MapleLeafSlamAttackRoutine());
        }
        
        private IEnumerator MapleLeafSlamAttackRoutine()
        {
            var count = GetStageValue(mapleLeafSlamStageCount);
            var delay = GetStageValue(mapleLeafSlamStageDelay);
            var attackSpeed = GetStageValue(mapleLeafSlamStageAttackSpeed);
            for (var i = 0; i < count; i++)
            {
                var mapleLeafSlamGameObject = Instantiate(mapleLeafSlamPrefab);
                var mapleLeafSlam = mapleLeafSlamGameObject.GetComponent<MapleLeafSlam>();
                mapleLeafSlam.Slam(delay);
                yield return new WaitForSeconds(attackSpeed);
            }
            yield return new WaitForSeconds(delay + attackCooldown);
            PerformAttack();
        }

        private void PerformHedgeSplitAttack()
        {
            StartCoroutine(HedgeSplitSlamAttackRoutine());
        }
        
        private IEnumerator HedgeSplitSlamAttackRoutine()
        {
            var delay = GetStageValue(hedgeSplitStageDelay);
            var duration = GetStageValue(hedgeSplitStageQuestionDuration);

            var hedgeSplitGameObject = Instantiate(hedgeSplitPrefab);
            var hedgeSplit = hedgeSplitGameObject.GetComponent<HedgeSplit>();

            hedgeSplit.Split(delay, duration);

            var question = GetRandomBossQuestion();
            if (question == null)
            {
                yield return new WaitForSeconds(delay + duration + attackCooldown);
                PerformAttack();
                yield break;
            }
            questionBubble.ShowMessage(question.question, delay);

            var setIndexes = new List<int>();
            var correctIndex = 0;
            var answerCount = question.answers.Length;
            for (var i = 0; i < answerCount; i++)
            {
                int index = Random.Range(0, answerCount);
                while (setIndexes.Contains(index)) index = (index + 1 + answerCount) % answerCount;
                setIndexes.Add(index);
                questions[i].text = question.answers[index];
                if (index == question.correctIndex) correctIndex = i;
            }
            
            yield return new WaitForSeconds(delay);

            questions[0].text = questions[1].text = questions[2].text = questions[3].text = string.Empty;

            var currentHealth = _health;
            StartCoroutine(SpawnHeartRoutine(correctIndex, duration));
            SpawnWrongAnswerSlams(correctIndex);

            yield return new WaitForSeconds(duration + attackCooldown);

            if (currentHealth == _health)
            {
                PerformAttack();
                yield break;
            }
            
            StartCoroutine(_health <= 0 ? DieRoutine() : ChangeStateRoutine());
        }

        private IEnumerator SpawnHeartRoutine(int correctIndex, float duration)
        {
            var heart = Instantiate(
                bossHeartPrefab,
                answerPositions[correctIndex].position,
                Quaternion.identity
            );
            
            var bossHeart = heart.GetComponent<BossHeart>();
            bossHeart.Damaged += BossHeartOnDamaged;

            yield return new WaitForSeconds(duration);
            bossHeart.Damaged -= BossHeartOnDamaged;
            Destroy(heart);
        }

        private void BossHeartOnDamaged(BossHeart bossHeart)
        {
            bossHeart.Damaged -= BossHeartOnDamaged;
            _health--;
            _currentPhaseIndex = Mathf.Clamp(maxHealth - _health, 0, 2);
        }

        private void SpawnWrongAnswerSlams(int correctIndex)
        {
            StartCoroutine(SlamWrongAnswersRoutine(correctIndex));
        }

        private IEnumerator SlamWrongAnswersRoutine(int correctIndex)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < answerPositions.Length; i++)
            {
                if (i == correctIndex) continue;

                var slam = Instantiate(questionMapleLeafSlamPrefab);

                var slamComp = slam.GetComponent<MapleLeafSlam>();
                var position = answerPositions[i].position;
                slamComp.transform.position = position;
                slamComp.SlamAtPosition(0.5f);
                yield return new WaitForSeconds(0.4f);
            }
        }

        private IEnumerator ChangeStateRoutine()
        {
            var stage = Mathf.Clamp(_currentPhaseIndex, 0, 2);
            var colorA = Color.white;
            var colorB= Color.white;
            switch (stage)
            {
                case 0:
                    colorA = stage0Color;
                    colorB = stage1Color;
                    break;
                case 1:
                    colorA = stage1Color;
                    colorB = stage2Color;
                    break;
                case 2:
                    colorA = stage2Color;
                    colorB = stage3Color;
                    break;
            }

            float timer = 0;
            while (timer < 3)
            {
                timer += Time.deltaTime;
                _spriteRenderer.color = Color.Lerp(colorA, colorB, timer / 3);
                yield return null;
            }
            
            PerformAttack();
        }

        private void InitializeQuestionPhases()
        {
            for (int phaseIndex = 0; phaseIndex < 3; phaseIndex++)
            {
                _phaseQuestionIndexes[phaseIndex] = new List<int>();
                _phaseQuestionQueue[phaseIndex] = new List<int>();
            }

            if (bossQuestions == null || bossQuestions.Length == 0) return;

            int[] phaseCounts =
            {
                Mathf.Max(0, questionPhaseCounts.x),
                Mathf.Max(0, questionPhaseCounts.y),
                Mathf.Max(0, questionPhaseCounts.z)
            };

            int bossQuestionIndex = 0;
            for (int phaseIndex = 0; phaseIndex < phaseCounts.Length && bossQuestionIndex < bossQuestions.Length; phaseIndex++)
            {
                for (int count = 0; count < phaseCounts[phaseIndex] && bossQuestionIndex < bossQuestions.Length; count++)
                {
                    _phaseQuestionIndexes[phaseIndex].Add(bossQuestionIndex);
                    bossQuestionIndex++;
                }
            }

            while (bossQuestionIndex < bossQuestions.Length)
            {
                _phaseQuestionIndexes[2].Add(bossQuestionIndex);
                bossQuestionIndex++;
            }

            for (int phaseIndex = 0; phaseIndex < 3; phaseIndex++)
            {
                if (_phaseQuestionIndexes[phaseIndex].Count == 0)
                {
                    _phaseQuestionIndexes[phaseIndex].AddRange(_phaseQuestionIndexes[Mathf.Max(0, phaseIndex - 1)]);
                }

                RefillPhaseQuestionQueue(phaseIndex);
            }
        }

        private void RefillPhaseQuestionQueue(int phaseIndex)
        {
            if (_phaseQuestionIndexes[phaseIndex] == null) return;

            _phaseQuestionQueue[phaseIndex].Clear();
            _phaseQuestionQueue[phaseIndex].AddRange(_phaseQuestionIndexes[phaseIndex]);

            for (int i = _phaseQuestionQueue[phaseIndex].Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                int temp = _phaseQuestionQueue[phaseIndex][i];
                _phaseQuestionQueue[phaseIndex][i] = _phaseQuestionQueue[phaseIndex][randomIndex];
                _phaseQuestionQueue[phaseIndex][randomIndex] = temp;
            }
        }

        private IEnumerator DieRoutine()
        {
            _audioSource.Stop();
            _animator.SetTrigger("Die");
            yield return new WaitForSeconds(3f);
            bridgeAnimator.SetTrigger("Show");
            if (enableOnFightStart) enableOnFightStart.SetActive(false);
            if (disableOnFightStart) disableOnFightStart.SetActive(true);
            bossDefeatedSignal?.Raise();
        }
    }
}
