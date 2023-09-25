Provenance in Games
====
# About

Provenance in Games (PinG) is a proposed conceptual framework that collects information during a game session and maps it to provenance terms, using digital provenance concepts for representing the game flow, and providing the means for a post-game analysis. The goal of this work is to improve the player’s understanding of the game flow, providing insights on how the story progressed and the influences in the outcome. In order to improve understanding, this conceptual framework provides the means for analyzing the game flow by using provenance.

The provenance analysis is done by processing collected gameplay data and generating a provenance graph, which relates the actions and events that occurred during the game session. This provenance graph might allow the player, or a third person, such as a mentor, to identify critical actions that influenced the game outcome and helps to understand how events were generated and which decisions influenced them. This process may also aid in the identification of mistakes, allowing the player to reflect upon them for future interactions.

The Provenance in Games conceptual framework was initially instantiated in the Software Development Manager (SDM) game as a proof of concept. The SDM game focuses on introducing Software Engineering concepts and skills to undergraduate students. The version of SDM with provenance support makes use of the conceptual framework for collecting provenance and can be viewed by using Prov Viewer, an integral tool for visualizing the provenance graph.

This project was initiated by Troy Kohwalter and professors Leonardo Murta and Esteban Clua during Troy's doctoral course at Universidade Federal Fluminense.

# Provenance in Games for Unity

Provenance in Games for Unity (PinGU) is a generic component capable of gathering provenance
during a game session, leading to a domain-independent and lowcoupling solution. PinGU is composed of components written in C# and UnityScript (a version of JavaScript used by old versions from Unity3D) that provides easier provenance extraction, requiring minimal coding in the game's existing components.

# Provenance in Games for Unity with Replay

Provenance in Games for Unity with Replay (PinGU Replay) is a generic component that implements Prov-Replay conceptual framework and use PinGU as the provenance tracker module. PinGU Replay is composed of components written in C# that provides easier way to configure and save game replay, requiring minimal coding in the game's existing components. Furthermore, PinGU Replay provides a visual representation of the provenance graph inside the game level space synced with a replay, allowing users to configure rules to interactively manipulate the graph by making it possible to omit or highlight desired elements during the analysis process.

This project was initiated by Leonardo Thurler, Sidney Melo and professors Esteban Clua and Troy Kohwalter during Leonardo's master course at Universidade Federal Fluminense.

# Team

* Troy Costa Kohwalter (joined in July 2014)
* Leonardo Gresta Paulino Murta (joined in July 2014)
* Esteban Walter Gonzalez Clua (joined in July 2014)

# Additional contributors

* Sidney Araujo Melo (joined in May 2019)
* Leonardo Pereira Thurler (joined in September 2023)

# About the project folders
* The Documents folder contains a tutorial about PinG and some diagrams.
* To use only PinGU, you can get the desired version C# or js at Unity3D/PinGU folder.
* To use PinGU Replay, you can get it at Unity/PinGUReplay folder. This folder contains PinGU and all dependencies that PinGU Replay needs. PinGU Replay is only available on C#.
* The Unity/PinGUReplaySampleUnityProject folder contains a sample project using PinGU Replay. This project is a simple platform 2D game where the player needs collect five coins to finish game.

# Documentation

* [PinGU - Published Paper](https://www.researchgate.net/profile/Troy_Kohwalter)
* [PinGU - Presentation](https://drive.google.com/file/d/1iwdYhujuxTWM8Lv3DEj7tHlyv3guJ1BF/view?usp=sharing)

# Videos

* [PinGU Replay - How to use](https://bit.ly/how-to-use-pingu-replay)

# Development

* [Source Code](https://github.com/gems-uff/ping)
* [Issue Tracking](https://github.com/gems-uff/ping/issues)

# Technologies

* [Unity](https://unity3d.com/)
* [C#](https://en.wikipedia.org/wiki/C_Sharp_(programming_language))
* [UnityScript](http://wiki.unity3d.com/index.php/UnityScript_versus_JavaScript)
* [Prov Viewer](https://github.com/gems-uff/prov-viewer)

# License

Copyright (c) 2014 Universidade Federal Fluminense (UFF)  
  
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:  
  
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.  
  
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
