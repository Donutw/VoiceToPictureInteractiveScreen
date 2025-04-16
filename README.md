# 流体交互粒子屏（开发中） / Fluid Particle Interaction Display (In Progress)

> 本项目基于 [SebLague/Fluid-Sim](https://github.com/SebLague/Fluid-Sim) 二次开发，用于校园咖啡厅的 AI 互动屏项目。
> This project is a continuation of [SebLague/Fluid-Sim], built for an interactive AI-powered café screen.

## 项目简介 / Project Overview

- 用户语音先转为文字提示，再生成图像并以流动粒子形式呈现  
- User speech is first transcribed into text prompts, which are then used to generate images visualized as flowing particles
- 语音识别灵敏度自适应：系统启动时自动检测环境音量，并可随时重新测试，动态调整触发阈值，以适应不同使用环境  
- Adaptive voice sensitivity: the system automatically detects ambient noise levels at startup and allows re-calibration at any time to adjust voice trigger thresholds to changing surroundings
- 支持手势交互，适用于公共展览屏幕  
- Gesture-based interaction adds intuitive control

## 技术栈 / Technologies

- Unity (C#)
- Python: Whisper, ComfyUI, Mediapipe
- Git for version control

## 使用到的模型与资源 / Used Resources

- [Whisper](https://github.com/openai/whisper) - Speech to text (MIT License)
- [ComfyUI](https://github.com/comfyanonymous/ComfyUI) - Image generation workflow
- [Mediapipe](https://github.com/google/mediapipe) - Hand tracking and gestures
