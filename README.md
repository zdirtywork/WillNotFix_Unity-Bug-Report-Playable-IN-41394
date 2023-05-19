# Unity-Bug-Report-Playable-IN-41394

## About this issue

When using **humanoid** animation, modifying the `velocity` property through `AnimationStream` did not take effect as expected.

This issue does not arise when using **generic** animation.

![Sample](./imgs/img_sample.png)

## How to reproduce

1. Open the `SampleScene`.
2. Enter Play Mode.
3. In the Game view, you can see the "Target Velocity" and the "Actual Velocity" are different.

## Further testing

Even if `AnimationStream.velocity` is not modified in animation bob, the final value of `AnimationStream.velocity` still have slight differences compared to the value of `Animator.velocity`. This phenomenon may be related to this bug.

## Solution

This issue can be temporarily fixed by manually collect and apply root motion data.

See: [ModifyVelocityTest_TempFix.cs](./Assets/ModifyVelocityTest_TempFix.cs).
