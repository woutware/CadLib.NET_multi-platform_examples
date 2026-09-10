# CadLib.NET multi-platform examples: read, write and display DWG and DXF files

[CadLib.NET (multi-platorm) is the multi-platform version of CadLib, a .NET library to read, write and display AutoCAD DWG and DXF files](https://www.woutware.com/cadlib-net). Example projects include converters to PDF, SVG and bitmaps.

For the Windows version of CadLib, please visit https://www.woutware.com/cadlib-net, and download the Windows trial version, which contains Win Forms, WPF and Open GL viewer examples.

The [CadLib.NET documentation with many examples can be found on the Wout Ware web site (needs registration to access)](https://www.woutware.com/doc/8.0/netcore/index.html). If you have any questions, please post your question on the [forum](https://www.woutware.com/forum).

This repository contains basic read, write and export samples for the CadLib.NET trial version. 

For these applications to work you will need a trial license.
The MyAppKeyPair.snk linked in the projects are not present in the repository, 
you should generate your own strong name key and keep it private.

1. You can generate a strong name key with the following command in the Visual Studio command prompt:
    ```sn -k MyKeyPair.snk```

1. The next step is to extract the public key file from the strong name key (which is a key pair):
    ```sn -p MyKeyPair.snk MyPublicKey.snk```

1. Display the public key token for the public key: 	
    ```sn -t MyPublicKey.snk```

1. Go to the project properties Signing tab (or Build -> signing in VS2022), and check the "Sign the assembly" checkbox, 
   and choose the strong name key you created.

1. Register and get your trial license from [https://www.woutware.com/SoftwareLicenses](https://www.woutware.com/SoftwareLicenses).
   Enter your strong name key public key token that you got at step 3.
