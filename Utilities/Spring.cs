using System;

namespace WinIsland
{
    /// <summary>
    /// 一个高性能的二阶物理弹簧模型，用于模拟丝滑的 UI 动画。
    /// 
    /// </summary>
    public class Spring
    {
        // 目标数值（比如：胶囊想要变成的宽度）
        public double Target { get; set; }

        // 当前数值
        public double Current { get; private set; }

        // 当前速度
        private double Velocity;

        // --- 物理参数调优 (Hand-Tuned for "Dynamic Island" feel) ---
        
        // 刚度 (Stiffness/Tension): 决定了响应速度。
        // 值越大，弹簧越硬，变宽变窄的速度越快。
        // 原值 200 -> 提升到 360，让动作更利落。
        // 公开物理参数以便调教手感
        public double Stiffness { get; set; } = 360; // 劲度系数
        public double Damping { get; set; } = 28;    // 阻尼系数

        // 质量 (Mass): 默认为 1，通常不需要改。
        private const double Mass = 1.0;

        // 精度阈值：小于这个距离直接吸附，避免 CPU 空转
        private const double PrecisionThreshold = 0.5;
        private const double VelocityThreshold = 0.5;

        public Spring() : this(0) { }

        public Spring(double startValue)
        {
            Current = startValue;
            Target = startValue;
            Velocity = 0;
        }

        // 每一帧调用一次
        public double Update(double dt)
        {
            // 防止 dt 过大导致物理爆炸 (Spiral of death)
            // 如果系统卡顿，我们将其钳制在 0.1s 以内，或者进行分步积分（这里简化为 Clamp）
            if (dt > 0.064) dt = 0.064; 

            // 1. 计算受力: Hooke's Law + Damping
            // F = -k(x - target) - c(v)
            double displacement = Current - Target;
            double springForce = -Stiffness * displacement;
            double dampingForce = -Damping * Velocity;
            
            // 2. 牛顿第二定律: F = ma => a = F/m
            double acceleration = (springForce + dampingForce) / Mass;

            // 3. 欧拉半隐式积分 (Semi-Implicit Euler) - 比标准欧拉更稳定
            Velocity += acceleration * dt;
            Current += Velocity * dt;

            // 4. 自动吸附 (Resting Check)
            // 如果非常接近目标且速度很慢，直接停下来
            if (Math.Abs(displacement) < PrecisionThreshold && Math.Abs(Velocity) < VelocityThreshold)
            {
                Current = Target;
                Velocity = 0;
            }

            return Current;
        }

        /// <summary>
        /// 强制重置状态（例如窗口初始化时）
        /// </summary>
        public void Reset(double value)
        {
            Current = value;
            Target = value;
            Velocity = 0;
        }
    }
}