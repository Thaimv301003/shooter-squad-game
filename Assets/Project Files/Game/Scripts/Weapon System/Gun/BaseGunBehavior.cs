using UnityEngine;
using Watermelon;

namespace Watermelon.SquadShooter
{
    public abstract class BaseGunBehavior : MonoBehaviour
    {
        private static readonly int PARTICLE_UPGRADE = "Gun Upgrade".GetHashCode();

        [Header("Animations")]
        [SerializeField] AnimationClip characterShootAnimation;

        [Space]
        [SerializeField] GunHolder gunHolder;

        [Space]
        [SerializeField] 
        protected Transform shootPoint;

        [Header("Upgrade")]
        [SerializeField] Vector3 upgradeParticleOffset;
        [SerializeField] float upgradeParticleSize = 1.0f;

        [Header("Custom Offsets")]
        [SerializeField] bool useCustomOffsets;
        [SerializeField] Vector3 lobbyPosition;
        [SerializeField] Vector3 lobbyRotation;
        [SerializeField] Vector3 gameplayPosition;
        [SerializeField] Vector3 gameplayRotation;

        protected CharacterBehaviour characterBehaviour;
        protected WeaponData weapon;

        protected DuoInt damage;
        public DuoInt Damage => damage;

        private Transform leftHandRigController;
        private Vector3 leftHandExtraRotation;

        private Transform rightHandRigController;
        private Vector3 rightHandExtraRotation;

        private GunHolder.HolderData activeHolderData;

        public virtual void Init(CharacterBehaviour characterBehaviour, WeaponData data)
        {
            this.characterBehaviour = characterBehaviour;
            this.weapon = data;
        }

        public void UpdateOffset(bool isInGame)
        {
            if (!useCustomOffsets) return;

            Transform visualTransform = transform.Find("GunOffset");
            if (visualTransform == null)
            {
                visualTransform = transform.Find("Shotgun_muzzle");
            }
            if (visualTransform == null)
            {
                visualTransform = transform.Find("lazel");
            }
            if (visualTransform == null)
            {
                visualTransform = transform.Find("lazel Variant");
            }
            if (visualTransform == null)
            {
                if (transform.childCount > 0)
                {
                    visualTransform = transform.GetChild(0);
                }
            }

            if (visualTransform != null)
            {
                if (isInGame)
                {
                    visualTransform.localPosition = gameplayPosition;
                    visualTransform.localRotation = Quaternion.Euler(gameplayRotation);
                }
                else
                {
                    visualTransform.localPosition = lobbyPosition;
                    visualTransform.localRotation = Quaternion.Euler(lobbyRotation);
                }
            }
        }

        public void InitCharacter(BaseCharacterGraphics characterGraphics)
        {
            leftHandRigController = characterGraphics.LeftHandRig.data.target;
            rightHandRigController = characterGraphics.RightHandRig.data.target;

            leftHandExtraRotation = characterGraphics.LeftHandExtraRotation;
            rightHandExtraRotation = characterGraphics.RightHandExtraRotation;

            characterGraphics.SetShootingAnimation(characterShootAnimation);

            CharacterData character = CharactersController.SelectedCharacter;
            activeHolderData = gunHolder.GetHolderData(character);
        }

        public virtual void OnLevelLoaded()
        {
            RecalculateDamage();
        }

        public virtual void GunUpdate()
        {

        }

        public void UpdateHandRig()
        {
            if (leftHandRigController == null || rightHandRigController == null) return;
            if (activeHolderData == null)
            {
                Debug.LogWarning($"[BaseGunBehavior] activeHolderData is null for weapon {gameObject.name}!");
                return;
            }
            if (activeHolderData.LeftHandHolder == null || activeHolderData.RightHandHolder == null)
            {
                Debug.LogWarning($"[BaseGunBehavior] LeftHandHolder or RightHandHolder is missing in GunHolder data for weapon {gameObject.name}! Please assign them in the prefab Inspector.");
                return;
            }

            leftHandRigController.position = activeHolderData.LeftHandHolder.position;
            rightHandRigController.position = activeHolderData.RightHandHolder.position;

#if UNITY_EDITOR
            if(characterBehaviour != null && characterBehaviour.Graphics != null)
            {
                leftHandExtraRotation = characterBehaviour.Graphics.LeftHandExtraRotation;
                rightHandExtraRotation = characterBehaviour.Graphics.RightHandExtraRotation;
            }
#endif

            leftHandRigController.rotation = Quaternion.Euler(activeHolderData.LeftHandHolder.eulerAngles + leftHandExtraRotation);
            rightHandRigController.rotation = Quaternion.Euler(activeHolderData.RightHandHolder.eulerAngles + rightHandExtraRotation);
        }

        public abstract void Reload();
        public abstract void OnGunUnloaded();
        public abstract void PlaceGun(BaseCharacterGraphics characterGraphics);

        public abstract void RecalculateDamage();

        public AnimationClip GetShootAnimationClip()
        {
            return characterShootAnimation;
        }

        public virtual void PlayBounceAnimation()
        {
            transform.localScale = Vector3.one * 0.6f;
            transform.DOScale(Vector3.one, 0.4f).SetEasing(Ease.Type.BackOut);
        }

        public void SetDamage(DuoInt damage)
        {
            this.damage = damage;
        }

        public void SetDamage(int minDamage, int maxDamage)
        {
            damage = new DuoInt(minDamage, maxDamage);
        }

        public void PlayUpgradeParticle()
        {
            ParticleCase particleCase = ParticlesController.PlayParticle(PARTICLE_UPGRADE).SetPosition(transform.position + upgradeParticleOffset).SetScale(upgradeParticleSize.ToVector3());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireCube(transform.position + upgradeParticleOffset, upgradeParticleSize.ToVector3());
        }

#if UNITY_EDITOR
        [Button("Prepare Weapon")]
        private void PrepareWeapon()
        {
            if(gunHolder.DefaultHolderData.LeftHandHolder == null)
            {
                GameObject leftHandHolderObject = new GameObject("Left Hand Holder");
                leftHandHolderObject.transform.SetParent(transform);
                leftHandHolderObject.transform.ResetLocal();
                leftHandHolderObject.transform.localPosition = new Vector3(-0.4f, 0, 0);

                GUIContent iconContent = UnityEditor.EditorGUIUtility.IconContent("sv_label_3");
                UnityEditor.EditorGUIUtility.SetIconForObject(leftHandHolderObject, (Texture2D)iconContent.image);

                gunHolder.DefaultHolderData.LeftHandHolder = leftHandHolderObject.transform;
            }

            if (gunHolder.DefaultHolderData.RightHandHolder == null)
            {
                GameObject rightHandHolderObject = new GameObject("Right Hand Holder");
                rightHandHolderObject.transform.SetParent(transform);
                rightHandHolderObject.transform.ResetLocal();
                rightHandHolderObject.transform.localPosition = new Vector3(0.4f, 0, 0);

                GUIContent iconContent = UnityEditor.EditorGUIUtility.IconContent("sv_label_4");
                UnityEditor.EditorGUIUtility.SetIconForObject(rightHandHolderObject, (Texture2D)iconContent.image);

                gunHolder.DefaultHolderData.RightHandHolder = rightHandHolderObject.transform;
            }

            if(shootPoint == null)
            {
                GameObject shootingPointObject = new GameObject("Shooting Point");
                shootingPointObject.transform.SetParent(transform);
                shootingPointObject.transform.ResetLocal();
                shootingPointObject.transform.localPosition = new Vector3(0, 0, 1);

                GUIContent iconContent = UnityEditor.EditorGUIUtility.IconContent("sv_label_1");
                UnityEditor.EditorGUIUtility.SetIconForObject(shootingPointObject, (Texture2D)iconContent.image);

                shootPoint = shootingPointObject.transform;
            }

            if(characterShootAnimation == null)
            {
                characterShootAnimation = RuntimeEditorUtils.GetAssetByName<AnimationClip>("Shot");
            }
        }
#endif
    }
}