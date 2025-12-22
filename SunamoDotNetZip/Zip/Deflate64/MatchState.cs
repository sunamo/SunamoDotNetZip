namespace Ionic.Zip.Deflate64;

internal enum MatchState
{
    HasSymbol = 1,
    HasMatch = 2,
    HasSymbolAndMatch = 3
}