## Overview
Foundry core is the glue that holds the foundry framework together, it contains interfaces for player interaction, scene navigation, and networking.

## Installation
The recommended way to install foundry packages is through our scoped registry, which can be added in Project Settings > Package Manager > Package Sources

```
Name: Foundry
Url: http://34.125.193.7:4873/
Scopes: com.cyberhub
```

Packages can also be manually cloned from our github repos using the docs provided in their readme files.

## Requirements
Foundry is developed on Unity version 2021.3.18f1, though newer versions should work as well

Core also requires a network provider to be installed, currently the only supported provider is Fusion Networking for 
Foundry, which can be installed from the package manager as well.

## Limitations
Foundry is still in beta and under active development, as such breaking changes will occur rather often until we reach a stable release.

## Reference
[Getting Started](Manual/GettingStarted/GettingStarted.md)<br/>