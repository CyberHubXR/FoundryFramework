# Getting Started
Getting started with foundry is super simple, we install a couple prerequisites and it almost takes care of itself from there.

## Prerequisites / Dependencies
- Unity version 2021.18.18f1 (Other versions may work, but this is the one that we explicitally suport at the moment)
- [Photon Fusion](https://dashboard.photonengine.com/download/fusion/photon-fusion-1.1.8-f-725.unitypackage) (v1.1.8 stable) (Handles the networking backend)
- [Photon Voice](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) (v2.53) (Handles voice chat through fusion)

There are more Dependencies but we'll install them later using the setup wizard.

## Installation
The photon fusion link will download into your downloads folder, from there double click it or drag it into your current unity application. Then just press import and all the fusion files will install into your project.

Photon voice is slightly different, You need to add photon voice to your assets via that asset store link, Then it will be available in your package manager assets under the sub directory (“my-assets”), Find it, Download it and import it ignoring these folders

* Uncheck Photon\PhotonChat

* Uncheck these folders:
    * Photon\PhotonUnityNetworking
    * Photon\PhotonVoice\Code\Pun
    * Photon\PhotonVoice\Demos contents except Photon\PhotonVoice\Fusion

## Setup
You will need to head over to https://dashboard.photonengine.com/ and create or sign into your account then create two new applications.

The first application will be your fusion application, This will generate a key that will handle the fusion backend. The second application will be your voice application, This will generate a key that will handle the voice backend.

Once you have created both applications you will be able to copy on the hidden api key and paste it into the respective fields in the assets/fusion/photonrealtimesettings.asset file.

## Installing Foundry
Theres a step before installing we need to do.

Head over to your project settings and find the package mangager tab, Then add the following name, url and scope(s)  to the package sources

```
Name: Foundry
Url: https://34.125.193.7:4873/
Scopes: com.spatialape
```

Then head over to the standard package manager, Change the dropdown 'in project' to 'my registries' and add the following packages

* Foundry Core (will install steam spatial audio automatically)
* Ready Player Me Avatars For Foundry
* Demo Authentication System For Foundry
* Fusion networking for Foundry

After you have installed these packages, please follow the steps inside the setup wizard to finish the installation. This should pop up automatically but if it doesn't it can be opened from the menus at `Foundry -> Setup Wizard`

# XR Manager
Foundry has a simple Desktop fallback mode for when an XR runtime is not found, to enable this just make sure that in the XR Package Manager settings auto-initalize is disabled to alow our systems to handle that.

Additonally make sure to Enable openXR under the XR Runtimes and make sure to add the interaction profiles for the devices you plan to support.

## Samples
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