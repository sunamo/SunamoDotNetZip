namespace Ionic.Zip;

// EN: Variable names have been checked and replaced with self-descriptive names
// CZ: Názvy proměnných byly zkontrolovány a nahrazeny samopopisnými názvy
// ZipFile.x-IEnumerable.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2006, 2007, 2008, 2009 Dino Chiesa and Microsoft Corporation.
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
// Time-stamp: <2009-December-26 15:13:26>
//
// ------------------------------------------------------------------
//
// This module defines smoe methods for IEnumerable support. It is
// particularly important for COM to have these things in a separate module.
//
// ------------------------------------------------------------------



    // For some weird reason, the method with the DispId(-4) attribute, which is used as
    // the _NewEnum() method, and which is required to get enumeration to work from COM
    // environments like VBScript and Javascript (etc) must be the LAST MEMBER in the
    // source.  In the event of Partial classes, it needs to be the last member defined
    // in the last source module.  The source modules are ordered alphabetically by
    // filename.  Not sure why this is true. In any case, we put the enumeration stuff
    // here in this oddly-named module, for this reason.
    //



    public partial class ZipFile
    {
        public System.Collections.Generic.IEnumerator<ZipEntry> GetEnumerator()
        {
            foreach (ZipEntry e in _entries.Values)
                yield return e;
        }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    // An IEnumerator, for use of a ZipFile in a foreach construct.
    // This method is included for COM support.
    [System.Runtime.InteropServices.DispId(-4)]
    public System.Collections.IEnumerator GetNewEnum()          // the name of this method is not significant
=> GetEnumerator();

}
