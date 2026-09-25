namespace Ionic.Zip;

// ExtractExistingFileAction.cs
// ------------------------------------------------------------------
//
// Copyright (c)  2009 Dino Chiesa
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
// Time-stamp: <2009-August-25 08:44:37>
//
// ------------------------------------------------------------------
//
// This module defines the ExtractExistingFileAction enum
//
// 
// ------------------------------------------------------------------

public enum ExtractExistingFileAction
{
    Throw,

    OverwriteSilently,

    DoNotOverwrite,

    InvokeExtractProgressEvent,
}