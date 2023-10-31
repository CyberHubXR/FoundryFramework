# Mapped Components
When supporting multiple network backends, there's going to be times when you want to use a commonly used component from the library we're using behind the scenes. 

For this we have "mapped components". These are components that are mapped to the library we're using behind the scenes, at the moment there are just two. NetworkTransform and NetworkVoiceOutput. 

NetworkTransform gets mapped to the library's transform or physics transform component. We use a "mapped properties" system to allow different libraries to define properties specific to them, and store those settings even if we switch between network providers.

Network Voice Output is a component that is used to output voice data to the network. It's mapped to whatever library's voice output component. Only tested with Photon Voice 2 at the moment, so it's probably going to get refactored at some point.