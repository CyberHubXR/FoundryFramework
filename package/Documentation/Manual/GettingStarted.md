# Getting Started
Getting started with foundry is super simple, we install a compatible unity version, add in foundry, and it guides you from there.

## Prerequisites / Dependencies
- Unity version 2021.18.18f1 (Other versions may work, but this is the one that we explicitly support at the moment)

We'll install other dependencies later using the Foundry setup wizard.

## Installing Foundry
Theres a step before installing we need to do.

Head over to your project settings and find the package manager tab, Then add the following name, url and scope(s)  to the package sources

```
Name: Foundry
Url: https://34.125.193.7:4873/
Scopes: com.cyberhub
```

Then head over to the standard package manager, Change the dropdown 'in project' to 'my registries' and add the following packages

* Foundry Core
* Fusion networking for Foundry

After you have installed these packages, please follow the steps inside the setup wizard to finish the installation. This should pop up automatically but if it doesn't it can be opened from the menus at `Foundry -> Setup Wizard`

# Networking
Foundry is a a networked framework at heart, and thus requires a networking solution to be installed, currently our most stable 
module for this is our Fusion Networking for Foundry package. This package is a wrapper around the Fusion networking framework, 
and provides a simple interface for Foundry users to use, while still allowing you to use Fusion specific features if you wish.

For it to work you'll need to install these two libraries into your project, either through these links or the ones in the setup wizard.
- [Photon Fusion](https://dashboard.photonengine.com/download/fusion/photon-fusion-1.1.8-f-725.unitypackage) (v1.1.8 stable) (Handles the networking backend)
- [Photon Voice](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) (v2.53) (Handles voice chat through fusion)

Follow the provided docs provided by Photon for each to get them set up, including setting up network keys and you should be good to go for networking.


# XR Manager
Foundry has a simple Desktop fallback mode for when an XR runtime is not found, to enable this just make sure that in the XR Package Manager settings auto-initialize is disabled to allow our systems to handle that.

Additionally make sure to Enable openXR under the XR Runtimes and make sure to add the interaction profiles for the devices you plan to support.

## Samples
Many of Foundry's core features are pre-implemented in some prefabs provided in the package samples.

After installation head back into your package manager and install the samples listed under foundry core and use those for testing.

Open up the offline scene and add it to your build settings (CTRL+SHIFT+B) and then click “add open scenes” button.

Then do the same for the online scene.

Then go back to the offline scene press play and it should all work.

You will join a blank environment with a blue table and a simple interaction prefab.

Build it to an apk or exe and send it to another person. If you both press play you will end up in a multiplayer scene together with voice chat and synced interactions.


### Troubleshooting

If you cant load from offline to online ensure you completed the openXR step and the fusion key step.

If you and another person cannot load into the same lobby ensure you have the same fusion key and voice key (will be in the build anyway) and ensure your on the same region / fixed region.

Finally if nothing works, [join our discord](https://discord.gg/tnMu5HRU4m) and ping @Staff 