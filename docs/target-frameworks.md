# Target frameworks

RootBlocks uses two targets. Packages that hold pure abstractions or a thin ADO.NET wrapper target `netstandard2.1`, so they can be used from more runtimes. Packages that integrate with ASP.NET Core, EF Core or other modern libraries target `net10.0`.

| Package | Target |
|---------|--------|
| `RootBlocks` | `netstandard2.1` |
| `RootBlocks.Persistence` | `netstandard2.1` |
| `RootBlocks.Persistence.SqlClient` | `netstandard2.1` |
| `RootBlocks.Persistence.MySqlClient` | `netstandard2.1` |
| `RootBlocks.Persistence.Oracle` | `netstandard2.1` |
| `RootBlocks.Persistence.SQLite` | `netstandard2.1` |
| `RootBlocks.Persistence.Npgsql` | `net10.0` |
| `RootBlocks.Persistence.EntityFramework` | `net10.0` |
| `RootBlocks.Messaging.MediatR` | `net10.0` |
| `RootBlocks.Serialization` | `net10.0` |
| `RootBlocks.Serialization.Newtonsoft.Json` | `net10.0` |
| `RootBlocks.Logging.Serilog` | `net10.0` |
| `RootBlocks.AspNetCore.OpenApi` | `net10.0` |

Note that the Npgsql driver targets `net10.0`, unlike the other database drivers.

## What this means for your projects

- **A domain (Core) project** that references only `RootBlocks` can target `netstandard2.1`, or any .NET (Core) 3.0+ version.
- **A read side** using the SQL Server, MySQL, Oracle or SQLite driver can also stay on `netstandard2.1`. With PostgreSQL it must target `net10.0` or later.
- **Infrastructure, application hosts and APIs** that use EF, MediatR, Serilog or OpenAPI must target `net10.0` or later.
- **.NET Framework is not supported.** .NET Standard 2.1 has no .NET Framework implementation, so even the `netstandard2.1` packages cannot be used from .NET Framework 4.x.

## Language version

The packages are compiled with C# 14 and nullable reference types enabled. The `netstandard2.1` packages use only features that compile down for that target. You do not need C# 14 to consume them, although the examples in these docs use recent syntax such as primary constructors and collection expressions.

## SDK

The repository pins the .NET SDK in `global.json` (currently `10.0.103`, rolling forward to the latest feature band). You only need it to build RootBlocks itself. Consuming the packages has no SDK requirement beyond what your target framework needs.
