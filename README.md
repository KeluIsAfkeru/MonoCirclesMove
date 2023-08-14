# 粒子系统演示项目

这个项目实现了一个粒子系统，使用MonoGame框架在Windows平台上进行2D图形渲染。

## 主要功能

- 初始生成上万个随机分布的圆形粒子
- 每个粒子具有随机位置、速度、颜色、加速度等属性
- 支持鼠标滚轮控制场景缩放
- 支持使用WSAD键控制场景移动
- 支持跟随单个粒子的视角模式
- 实现场景边界，粒子碰到边界会反弹
- 优化了大量粒子的渲染性能
- 后期也会增加其他功能

## 主要特性

- 粒子数量可扩展到10万甚至更高
- 场景边界避免粒子飘出视野范围
- 支持流畅缩放和移动视角
- 可监测渲染性能指标，如帧率、帧时间稳定性等

总体来说，这个项目实现了一个自定义的2D粒子系统，具有一定的可扩展性，可以作为高性能2D粒子系统和自定义渲染管线的代码示例。
