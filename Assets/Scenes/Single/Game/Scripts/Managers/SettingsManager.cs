using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("UI elements (assign in inspector OR name them in scene)")]
    public TMP_Dropdown qualityDropdown;
    public TMP_Dropdown aaDropdown;
    public TMP_Dropdown textureQualityDropdown;
    public TMP_Dropdown shadowResolutionDropdown;
    public TMP_Dropdown anisotropicDropdown;
    public TMP_Dropdown shadowCascadesDropdown;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown presetDropdown;
    public Slider lodBiasSlider;
    public Slider shadowDistanceSlider;
    public Slider shadowStrengthSlider;
    public Slider fogDensitySlider;
    public Toggle fullscreenToggle;
    public Toggle bloomToggle;
    public Toggle vignetteToggle;
    public Toggle dofToggle;
    public Toggle aoToggle;
    public Toggle colorGradingToggle;
    public Toggle fogToggle;
    public Toggle vSyncToggle;
    public Toggle motionBlurToggle;

    public Light directionalLight;

    [Header("Optional: URP Asset (for MSAA)")]
    public UniversalRenderPipelineAsset urpAsset;

    [Header("Preview (assign preview camera + UI RawImage)")]
    public Camera previewCamera;
    public RawImage previewImage;
    public Light previewDirectionalLight;


    Volume globalVolume;
    Bloom bloom;
    Vignette vignette;
    DepthOfField dof;
    ColorAdjustments colorAdj;
    MotionBlur motionBlur;


    Volume previewVolume;
    Bloom previewBloom;
    Vignette previewVignette;
    DepthOfField previewDOF;
    ColorAdjustments previewColorAdj;
    MotionBlur previewMotionBlur;

    RenderTexture createdPreviewRT;
    VolumeProfile createdPreviewProfile;


    const string PREF_QUALITY = "gfx_quality";
    const string PREF_FULLSCREEN = "gfx_fullscreen";
    const string PREF_TEXQ = "gfx_texq";
    const string PREF_LODBIAS = "gfx_lodbias";
    const string PREF_SHDIST = "gfx_shdist";
    const string PREF_SHSTR = "gfx_shstr";
    const string PREF_BLOOM = "gfx_bloom";
    const string PREF_VIGN = "gfx_vign";
    const string PREF_DOF = "gfx_dof";
    const string PREF_CG = "gfx_cg";
    const string PREF_FOG = "gfx_fog";
    const string PREF_FOGD = "gfx_fogd";
    const string PREF_VSYNC = "gfx_vsync";
    const string PREF_MBLUR = "gfx_mblur";
    const string PREF_AAIDX = "gfx_aai";
    const string KEY_RES_IDX = "gfx_res_idx";
    const string KEY_SH_RES = "gfx_shres";
    const string KEY_SH_C = "gfx_shc";
    const string KEY_ANISO = "gfx_aniso";


    int currentQuality;
    bool currentFullscreen;
    int currentAAIndex;
    int currentTextureQuality;
    int currentShadowResolution;
    int currentShadowCascades;
    int currentAniso;
    int currentResolutionIndex;
    float currentLODBias;
    float currentShadowDistance;
    float currentShadowStrength;
    float currentFogDensity;
    bool currentBloom;
    bool currentVignette;
    bool currentDOF;
    bool currentColorGrading;
    bool currentFog;
    bool currentVSync;
    bool currentMotionBlur;

    bool currentAO;

    void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (urpAsset == null)
        {
            var asset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (asset != null) urpAsset = asset;
        }


#if UNITY_2023_2_OR_NEWER
        globalVolume = FindFirstObjectByType<Volume>();
#else
        globalVolume = FindObjectOfType<Volume>();
#endif
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out bloom);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out dof);
            globalVolume.profile.TryGet(out colorAdj);
            globalVolume.profile.TryGet(out motionBlur);
        }

        LoadStateFromPrefs();

        SetupPreview();

        ApplyStateToEngine();
        ApplyStateToPreview();
    }

    void Start()
    {

        TryBindUI();

        SetupUIOptions();

        ApplyStateToUI();
    }

    void SetupPreview()
    {

        if (previewCamera == null || previewImage == null) return;


        if (createdPreviewRT == null)
        {
            createdPreviewRT = new RenderTexture(1024, 1024, 16)
            {
                name = "PreviewRT",
                filterMode = FilterMode.Bilinear,
                useMipMap = false,
                autoGenerateMips = false
            };
        }


        previewCamera.targetTexture = createdPreviewRT;
        previewImage.texture = createdPreviewRT;


        if (!previewCamera.gameObject.activeInHierarchy) previewCamera.gameObject.SetActive(true);
        if (!previewCamera.enabled) previewCamera.enabled = true;


        previewVolume = previewCamera.GetComponent<Volume>();
        if (previewVolume == null)
        {
            previewVolume = previewCamera.gameObject.AddComponent<Volume>();
            previewVolume.isGlobal = true;
        }

        if (previewVolume.profile == null)
        {
            if (createdPreviewProfile == null)
                createdPreviewProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            previewVolume.profile = createdPreviewProfile;
        }
        else
        {

            if (previewVolume.profile != createdPreviewProfile)
                createdPreviewProfile = null;
        }

        AddOrGet(previewVolume.profile, out previewBloom);
        AddOrGet(previewVolume.profile, out previewVignette);
        AddOrGet(previewVolume.profile, out previewDOF);
        AddOrGet(previewVolume.profile, out previewColorAdj);
        AddOrGet(previewVolume.profile, out previewMotionBlur);
    }


    void AddOrGet<T>(VolumeProfile profile, out T comp) where T : VolumeComponent
    {
        comp = null;
        if (profile == null) return;
        if (!profile.TryGet(out comp))
        {
            try
            {
                profile.Add<T>(true);
                profile.TryGet(out comp);
            }
            catch { comp = null; }
        }
    }



    void SetupUIOptions()
    {

        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string> { "Низкое", "Среднее", "Высокое", "Очень высокое" });
            qualityDropdown.RefreshShownValue();
        }


        if (aaDropdown != null)
        {
            aaDropdown.ClearOptions();
            aaDropdown.AddOptions(new List<string> { "Выкл", "2x", "4x", "8x" });
            aaDropdown.RefreshShownValue();
        }


        if (textureQualityDropdown != null)
        {
            textureQualityDropdown.ClearOptions();
            textureQualityDropdown.AddOptions(new List<string> { "Полное", "Половина", "Четверть", "1/8" });
            textureQualityDropdown.RefreshShownValue();
        }


        if (shadowResolutionDropdown != null)
        {
            shadowResolutionDropdown.ClearOptions();
            shadowResolutionDropdown.AddOptions(new List<string> { "Низкое", "Среднее", "Высокое", "Очень высокое" });
            shadowResolutionDropdown.RefreshShownValue();
        }


        if (shadowCascadesDropdown != null)
        {
            shadowCascadesDropdown.ClearOptions();
            shadowCascadesDropdown.AddOptions(new List<string> { "Нет", "Два", "Четыре" });
            shadowCascadesDropdown.RefreshShownValue();
        }


        if (anisotropicDropdown != null)
        {
            anisotropicDropdown.ClearOptions();
            anisotropicDropdown.AddOptions(new List<string> { "Выкл", "Вкл", "Принудительно Вкл" });
            anisotropicDropdown.RefreshShownValue();
        }


        if (presetDropdown != null)
        {
            presetDropdown.ClearOptions();
            presetDropdown.AddOptions(new List<string> { "Низкий", "Средний", "Высокий", "Ультра" });
            presetDropdown.RefreshShownValue();
        }


        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            List<string> options = new List<string>();
            var resolutions = Screen.resolutions;
            int currentIndex = 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                Resolution r = resolutions[i];
                float refreshRate = (float)r.refreshRateRatio.numerator / r.refreshRateRatio.denominator;
                string option = r.width + "x" + r.height + " @ " + refreshRate + "Hz";
                options.Add(option);

                var current = Screen.currentResolution;
                float currentRefresh = (float)current.refreshRateRatio.numerator / current.refreshRateRatio.denominator;

                if (r.width == current.width && r.height == current.height && Mathf.Approximately(refreshRate, currentRefresh))
                {
                    currentIndex = i;
                }
            }

            resolutionDropdown.AddOptions(options);
            resolutionDropdown.value = Mathf.Clamp(currentIndex, 0, options.Count - 1);
            resolutionDropdown.RefreshShownValue();
        }
    }




    void LoadStateFromPrefs()
    {
        currentQuality = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
        currentFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        currentAAIndex = PlayerPrefs.GetInt(PREF_AAIDX, AaIndexFromValue(QualitySettings.antiAliasing));
        currentTextureQuality = PlayerPrefs.GetInt(PREF_TEXQ, QualitySettings.globalTextureMipmapLimit);
        currentLODBias = PlayerPrefs.GetFloat(PREF_LODBIAS, QualitySettings.lodBias);
        currentShadowDistance = PlayerPrefs.GetFloat(PREF_SHDIST, QualitySettings.shadowDistance);
        currentShadowStrength = PlayerPrefs.GetFloat(PREF_SHSTR, directionalLight ? directionalLight.shadowStrength : 1f);
        currentShadowResolution = PlayerPrefs.GetInt(KEY_SH_RES, (int)QualitySettings.shadowResolution);
        currentShadowCascades = PlayerPrefs.GetInt(KEY_SH_C, QualitySettings.shadowCascades);
        currentAniso = PlayerPrefs.GetInt(KEY_ANISO, (int)QualitySettings.anisotropicFiltering);
        currentFogDensity = PlayerPrefs.GetFloat(PREF_FOGD, RenderSettings.fogDensity);
        currentBloom = PlayerPrefs.GetInt(PREF_BLOOM, bloom != null && bloom.active ? 1 : 0) == 1;
        currentVignette = PlayerPrefs.GetInt(PREF_VIGN, vignette != null && vignette.active ? 1 : 0) == 1;
        currentDOF = PlayerPrefs.GetInt(PREF_DOF, dof != null && dof.active ? 1 : 0) == 1;
        currentColorGrading = PlayerPrefs.GetInt(PREF_CG, colorAdj != null && colorAdj.active ? 1 : 0) == 1;
        currentFog = PlayerPrefs.GetInt(PREF_FOG, RenderSettings.fog ? 1 : 0) == 1;
        currentVSync = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        currentMotionBlur = PlayerPrefs.GetInt(PREF_MBLUR, previewMotionBlur != null && previewMotionBlur.active ? 1 : 0) == 1;
        currentAO = PlayerPrefs.GetInt("gfx_ao", 0) == 1;
        currentResolutionIndex = PlayerPrefs.GetInt(KEY_RES_IDX, 0);
    }

    void SaveStateToPrefs()
    {
        PlayerPrefs.SetInt(PREF_QUALITY, currentQuality);
        PlayerPrefs.SetInt(PREF_FULLSCREEN, currentFullscreen ? 1 : 0);
        PlayerPrefs.SetInt(PREF_AAIDX, currentAAIndex);
        PlayerPrefs.SetInt(PREF_TEXQ, currentTextureQuality);
        PlayerPrefs.SetFloat(PREF_LODBIAS, currentLODBias);
        PlayerPrefs.SetFloat(PREF_SHDIST, currentShadowDistance);
        PlayerPrefs.SetFloat(PREF_SHSTR, currentShadowStrength);
        PlayerPrefs.SetInt(KEY_SH_RES, currentShadowResolution);
        PlayerPrefs.SetInt(KEY_SH_C, currentShadowCascades);
        PlayerPrefs.SetInt(KEY_ANISO, currentAniso);
        PlayerPrefs.SetFloat(PREF_FOGD, currentFogDensity);
        PlayerPrefs.SetInt(PREF_BLOOM, currentBloom ? 1 : 0);
        PlayerPrefs.SetInt(PREF_VIGN, currentVignette ? 1 : 0);
        PlayerPrefs.SetInt(PREF_DOF, currentDOF ? 1 : 0);
        PlayerPrefs.SetInt(PREF_CG, currentColorGrading ? 1 : 0);
        PlayerPrefs.SetInt(PREF_FOG, currentFog ? 1 : 0);
        PlayerPrefs.SetInt(PREF_VSYNC, currentVSync ? 1 : 0);
        PlayerPrefs.SetInt(PREF_MBLUR, currentMotionBlur ? 1 : 0);
        PlayerPrefs.SetInt("gfx_ao", currentAO ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RES_IDX, currentResolutionIndex);

        PlayerPrefs.Save();
    }


    void ApplyStateToEngine()
    {
        QualitySettings.SetQualityLevel(currentQuality, true);


        Screen.fullScreen = currentFullscreen;


        int aa = currentAAIndex switch { 0 => 0, 1 => 2, 2 => 4, 3 => 8, _ => 0 };
        QualitySettings.antiAliasing = aa;
        if (urpAsset != null)
        {
            try { urpAsset.msaaSampleCount = aa; } catch { }
        }


        QualitySettings.globalTextureMipmapLimit = currentTextureQuality;


        QualitySettings.lodBias = currentLODBias;


        QualitySettings.shadowDistance = currentShadowDistance;
        QualitySettings.shadowCascades = currentShadowCascades;
        QualitySettings.shadowResolution = currentShadowResolution switch
        {
            0 => UnityEngine.ShadowResolution.Low,
            1 => UnityEngine.ShadowResolution.Medium,
            2 => UnityEngine.ShadowResolution.High,
            _ => UnityEngine.ShadowResolution.VeryHigh
        };
        if (directionalLight != null) directionalLight.shadowStrength = currentShadowStrength;


        QualitySettings.anisotropicFiltering = currentAniso switch
        {
            0 => AnisotropicFiltering.Disable,
            1 => AnisotropicFiltering.Enable,
            _ => AnisotropicFiltering.ForceEnable
        };


        QualitySettings.vSyncCount = currentVSync ? 1 : 0;


        RenderSettings.fog = currentFog;
        RenderSettings.fogDensity = currentFogDensity;


        if (motionBlur != null) motionBlur.active = currentMotionBlur;
        if (bloom != null) bloom.active = currentBloom;
        if (vignette != null) vignette.active = currentVignette;
        if (dof != null) dof.active = currentDOF;
        if (colorAdj != null) colorAdj.active = currentColorGrading;
    }

    void ApplyStateToPreview()
    {

        if (previewCamera != null)
        {
            if (previewCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data))
            {
                data.renderShadows = currentShadowDistance > 0f;

            }
            previewCamera.targetTexture = createdPreviewRT;
            previewCamera.enabled = true;
        }


        if (previewDirectionalLight == null && directionalLight != null)
        {

            previewDirectionalLight = directionalLight;
        }

        if (previewDirectionalLight != null)
        {
            previewDirectionalLight.shadowStrength = currentShadowStrength;


        }


        ApplyEffectState(bloom, previewBloom, currentBloom);
        ApplyEffectState(vignette, previewVignette, currentVignette);
        ApplyEffectState(dof, previewDOF, currentDOF);
        ApplyEffectState(colorAdj, previewColorAdj, currentColorGrading);
        ApplyEffectState(motionBlur, previewMotionBlur, currentMotionBlur);


        RenderSettings.fog = currentFog;
        RenderSettings.fogDensity = currentFogDensity;


        if (urpAsset != null)
        {
            try { urpAsset.msaaSampleCount = (currentAAIndex == 1 ? 2 : currentAAIndex == 2 ? 4 : currentAAIndex == 3 ? 8 : 0); } catch { }
        }


        if (previewImage != null)
        {
            if (previewImage.texture != createdPreviewRT)
                previewImage.texture = createdPreviewRT;
        }
    }

    private void ApplyEffectState(VolumeComponent globalEffect, VolumeComponent previewEffect, bool state)
    {
        if (globalEffect != null) globalEffect.active = state;
        if (previewEffect != null) previewEffect.active = state;
    }


    void ApplyStateToUI()
    {

        UnsubscribeUIListeners();

        if (qualityDropdown != null)
        {
            qualityDropdown.value = Mathf.Clamp(currentQuality, 0, qualityDropdown.options.Count - 1);
            qualityDropdown.RefreshShownValue();
        }

        if (fullscreenToggle != null) fullscreenToggle.isOn = currentFullscreen;
        if (aaDropdown != null)
        {
            aaDropdown.value = Mathf.Clamp(currentAAIndex, 0, aaDropdown.options.Count - 1);
            aaDropdown.RefreshShownValue();
        }
        if (textureQualityDropdown != null) textureQualityDropdown.value = Mathf.Clamp(currentTextureQuality, 0, textureQualityDropdown.options.Count - 1);
        if (lodBiasSlider != null) lodBiasSlider.value = currentLODBias;
        if (shadowDistanceSlider != null) shadowDistanceSlider.value = currentShadowDistance;
        if (shadowStrengthSlider != null) shadowStrengthSlider.value = currentShadowStrength;
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.value = Mathf.Clamp(currentShadowResolution, 0, shadowResolutionDropdown.options.Count - 1);
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.value = Mathf.Clamp(currentShadowCascades, 0, shadowCascadesDropdown.options.Count - 1);
        if (anisotropicDropdown != null) anisotropicDropdown.value = Mathf.Clamp(currentAniso, 0, anisotropicDropdown.options.Count - 1);
        if (vSyncToggle != null) vSyncToggle.isOn = currentVSync;
        if (motionBlurToggle != null) motionBlurToggle.isOn = currentMotionBlur;
        if (bloomToggle != null) bloomToggle.isOn = currentBloom;
        if (vignetteToggle != null) vignetteToggle.isOn = currentVignette;
        if (dofToggle != null) dofToggle.isOn = currentDOF;
        if (colorGradingToggle != null) colorGradingToggle.isOn = currentColorGrading;
        if (fogToggle != null) fogToggle.isOn = currentFog;
        if (fogDensitySlider != null) fogDensitySlider.value = currentFogDensity;
        if (textureQualityDropdown != null) textureQualityDropdown.RefreshShownValue();
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.RefreshShownValue();
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.RefreshShownValue();
        if (anisotropicDropdown != null) anisotropicDropdown.RefreshShownValue();


        SubscribeUIListeners();
    }



    void TryBindUI()
    {

        Scene active = SceneManager.GetActiveScene();


        bool NeedFindDropdown(TMP_Dropdown dd) => dd == null || dd.gameObject.scene != active;
        bool NeedFindSlider(Slider s) => s == null || s.gameObject.scene != active;
        bool NeedFindToggle(Toggle t) => t == null || t.gameObject.scene != active;

        if (NeedFindDropdown(qualityDropdown)) qualityDropdown = FindTMP("QualityDropdown", "qualityDropdown", "Quality Dropdown");
        if (NeedFindDropdown(aaDropdown)) aaDropdown = FindTMP("AADropdown", "aaDropdown", "AA Dropdown");
        if (NeedFindDropdown(textureQualityDropdown)) textureQualityDropdown = FindTMP("TextureQualityDropdown", "textureQualityDropdown");
        if (NeedFindDropdown(shadowResolutionDropdown)) shadowResolutionDropdown = FindTMP("ShadowResolutionDropdown", "shadowResolutionDropdown");
        if (NeedFindDropdown(shadowCascadesDropdown)) shadowCascadesDropdown = FindTMP("ShadowCascadesDropdown", "shadowCascadesDropdown");
        if (NeedFindDropdown(anisotropicDropdown)) anisotropicDropdown = FindTMP("AnisotropicDropdown", "anisotropicDropdown");
        if (NeedFindDropdown(resolutionDropdown)) resolutionDropdown = FindTMP("ResolutionDropdown", "resolutionDropdown");
        if (NeedFindDropdown(presetDropdown)) presetDropdown = FindTMP("PresetDropdown", "presetDropdown");

        if (NeedFindSlider(lodBiasSlider)) lodBiasSlider = FindSlider("LODBiasSlider", "lodBiasSlider");
        if (NeedFindSlider(shadowDistanceSlider)) shadowDistanceSlider = FindSlider("ShadowDistanceSlider", "shadowDistanceSlider");
        if (NeedFindSlider(shadowStrengthSlider)) shadowStrengthSlider = FindSlider("ShadowStrengthSlider", "shadowStrengthSlider");
        if (NeedFindSlider(fogDensitySlider)) fogDensitySlider = FindSlider("FogDensitySlider", "fogDensitySlider");

        if (NeedFindToggle(fullscreenToggle)) fullscreenToggle = FindToggle("FullscreenToggle", "fullscreenToggle");
        if (NeedFindToggle(bloomToggle)) bloomToggle = FindToggle("BloomToggle", "bloomToggle");
        if (NeedFindToggle(vignetteToggle)) vignetteToggle = FindToggle("VignetteToggle", "vignetteToggle");
        if (NeedFindToggle(dofToggle)) dofToggle = FindToggle("DOFToggle", "dofToggle", "DofToggle");
        if (NeedFindToggle(aoToggle)) aoToggle = FindToggle("AOToggle", "aoToggle", "AoToggle");
        if (NeedFindToggle(colorGradingToggle)) colorGradingToggle = FindToggle("ColorGradingToggle", "colorGradingToggle");
        if (NeedFindToggle(fogToggle)) fogToggle = FindToggle("FogToggle", "fogToggle");
        if (NeedFindToggle(vSyncToggle)) vSyncToggle = FindToggle("VSyncToggle", "vSyncToggle");
        if (NeedFindToggle(motionBlurToggle)) motionBlurToggle = FindToggle("MotionBlurToggle", "motionBlurToggle");

        if (previewCamera == null || previewCamera.gameObject.scene != active)
            previewCamera = GameObject.Find("PreviewCamera")?.GetComponent<Camera>();


        if (previewImage == null || previewImage.gameObject.scene != active)
            previewImage = GameObject.Find("PreviewImage")?.GetComponent<UnityEngine.UI.RawImage>();


        if (previewDirectionalLight == null || previewDirectionalLight.gameObject.scene != active)
        {
            previewDirectionalLight = GameObject.Find("PreviewDirectionalLight")?.GetComponent<Light>();
            if (previewDirectionalLight == null)
            {
#if UNITY_2023_2_OR_NEWER
                previewDirectionalLight = Object.FindFirstObjectByType<Light>();
#else
            previewDirectionalLight = FindObjectOfType<Light>();
#endif
            }
        }


        if (directionalLight == null || directionalLight.gameObject.scene != active)
        {
#if UNITY_2023_2_OR_NEWER
            directionalLight = Object.FindFirstObjectByType<Light>();
#else
        directionalLight = FindObjectOfType<Light>();
#endif
        }


        SetupPreview();

        Debug.Log($"SettingsManager: TryBindUI -> qualityDropdown={qualityDropdown != null}, aa={aaDropdown != null}, resolution={resolutionDropdown != null}, previewCamera={(previewCamera != null)}, previewImage={(previewImage != null)}");

        SetupUIOptions();

        RemoveOurListeners();
        AddOurListeners();
        ApplyStateToUI();
        ApplyStateToPreview();


    }


    void AddOurListeners()
    {
        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
        if (aaDropdown != null) aaDropdown.onValueChanged.AddListener(OnAADropdownChanged);
        if (textureQualityDropdown != null) textureQualityDropdown.onValueChanged.AddListener(OnTextureQualityChanged);
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.onValueChanged.AddListener(OnShadowResolutionChanged);
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.onValueChanged.AddListener(OnShadowCascadesChanged);
        if (anisotropicDropdown != null) anisotropicDropdown.onValueChanged.AddListener(OnAnisoChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (presetDropdown != null) presetDropdown.onValueChanged.AddListener(OnPresetChanged);

        if (lodBiasSlider != null) lodBiasSlider.onValueChanged.AddListener(OnLODBiasChanged);
        if (shadowDistanceSlider != null) shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
        if (shadowStrengthSlider != null) shadowStrengthSlider.onValueChanged.AddListener(OnShadowStrengthChanged);
        if (fogDensitySlider != null) fogDensitySlider.onValueChanged.AddListener(OnFogDensityChanged);

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (bloomToggle != null) bloomToggle.onValueChanged.AddListener(OnBloomChanged);
        if (vignetteToggle != null) vignetteToggle.onValueChanged.AddListener(OnVignetteChanged);
        if (dofToggle != null) dofToggle.onValueChanged.AddListener(OnDOFChanged);
        if (aoToggle != null) aoToggle.onValueChanged.AddListener(OnAOChanged);
        if (colorGradingToggle != null) colorGradingToggle.onValueChanged.AddListener(OnColorGradingChanged);
        if (fogToggle != null) fogToggle.onValueChanged.AddListener(OnFogChanged);
        if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (motionBlurToggle != null) motionBlurToggle.onValueChanged.AddListener(OnMotionBlurChanged);
    }

    void RemoveOurListeners()
    {
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveListener(OnQualityDropdownChanged);
        if (aaDropdown != null) aaDropdown.onValueChanged.RemoveListener(OnAADropdownChanged);
        if (textureQualityDropdown != null) textureQualityDropdown.onValueChanged.RemoveListener(OnTextureQualityChanged);
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.onValueChanged.RemoveListener(OnShadowResolutionChanged);
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.onValueChanged.RemoveListener(OnShadowCascadesChanged);
        if (anisotropicDropdown != null) anisotropicDropdown.onValueChanged.RemoveListener(OnAnisoChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
        if (presetDropdown != null) presetDropdown.onValueChanged.RemoveListener(OnPresetChanged);

        if (lodBiasSlider != null) lodBiasSlider.onValueChanged.RemoveListener(OnLODBiasChanged);
        if (shadowDistanceSlider != null) shadowDistanceSlider.onValueChanged.RemoveListener(OnShadowDistanceChanged);
        if (shadowStrengthSlider != null) shadowStrengthSlider.onValueChanged.RemoveListener(OnShadowStrengthChanged);
        if (fogDensitySlider != null) fogDensitySlider.onValueChanged.RemoveListener(OnFogDensityChanged);

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        if (bloomToggle != null) bloomToggle.onValueChanged.RemoveListener(OnBloomChanged);
        if (vignetteToggle != null) vignetteToggle.onValueChanged.RemoveListener(OnVignetteChanged);
        if (dofToggle != null) dofToggle.onValueChanged.RemoveListener(OnDOFChanged);
        if (aoToggle != null) aoToggle.onValueChanged.RemoveListener(OnAOChanged);
        if (colorGradingToggle != null) colorGradingToggle.onValueChanged.RemoveListener(OnColorGradingChanged);
        if (fogToggle != null) fogToggle.onValueChanged.RemoveListener(OnFogChanged);
        if (vSyncToggle != null) vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
        if (motionBlurToggle != null) motionBlurToggle.onValueChanged.RemoveListener(OnMotionBlurChanged);
    }


    TMP_Dropdown FindTMP(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                if (go.TryGetComponent<TMP_Dropdown>(out var dd)) return dd;
            }
        }
        return null;
    }

    Slider FindSlider(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                if (go.TryGetComponent<Slider>(out var s)) return s;
            }
        }
        return null;
    }

    Toggle FindToggle(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                if (go.TryGetComponent<Toggle>(out var t)) return t;
            }
        }
        return null;
    }

    void SubscribeUIListeners()
    {

        UnsubscribeUIListeners();

        if (qualityDropdown != null) qualityDropdown.onValueChanged.AddListener(OnQualityDropdownChanged);
        if (aaDropdown != null) aaDropdown.onValueChanged.AddListener(OnAADropdownChanged);
        if (textureQualityDropdown != null) textureQualityDropdown.onValueChanged.AddListener(OnTextureQualityChanged);
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.onValueChanged.AddListener(OnShadowResolutionChanged);
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.onValueChanged.AddListener(OnShadowCascadesChanged);
        if (anisotropicDropdown != null) anisotropicDropdown.onValueChanged.AddListener(OnAnisoChanged);
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (presetDropdown != null) presetDropdown.onValueChanged.AddListener(OnPresetChanged);

        if (lodBiasSlider != null) lodBiasSlider.onValueChanged.AddListener(OnLODBiasChanged);
        if (shadowDistanceSlider != null) shadowDistanceSlider.onValueChanged.AddListener(OnShadowDistanceChanged);
        if (shadowStrengthSlider != null) shadowStrengthSlider.onValueChanged.AddListener(OnShadowStrengthChanged);
        if (fogDensitySlider != null) fogDensitySlider.onValueChanged.AddListener(OnFogDensityChanged);

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        if (bloomToggle != null) bloomToggle.onValueChanged.AddListener(OnBloomChanged);
        if (vignetteToggle != null) vignetteToggle.onValueChanged.AddListener(OnVignetteChanged);
        if (dofToggle != null) dofToggle.onValueChanged.AddListener(OnDOFChanged);
        if (aoToggle != null) aoToggle.onValueChanged.AddListener(OnAOChanged);
        if (colorGradingToggle != null) colorGradingToggle.onValueChanged.AddListener(OnColorGradingChanged);
        if (fogToggle != null) fogToggle.onValueChanged.AddListener(OnFogChanged);
        if (vSyncToggle != null) vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (motionBlurToggle != null) motionBlurToggle.onValueChanged.AddListener(OnMotionBlurChanged);
    }

    void UnsubscribeUIListeners()
    {
        if (qualityDropdown != null) qualityDropdown.onValueChanged.RemoveAllListeners();
        if (aaDropdown != null) aaDropdown.onValueChanged.RemoveAllListeners();
        if (textureQualityDropdown != null) textureQualityDropdown.onValueChanged.RemoveAllListeners();
        if (shadowResolutionDropdown != null) shadowResolutionDropdown.onValueChanged.RemoveAllListeners();
        if (shadowCascadesDropdown != null) shadowCascadesDropdown.onValueChanged.RemoveAllListeners();
        if (anisotropicDropdown != null) anisotropicDropdown.onValueChanged.RemoveAllListeners();
        if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveAllListeners();
        if (presetDropdown != null) presetDropdown.onValueChanged.RemoveAllListeners();

        if (lodBiasSlider != null) lodBiasSlider.onValueChanged.RemoveAllListeners();
        if (shadowDistanceSlider != null) shadowDistanceSlider.onValueChanged.RemoveAllListeners();
        if (shadowStrengthSlider != null) shadowStrengthSlider.onValueChanged.RemoveAllListeners();
        if (fogDensitySlider != null) fogDensitySlider.onValueChanged.RemoveAllListeners();

        if (fullscreenToggle != null) fullscreenToggle.onValueChanged.RemoveAllListeners();
        if (bloomToggle != null) bloomToggle.onValueChanged.RemoveAllListeners();
        if (vignetteToggle != null) vignetteToggle.onValueChanged.RemoveAllListeners();
        if (dofToggle != null) dofToggle.onValueChanged.RemoveAllListeners();
        if (aoToggle != null) aoToggle.onValueChanged.RemoveAllListeners();
        if (colorGradingToggle != null) colorGradingToggle.onValueChanged.RemoveAllListeners();
        if (fogToggle != null) fogToggle.onValueChanged.RemoveAllListeners();
        if (vSyncToggle != null) vSyncToggle.onValueChanged.RemoveAllListeners();
        if (motionBlurToggle != null) motionBlurToggle.onValueChanged.RemoveAllListeners();
    }


    void OnQualityDropdownChanged(int idx) { SetQuality(idx); }
    void OnAADropdownChanged(int idx) { SetAntiAliasing(idx); }
    void OnTextureQualityChanged(int idx) { SetTextureQuality(idx); }
    void OnShadowResolutionChanged(int idx) { SetShadowResolution(idx); }
    void OnShadowCascadesChanged(int idx) { SetShadowCascades(idx); }
    void OnAnisoChanged(int idx) { SetAnisotropicFiltering(idx); }
    void OnResolutionChanged(int idx) { SetResolution(idx); }
    void OnPresetChanged(int idx) { SetPreset(idx); }

    void OnLODBiasChanged(float v) { SetLODBias(v); }
    void OnShadowDistanceChanged(float v) { SetShadowDistance(v); }
    void OnShadowStrengthChanged(float v) { SetShadowStrength(v); }
    void OnFogDensityChanged(float v) { SetFogDensity(v); }

    void OnFullscreenChanged(bool v) { SetFullscreen(v); }
    void OnBloomChanged(bool v) { SetBloomEnabled(v); }
    void OnVignetteChanged(bool v) { SetVignetteEnabled(v); }
    void OnDOFChanged(bool v) { SetDOFEnabled(v); }
    void OnAOChanged(bool v) { SetAOEnabled(v); }
    void OnColorGradingChanged(bool v) { SetColorGradingEnabled(v); }
    void OnFogChanged(bool v) { SetFogEnabled(v); }
    void OnVSyncChanged(bool v) { SetVSync(v); }
    void OnMotionBlurChanged(bool v) { SetMotionBlurEnabled(v); }


    public void SetQuality(int idx)
    {
        currentQuality = idx;
        QualitySettings.SetQualityLevel(idx, true);
        SaveStateToPrefs();
    }

    public void SetFullscreen(bool fs)
    {
        currentFullscreen = fs;
        Screen.fullScreen = fs;
        SaveStateToPrefs();
    }

    public void SetAntiAliasing(int idx)
    {
        currentAAIndex = idx;
        int aa = idx switch { 0 => 0, 1 => 2, 2 => 4, 3 => 8, _ => 0 };
        QualitySettings.antiAliasing = aa;
        if (urpAsset != null)
        {
            try { urpAsset.msaaSampleCount = aa; } catch { }
        }
        SaveStateToPrefs();
    }

    public void SetTextureQuality(int idx)
    {
        currentTextureQuality = idx;
        QualitySettings.globalTextureMipmapLimit = idx;
        SaveStateToPrefs();
    }

    public void SetLODBias(float v)
    {
        currentLODBias = v;
        QualitySettings.lodBias = v;
        SaveStateToPrefs();
    }

    public void SetShadowDistance(float v)
    {
        currentShadowDistance = v;
        QualitySettings.shadowDistance = v;
        SaveStateToPrefs();

        if (previewCamera != null)
        {
            if (previewCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data)) data.renderShadows = v > 0f;
        }
    }

    public void SetShadowStrength(float v)
    {
        currentShadowStrength = v;
        if (directionalLight != null) directionalLight.shadowStrength = v;
        if (previewDirectionalLight != null) previewDirectionalLight.shadowStrength = v;
        else if (previewCamera != null)
        {
            var pl = previewCamera.GetComponentInChildren<Light>();
            if (pl != null) pl.shadowStrength = v;
        }
        SaveStateToPrefs();
    }

    public void SetShadowResolution(int idx)
    {
        currentShadowResolution = idx;
        QualitySettings.shadowResolution = idx switch
        {
            0 => UnityEngine.ShadowResolution.Low,
            1 => UnityEngine.ShadowResolution.Medium,
            2 => UnityEngine.ShadowResolution.High,
            _ => UnityEngine.ShadowResolution.VeryHigh
        };
        SaveStateToPrefs();
    }

    public void SetShadowCascades(int idx)
    {
        currentShadowCascades = idx switch { 0 => 0, 1 => 2, _ => 4 };
        QualitySettings.shadowCascades = currentShadowCascades;
        SaveStateToPrefs();
    }

    public void SetAnisotropicFiltering(int idx)
    {
        currentAniso = idx;
        QualitySettings.anisotropicFiltering = idx switch
        {
            0 => AnisotropicFiltering.Disable,
            1 => AnisotropicFiltering.Enable,
            _ => AnisotropicFiltering.ForceEnable
        };
        SaveStateToPrefs();
    }

    public void SetVSync(bool on)
    {
        currentVSync = on;
        QualitySettings.vSyncCount = on ? 1 : 0;
        SaveStateToPrefs();
    }

    public void SetMotionBlurEnabled(bool on)
    {
        currentMotionBlur = on;
        if (motionBlur != null) motionBlur.active = on;
        if (previewMotionBlur != null) previewMotionBlur.active = on;
        SaveStateToPrefs();
    }

    public void SetResolution(int idx)
    {
        currentResolutionIndex = idx;
        var resolutions = Screen.resolutions;
        if (idx >= 0 && idx < resolutions.Length)
        {
            var res = resolutions[idx];
            FullScreenMode mode = Screen.fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            RefreshRate rr = new()
            {
                numerator = res.refreshRateRatio.numerator,
                denominator = res.refreshRateRatio.denominator
            };

            Screen.SetResolution(res.width, res.height, mode, rr);
        }
        SaveStateToPrefs();
    }

    public void SetPreset(int idx)
    {
        switch (idx)
        {
            case 0:
                SetShadowResolution(0);
                SetShadowDistance(30f);
                SetShadowStrength(0.5f);
                SetAntiAliasing(0);
                SetTextureQuality(3);
                SetAnisotropicFiltering(0);
                SetMotionBlurEnabled(false);
                SetBloomEnabled(false);
                SetVignetteEnabled(false);
                SetDOFEnabled(false);
                SetColorGradingEnabled(false);
                SetAOEnabled(false);
                SetFogEnabled(false);
                SetLODBias(0.8f);
                break;

            case 1:
                SetShadowResolution(1);
                SetShadowDistance(60f);
                SetShadowStrength(0.7f);
                SetAntiAliasing(2);
                SetTextureQuality(1);
                SetAnisotropicFiltering(1);
                SetMotionBlurEnabled(false);
                SetBloomEnabled(true);
                SetVignetteEnabled(true);
                SetDOFEnabled(false);
                SetColorGradingEnabled(true);
                SetAOEnabled(false);
                SetFogEnabled(true);
                SetLODBias(1f);
                SetFogDensity(0.02f);
                break;

            case 2:
                SetShadowResolution(2);
                SetShadowDistance(100f);
                SetShadowStrength(0.85f);
                SetAntiAliasing(4);
                SetTextureQuality(0);
                SetAnisotropicFiltering(2);
                SetMotionBlurEnabled(true);
                SetBloomEnabled(true);
                SetVignetteEnabled(true);
                SetDOFEnabled(true);
                SetColorGradingEnabled(true);
                SetAOEnabled(true);
                SetFogEnabled(true);
                SetLODBias(1.2f);
                SetFogDensity(0.03f);
                break;

            case 3:
                SetShadowResolution(3);
                SetShadowDistance(200f);
                SetShadowStrength(1f);
                SetAntiAliasing(8);
                SetTextureQuality(0);
                SetAnisotropicFiltering(2);
                SetMotionBlurEnabled(true);
                SetBloomEnabled(true);
                SetVignetteEnabled(true);
                SetDOFEnabled(true);
                SetColorGradingEnabled(true);
                SetAOEnabled(true);
                SetFogEnabled(true);
                SetLODBias(1.5f);
                SetFogDensity(0.05f);
                break;
        }


        ApplyStateToPreview();
        ApplyStateToUI();
        SaveStateToPrefs();
    }

    public void SetBloomEnabled(bool on)
    {
        currentBloom = on;
        if (bloom != null) bloom.active = on;
        if (previewBloom != null) previewBloom.active = on;
        SaveStateToPrefs();
    }

    public void SetVignetteEnabled(bool on)
    {
        currentVignette = on;
        if (vignette != null) vignette.active = on;
        if (previewVignette != null) previewVignette.active = on;
        SaveStateToPrefs();
    }

    public void SetDOFEnabled(bool on)
    {
        currentDOF = on;
        if (dof != null) dof.active = on;
        if (previewDOF != null) previewDOF.active = on;
        SaveStateToPrefs();
    }

    public void SetAOEnabled(bool on)
    {
        currentAO = on;
        PlayerPrefs.SetInt("gfx_ao", on ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("AO toggled (if AO component present it must be enabled in Volume profile).");
    }

    public void SetColorGradingEnabled(bool on)
    {
        currentColorGrading = on;
        if (colorAdj != null) colorAdj.active = on;
        if (previewColorAdj != null) previewColorAdj.active = on;
        SaveStateToPrefs();
    }

    public void SetFogEnabled(bool on)
    {
        currentFog = on;
        RenderSettings.fog = on;
        SaveStateToPrefs();
    }

    public void SetFogDensity(float v)
    {
        currentFogDensity = v;
        RenderSettings.fogDensity = v;
        SaveStateToPrefs();
    }


    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

#if UNITY_2023_2_OR_NEWER
        if (directionalLight == null) directionalLight = FindFirstObjectByType<Light>();
        if (globalVolume == null) globalVolume = FindFirstObjectByType<Volume>();
#else
        if (directionalLight == null) directionalLight = FindObjectOfType<Light>();
        if (globalVolume == null) globalVolume = FindObjectOfType<Volume>();
#endif
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out bloom);
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out dof);
            globalVolume.profile.TryGet(out colorAdj);
            globalVolume.profile.TryGet(out motionBlur);
        }


        ApplyStateToEngine();

        TryBindUI();

        ApplyStateToUI();

        ApplyStateToPreview();
    }

    int AaIndexFromValue(int aa)
    {
        return aa switch { 8 => 3, 4 => 2, 2 => 1, _ => 0 };
    }

    void OnApplicationQuit()
    {
        SaveStateToPrefs();
    }

    void OnDestroy()
    {
        if (createdPreviewRT != null)
        {
            if (previewCamera != null && previewCamera.targetTexture == createdPreviewRT)
                previewCamera.targetTexture = null;
            createdPreviewRT.Release();
            Destroy(createdPreviewRT);
            createdPreviewRT = null;
        }

        if (createdPreviewProfile != null)
        {
            if (previewVolume != null && previewVolume.profile == createdPreviewProfile)
                previewVolume.profile = null;
            Destroy(createdPreviewProfile);
            createdPreviewProfile = null;
        }
    }

    public void BackToMainMenu() => SceneManager.LoadScene("MainMenu");
}
