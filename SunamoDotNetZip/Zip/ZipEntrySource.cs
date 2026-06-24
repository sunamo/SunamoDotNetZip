namespace Ionic.Zip;

// ZipEntrySource.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa
// All rights reserved.
//
// This code module is part of DotNetZip, a zipfile class library.
//
// ------------------------------------------------------------------
//
// This code is licensed under the Microsoft Public License. 
// See the file License.txt for the license details.
// More info on: http://dotnetzip.codeplex.com
//
// ------------------------------------------------------------------
//
// last saved (in emacs): 
// Time-stamp: <2009-November-19 11:18:42>
//
// ------------------------------------------------------------------
//

public enum ZipEntrySource
{
    None = 0,
    FileSystem,
    Stream,
    ZipFile,
    WriteDelegate,
    JitStream,
    ZipOutputStream,
}