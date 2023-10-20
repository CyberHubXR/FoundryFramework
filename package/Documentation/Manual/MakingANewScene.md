# Making a new scene
While we have samples, you probbably want to make some stuff from scratch, here's what you need to know.

## Scene types 

The flow of a Foundry based Experience is loosly split into two parts. A non-networked login scene, and then some scenes that contain the actual experence. For foundry to work correctly experence scenes need to be accessed through a login or auth scene most of the time.

1. Login scene
This is usually set to be the first scene in the Unity build settings. We suggest that when you start out to just copy one of our existing auth scenes and edit it to your liking.

Starting from scratch first delete the main camera and then add a FoundryPersistant prefab. This is the only scene that needs a persistant prefab as it will set itself to do not destroy on load and tag along to all other scenes.

Next you will need some login UI and a way to interact with it.

Add a MenuPlayer prefab to the scene, taking note of where the camera is pointing since in desktop mode the camera will not move (mostly it will set it's local y to about the hight of a person in meeters, but will then be stationary).

Now for the UI. You have a couple different options of what to do here. I suggest lookng at a couple example scenes for the more nuanced struture, but on the most basic level you need to create some UI that interacts with an auth script. 
Due to the modular nature of foundry there are a couple different options (at the time of writing the demo auth system and the beamable auth system) so refer to the documentation for that system for how to set it up. 

Once you have some UI set up you still will not be able to interact with it quite yet. When you created a UI canvas Unity usually creaets an EventSystem object (Name check this later), find this if it exists and delete it. Then we want to replace it with a FoundryUIEventSystem prefab

2. Experience Scene

For networked scenes refer to the [networking docs on seting up a network manage](Networking/Networking.md) for a non networked scene just add a OfflinePlayer prefab and your're set (we don't give offile enough love, so the prefab might break between versions as we change stuff. Please tell us when it does!)

If you wish to interact with UI in the scene the steps are the same as above, delete any pre-existing EventSystem objects and replace them with a FoundryUIEventSystem.