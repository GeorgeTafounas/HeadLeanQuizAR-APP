# 🧠 HeadLeanQuiz AR
**An Accessible, Gesture-Based Augmented Reality Learning Tool**

![Unity](https://img.shields.io/badge/Unity-2021.3%2B-black?logo=unity)
![ARFoundation](https://img.shields.io/badge/ARFoundation-4.0%2B-blue)
![License](https://img.shields.io/badge/License-MIT-green)
![Accessibility](https://img.shields.io/badge/Accessibility-High--Contrast-orange)

## 🌟 Project Vision
**HeadLeanQuiz AR** is designed to bridge the gap in digital education for students with motor impairments. By utilizing front-facing AR tracking, the application allows users to navigate and answer quiz questions using only **head-tilt gestures**.

This project prioritizes **High-Contrast Visibility** and **Cognitive Ease**, ensuring that the technology is a bridge, not a barrier, to learning.

---

## ✨ Key Features

### ♿ Accessibility-First Design
* **High-Contrast Palette:** Uses a specific "AR-Safe" Gold and Cyan color scheme to ensure text is readable regardless of the physical environment.
* **No-Wrap Questions:** Custom logic ensures questions always stay on a single line, preventing visual clutter.
* **Visual Haptics:** The UI "pulses" and scales during selection, providing a non-verbal confirmation that the user's movement is being registered.
* **Blink Alerts:** Critical system states (like "Face Lost") use rhythmic blinking to alert users with low peripheral vision.

### 🛠 Technical Excellence
* **Stabilized Tracking:** Implements normalized -180° to 180° angle math to eliminate the "flipping" glitches common in standard Euler rotations.
* **Dwell-Time Logic:** Prevents accidental triggers by requiring a sustained hold, adjustable for different mobility levels.
* **Auto-Sizing UI:** Leverages TextMeshPro's dynamic scaling to maintain "Perfect UI" proportions across different device screen sizes.

---

## 🚀 Getting Started

### Prerequisites
* **Hardware:** iOS (ARKit compatible) device with a front-facing camera.
* **Software:** Unity 2021.3 LTS or later.
* **Packages:** AR Foundation, ARKit XR Plugins.

### Installation
1. Clone the repo:
   ```bash
   git clone [https://github.com/GeorgeTafounas/HeadLeanQuizAR-APP.git](https://github.com/GeorgeTafounas/HeadLeanQuizAR-APP.git)
