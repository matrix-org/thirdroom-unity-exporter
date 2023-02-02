# Third Room Unity Exporter

Export Third Room scenes from Unity. Powered by [UnityGLTF](https://github.com/prefrontalcortex/UnityGLTF/tree/dev)

## Getting Started

First you'll need a copy of Unity which can be downloaded [here](https://unity.com/download).

### Versions
- **Recommended:** 2022.1.13f1 ([Unity Hub Link](unityhub://2022.1.13f1/22856944e6d2))
- Supported: 2021.3+
- Unsupported but potentially compatible: 2020.3+

Unity 2021 does not have support for HDR Cubemaps which we use for reflection probes. If you'd like support for them, use Unity 2022.

### Installation

We recommend first starting with creating a brand new project in Unity Hub.

You should use the Universal Render Pipeline (URP) to maximize compatibility with UnityGLTF.

This package depends on a branch of UnityGLTF that needs to be installed before you install the exporter.

1. Open `Window > Package Manager`
2. In Package Manager, click <kbd>+</kbd> and select <kbd>Add Package from git URL</kbd>
3. Paste ```https://github.com/matrix-org/UnityGLTF.git?path=/UnityGLTF/Assets/UnityGLTF#thirdroom/dev```
4. Click <kbd>Add</kbd>.

After that is installed you can do the same thing for the Third Room Exporter:

1. In Package Manager, click <kbd>+</kbd> and select <kbd>Add Package from git URL</kbd>
2. Paste ```https://github.com/matrix-org/thirdroom-unity-exporter.git?path=/Packages/thirdroom-unity-exporter```
3. Click <kbd>Add</kbd>.

If you're interested installing the sample scenes (recommended for first time users), click the import button on the right side of the package manager under "Samples".

[Read the rest of the documentation here](/Documentation~/index.md)

## Credits

Many thanks go to the team at [Needle Tools](https://needle.tools/) who have done an incredible job maintaining the UnityGLTF library and helping with the pain points we had when creating this exporter!
