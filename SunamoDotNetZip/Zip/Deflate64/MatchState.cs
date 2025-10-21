// EN: Variable names have been checked and replaced with self-descriptive names
// CZ: Názvy proměnných byly zkontrolovány a nahrazeny samopopisnými názvy
namespace Ionic.Zip.Deflate64;

internal enum MatchState
{
    HasSymbol = 1,
    HasMatch = 2,
    HasSymbolAndMatch = 3
}