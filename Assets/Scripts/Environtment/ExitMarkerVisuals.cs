using UnityEngine;

/// <summary>
/// Drives the visual polish on the ExitMarker prefab: spin, bob, emissive pulse, light pulse.
///
/// IMPORTANT HIERARCHY:
///   ExitMarker (root)  ← ExitTrigger + BoxCollider here, NEVER moves
///   └── VisualRoot     ← ExitMarkerVisuals here, this child bobs/spins
///         ├── Pillar
///         └── GlowLight
///
/// Keeping the collider on the root prevents the trigger from drifting into the player
/// while the visuals animate.
/// </summary>
[DisallowMultipleComponent]
public class ExitMarkerVisuals : MonoBehaviour
{
    [Header("Animation Target")]
    [Tooltip("The child transform to rotate and bob. Assign your visual child (e.g. VisualRoot) so the " +
             "trigger collider on the parent stays stationary. Leave empty to animate this transform.")]
    [SerializeField] private Transform animatedVisual;

    [Header("Rotation")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    [Header("Bobbing")]
    [SerializeField] private bool bob = true;
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobFrequency = 1f;

    [Header("Emissive Pulse")]
    [Tooltip("Renderer whose material has _EmissionColor (URP/Lit with Emission toggled ON in the material).")]
    [SerializeField] private Renderer targetRenderer;
    [Tooltip("Enable to ignore the material color and use the color field below instead.")]
    [SerializeField] private bool overrideEmissionColor = false;
    [SerializeField] private Color emissionColor = new Color(0.15f, 1f, 0.45f);
    [SerializeField, Min(0f)] private float minIntensity = 1.5f;
    [SerializeField, Min(0f)] private float maxIntensity = 4.5f;
    [SerializeField, Min(0f)] private float pulseFrequency = 1.5f;

    [Header("Light Pulse (optional)")]
    [SerializeField] private Light pulseLight;
    [SerializeField, Min(0f)] private float lightMinIntensity = 1.5f;
    [SerializeField, Min(0f)] private float lightMaxIntensity = 5f;
    [Tooltip("Tint the light to match the resolved emission color.")]
    [SerializeField] private bool tintLightToEmission = true;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock _mpb;
    private Transform _target;
    private Vector3 _baseLocalPosition;
    private Color _resolvedEmissionColor;

    private void Awake()
    {
        _target = animatedVisual != null ? animatedVisual : transform;
        _baseLocalPosition = _target.localPosition;

        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();

        ResolveEmissionColor();
    }

    private void ResolveEmissionColor()
    {
        if (!overrideEmissionColor && targetRenderer != null && targetRenderer.sharedMaterial != null)
        {
            Color matColor = targetRenderer.sharedMaterial.GetColor(EmissionColorId);

            // If the material emission is black the user hasn't set it up yet — fall back and warn.
            if (matColor.maxColorComponent < 0.01f)
            {
                Debug.LogWarning(
                    "[ExitMarkerVisuals] Material '_EmissionColor' is black. " +
                    "In the material inspector, enable Emission and set a non-black HDR color. " +
                    "Falling back to the script's emissionColor field until then.", this);
                _resolvedEmissionColor = emissionColor;
            }
            else
            {
                _resolvedEmissionColor = matColor;
            }
        }
        else
        {
            _resolvedEmissionColor = emissionColor;
        }
    }

    private void OnEnable()
    {
        if (targetRenderer == null) return;
        foreach (var mat in targetRenderer.sharedMaterials)
        {
            if (mat == null) continue;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
    }

    private void Update()
    {
        if (rotate)
            _target.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        if (bob)
        {
            float yOffset = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            _target.localPosition = _baseLocalPosition + new Vector3(0f, yOffset, 0f);
        }

        float pulse01 = (Mathf.Sin(Time.time * pulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;

        if (targetRenderer != null)
        {
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse01);
            targetRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionColorId, _resolvedEmissionColor * intensity);
            targetRenderer.SetPropertyBlock(_mpb);
        }

        if (pulseLight != null)
        {
            pulseLight.intensity = Mathf.Lerp(lightMinIntensity, lightMaxIntensity, pulse01);
            if (tintLightToEmission) pulseLight.color = _resolvedEmissionColor;
        }
    }
}
