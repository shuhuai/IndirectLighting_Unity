# Indirect Lighting for Unity4
The project is about a set of new components to support physically-based direct and indirect lighting in Unity4. It uses a new shader for the physically-based lighting model (GGX). This shader supports indirect specular lighting, so a physically-based IBL (image-based lighting) technique is used to generate indirect specular lighting. 

There are two main components in this system to manage IBL. The first one is reflection probes to capture the local environment as a cube map. The second one is an environment map manager to assign lighting data to those objects that support indirect specular lighting. To compute physically-based IBL, there are options to convert original cube maps to physically-based IBL data. In addition, this project has a function to convert cube maps to spherical harmonics coefficients and a preview tool to preview indirect lighting effect.
### Requirements
* Unity 4.34 or newer Unity 4 (http://unity3d.com/cn/get-unity/download/archive)
* DirectX11 supported graphic cards 

### Interactions
Mouse/Keyboard:
- Mouse : Rotate camera (left click on press)

 Buttons:
* Click buttons to switch different materials and positions
