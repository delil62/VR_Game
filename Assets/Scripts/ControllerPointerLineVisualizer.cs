using UnityEngine;

public class ControllerPointerLineVisualizer : MonoBehaviour
{
    [SerializeField]
    Transform m_LeftController;

    [SerializeField]
    Transform m_RightController;

    [SerializeField]
    float m_LineLength = 10f;

    [SerializeField]
    float m_LineWidth = 0.003f;

    [SerializeField]
    Color m_LineColor = new(1f, 1f, 1f, 0.5f);

    LineRenderer m_LeftLine;
    LineRenderer m_RightLine;
    bool m_Visible = true;
    bool m_RightTeleportMode;

    void Awake()
    {
        if (m_LeftController == null)
            m_LeftController = GameObject.Find("Left Hand Controller")?.transform;

        if (m_RightController == null)
            m_RightController = GameObject.Find("Right Hand Controller")?.transform;

        m_LeftLine = CreateLine("Left Controller Pointer Line", m_LeftController);
        m_RightLine = CreateLine("Right Controller Pointer Line", m_RightController);
    }

    void LateUpdate()
    {
        UpdateLine(m_LeftLine, m_LeftController);
        UpdateLine(m_RightLine, m_RightController);
        UpdateTeleportPulse();
    }

    public void SetVisible(bool visible)
    {
        m_Visible = visible;

        if (m_LeftLine != null)
            m_LeftLine.enabled = visible;

        if (m_RightLine != null)
            m_RightLine.enabled = visible;
    }

    public void SetRightTeleportMode(bool enabled)
    {
        m_RightTeleportMode = enabled;

        if (m_RightLine != null)
            m_RightLine.enabled = enabled || m_Visible;

        if (!enabled && m_RightLine != null)
        {
            m_RightLine.startWidth = m_LineWidth;
            m_RightLine.endWidth = m_LineWidth;
            m_RightLine.startColor = m_LineColor;
            m_RightLine.endColor = new Color(m_LineColor.r, m_LineColor.g, m_LineColor.b, 0f);
        }
    }

    LineRenderer CreateLine(string lineName, Transform parent)
    {
        if (parent == null)
            return null;

        var lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(parent, false);

        var line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = m_LineWidth;
        line.endWidth = m_LineWidth;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = m_LineColor;
        line.endColor = new Color(m_LineColor.r, m_LineColor.g, m_LineColor.b, 0f);
        line.enabled = m_Visible;

        return line;
    }

    void UpdateLine(LineRenderer line, Transform controller)
    {
        if (line == null || controller == null)
            return;

        line.SetPosition(0, controller.position);
        line.SetPosition(1, controller.position + controller.forward * m_LineLength);
    }

    void UpdateTeleportPulse()
    {
        if (!m_RightTeleportMode || m_RightLine == null)
            return;

        var pulse = 0.65f + Mathf.Sin(Time.time * 7f) * 0.35f;
        var color = new Color(0.1f, 0.75f, 1f, 0.55f + pulse * 0.4f);
        var width = Mathf.Lerp(m_LineWidth * 2f, m_LineWidth * 4f, pulse);

        m_RightLine.startWidth = width;
        m_RightLine.endWidth = width;
        m_RightLine.startColor = color;
        m_RightLine.endColor = new Color(color.r, color.g, color.b, 0.05f);
    }
}
