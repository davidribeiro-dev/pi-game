using UnityEngine;

[RequireComponent(typeof(TagPlayerState))]
public class TagVisuals : MonoBehaviour
{
    [SerializeField] private Renderer[] targetRenderers;
    [SerializeField] private Color itColor = new Color(0.5f, 0f, 0f);
    [SerializeField] private bool tintBodyColor = true;
    [SerializeField] private float emissionIntensity = 1.5f;
    [SerializeField] private bool setOutlineColor = true;
    [SerializeField] private bool setEmissionColor = true;
    [SerializeField] private string baseColorProperty = "_BaseColor";
    [SerializeField] private string legacyColorProperty = "_Color";
    [SerializeField] private string emissionProperty = "_EmissionColor";
    [SerializeField] private string outlineProperty = "_OutlineColor";

    private TagPlayerState playerState;
    private MaterialPropertyBlock propertyBlock;
    private Color[] originalColors;
    private string[] colorProperties;

    private void Awake()
    {
        playerState = GetComponent<TagPlayerState>();
        propertyBlock = new MaterialPropertyBlock();

        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>(true);

        CacheOriginalColors();
    }

    private void OnEnable()
    {
        if (playerState != null)
        {
            playerState.ItStateChanged += OnItStateChanged;
            ApplyState(playerState.IsIt);
        }
    }

    private void OnDisable()
    {
        if (playerState != null)
            playerState.ItStateChanged -= OnItStateChanged;
    }

    private void OnItStateChanged(bool isIt)
    {
        ApplyState(isIt);
    }

    private void ApplyState(bool isIt)
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
            return;

        Color emissionColor = isIt ? itColor * emissionIntensity : Color.black;
        Color outlineColor = isIt ? itColor : Color.black;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer rendererRef = targetRenderers[i];
            if (rendererRef == null)
                continue;

            rendererRef.GetPropertyBlock(propertyBlock);

            if (tintBodyColor && colorProperties != null && i < colorProperties.Length && !string.IsNullOrEmpty(colorProperties[i]))
            {
                Color targetColor = isIt ? itColor : originalColors[i];
                propertyBlock.SetColor(colorProperties[i], targetColor);
            }

            if (setEmissionColor)
                propertyBlock.SetColor(emissionProperty, emissionColor);

            if (setOutlineColor)
                propertyBlock.SetColor(outlineProperty, outlineColor);

            rendererRef.SetPropertyBlock(propertyBlock);
        }
    }

    private void CacheOriginalColors()
    {
        if (targetRenderers == null)
            return;

        originalColors = new Color[targetRenderers.Length];
        colorProperties = new string[targetRenderers.Length];

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            Renderer rendererRef = targetRenderers[i];
            originalColors[i] = Color.white;
            colorProperties[i] = null;

            if (rendererRef == null || rendererRef.sharedMaterial == null)
                continue;

            Material mat = rendererRef.sharedMaterial;
            if (mat.HasProperty(baseColorProperty))
            {
                colorProperties[i] = baseColorProperty;
                originalColors[i] = mat.GetColor(baseColorProperty);
                continue;
            }

            if (mat.HasProperty(legacyColorProperty))
            {
                colorProperties[i] = legacyColorProperty;
                originalColors[i] = mat.GetColor(legacyColorProperty);
            }
        }
    }
}