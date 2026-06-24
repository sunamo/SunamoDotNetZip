namespace Ionic.Zip;

// EN: Variable names have been checked and replaced with self-descriptive names
// CZ: Názvy proměnných byly zkontrolovány a nahrazeny samopopisnými názvy
// ComHelper.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009 Dino Chiesa.
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
// Time-stamp: <2011-June-13 17:04:06>
//
// ------------------------------------------------------------------
//
// This module defines a COM Helper class.
//
// Created: Tue, 08 Sep 2009  22:03
//

using Interop=System.Runtime.InteropServices;

    [System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d0000F")]
    [System.Runtime.InteropServices.ComVisible(true)]
    [System.Runtime.InteropServices.ClassInterface(System.Runtime.InteropServices.ClassInterfaceType.AutoDispatch)]

    public class ComHelper
    {
    public bool IsZipFile(string filename) => ZipFile.IsZipFile(filename);

    public bool IsZipFileWithExtract(string filename) => ZipFile.IsZipFile(filename, true);

    public bool CheckZip(string filename) => ZipFile.CheckZip(filename);

    public bool CheckZipPassword(string filename, string password) => ZipFile.CheckZipPassword(filename, password);

    public void FixZipDirectory(string filename) => ZipFile.FixZipDirectory(filename);

    public string GetZipLibraryVersion() => ZipFile.LibraryVersion.ToString();

}