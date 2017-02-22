using System.Reflection;


[assembly: AssemblyCompany("Keen IO")]
[assembly: AssemblyCopyright("Copyright © 2014 Keen IO")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyProduct("Keen IO .NET SDK")]

// Add more configurations as neede so as to match the build env.
#if (DEBUG)
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCulture("")]