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
    }

    public void SetVisible(bool visible)
    {
        m_Visible = visible;

        if (m_LeftLine != null)
            m_LeftLine.enabled = visible;

        if (m_RightLine != null)
            m_RightLine.enabled = visible;
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
}
