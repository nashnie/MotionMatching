# MotionMatching

### 解决什么问题？
![MotionMatching](/images/anim-state-machine.png)<br>
让人头疼的状态机，维护成本非常高，这还不是最复杂的状态机。<br>

### 怎么解决？
根据玩家输入包括速度、方向等以及玩家当前骨骼位置、旋转、速度等对比离线烘焙的所有动画骨骼以及根据 rootMotion 预测的坐标数据，选择最匹配的一个。

[MotionMatching pdf](https://twvideo01.ubm-us.net/o1/vault/gdc2016/Presentations/Clavet_Simon_MotionMatching.pdf)<br>
[MotionMatching video](https://www.gdcvault.com/play/1023280/Motion-Matching-and-The-Road)<br>
