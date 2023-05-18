using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// This issue can be temporarily fixed by manually collect and apply root motion data.

public struct AnimationRootJob : IAnimationJob
{
    public NativeReference<Vector3> m_velocityRef;
    public NativeReference<Vector3> m_angularVelocityRef;
    public NativeReference<float> m_deltaTimeRef;

    // Collect root motion data here
    public void ProcessRootMotion(AnimationStream stream)
    {
        m_velocityRef.Value = stream.velocity;
        m_angularVelocityRef.Value = stream.angularVelocity;
        m_deltaTimeRef.Value = stream.deltaTime;
    }

    public void ProcessAnimation(AnimationStream stream) { }
}

public class ModifyVelocityTest_TempFix : MonoBehaviour
{
    public AnimationClip clip;
    public Text targetVelocityText;
    public Text actualVelocityText;
    public Vector3 targetVelocity;
    public Vector3 actualVelocity;

    private Animator m_animator;
    private PlayableGraph m_graph;
    private AnimationScriptPlayable m_asp;
    private AnimationScriptPlayable m_rootAsp;

    private NativeReference<Vector3> m_velocityRef;
    private NativeReference<Vector3> m_angularVelocityRef;
    private NativeReference<float> m_deltaTimeRef;


    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_graph = PlayableGraph.Create("ModifyVelocityTest_TempFix");
        m_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        // AnimationClip
        var acp = AnimationClipPlayable.Create(m_graph, clip);

        // ModifyVelocityJob
        var jobData = new ModifyVelocityJob
        {
            targetVelocity = targetVelocity,
        };
        m_asp = AnimationScriptPlayable.Create(m_graph, jobData);
        m_asp.AddInput(acp, 0, 1f);

        // AnimationRootJob(Collect root motion data)
        m_velocityRef = new NativeReference<Vector3>(Allocator.Persistent);
        m_angularVelocityRef = new NativeReference<Vector3>(Allocator.Persistent);
        m_deltaTimeRef = new NativeReference<float>(Allocator.Persistent);
        var rootJobData = new AnimationRootJob
        {
            m_velocityRef = m_velocityRef,
            m_angularVelocityRef = m_angularVelocityRef,
            m_deltaTimeRef = m_deltaTimeRef,
        };
        m_rootAsp = AnimationScriptPlayable.Create(m_graph, rootJobData);
        m_rootAsp.AddInput(m_asp, 0, 1f);

        // Output, use the m_rootAsp as the source playable
        var animOutput = AnimationPlayableOutput.Create(m_graph, "Anim Output", m_animator);
        animOutput.SetSourcePlayable(m_rootAsp);

        m_graph.Play();
    }

    private void Update()
    {
        var jobData = m_asp.GetJobData<ModifyVelocityJob>();
        jobData.targetVelocity = targetVelocity;
        m_asp.SetJobData(jobData);
    }

    private void OnAnimatorMove()
    {
        actualVelocity = m_velocityRef.Value;

        // Apply root motion data here
        // The values of FrameData.deltaTime and AnimationStream.deltaTime are not affected by Time.timeScale.
        // However, when calculating animation data in the Animation Playable, Time.timeScale has already been introduced,
        // so there is no need to handle Time.timeScale separately in this case.
        //ApplyComponentSpaceVelocity(m_animator, m_velocityRef.Value, m_angularVelocityRef.Value, m_deltaTimeRef.Value);
    }

    private void LateUpdate()
    {
        targetVelocityText.text = "Target Velocity: " + targetVelocity.ToString("F3");
        actualVelocityText.text = "Actual Velocity: " + actualVelocity.ToString("F3");
    }

    private void OnDestroy()
    {
        m_graph.Destroy();

        if (m_velocityRef.IsCreated)
        {
            m_velocityRef.Dispose();
        }

        if (m_angularVelocityRef.IsCreated)
        {
            m_angularVelocityRef.Dispose();
        }

        if (m_deltaTimeRef.IsCreated)
        {
            m_deltaTimeRef.Dispose();
        }
    }

    public static void ApplyComponentSpaceVelocity(Animator target,
        Vector3 compSpaceVelocity, Vector3 compSpaceAngularVelocityInRadian, float deltaTime)
    {
        var targetTransform = target.transform;
        var compSpaceDeltaRotation = Quaternion.Euler(compSpaceAngularVelocityInRadian * Mathf.Rad2Deg * deltaTime);
        var worldSpaceDeltaPosition = targetTransform.TransformDirection(compSpaceVelocity) * deltaTime;
        // 先转换worldSpaceDeltaPosition，再真正施加旋转，动画会和ApplyBuiltinRootMotion更接近
        targetTransform.rotation = compSpaceDeltaRotation * targetTransform.rotation;
        targetTransform.position += worldSpaceDeltaPosition;
    }
}
