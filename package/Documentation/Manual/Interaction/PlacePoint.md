# Place Point
@"Foundry.PlacePoint"

![Spatial Touchable Script](/Media/Manual/Interaction/PlacePointScript.png)

## Description
A multipurpose script for snap placement of grabbables. 

## Usage
Add to an object, and then you're done for basic functionality.

The radius and location of where the trigger for placement is can be set with the `Place Radius` and `Radius Offset` properties.

If you wish you can blacklist or whitelist objects by, name, tag, layer, or direct reference using the settings in the Place Requirements section.

## Networking
To network the object add a @"Foundry.Networking.NetworkObject", then events will trigger over the network.