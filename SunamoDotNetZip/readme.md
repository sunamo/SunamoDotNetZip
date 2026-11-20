# $PROJECT$

## Target Frameworks

**TargetFrameworks:** `net10.0;net9.0`

**Reason:** Dependencies require .NET 9.0+:
- PowerShell SDK 7.5.0+ requires net9.0
- System.Management.Automation 7.5.0 requires net9.0
- Lock type (System.Threading.Lock) available from net9.0

## License

This package is a fork of DotNetZip by Dino Chiesa. The library is licensed as follows:

- DotNetZip code (Dino Chiesa, Microsoft Corporation): Microsoft Public License (MS-PL).
- `Zlib` (ported from JZlib, Copyright (c) 2000-2003 ymnk, JCraft, Inc.): BSD 3-Clause license.
- `BZip2` (based on code from the Apache Software Foundation): Apache License 2.0.

The license headers in the source files are kept unchanged.
