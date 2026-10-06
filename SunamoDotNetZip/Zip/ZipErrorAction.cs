namespace Ionic.Zip;

// ZipErrorAction.cs
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
// Time-stamp: <2009-September-01 18:43:20>
//
// ------------------------------------------------------------------
//
// This module defines the ZipErrorAction enum, which provides
// an action to take when errors occur when opening or reading
// files to be added to a zip file. 
// 
// ------------------------------------------------------------------
///
///
public enum ZipErrorAction
{
    Throw,
    Skip,
    Retry,
    InvokeErrorEvent,
}