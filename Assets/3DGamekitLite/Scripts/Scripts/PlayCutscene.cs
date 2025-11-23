using Cinemachine;
using Gamekit3D.GameCommands;
using UnityEngine;

public class PlayCutscene : GameCommandHandler
{
    [Header("Cutscene Settings")]
    public CinemachineVirtualCamera virtualCamera;
    public float cutsceneDuration = 3f;
    public int highPriority = 20;

    [Header("Camera Blend Settings")]
    [Tooltip("Custom blend time for the transition to the cutscene camera (in seconds)")]
    public float blendInTime = 1f;
    [Tooltip("Custom blend time for the transition back from the cutscene camera (in seconds)")]
    public float blendOutTime = 1f;
    [Tooltip("The blend curve style to use for camera transitions")]
    public CinemachineBlendDefinition.Style blendStyle = CinemachineBlendDefinition.Style.EaseInOut;
    public bool pauseEllen = true;

    private int originalPriority;
    private CinemachineBrain cinemachineBrain;
    private CinemachineBlendDefinition originalDefaultBlend;
    private bool hasOriginalBlend = false;


    protected override void Awake()
    {
        base.Awake();

        // ⚠ EXACTEMENT comme SimpleTransformer : tout faire au Awake APRES base.Awake()
        if (virtualCamera == null)
            virtualCamera = GetComponent<CinemachineVirtualCamera>();

        virtualCamera.enabled = false;

        // Trouver le CinemachineBrain dans la scène (généralement sur la Main Camera)
        cinemachineBrain = Camera.main?.GetComponent<CinemachineBrain>();
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
        }

        if (cinemachineBrain != null)
        {
            // Sauvegarder le blend par défaut
            originalDefaultBlend = cinemachineBrain.m_DefaultBlend;
            hasOriginalBlend = true;
        }
        else
        {
            Debug.LogWarning("[PlayCutscene] No CinemachineBrain found in the scene.");
        }

        Debug.Log("[PlayCutscene] Registered with interactionType = " + interactionType);
    }

    public override void PerformInteraction()
    {
        Debug.Log("[PlayCutscene] PerformInteraction RECEIVED !");

        if (virtualCamera == null)
        {
            Debug.LogWarning("[PlayCutscene] No virtual camera found.");
            return;
        }

        // Appliquer le blend personnalisé pour la transition IN
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendInTime);
            Debug.Log("[PlayCutscene] Blend IN applied: " + blendStyle + ", duration: " + blendInTime + "s");
        }

        virtualCamera.enabled = true;

        // Sauvegarde et override de la priorité
        originalPriority = virtualCamera.Priority;
        virtualCamera.Priority = highPriority;

        if (pauseEllen)
            PlayerInput.Instance.ReleaseControl();

        Invoke("EndCutscene", cutsceneDuration);
    }

    private void EndCutscene()
    {
        // Appliquer le blend personnalisé pour la transition OUT
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend = new CinemachineBlendDefinition(blendStyle, blendOutTime);
            Debug.Log("[PlayCutscene] Blend OUT applied: " + blendStyle + ", duration: " + blendOutTime + "s");
        }

        // Restaure la priorité originale
        virtualCamera.Priority = originalPriority;
        virtualCamera.enabled = false;

        // Attendre que la transition OUT soit terminée avant de restaurer le blend par défaut
        Invoke("RestoreDefaultBlend", blendOutTime + 0.1f);

        Debug.Log("[PlayCutscene] Cutscene finished, priority restored.");

        if (pauseEllen)
            PlayerInput.Instance.GainControl();

    }

    private void RestoreDefaultBlend()
    {
        // Restaurer le blend par défaut original
        if (cinemachineBrain != null && hasOriginalBlend)
        {
            cinemachineBrain.m_DefaultBlend = originalDefaultBlend;
            Debug.Log("[PlayCutscene] Default blend restored.");
        }
    }

    void OnDestroy()
    {
        // S'assurer que le blend est restauré si l'objet est détruit
        if (cinemachineBrain != null && hasOriginalBlend)
        {
            cinemachineBrain.m_DefaultBlend = originalDefaultBlend;
        }
    }
}