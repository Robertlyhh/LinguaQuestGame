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

        private void Awake()
        {
            _health = maxHealth;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _spriteRenderer.color = stage0Color;
            _boxCollider2D = GetComponent<BoxCollider2D>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();
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
            return bossQuestions[Random.Range(0, bossQuestions.Length)];
        }

        public void PerformAttack()
        {
            if (_attacksCount >= attacksUntilQuestion)
            {
                _attacksCount = 0;
                PerformHedgeSplitAttack();
                return;
            }
            
            //PerformMapleLeafSlamAttack();
            //return;
            
            var rand = Random.Range(0, 3);
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
            var ratio = (float)_health / maxHealth;
            if (ratio < hardStageUpperPercentage) return value.z;
            return ratio < mediumStageUpperPercentage ? value.y : value.x;
        }

        public void PerformRollingLogAttack()
        {
            StartCoroutine(RollingLogAttackRoutine());
        }

        private IEnumerator RollingLogAttackRoutine()
        {
            var count = GetStageValue(rollingLogStageCount);
            var speed = GetStageValue(rollingLogStageSpeed);
            var attackSpeed = GetStageValue(rollingLogStageAttackSpeed);
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
            var stage = GetStageValue(new Vector3(0, 1, 2));
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
