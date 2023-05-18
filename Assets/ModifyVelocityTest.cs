using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

// About this issue:
// 
// When using humanoid animation, modifying the `velocity` property through `AnimationStream` did not take effect as expected.
// This issue does not arise when using generic animation.
// 
// How to reproduce:
// 
// a. Open the `SampleScene`.
// b. Enter Play Mode.
// c. In the Game view, you can see the "Target Velocity" and the "Actual Velocity" are different.

public struct ModifyVelocityJob : IAnimationJob
{
    public Vector3 targetVelocity;

    public void ProcessRootMotion(AnimationStream stream) { stream.velocity = targetVelocity; }
    public void ProcessAnimation(AnimationStream stream) { }
}

public class ModifyVelocityTest : MonoBehaviour
{
    public AnimationClip clip;
    public Text targetVelocityText;
    public Text actualVelocityText;
    public Vector3 targetVelocity;
    public Vector3 actualVelocity;

    private Animator m_animator;
    private PlayableGraph m_graph;
    private AnimationScriptPlayable m_asp;


    private void Awake()
    {
        m_animator = GetComponent<Animator>();
        m_graph = PlayableGraph.Create("ModifyVelocityTest");
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

        // Output
        var animOutput = AnimationPlayableOutput.Create(m_graph, "Anim Output", m_animator);
        animOutput.SetSourcePlayable(m_asp);

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
        actualVelocity = m_animator.velocity;
    }

    private void LateUpdate()
    {
        targetVelocityText.text = "Target Velocity: " + targetVelocity.ToString("F3");
        actualVelocityText.text = "Actual Velocity: " + actualVelocity.ToString("F3");
    }

    private void OnDestroy()
    {
        m_graph.Destroy();
    }
}
