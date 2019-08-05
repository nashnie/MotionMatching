# MotionMatching

### 想解决什么问题？
这是一个普通的动画状态机。<br>
![anim-state-machine](/Images/anim-state-machine.png)<br>
各种状态切来切去，维护很繁琐，消耗很多时间，也容易产生 BUG，简直是程序员和动画师的噩梦^^

### 怎么解决？
根据玩家输入包括速度、方向、跳跃等和玩家当前骨骼位置、旋转、速度等对比离线烘焙的所有动画骨骼以及根据 RootMotion 预测的坐标数据，选择最匹配的一个**动画帧**播放。
![MM](/Images/motion-matching.gif)<br>
StandIdle、CrouchIdle、Walk、CrouchWalk、Turn等匹配。<br>
更多feature开发中...<br>
1. Multi-thread acceleration match calculation.
2. Compress baked motion field.

### Conclusion
**Motion Matching is a simple idea, that helps us reason about movement description and control.**
**It’s also a new type of animation system, with three advantages:**
1. High quality
2. Controllable responsiveness
3. Minimal manual work

For more details<br>
[MotionMatching pdf](https://twvideo01.ubm-us.net/o1/vault/gdc2016/Presentations/Clavet_Simon_MotionMatching.pdf)<br>
[MotionMatching video](https://www.gdcvault.com/play/1023280/Motion-Matching-and-The-Road)<br>
[MotionMatching](https://zhuanlan.zhihu.com/p/50141261)<br>
