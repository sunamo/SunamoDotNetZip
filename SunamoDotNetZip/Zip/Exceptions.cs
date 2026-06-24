// variables names: ok
// EN: Variable names have been checked and replaced with self-descriptive names
// CZ: Názvy proměnných byly zkontrolovány a nahrazeny samopopisnými názvy


// Exceptions.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2008, 2009 Dino Chiesa and Microsoft Corporation.
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
// Time-stamp: <2011-July-12 12:19:10>
//
// ------------------------------------------------------------------
//
// This module defines exceptions used in the class library.
//
namespace Ionic.Zip;
//[AttributeUsage(AttributeTargets.Class)]
//public class ZipExceptionAttribute : Attribute { }
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d0000B")]
public class BadPasswordException : ZipException
{
    public BadPasswordException() { }
    public BadPasswordException(String message)
        : base(message)
    { }
    public BadPasswordException(String message, Exception innerException)
        : base(message, innerException)
    {
    }
    protected BadPasswordException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d0000A")]
public class BadReadException : ZipException
{
    public BadReadException() { }
    public BadReadException(String message)
        : base(message)
    { }
    public BadReadException(String message, Exception innerException)
        : base(message, innerException)
    {
    }
    protected BadReadException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00009")]
public class BadCrcException : ZipException
{
    public BadCrcException() { }
    public BadCrcException(String message)
        : base(message)
    { }
    protected BadCrcException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00008")]
public class SfxGenerationException : ZipException
{
    public SfxGenerationException() { }
    public SfxGenerationException(String message)
        : base(message)
    { }
    protected SfxGenerationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00007")]
public class BadStateException : ZipException
{
    public BadStateException() { }
    public BadStateException(String message)
        : base(message)
    { }
    public BadStateException(String message, Exception innerException)
        : base(message, innerException)
    { }
    protected BadStateException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
}
[Serializable]
[System.Runtime.InteropServices.GuidAttribute("ebc25cf6-9120-4283-b972-0e5520d00006")]
public class ZipException : Exception
{
    public ZipException() { }
    public ZipException(String message) : base(message) { }
    public ZipException(String message, Exception innerException)
        : base(message, innerException)
    { }
#pragma warning disable SYSLIB0051 // Type or member is obsolete
    protected ZipException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    { }
#pragma warning restore SYSLIB0051
}