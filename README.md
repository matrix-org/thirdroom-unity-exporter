# Third Room Unity Exporter

Export Third Room scenes from Unity. Powered by [UnityGLTF](https://github.com/prefrontalcortex/UnityGLTF/tree/dev)

## Getting Started

First you'll need a copy of Unity which can be downloaded [here](https://unity.com/download).

Internally we use Unity 2021.3.17f1 (LTS), Unity versions 2020.3+ should be supported, but we can't make any promises. 2021.3+ would be preferred.

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
