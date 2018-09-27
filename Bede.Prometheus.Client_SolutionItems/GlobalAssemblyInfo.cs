using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyProduct("Bede Prometheus Client")]

[assembly: AssemblyCompany("Bede Gaming Limitted")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
 [assembly: AssemblyConfiguration("Release")]
#endif

// This is the real version number, used in NuGet packages and for display purposes.
[assembly: AssemblyFileVersion("1.0.0")]

// Only use major version here, with others kept at zero, for correct assembly binding logic.
[assembly: AssemblyVersion("1.0.0")]

[assembly: InternalsVisibleTo("Bede.Prometheus.Client_Tests.NetFramework")]
[assembly: InternalsVisibleTo("Bede.Prometheus.Client_Tests.NetCore")]
[assembly: InternalsVisibleTo("Bede.Prometheus.Client_Benchmark.NetFramework")]
