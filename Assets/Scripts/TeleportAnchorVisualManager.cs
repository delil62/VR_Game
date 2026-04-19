using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

public class TeleportAnchorVisualManager : MonoBehaviour
{
    [SerializeField]
    string m_HoverPrompt = "Press Grip to teleport";

    [SerializeField]
    Color m_BaseColor = new(0.1f, 0.85f, 1f, 0.75f);

    [SerializeField]
    Color m_HoverColor = new(1f, 0.9f, 0.2f, 0.95f);

    [SerializeField]
    Vector3 m_BaseMarkerScale = new(0.9f, 0.03f, 0.9f);

    [SerializeField]
    Vector3 m_HoverMarkerScale = new(1.15f, 0.04f, 1.15f);

    [SerializeField]
    float m_PulseAmplitude = 0.08f;

    [SerializeField]
    float m_PulseSpeed = 2.25f;

    [SerializeField]
    float m_BobHeight = 0.12f;

    [SerializeField]
    float m_BobSpeed = 2.8f;

    [SerializeField]
    float m_TextHeight = 0.22f;

    [SerializeField]
    Vector3 m_ColliderSize = new(3f, 12f, 3f);

    [SerializeField]
    bool m_ShowAnchorName = false;

    readonly List<AnchorVisual> m_anchors = new();
    Material m_BaseMaterial;
    Material m_HoverMaterial;
    Camera m_MainCamera;

    void Awake()
    {
        EnsureMaterials();
        CacheAnchors();
    }

    void OnEnable()
    {
        foreach (var anchor in m_anchors)
            anchor.Bind();
    }

    void Start()
    {
        m_MainCamera = Camera.main;
        foreach (var anchor in m_anchors)
            anchor.RefreshImmediate(m_MainCamera, 0f);
    }

    void Update()
    {
        if (m_MainCamera == null || !m_MainCamera.isActiveAndEnabled)
            m_MainCamera = Camera.main;

        var time = Time.time;
        foreach (var anchor in m_anchors)
            anchor.RefreshImmediate(m_MainCamera, time);
    }

    void OnDisable()
    {
        foreach (var anchor in m_anchors)
            anchor.Unbind();
    }

    void OnDestroy()
    {
        foreach (var anchor in m_anchors)
            anchor.Dispose();

        m_anchors.Clear();

        if (Application.isPlaying)
        {
            Destroy(m_BaseMaterial);
            Destroy(m_HoverMaterial);
            return;
        }

        DestroyImmediate(m_BaseMaterial);
        DestroyImmediate(m_HoverMaterial);
    }

    void EnsureMaterials()
    {
        if (m_BaseMaterial != null && m_HoverMaterial != null)
            return;

        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        shader ??= Shader.Find("Sprites/Default");
        shader ??= Shader.Find("Standard");

        m_BaseMaterial = new Material(shader)
        {
            color = m_BaseColor
        };

        m_HoverMaterial = new Material(shader)
        {
            color = m_HoverColor
        };

        ConfigureMaterial(m_BaseMaterial);
        ConfigureMaterial(m_HoverMaterial);
    }

    void ConfigureMaterial(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend"))
            material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", material.color);
        if (material.HasProperty("_BaseMap"))
            material.SetTexture("_BaseMap", null);
        if (material.HasProperty("_EmissionColor"))
            material.SetColor("_EmissionColor", material.color * 0.75f);

        material.renderQueue = 3000;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_EMISSION");
    }

    void CacheAnchors()
    {
        m_anchors.Clear();

        var anchors = GetComponentsInChildren<TeleportationAnchor>(true);
        foreach (var anchor in anchors)
        {
            if (anchor == null)
                continue;

            PrepareCollider(anchor);
            m_anchors.Add(new AnchorVisual(
                anchor,
                transform,
                m_HoverPrompt,
                m_ShowAnchorName,
                m_BaseMaterial,
                m_HoverMaterial,
                m_BaseMarkerScale,
                m_HoverMarkerScale,
                m_PulseAmplitude,
                m_PulseSpeed,
                m_BobHeight,
                m_BobSpeed,
                m_TextHeight));
        }
    }

    void PrepareCollider(TeleportationAnchor anchor)
    {
        var boxCollider = anchor.GetComponent<BoxCollider>();
        if (boxCollider == null)
            return;

        boxCollider.size = m_ColliderSize;
        boxCollider.center = Vector3.zero;
    }

    sealed class AnchorVisual
    {
        readonly TeleportationAnchor m_Anchor;
        readonly Transform m_RuntimeRoot;
        readonly Transform m_MarkerTransform;
        readonly Transform m_OrbTransform;
        readonly TextMesh m_TextMesh;
        readonly MeshRenderer m_MarkerRenderer;
        readonly MeshRenderer m_OrbRenderer;
        readonly Material m_BaseMaterial;
        readonly Material m_HoverMaterial;
        readonly Vector3 m_BaseMarkerScale;
        readonly Vector3 m_HoverMarkerScale;
        readonly float m_PulseAmplitude;
        readonly float m_PulseSpeed;
        readonly float m_BobHeight;
        readonly float m_BobSpeed;
        readonly float m_TextHeight;
        readonly string m_PromptText;
        int m_HoverCount;

        public AnchorVisual(
            TeleportationAnchor anchor,
            Transform parent,
            string hoverPrompt,
            bool showAnchorName,
            Material baseMaterial,
            Material hoverMaterial,
            Vector3 baseMarkerScale,
            Vector3 hoverMarkerScale,
            float pulseAmplitude,
            float pulseSpeed,
            float bobHeight,
            float bobSpeed,
            float textHeight)
        {
            m_Anchor = anchor;
            m_BaseMaterial = baseMaterial;
            m_HoverMaterial = hoverMaterial;
            m_BaseMarkerScale = baseMarkerScale;
            m_HoverMarkerScale = hoverMarkerScale;
            m_PulseAmplitude = pulseAmplitude;
            m_PulseSpeed = pulseSpeed;
            m_BobHeight = bobHeight;
            m_BobSpeed = bobSpeed;
            m_TextHeight = textHeight;

            var prompt = hoverPrompt;
            if (showAnchorName)
                prompt = $"{anchor.gameObject.name}\n{hoverPrompt}";
            m_PromptText = prompt;

            var rootObject = new GameObject($"{anchor.gameObject.name}_TeleportVisual");
            m_RuntimeRoot = rootObject.transform;
            m_RuntimeRoot.SetParent(parent, false);

            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObject.name = "Marker";
            markerObject.transform.SetParent(m_RuntimeRoot, false);
            DestroyCollider(markerObject);
            m_MarkerTransform = markerObject.transform;
            m_MarkerRenderer = markerObject.GetComponent<MeshRenderer>();
            m_MarkerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_MarkerRenderer.receiveShadows = false;
            m_MarkerRenderer.sharedMaterial = m_BaseMaterial;

            var orbObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orbObject.name = "HoverOrb";
            orbObject.transform.SetParent(m_RuntimeRoot, false);
            DestroyCollider(orbObject);
            m_OrbTransform = orbObject.transform;
            m_OrbRenderer = orbObject.GetComponent<MeshRenderer>();
            m_OrbRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_OrbRenderer.receiveShadows = false;
            m_OrbRenderer.sharedMaterial = m_BaseMaterial;

            var textObject = new GameObject("Prompt");
            textObject.transform.SetParent(m_RuntimeRoot, false);
            m_TextMesh = textObject.AddComponent<TextMesh>();
            m_TextMesh.text = m_PromptText;
            m_TextMesh.anchor = TextAnchor.MiddleCenter;
            m_TextMesh.alignment = TextAlignment.Center;
            m_TextMesh.characterSize = 0.045f;
            m_TextMesh.fontSize = 48;
            m_TextMesh.color = Color.white;
            m_TextMesh.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (m_TextMesh.font != null)
            {
                var textRenderer = m_TextMesh.GetComponent<MeshRenderer>();
                textRenderer.sharedMaterial = m_TextMesh.font.material;
                textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                textRenderer.receiveShadows = false;
            }
            m_TextMesh.gameObject.SetActive(false);
        }

        public void Bind()
        {
            m_Anchor.hoverEntered.AddListener(OnHoverEntered);
            m_Anchor.hoverExited.AddListener(OnHoverExited);
        }

        public void Unbind()
        {
            m_Anchor.hoverEntered.RemoveListener(OnHoverEntered);
            m_Anchor.hoverExited.RemoveListener(OnHoverExited);
        }

        public void RefreshImmediate(Camera mainCamera, float time)
        {
            if (m_Anchor == null)
                return;

            var anchorTransform = m_Anchor.transform;
            var anchorPosition = anchorTransform.position;
            var anchorRotation = Quaternion.Euler(0f, anchorTransform.eulerAngles.y, 0f);
            m_RuntimeRoot.SetPositionAndRotation(anchorPosition, anchorRotation);

            var isHovered = m_HoverCount > 0;
            var pulse = 1f + Mathf.Sin(time * m_PulseSpeed + anchorPosition.x + anchorPosition.z) * m_PulseAmplitude;
            var markerScale = Vector3.Scale(isHovered ? m_HoverMarkerScale : m_BaseMarkerScale, new Vector3(pulse, 1f, pulse));
            m_MarkerTransform.localPosition = Vector3.zero;
            m_MarkerTransform.localScale = markerScale;

            var orbHeight = 0.08f + Mathf.Sin(time * m_BobSpeed + anchorPosition.x) * m_BobHeight;
            m_OrbTransform.localPosition = new Vector3(0f, orbHeight, 0f);
            m_OrbTransform.localScale = isHovered ? Vector3.one * 0.17f : Vector3.one * 0.12f;

            var activeMaterial = isHovered ? m_HoverMaterial : m_BaseMaterial;
            m_MarkerRenderer.sharedMaterial = activeMaterial;
            m_OrbRenderer.sharedMaterial = activeMaterial;

            if (!m_TextMesh.gameObject.activeSelf)
                return;

            var textTransform = m_TextMesh.transform;
            textTransform.position = anchorPosition + Vector3.up * m_TextHeight;
            if (mainCamera != null)
            {
                var flatForward = mainCamera.transform.position - textTransform.position;
                if (flatForward.sqrMagnitude > 0.0001f)
                    textTransform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
        }

        public void Dispose()
        {
            if (m_RuntimeRoot == null)
                return;

            if (Application.isPlaying)
            {
                Object.Destroy(m_RuntimeRoot.gameObject);
                return;
            }

            Object.DestroyImmediate(m_RuntimeRoot.gameObject);
        }

        void OnHoverEntered(HoverEnterEventArgs args)
        {
            m_HoverCount++;
            m_TextMesh.gameObject.SetActive(true);
        }

        void OnHoverExited(HoverExitEventArgs args)
        {
            m_HoverCount = Mathf.Max(0, m_HoverCount - 1);
            m_TextMesh.gameObject.SetActive(m_HoverCount > 0);
        }

        static void DestroyCollider(GameObject gameObject)
        {
            var collider = gameObject.GetComponent<Collider>();
            if (collider == null)
                return;

            if (Application.isPlaying)
            {
                Object.Destroy(collider);
                return;
            }

            Object.DestroyImmediate(collider);
        }
    }
}
