# $PROJECT$

## Target Frameworks

**TargetFrameworks:** `net10.0;net9.0`

**Reason:** Dependencies require .NET 9.0+:
- PowerShell SDK 7.5.0+ requires net9.0
- System.Management.Automation 7.5.0 requires net9.0
- Lock type (System.Threading.Lock) available from net9.0
