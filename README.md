# MotionMatching

### 想解决什么问题？
这是一个普通的动画状态机。<br>
![anim-state-machine](/Images/anim-state-machine.png)<br>
各种状态直接切来切去，维护起来很繁琐，消耗很多时间，容易产生很多 BUG。上面的状态机甚至还不算复杂。<br>

### 怎么解决？
根据玩家输入包括速度、方向、跳跃等和玩家当前骨骼位置、旋转、速度等对比离线烘焙的所有动画骨骼以及根据 RootMotion 预测的坐标数据，选择最匹配的一个**动画帧**播放。
![MM](/Images/motion-matching.gif)<br>
基本的Idle和Walk匹配。图左黄字显示输入，图右黄字显示匹配 cost.<br>
更多feature开发中...

### Conclusion
**Motion Matching is a simple idea, that helps us reason about movement description and control.**
**It’s also a new type of animation system, with three advantages:**
1. High quality
2. Controllable responsiveness
3. Minimal manual work

For more details<br>
[MotionMatching pdf](https://twvideo01.ubm-us.net/o1/vault/gdc2016/Presentations/Clavet_Simon_MotionMatching.pdf)<br>
[MotionMatching video](https://www.gdcvault.com/play/1023280/Motion-Matching-and-The-Road)<br>
