using UnityEngine;

namespace Watermelon.SquadShooter
{
    public class RocketGunBehavior : BaseGunBehavior
    {
        [LineSpacer]
        [SerializeField] ParticleSystem shootParticleSystem;

        [SerializeField] LayerMask targetLayers;
        [SerializeField] float bulletDisableTime = 3f;

        private float attackDelay;
        private DuoFloat bulletSpeed;
        private float bulletSpreadAngle;

        private float nextShootTime;
        private float lastShootTime;

        private Pool bulletPool;

        private TweenCase shootTweenCase;
        private Vector3 shootDirection;

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
            bulletSpreadAngle = currentUpgrade.Spread;
            attackDelay = 1f / currentUpgrade.FireRate;
            bulletSpeed = currentUpgrade.BulletSpeed;
        }

        public override void GunUpdate()
        {
            AttackButtonBehavior.SetReloadFill(1 - (Time.timeSinceLevelLoad - lastShootTime) / (nextShootTime - lastShootTime));

            // Combat
            if (!characterBehaviour.IsCloseEnemyFound)
                return;

            if (nextShootTime >= Time.timeSinceLevelLoad) return;
            if (!characterBehaviour.IsAttackingAllowed) return;

            AttackButtonBehavior.SetReloadFill(0);

            shootDirection = characterBehaviour.ClosestEnemyBehaviour.transform.position.SetY(shootPoint.position.y) - shootPoint.position;

            var raycastOrigin = shootPoint.position - shootDirection.normalized * 0.5f;
            if (Physics.Raycast(raycastOrigin, shootDirection, out var hitInfo, 300f, targetLayers))
            {
                if (hitInfo.collider.gameObject.layer == PhysicsHelper.LAYER_ENEMY)
                {
                    if (Vector3.Angle(shootDirection, transform.forward) < 40f)
                    {
                        characterBehaviour.SetTargetActive();

                        shootTweenCase.KillActive();

                        // Visual recoil animation
                        shootTweenCase = transform.DOLocalMoveZ(-0.25f, 0.15f).OnComplete(delegate
                        {
                            shootTweenCase = transform.DOLocalMoveZ(0, 0.25f);
                        });

                        if (shootParticleSystem != null)
                        {
                            shootParticleSystem.Play();
                        }

                        nextShootTime = Time.timeSinceLevelLoad + attackDelay;
                        lastShootTime = Time.timeSinceLevelLoad;

                        int bulletsNumber = weapon.GetCurrentUpgrade().BulletsPerShot.Random();

                        Vector3 targetEuler = Quaternion.LookRotation(shootDirection).eulerAngles;
                        for (int i = 0; i < bulletsNumber; i++)
                        {
                            // Spawn rocket bullet
                            RocketBulletBehavior bullet = bulletPool.GetPooledObject()
                                .SetPosition(shootPoint.position)
                                .SetEulerAngles(targetEuler)
                                .GetComponent<RocketBulletBehavior>();

                            if (bullet != null)
                            {
                                bullet.Init(damage.Random() * characterBehaviour.Stats.BulletDamageMultiplier, bulletSpeed.Random(), characterBehaviour.ClosestEnemyBehaviour, bulletDisableTime);
                                bullet.transform.Rotate(new Vector3(0f, i == 0 ? 0f : Random.Range(bulletSpreadAngle * -0.5f, bulletSpreadAngle * 0.5f), 0f));
                            }
                        }

                        characterBehaviour.OnGunShooted(); 
                        
                        VirtualCamera gameCameraCase = CameraController.GetCamera(CameraType.Gameplay);
                        if (gameCameraCase != null)
                        {
                            gameCameraCase.Shake(0.08f, 0.08f, 0.4f, 1.0f);
                        }

                        AudioController.PlaySound(AudioController.AudioClips.shotShotgun, volumePercentage: 1.0f); // Heavy shotgun blast as rocket launch sound
                    }
                }
                else
                {
                    characterBehaviour.SetTargetUnreachable();
                }
            }
            else
            {
                characterBehaviour.SetTargetUnreachable();
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

            Gizmos.DrawLine(shootPoint.position - shootDirection.normalized * 10f, characterBehaviour.ClosestEnemyBehaviour.transform.position.SetY(shootPoint.position.y));

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
            transform.SetParent(characterGraphics.RocketHolderTransform);
            transform.ResetLocal();
        }

        public override void Reload()
        {
            bulletPool?.ReturnToPoolEverything();
        }
    }
}
