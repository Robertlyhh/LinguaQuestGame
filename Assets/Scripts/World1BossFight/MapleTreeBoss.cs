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
