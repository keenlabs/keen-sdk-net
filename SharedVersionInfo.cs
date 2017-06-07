using System.Reflection;


// Version information for an assembly consists of the following four values:
//
//     <Major Version>.<Minor Version>.<Build Number>.<Revision>
//
// But, SemVer versioning is generally as such:
//
//     <Major Version>.<Minor Version>.<Patch>[.Pre-Release+Metadata]
//
// Since we plan to use this version info for NuGet, we'll follow SemVer here.

// AssemblyVersion can only contain numerical values, so no pre-release or metadata info like
// "-alpha123" can go in here.
[assembly: AssemblyVersion("0.3.16")]

// AssemblyInformationalVersion can have more information in non-numerical format. Here is
// where we could/should put pre-release and/or metadata info if we want to release a version
// as "1.2.3-beta" or similar.
[assembly: AssemblyInformationalVersion("0.3.16")]

// AssemblyFileVersion can and should differ in each assembly if we get into a situation where
// a given assembly needs to be rebuilt and we'd like to track that separately, but we don't
// intend to bump the SDK version nor the NuGet version. Leave it here until then.
[assembly: AssemblyFileVersion("0.3.16")]
