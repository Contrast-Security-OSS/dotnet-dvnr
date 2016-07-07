**ContrastDvnr 1.0**:  Utility for displaying information about the IIS Site and applications on the current machine.  It creates a report file about the following:

 * Machine Information - OS Version, Processor Speed, Memory Available, .NET versions installed
 * IIS 7.0 Site Information 
    - Bindings - Ip Address, Port, Binding Information
    - Applications - AppPool, Authentication Mode, .NET Dlls, HttpModules 
    - AppPools - .NET Framework version, Pipeline mode, .NET x86/64bit.  *(AppPools not used by any application are ignored)*
 * GAC .NET Dlls. *(Microsoft Dll's are ignored)*

By default results are written to report.xml file in XML format.  JSON or text output format can be chosen instead.  Output can also be written to another file or output to the screen.  Full programs options:

    Usage: 
        ContrastDvnr.exe [xml | json | text] [--file=<FILE>] [--screen]
        ContrastDvnr.exe (-h | --help)
        ContrastDvnr.exe --version 

    Options:
        --file=<FILE>     Different name/path for the report file [default: report.xml]
        --screen          Display to standard output stream instead of file

Prerequisites:
    * Windows 2008/2008, Windows 7/8/10 or newer
    * .NET Framework 4.0 or above

Binaries can be downloaded from the *dist* folder in this repository.
    