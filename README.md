# rubens-psx-engine
A PSX rendering engine made in monogame.

Base code for this engine is based off: https://github.com/blendogames/fna_starterkit
but it was ported over to monogame because of the Content pipeline and shader support.

<!-- ![image](ps1_render.gif) -->
![image](RPE_logo.gif)

A couple of tips to get his running. You'll need the [dotnet cli](https://dotnet.microsoft.com/en-us/download) and run the template command to get monogame installed:

```dotnet new install MonoGame.Templates.CSharp```

if it needs cleaning and you get the "package mapper error:
```dotnet nuget locals -c all```
```dotnet restore```

