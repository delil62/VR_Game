using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

public sealed class ResetSceneOnControllerCombo : MonoBehaviour
{
    const string k_ResetSceneName = "SampleScene";
    const string k_ResetScenePath = "Assets/Scenes/SampleScene.unity";

    static ResetSceneOnControllerCombo s_Instance;

    InputAction m_LeftResetAction;
    InputAction m_RightResetAction;
    bool m_ResetTriggeredWhileHeld;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstance()
    {
        if (s_Instance != null)
            return;

        var gameObject = new GameObject(nameof(ResetSceneOnControllerCombo));
        s_Instance = gameObject.AddComponent<ResetSceneOnControllerCombo>();
        DontDestroyOnLoad(gameObject);
    }

    void Awake()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;
        CreateResetActions();
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        m_LeftResetAction?.Enable();
        m_RightResetAction?.Enable();
    }

    void OnDisable()
    {
        m_LeftResetAction?.Disable();
        m_RightResetAction?.Disable();
    }

    void OnDestroy()
    {
        m_LeftResetAction?.Dispose();
        m_RightResetAction?.Dispose();
        m_LeftResetAction = null;
        m_RightResetAction = null;
    }

    void Update()
    {
        var leftPrimaryPressed = IsResetButtonPressed(m_LeftResetAction, XRNode.LeftHand);
        var rightPrimaryPressed = IsResetButtonPressed(m_RightResetAction, XRNode.RightHand);
        var resetComboPressed = leftPrimaryPressed && rightPrimaryPressed;

        if (!resetComboPressed)
        {
            m_ResetTriggeredWhileHeld = false;
            return;
        }

        if (m_ResetTriggeredWhileHeld)
            return;

        m_ResetTriggeredWhileHeld = true;
        ReloadGameScene();
    }

    void CreateResetActions()
    {
        if (m_LeftResetAction == null)
        {
            m_LeftResetAction = new InputAction("Reset Left Button", InputActionType.Button, expectedControlType: "Button");
            m_LeftResetAction.AddBinding("<XRController>{LeftHand}/{PrimaryButton}");
        }

        if (m_RightResetAction == null)
        {
            m_RightResetAction = new InputAction("Reset Right Button", InputActionType.Button, expectedControlType: "Button");
            m_RightResetAction.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
        }
    }

    static bool IsResetButtonPressed(InputAction action, XRNode handNode)
    {
        if (action != null && action.enabled && action.IsPressed())
            return true;

        var device = InputDevices.GetDeviceAtXRNode(handNode);
        if (!device.isValid)
            return false;

        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var isPressed) && isPressed;
    }

    static void ReloadGameScene()
    {
        var sceneName = SceneUtility.GetBuildIndexByScenePath(k_ResetScenePath) >= 0
            ? k_ResetSceneName
            : SceneManager.GetActiveScene().name;

        if (string.IsNullOrEmpty(sceneName))
            return;

        Debug.Log($"Reloading scene '{sceneName}' via left X + right A reset shortcut.");
        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
