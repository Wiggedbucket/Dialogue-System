# Dialogue System for Unity

A modular, data-driven dialogue system built for Unity using **ScriptableObjects**, **TextMeshPro**, and **UI Toolkit/UGUI**.
It supports branching dialogue, dynamic variables via a blackboard, customizable text effects, and smooth background and character transitions.

## Features
- **Dynamic Dialogue Graphs** - Runtime nodes with branching paths, splitters, and choices.
- **Dialogue Blackboard** - Store and modify variables during dialogue flow.
- **Character Management** - Animated characters that move, scale, and fade between poses.
- **Background Transitions** - Smooth fades, slides, and black background fallback support.
- **Audio Integration** - Easily queue and play music or voice clips.
- **Customizable Text Appearance** - Font, color, alignment, and style.

## Getting Started
### 1. Set up Project
1. In Unity, **Window -> Package Management -> Package Manager -> Install Package by Name**
2. Fill in: `com.unity.graphtoolkit` and hit install.
3. Install the TextMeshPro package.
4. Go to **Edit -> Project Settings.. -> Player -> Other Settings -> Active Input Handling*** and set it to `Both`

### 2. Import the Package
1. Go to **Assets -> Import Package -> Custom Package…**
2. Select `DialogueSystem_v{Version}.unitypackage`.
3. Check everything you want to include.
4. The Feature Showcase example shows most of the features and also has a prebuilt UI which you can use.

### 3. Scene Setup
1. Create an empty GameObject in your scene and name it **DialogueManager**.
2. Add the following components:
	- `DialogueManager`
	- `DialogueUIManager`
	- `NodeProcessor`
	- `AudioManager`
    - `DialogueCharacterManager`
    - `BackgroundTransitionController`
	- `DialogueBoxTransitionController`
3. Assign the required references (TMP Text fields, button container, background images, etc.).

### 4. Create a Dialogue Graph
1. In the **Project window**, right-click →  
	**Create → Dialogue → Runtime Dialogue Graph**
2. Add nodes (Dialogue, Splitter, Choices, etc.) to define your story flow.
3. Link nodes together via their `out` port.

### 5. Trigger the Dialogue
1. The `DialogueManager` has 4 functions which you can use:
	 `StartDialogue`
	 `InteruptDialogue`
	 `ResumeDialogue`
	 `EndDialogue`
 2. The `DialogueEvents` contains all of the relevant events. You can trigger them using the given functions.

## Runtime Blackboard
The `DialogueBlackboard` stores values globally accessible by dialogue nodes.
Example:
```
Blackboard.SetValue("PlayerName", "Alex");
Blackboard.TryGetValue("PlayerName", out string playerName);
```
Variables can be used in node conditions or referenced dynamically by name.

## Requirements
- **Graph Toolkit Package version:** 0.4.0-exp.2
- **Unity Version:** 6000.2.6f2
- **TextMeshPro**

## License
Free for personal and commercial use.  
Attribution appreciated but not required.
