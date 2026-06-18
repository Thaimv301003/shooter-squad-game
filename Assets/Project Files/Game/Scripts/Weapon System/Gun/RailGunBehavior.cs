using UnityEngine;

namespace Watermelon.SquadShooter
{
    public class RailGunBehavior : BaseGunBehavior
    {
        [LineSpacer]
        [SerializeField] ParticleSystem shootParticleSystem;
        [SerializeField] GameObject chargeLoopParticle;

        [SerializeField] LayerMask targetLayers;
        [SerializeField] float chargeDuration;

        private DuoFloat bulletSpeed;
        private float spread;
        private float attackDelay;
        private float bulletDisableTime = 3f;

        private Pool bulletPool;

        private TweenCase shootTweenCase;
        private Vector3 shootDirection;

        private bool isCharging;
        private bool isCharged;
        private bool isChargeParticleActivated;
        private float fullChargeTime;
        private float startChargeTime;

        public override void Init(CharacterBehaviour characterBehaviour, WeaponData weapon)
        {
            base.Init(characterBehaviour, weapon);

            WeaponUpgrade currentUpgrade = weapon.GetCurrentUpgrade();

            GameObject bulletObj = currentUpgrade.BulletPrefab;

            bulletPool = new Pool(bulletObj, bulletObj.name);

            RecalculateDamage();
        }

        private void OnDestroy()
        {
            if (bulletPool != null)
                PoolManager.DestroyPool(bulletPool);
        }

        public override void OnLevelLoaded()
        {
            RecalculateDamage();
        }

        public override void RecalculateDamage()
        {
            WeaponUpgrade currentUpgrade = weapon.GetCurrentUpgrade();

            damage = currentUpgrade.Damage;
            bulletSpeed = currentUpgrade.BulletSpeed;
            spread = currentUpgrade.Spread;
            // For Railgun, attackDelay can be the chargeDuration or a mix of both. We default to using chargeDuration.
            if (chargeDuration <= 0f)
            {
                chargeDuration = 0.5f; // fallback default charge duration
            }
        }

        public override void GunUpdate()
        {
            if (!isCharging && !isCharged)
            {
                AttackButtonBehavior.SetReloadFill(1);
            }

            // If no enemy - cancel charge
            if (!characterBehaviour.IsCloseEnemyFound)
            {
                if (isCharging || isCharged)
                {
                    CancelCharge();
                }

                return;
            }

            // If not charging - start charging
            if (!isCharging && !isCharged)
            {
                isCharging = true;
                isChargeParticleActivated = false;
                fullChargeTime = Time.timeSinceLevelLoad + chargeDuration;
                startChargeTime = Time.timeSinceLevelLoad;
            }

            // Wait for full charge
            if (fullChargeTime >= Time.timeSinceLevelLoad)
            {
                AttackButtonBehavior.SetReloadFill(1 - (Time.timeSinceLevelLoad - startChargeTime) / (fullChargeTime - startChargeTime));

                // Start charge particle 0.3 sec before charge complete
                if (!isChargeParticleActivated && fullChargeTime - Time.timeSinceLevelLoad <= 0.3f)
                {
                    isChargeParticleActivated = true;
                    if (shootParticleSystem != null)
                    {
                        shootParticleSystem.Play();
                    }
                }

                if (IsEnemyVisible())
                {
                    characterBehaviour.SetTargetActive();
                }
                else
                {
                    characterBehaviour.SetTargetUnreachable();
                }

                return;
            }
            // Activate loop particle once charged
            else if (!isCharged)
            {
                AttackButtonBehavior.SetReloadFill(0);
                isCharged = true;
                if (chargeLoopParticle != null)
                {
                    chargeLoopParticle.SetActive(true);
                }
            }

            if (IsEnemyVisible() && characterBehaviour.IsAttackingAllowed)
            {
                characterBehaviour.SetTargetActive();

                shootTweenCase.KillActive();

                shootTweenCase = transform.DOLocalMoveZ(-0.15f, chargeDuration * 0.3f).OnComplete(delegate
                {
                    shootTweenCase = transform.DOLocalMoveZ(0, chargeDuration * 0.6f);
                });

                int bulletsNumber = weapon.GetCurrentUpgrade().BulletsPerShot.Random();

                Vector3 targetEuler = Quaternion.LookRotation(shootDirection).eulerAngles;
                for (int k = 0; k < bulletsNumber; k++)
                {
                    // Spawn piercing electric beam bullet
                    ElectricBeamBulletBehavior bullet = bulletPool.GetPooledObject()
                        .SetPosition(shootPoint.position)
                        .SetEulerAngles(targetEuler + Vector3.up * Random.Range(-spread, spread))
                        .GetComponent<ElectricBeamBulletBehavior>();
                    
                    if (bullet != null)
                    {
                        // autoDisableOnHit is set to false to allow piercing
                        bullet.Init(damage.Random() * characterBehaviour.Stats.BulletDamageMultiplier, bulletSpeed.Random(), characterBehaviour.ClosestEnemyBehaviour, bulletDisableTime, false);
                    }
                }

                characterBehaviour.OnGunShooted();

                VirtualCamera gameCameraCase = CameraController.GetCamera(CameraType.Gameplay);
                if (gameCameraCase != null)
                {
                    gameCameraCase.Shake(0.06f, 0.06f, 0.4f, 1.2f); // stronger shake for Railgun
                }

                CancelCharge();

                AudioController.PlaySound(AudioController.AudioClips.shotTesla, volumePercentage: 0.9f); // play high-tech shot sound
            }
            else
            {
                characterBehaviour.SetTargetUnreachable();
            }
        }

        public bool IsEnemyVisible()
        {
            if (!characterBehaviour.IsCloseEnemyFound)
                return false;

            shootDirection = characterBehaviour.ClosestEnemyBehaviour.transform.position.SetY(shootPoint.position.y) - shootPoint.position;

            RaycastHit hitInfo;
            if (Physics.Raycast(shootPoint.position - shootDirection.normalized * 1.5f, shootDirection, out hitInfo, 300f, targetLayers) ||
                Physics.Raycast(shootPoint.position, shootDirection, out hitInfo, 300f, targetLayers)
            )
            {
                if (hitInfo.collider.gameObject.layer == PhysicsHelper.LAYER_ENEMY)
                {
                    if (Vector3.Angle(shootDirection, transform.forward) < 40f)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CancelCharge()
        {
            isCharging = false;
            isCharged = false;
            isChargeParticleActivated = false;
            if (chargeLoopParticle != null)
            {
                chargeLoopParticle.SetActive(false);
            }
            if (shootParticleSystem != null)
            {
                shootParticleSystem.Stop();
            }
        }

        private void OnDrawGizmos()
        {
            if (characterBehaviour == null)
                return;

            if (characterBehaviour.ClosestEnemyBehaviour == null)
                return;

            Color defCol = Gizmos.color;
            Gizmos.color = Color.red;

            Vector3 shootDirection = characterBehaviour.ClosestEnemyBehaviour.transform.position.SetY(shootPoint.position.y) - shootPoint.position;

            Gizmos.DrawLine(shootPoint.position - shootDirection.normalized * 1.5f, characterBehaviour.ClosestEnemyBehaviour.transform.position.SetY(shootPoint.position.y));

            Gizmos.color = defCol;
        }

        public override void OnGunUnloaded()
        {
            if (bulletPool != null)
            {
                PoolManager.DestroyPool(bulletPool);
                bulletPool = null;
            }
        }

        public override void PlaceGun(BaseCharacterGraphics characterGraphics)
        {
            transform.SetParent(characterGraphics.RailgunHolderTransform);
            transform.ResetLocal();
        }

        public override void Reload()
        {
            if (bulletPool != null)
            {
                bulletPool.ReturnToPoolEverything();
            }
        }
    }
}
