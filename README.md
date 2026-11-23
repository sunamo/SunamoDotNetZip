# SunamoDotNetZip

## Short description

Fork knihovny DotNetZip pro .NET 9, vytvořený kvůli závažné bezpečnostní zranitelnosti původní verze. Poskytuje práci s archivy ZIP.


.NET 9 fork for DotNetZip because have high security vulnerability

## Overview

SunamoDotNetZip is part of the Sunamo package ecosystem, providing modular, platform-independent utilities for .NET development.

## Main Components

### Key Classes

- **BZip2InputStream**
- **BZip2OutputStream**
- **ParallelBZip2OutputStream**
- **ComHelper**

### Key Methods

- `Reset()`
- `WriteBits()`
- `WriteByte()`
- `WriteInt()`
- `Flush()`
- `Fill()`
- `CompressAndWrite()`
- `MatchesSig()`
- `IsZipFile()`
- `IsZipFileWithExtract()`

## Installation

```bash
dotnet add package SunamoDotNetZip
```

## Dependencies

- **System.Security.Permissions** (v9.0.3)
- **System.Text.Encoding.CodePages** (v9.0.3)
- **Microsoft.Extensions.Logging.Abstractions** (v9.0.3)
- **DotNet.ReproducibleBuilds** (v1.2.25)

## Package Information

- **Package Name**: SunamoDotNetZip
- **Version**: 25.6.16.1
- **Target Framework**: net9.0
- **Category**: Platform-Independent NuGet Package
- **Source Files**: 60

## Related Packages

This package is part of the Sunamo package ecosystem. For more information about related packages, visit the main repository.

## License

See the repository root for license information.

<!-- sunamo:test-coverage:start -->
## Test coverage

| Metric | Value |
|---|---|
| Method | static |
| Test projects | 1 |
| Public types referenced by tests | 0/47 (0.0 %) |
| Public methods referenced by tests | 0/123 (0.0 %) |
| Line coverage | n/a |
| Branch coverage | n/a |
| Generated (UTC) | 2026-11-22 21:24 |

<!-- sunamo:test-coverage:data {"package":"SunamoDotNetZip","container":"pinpf","testProjects":1,"publicTypes":47,"typesReferencedByTests":0,"publicMethods":123,"methodsReferencedByTests":0,"lineRate":null,"branchRate":null,"method":"static","generatedUtc":"2026-11-22 21:24"} -->
<!-- sunamo:test-coverage:end -->
