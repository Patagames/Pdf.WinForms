using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if DOTNET20
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 2.0)")]
#elif DOTNET30
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 3.0)")]
#elif DOTNET35
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 3.5)")]
#elif DOTNET40
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.0)")]
#elif DOTNET45
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.5)")]
#elif DOTNET451
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.5.1)")]
#elif DOTNET452
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.5.2)")]
#elif DOTNET46
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.6)")]
#elif DOTNET461
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.6.1)")]
#elif DOTNET462
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.6.2)")]
#elif DOTNET47
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls (.net 4.7)")]
#else
[assembly: AssemblyTitle("Pdfium.Net SDK - WinForms controls")]
#endif
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Patagames Software")]
[assembly: AssemblyProduct("Pdfium.Net SDK - WinForms controls")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("3ee5b54a-d560-4066-8882-939b9cbf611d")]

// Version information for an assembly consists of the following four values:
//      Major Version
//      Minor Version - Improvments
//      Build Number - BugFixes
//      Revision -.Net framework

[assembly: AssemblyInformationalVersion("3.15.2704")]  //Should be equal to save property of Patagames.Pdf assembly
[assembly: AssemblyVersion("3.14.2."+
#if DOTNET20
"20"
#elif DOTNET30
"30"
#elif DOTNET35
"35"
#elif DOTNET40
"40"
#elif DOTNET45
"45"
#elif DOTNET451
"451"
#elif DOTNET452
"452"
#elif DOTNET46
"46"
#elif DOTNET461
"461"
#elif DOTNET462
"462"
#elif DOTNET47
"47"
#else
"0"
#endif
)]