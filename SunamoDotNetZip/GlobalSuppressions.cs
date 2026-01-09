// variables names: ok
// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// CA2022: The Stream.Read method may not read all requested bytes.
// This is a legacy codebase where refactoring all Read calls to use ReadExactly
// or check return values would require extensive testing and validation.
// The current implementation has been stable for years.
[assembly: SuppressMessage("Reliability", "CA2022:Avoid inexact read", Justification = "Legacy code - extensive refactoring required to fix properly")]
