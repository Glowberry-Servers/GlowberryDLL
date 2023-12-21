# GlowberryDLL

GlowberryDLL is the the main DLL (Dynamic-Link Library) for Glowberry. This DLL is crucial for the internal backend code used throughout the various applications.
All of the documentation will (eventually) be provided. For now, the comments are the only way.

## License

This project is licensed under the terms of the MIT license.

## Issues

For any questions or issues, please open an issue in the relevant repository (Not this one!).

## Usage

Since this is a DLL file, all that is needed is for it and its dependencies to be added to the program package, and, inside the development environment, for the reference to be added. (This is following the normal file structure for the Glowberry projects; A Glowberry directory with all of them inside it. Change the hint to your path.)

```xml
<Reference Include="GlowberryDLL">
  <HintPath>..\GlowberryDLL\bin\Release\GlowberryDLL.dll</HintPath>
</Reference>
```

