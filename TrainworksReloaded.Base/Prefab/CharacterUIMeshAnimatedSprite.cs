using DG.Tweening;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static CharacterUI;
using static SaveManager;

namespace TrainworksReloaded.Base.Prefab
{
    [Serializable]
    public class SpriteAnimationClip : ScriptableObject
    {
        [SerializeField]
        public CharacterUI.Anim animType;
        [SerializeField]
        public List<Sprite> frames = [];
        [SerializeField]
        public bool isLooping;
        [SerializeField]
        public float frameRate = 12f;
        [SerializeField]
        public Vector2 adjustment = Vector2.zero;
        public SpriteAnimationClip() {}

        public float FrameDuration => frameRate > 0f ? 1f / frameRate : 0.1f;
        public float TotalDuration => frames.Count * FrameDuration;
    }

    [Serializable]
    public class AnimatedSpriteModel : ScriptableObject
    {
        [SerializeField]
        public int modelId;
        [SerializeField]
        private readonly List<SpriteAnimationClip> clips = [];

        public AnimatedSpriteModel() { }

        public void AddClip(SpriteAnimationClip clip)
        {
            clips.Add(clip);
        }

        public SpriteAnimationClip GetClip(CharacterUI.Anim animType)
        {
            return clips.Find(c => c.animType == animType);
        }

        public bool HasAnim(CharacterUI.Anim animType) => clips.Exists(c => c.animType == animType);

        public int AnimCount() => clips.Count;
    }

    [Serializable]
    public class AnimatedSpriteContainer : ScriptableObject
    {
        [SerializeField]
        private readonly List<AnimatedSpriteModel> models = [];

        public AnimatedSpriteContainer() {}

        public void AddModel(AnimatedSpriteModel model)
        {
            models.Add(model);
        }

        private AnimatedSpriteModel? FindModel(int modelIndex)
        {
            return models.Find(c => c.modelId == modelIndex);
        }

        public bool HasModel(int modelIndex)
        {
            return models.Exists(c => c.modelId == modelIndex);
        }

        public SpriteAnimationClip? GetClip(int modelIndex, CharacterUI.Anim animType)
        {
            return FindModel(modelIndex)?.GetClip(animType);
        }

        public bool HasAnim(int modelIndex, CharacterUI.Anim animType)
        {
            return FindModel(modelIndex)?.HasAnim(animType) ?? false;
        }

        public int AnimCount(int modelIndex)
        {
            return FindModel(modelIndex)?.AnimCount() ?? 0;
        }
    }

    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class CharacterUIMeshAnimatedSprite : CharacterUIMeshBase
    {
        // Shader property IDs corresponding to CharacterShader2.0 Graph
        private static readonly int _matPropIdMainTex = Shader.PropertyToID("_MainTex");
        private static readonly int _matPropIdColor = Shader.PropertyToID("_Color");
        private static readonly int _matPropIdGrayscale = Shader.PropertyToID("_GrayScale_Factor");
        private static readonly int _matPropIdDissolveAmount = Shader.PropertyToID("_DissolveAmount");
        private static readonly int _matPropIdSelectionOutline = Shader.PropertyToID("_Selection_Outline");
        private static readonly int _matPropIdSelectionValid = Shader.PropertyToID("_Selected");
        private static readonly int _matPropIdSelectionInvalid = Shader.PropertyToID("_Invalid");
        private static readonly int _matPropIdBlackoutSlider = Shader.PropertyToID("_Blackout_Slider");
        private static readonly int _matPropIdChosen = Shader.PropertyToID("_Chosen");
        private static readonly int _matPropIdChosen2 = Shader.PropertyToID("_Chosen_2");

        private MeshRenderer? _meshRenderer;
        private MaterialPropertyBlock? _matProps;
        private CharacterClipAgainstRoom? _clipAgainstRoom;
        private Material? _clonedMaterial;

        // Tweens
        private Tween? _grayscaleTween;
        private Tween? _colorTween;
        private Tween? _dissolveTween;

        // Animation runtime state
        [SerializeField]
        private AnimatedSpriteContainer? _animContainer;
        [SerializeField]
        private SpriteAnimationClip? _currentClip;
        private int _currentFrameIndex;
        private float _frameTimer;
        private bool _isPlaying;
        private bool _overrideLooping;
        private float _baseHeight = 1f;
        private Vector2 _currentClipOffset = Vector2.zero;
        private CharacterUI.Anim _currentAnimType = CharacterUI.Anim.None;
        private Action<CharacterUI.AnimNote>? _animCallback;

        // Sorting & Multi-model Variations
        private int _sortingOrder;
        private SortingLayers _sortingLayer;
        private int _curModelIndex;
        [SerializeField]
        private List<CharacterUIMeshSpine.ModelEntry> _emptyModelVariations = [];

        public MeshRenderer meshRenderer
        {
            get
            {
                if (_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }
                return _meshRenderer;
            }
        }

        public Color MaterialColor
        {
            get => _matProps != null ? _matProps.GetColor(_matPropIdColor) : CharacterUIMeshBase.FadeinColor;
            set
            {
                _matProps!.SetColor(_matPropIdColor, value);
                meshRenderer.SetPropertyBlock(_matProps);
            }
        }

        public float MaterialGrayscale
        {
            get => _matProps != null ? _matProps.GetFloat(_matPropIdGrayscale) : 0f;
            set
            {
                _matProps!.SetFloat(_matPropIdGrayscale, value);
                meshRenderer.SetPropertyBlock(_matProps);
            }
        }

        public float MaterialDissolveAmount
        {
            get => _matProps != null ? _matProps.GetFloat(_matPropIdDissolveAmount) : 0f;
            set
            {
                _matProps!.SetFloat(_matPropIdDissolveAmount, value);
                meshRenderer.SetPropertyBlock(_matProps);
            }
        }

        private void Awake()
        {
            _matProps ??= new MaterialPropertyBlock();
        }

        private void OnDestroy()
        {
            CharacterUIMeshBase.KillTween(ref _grayscaleTween);
            CharacterUIMeshBase.KillTween(ref _colorTween);
            CharacterUIMeshBase.KillTween(ref _dissolveTween);

            if (_clonedMaterial != null)
            {
                Destroy(_clonedMaterial);
                _clonedMaterial = null;
            }

            if (_clipAgainstRoom != null)
            {
                _clipAgainstRoom.OnDestroy();
                _clipAgainstRoom = null;
            }
        }

        public void InitializeClips(AnimatedSpriteContainer animContainer)
        {
            _animContainer = animContainer;
        }

        public override void Setup(Sprite charSprite, float facingDir, string debugName, out Bounds meshBounds)
        {
            if (_clonedMaterial == null)
            {
                _clonedMaterial = meshRenderer.material;
            }

            if (_matProps == null)
            {
                _matProps = new MaterialPropertyBlock();
            }

            // Capture the base reference height from the idle/character sprite
            if (charSprite != null)
            {
                _baseHeight = charSprite.bounds.size.y;
            }
            else
            {
                var idleClip = _animContainer!.GetClip(0, CharacterUI.Anim.Idle);
                _baseHeight = idleClip?.frames[0].bounds.size.y ?? 1.5f;
            }

            // Initialize default shader values for CharacterShader2.0 Graph
            _matProps.SetColor(_matPropIdColor, CharacterUIMeshBase.FadeinColor);
            _matProps.SetFloat(_matPropIdGrayscale, 0f);
            _matProps.SetFloat(_matPropIdDissolveAmount, 0f);
            _matProps.SetFloat(_matPropIdBlackoutSlider, 0f);
            _matProps.SetFloat(_matPropIdSelectionOutline, 0f);
            _matProps.SetFloat(_matPropIdSelectionValid, 0f);
            _matProps.SetFloat(_matPropIdSelectionInvalid, 0f);

            if (charSprite != null)
            {
                SetSpriteFrame(charSprite);
                // Calculate bounds using sprite dimensions so CharacterUI anchors and scaling match floor height
                meshBounds = new Bounds(Vector3.zero, new Vector3(charSprite.bounds.size.x, charSprite.bounds.size.y, 0.1f));
            }
            else
            {
                meshBounds = GetComponent<MeshFilter>().mesh.bounds;
            }

            _clipAgainstRoom ??= new CharacterClipAgainstRoom(transform, meshBounds);

            if (HasAnim(CharacterUI.Anim.Idle))
            {
                PlayAnimLoop(CharacterUI.Anim.Idle, 0f);
            }
        }

        private float GetAnimSpeedMultiplier(CharacterUI.Anim anim, GameSpeed battleSpeed)
        {
            // Idle and Death run at static real-time speed
            if (anim == CharacterUI.Anim.Idle || anim == CharacterUI.Anim.Death || anim == CharacterUI.Anim.Hover || anim == CharacterUI.Anim.Idle_Relentless || anim == CharacterUI.Anim.Talk)
            {
                return 1.0f;
            }

            switch (battleSpeed)
            {
                case GameSpeed.Fast:
                    return 1.714f; // 0.60s / 0.35s

                case GameSpeed.Ultra:
                    return 2.400f; // 0.60s / 0.25s

                case GameSpeed.SuperUltra:
                    if (anim == CharacterUI.Anim.Attack)
                    {
                        return 8.0f; // 0.60s / 0.075s
                    }
                    if (anim == CharacterUI.Anim.HitReact)
                    {
                        return 3.0f; // 0.60s / 0.20s
                    }
                    return 3.0f;

                case GameSpeed.Normal:
                default:
                    return 1.0f;
            }
        }

        private void Update()
        {
            if (!_isPlaying || _currentClip == null || _currentClip.frames == null || _currentClip.frames.Count == 0)
            {
                return;
            }

            float speedMultiplier = GetAnimSpeedMultiplier(_currentClip.animType, AllGameManagers.Instance!.GetSaveManager().GetActiveGameSpeed());
            _frameTimer += Time.deltaTime * speedMultiplier;

            float frameDuration = _currentClip.FrameDuration;

            while (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _currentFrameIndex++;

                if (_currentFrameIndex >= _currentClip.frames.Count)
                {
                    if (_overrideLooping || _currentClip.isLooping)
                    {
                        _currentFrameIndex = 0;
                        ApplyCurrentFrame();
                    }
                    else
                    {
                        _currentFrameIndex = _currentClip.frames.Count - 1;
                        _isPlaying = false;
                        ApplyCurrentFrame();

                        // Complete is required to return characters from HitReact and action sequences to Idle
                        Action<CharacterUI.AnimNote>? callback = _animCallback;
                        _animCallback = null;
                        callback?.Invoke(CharacterUI.AnimNote.Complete);
                        return;
                    }
                }
                else
                {
                    ApplyCurrentFrame();
                }
            }
        }

        private void ApplyCurrentFrame()
        {
            if (_currentClip != null && _currentFrameIndex >= 0 && _currentFrameIndex < _currentClip.frames.Count)
            {
                Sprite currentSprite = _currentClip.frames[_currentFrameIndex];
                if (currentSprite != null)
                {
                    SetSpriteFrame(currentSprite);
                }
            }
        }

        private void SetSpriteFrame(Sprite sprite)
        {
            _matProps!.SetTexture(_matPropIdMainTex, sprite.texture);
            meshRenderer.SetPropertyBlock(_matProps);

            float width = sprite.bounds.size.x;
            float height = sprite.bounds.size.y;

            //meshRenderer.transform.localScale = new Vector3(width, height, 1f);

            float autoLiftY = (height - _baseHeight) * 0.5f;
            meshRenderer.transform.localPosition = new Vector3(
                    _currentClipOffset.x,
                    autoLiftY + _currentClipOffset.y,
                    0f
                );
        }

        // --- Animation Playback ---

        public override CharacterUI.Anim GetCurrentAnim() => _currentAnimType;

        public override bool HasAnim(CharacterUI.Anim animType)
        {
            return HasAnim(_curModelIndex, animType);
        }

        private bool HasAnim(int modelIndex, CharacterUI.Anim animType)
        {
            return _animContainer != null && _animContainer.HasAnim(modelIndex, animType);
        }

        public override float GetAnimDuration(CharacterUI.Anim animType)
        {
            return GetAnimDuration(_curModelIndex, animType);
        }

        private float GetAnimDuration(int modelIndex, CharacterUI.Anim animType)
        {
            return _animContainer?.GetClip(modelIndex, animType)?.TotalDuration ?? 0f;
        }

        public override void PlayAnim(CharacterUI.Anim animType, float duration, float startTime, Action<CharacterUI.AnimNote> animCallback)
        {
            PlayAnimInternal(_curModelIndex, animType, loop: false, startTime, animCallback);
        }

        public void PlayAnim(CharacterUI.Anim animType, float duration, float startTime)
        {
            PlayAnimInternal(_curModelIndex, animType, loop: false, startTime, null);
        }

        public override void PlayAnimLoop(CharacterUI.Anim animType, float startTime, Action<CharacterUI.AnimNote>? animCallback = null)
        {
            PlayAnimInternal(_curModelIndex, animType, loop: true, startTime, animCallback);
        }

        public void PlayAnimLoop(CharacterUI.Anim animType, float startTime)
        {
            PlayAnimInternal(_curModelIndex, animType, loop: true, startTime, null);
        }

        private void PlayAnimInternal(int modelIndex, CharacterUI.Anim animType, bool loop, float startTime, Action<CharacterUI.AnimNote>? animCallback)
        {
            if (_animContainer == null || !_animContainer.HasModel(modelIndex) || !HasAnim(modelIndex, animType))
            {
                return;
            }

            // Never allow HitReact or Idle to interrupt an active Death animation
            if (_currentAnimType == CharacterUI.Anim.Death && animType != CharacterUI.Anim.Death)
            {
                return;
            }

            SpriteAnimationClip? clip = _animContainer.GetClip(modelIndex, animType);
            if (clip == null || clip.frames == null || clip.frames.Count == 0)
                return;

            _currentClip = clip;
            _currentAnimType = animType;
            _animCallback = animCallback;
            _overrideLooping = loop;
            _isPlaying = true;
            _curModelIndex = modelIndex;

            int startFrame = Mathf.FloorToInt(startTime / clip.FrameDuration);
            _currentFrameIndex = Mathf.Clamp(startFrame, 0, clip.frames.Count - 1);
            _frameTimer = startTime % clip.FrameDuration;

            _currentClipOffset = clip != null ? clip.adjustment : Vector2.zero;

            ApplyCurrentFrame();
        }

        // --- Visual Effects & Tweens ---

        public override void DoSpriteFadeout(float duration, bool fadeToBlack, float setDelay = 0f)
        {
            CharacterUIMeshBase.KillTween(ref _colorTween);
            Color initialColor = fadeToBlack ? CharacterUIMeshBase.FadeinColor : CharacterUIMeshBase.FadeoutColor;
            Color endColor = fadeToBlack ? CharacterUIMeshBase.FadeoutColor : CharacterUIMeshBase.FadeinColor;

            MaterialColor = initialColor;
            _colorTween = DOTween.To(() => MaterialColor, x => MaterialColor = x, endColor, duration).SetDelay(setDelay);
        }

        public override void DoSpriteFade(float duration, float alpha)
        {
            CharacterUIMeshBase.KillTween(ref _colorTween);
            alpha = Mathf.Clamp01(alpha);
            Color endColor = new Color(1f, 1f, 1f, alpha);
            float currentAlpha = MaterialColor.a;
            duration *= Mathf.Abs(alpha - currentAlpha);
            _colorTween = DOTween.To(() => MaterialColor, x => MaterialColor = x, endColor, duration);
        }

        public override void DoColorFlash(float duration, Color toColor, Action? callback = null)
        {
            CharacterUIMeshBase.KillTween(ref _colorTween);
            MaterialColor = CharacterUIMeshBase.FadeinColor;
            _colorTween = DOTween.To(() => MaterialColor, x => MaterialColor = x, toColor, duration * 0.5f).OnComplete(() =>
            {
                CharacterUIMeshBase.KillTween(ref _colorTween);
                _colorTween = DOTween.To(() => MaterialColor, x => MaterialColor = x, CharacterUIMeshBase.FadeinColor, duration * 0.5f);
                callback?.Invoke();
            });
        }

        public override void DoGrayscale(float transitionTime, float grayscaleValue, Action? callback = null)
        {
            CharacterUIMeshBase.KillTween(ref _grayscaleTween);
            grayscaleValue = Mathf.Clamp01(grayscaleValue);
            float duration = Mathf.Max(0.05f, transitionTime * Mathf.Abs(grayscaleValue - MaterialGrayscale));
            _grayscaleTween = DOTween.To(() => MaterialGrayscale, x => MaterialGrayscale = x, grayscaleValue, duration).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }

        public override void DoDissolve(float duration, float dissolveAmount)
        {
            CharacterUIMeshBase.KillTween(ref _dissolveTween);
            dissolveAmount = Mathf.Clamp01(dissolveAmount);
            duration = Mathf.Max(0.05f, duration * Mathf.Abs(dissolveAmount - MaterialDissolveAmount));
            _dissolveTween = DOTween.To(() => MaterialDissolveAmount, x => MaterialDissolveAmount = x, dissolveAmount, duration).SetEase(Ease.Linear);
        }

        // --- Targeting, Highlights & Variants ---

        public override void SetHighlightVisible(bool setVisible, SelectionStyle highlightColor)
        {
            if (!setVisible)
            {
                _matProps!.SetFloat(_matPropIdSelectionOutline, 0f);
                _matProps.SetFloat(_matPropIdSelectionValid, 0f);
                _matProps.SetFloat(_matPropIdSelectionInvalid, 0f);
            }
            else
            {
                _matProps!.SetFloat(_matPropIdSelectionOutline, 1f);
                _matProps.SetFloat(_matPropIdSelectionValid, highlightColor.TargetType == RoomTargetingUI.TargetType.Valid ? 1f : 0f);
                _matProps.SetFloat(_matPropIdSelectionInvalid, highlightColor.TargetType == RoomTargetingUI.TargetType.Invalid ? 1f : 0f);
            }
            meshRenderer.SetPropertyBlock(_matProps);
        }

        public override void SetInSilhouette(bool isInSilhouette)
        {
            MaterialColor = isInSilhouette ? Color.black : CharacterUIMeshBase.FadeinColor;
            _matProps!.SetFloat(_matPropIdBlackoutSlider, isInSilhouette ? 1f : 0f);
            meshRenderer.SetPropertyBlock(_matProps);
        }

        public override void SetChosenVariantVisuals()
        {
            _matProps!.SetFloat(_matPropIdChosen, 1f);
            meshRenderer.SetPropertyBlock(_matProps);
        }

        public override void SetLifemotherVariantVisuals()
        {
            _matProps!.SetFloat(_matPropIdChosen2, 1f);
            meshRenderer.SetPropertyBlock(_matProps);
        }

        // --- Room Clipping & Sorting ---

        public override void UpdateClipBottom(CharacterState characterState, CameraControls gameCamera, RoomManager roomManager)
        {
            _clipAgainstRoom?.UpdateClipBottom(characterState, gameCamera, roomManager, _clonedMaterial, null);
        }

        public override int GetSortingOrder() => _sortingOrder;
        public override SortingLayers GetSortingLayer() => _sortingLayer;

        public override bool SetSortingOrder(int sortingOrder)
        {
            if (_sortingOrder != sortingOrder)
            {
                _sortingOrder = sortingOrder;
                meshRenderer.sortingOrder = sortingOrder;
                return true;
            }
            return false;
        }

        public override bool SetSortingLayer(SortingLayers sortingLayer)
        {
            if (_sortingLayer != sortingLayer)
            {
                _sortingLayer = sortingLayer;
                meshRenderer.sortingLayerID = sortingLayer.LayerID();
                return true;
            }
            return false;
        }

        public override MeshRenderer GetMeshRenderer(CharacterUI.Anim animType) => meshRenderer;

        // --- Spine Multi-Mesh & Bone VFX Stubs ---

        public override void UpdateGradientEffect() { }
        public override void UpdateDissolveEffect() { }
        public override void GetVfxPositions(List<Vector3> positionsFound, VfxAtLoc.Location location) => positionsFound.Clear();
        public override void SpawnPersistentVFX(List<GameObject> vfxSpawned, VfxAtLoc vfxAtLoc) => vfxSpawned.Clear();
        public override void SetRimLight(RimLight rimLight) { }
        public override IReadOnlyList<CharacterUIMeshSpine.ModelEntry> GetModelVariations() => _emptyModelVariations;
        public override void UpdateModelVariation(int modelIndex) 
        {
            if (modelIndex != _curModelIndex && _animContainer != null && _animContainer.HasModel(modelIndex))
            {
                PlayAnimInternal(modelIndex, _currentAnimType, _overrideLooping, 0f, _animCallback);
            }
        }

        public override void SetDestroyedState(CharacterState.DestroyedState destroyedState) { }
        public override void SetAllowDestroyedAccess(bool allow) { }
        public override void AssertNotDestroyed(CharacterState.DestroyedState destroyedStateNotAllowed = CharacterState.DestroyedState.InRemoveList) { }
    }
}
