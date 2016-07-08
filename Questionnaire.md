This is a template for manually collect application server information.  All of the following information can be obtained automatically in the template text format by running the ContrastDvnr tool as ```ContrastDvnr.exe text```.  
Contrast Security would prefer the report in XML format which can be obtained by running ```ContrastDvnr.exe``` and sending us the generated ```report.xml``` file.  

##TEMPLATE
-----------------------


### 1). Enter the following information for your application server.
```
====================================================
MACHINE INFORMATION
====================================================
* OS: ex- Windows Server 2012 R2
* OS Architecture: 64-bit/32-bit
* Processor:
* Cores (Physical)
* RAM:
* IIS Version:
* IIS Express Version:
* .NET Versions Installed:
```

### 2). For each AppPool that is used by 1 or more application on your IIS7+ server list its 
* Name 
* Pipeline Mode (Classic or Integrated)
* Bitness (32bit or 64bit)
* CLR version (v2.0 or v4.0) 
* Username (if not using the default)


 Example:
```
====================================================
APP Pools
====================================================

Name: DefaultAppPool
Pipeline Mode: Integrated
64bit: True
Identity: ApplicationPoolIdentity
Username: DefaultAppPool
CLR Version: v4.0
----------------------------------------------------
Name: v2_x64_IPM
Pipeline Mode: Integrated
64bit: False
Identity: ApplicationPoolIdentity
Username: v2_x64_IPM
CLR Version: v2.0
----------------------------------------------------
```

### 3). For each Site on your IIS7+ server, list its 
    * Name
    * Default AppPool.
    * Number of Applications  
    
Then under each site list all of its applications.  For each application list its 
    * Path
    * AppPool 
    * Protocols 
    * User (if not default) 
    * Type/Name of any custom HttpModules (from its config file)
    * List of all .NET Dll's in its bin folder. For each library list its assembly information, at least name and version.
        - Name 
        - Assembly Version.  
        - SHA1 Hash
        - Public Key Token

Also list site bindings for each site.  List the following information for each binding.  Include at least the binding information and protocol.
    * Protocol
    * Hostname
    * Port
    * IP Address
    * Binding Information        

Example:
```
====================================================
SITES
====================================================
Name: Default Web Site
Default AppPool: DefaultAppPool
Applications (2):
    ====================================================
    Path: /CorePolicy_v2_x86_CPM
    Physical Path: C:\inetpub\wwwroot\CorePolicy_v2_x86_CPM
    AppPool: v2_x86_CPM
    Logon Method: ClearText
    Protocols: http
    Enable Preload: False
    User: 
    Libraries (3):
        Filename: AntiXSSLibrary.dll
        Name: AntiXssLibrary
        Assembly Version: 4.2.0.0
        Public Key Token: d127efab8a9c114f
        SHA1 Hash: C31BFE539961FDA78EDA55B6BB2C245DA21BF689	    
        ----------------------------------------------------
        Filename: CorePolicy.dll
        Name: CorePolicy
        Assembly Version: 1.0.0.0
        Public Key Token: None
        SHA1 Hash: DCAC774174BCFB94DE578D3755A03BAFEDEB51DC
        
        ----------------------------------------------------
        Filename: Newtonsoft.Json.dll
        Name: Newtonsoft.Json
        Assembly Version: 6.0.0.0
        Public Key Token: 30ad4fe6b2a6aeed
        SHA1 Hash: 740C0A5899FBF4EE11238B6FA5C9EF1FC80786E2
        ----------------------------------------------------
    HttpModules (1)
        Name: CassetteHttpModule
        Type: Cassette.Aspnet.CassetteHttpModule, Cassette.Aspnet
        ----------------------------------------------------
    ====================================================
        Path: /AuthzTest
        Physical Path: C:\inetpub\wwwroot\AuthzTest
        AppPool: DefaultAppPool
        Logon Method: ClearText
        Protocols: http
        Enable Preload: False
        User: AuthzUserApplicationAccount
        Libraries (1)
            Filename: AuthzTest.dll
            Name: AuthzTest
            Assembly Version: 1.0.0.0
            Public Key Token: None
            SHA1 Hash: B0EF86BDFAED929A7C24AAB7B3E0F7E569970A9C
        ----------------------------------------------------
        HttpModules (0)        
====================================================
Default Web Site Bindings (2)
====================================================
Protocol: http
Hostname: 
Port: 80
IP Address: *
Binding Information: *:80:
----------------------------------------------------
Protocol: net.tcp
Hostname: 
Port: 
IP Address: 
Binding Information: 808:*
----------------------------------------------------
```

### 4). If possible, include library information for all libraries installed in the GAC
This can be obtained by running the ```gacutil.exe``` tool and running ```gacutil /l```.

Example:
```
====================================================
GAC LIBRARIES 
====================================================

CustomMarshalers, Version=2.0.0.0, Culture=neutral,PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=AMD64
ISymWrapper, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a, processorArchitecture=AMD64
Microsoft.Ink, Version=6.1.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=AMD64
Microsoft.Interop.Security.AzRoles, Version=2.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35, processorArchitecture=AMD64
Microsoft.SqlServer.BatchParser, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91, processorArchitecture=AMD64
Microsoft.SqlServer.BatchParser, Version=12.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91, processorArchitecture=AMD64
Microsoft.SqlServer.GridControl, Version=10.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91, processorArchitecture=AMD64
...
```

Enter the information for you server in the template below.  You can also run ```ContrastDvnr.exe text``` to generate a ```report.txt``` file which lists all this information in this template format.

```

====================================================
MACHINE INFORMATION
====================================================

====================================================
APP Pools
====================================================  

====================================================
SITES
====================================================  

====================================================
GAC LIBRARIES 
====================================================

```            
