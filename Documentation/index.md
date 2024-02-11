## Overview
Foundry core is the glue that holds the foundry framework together, it contains interfaces for player interaction, scene navigation, and networking.

## Installation
The recommended way to install foundry packages is through importing [our GitHub repos](https://github.com/orgs/CyberHubXR/repositories). 

To import foundry core, got to the Unity Package Manager, click the add button from the top left corner, and select "Add package from git URL", then paste in this:

`https://github.com/CyberHubXR/FoundryCore.git?path=Assets/Package`

This can be done for any foundry repo, just replace the url of the package and make sure to keep the `?path=Assets/Package` at the end.

## Requirements
Foundry is developed on Unity version 2021.3.18f1, though newer versions should work as well

Core also requires a network provider to be installed, currently the only supported provider is Fusion Networking for 
Foundry, which can be installed from the package manager as well.

## Limitations
Foundry is still in beta and under active development, as such breaking changes will occur rather often until we reach a stable release.

## Reference
[Getting Started](Manual/GettingStarted.md)<br/>