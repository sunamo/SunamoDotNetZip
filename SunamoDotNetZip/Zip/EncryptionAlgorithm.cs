namespace Ionic.Zip;

// EncryptionAlgorithm.cs
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
// Time-stamp: <2009-October-21 17:24:45>
//
// ------------------------------------------------------------------
//
// This module defines the EncryptionAgorithm enum
//
// 
// ------------------------------------------------------------------
public enum EncryptionAlgorithm
{
    None = 0,
    PkzipWeak,
#if AESCRYPTO
    WinZipAes128,
    WinZipAes256,
#endif
    Unsupported = 4,
    // others... not implemented (yet?)
}