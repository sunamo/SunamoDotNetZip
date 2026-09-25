namespace Ionic.Zip;

// ZipFile.Selector.cs
// ------------------------------------------------------------------
//
// Copyright (c) 2009-2010 Dino Chiesa.
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
// Time-stamp: <2011-August-06 09:35:58>
//
// ------------------------------------------------------------------
//
// This module defines methods in the ZipFile class associated to the FileFilter
// capability - selecting files to add into the archive, or selecting entries to
// retrieve from the archive based on criteria including the filename, size, date, or
// attributes.  It is something like a "poor man's LINQ".  I included it into DotNetZip
// because not everyone has .NET 3.5 yet.  When using DotNetZip on .NET 3.5, the LINQ
// query/selection will be superior.
//
// These methods are segregated into a different module to facilitate easy exclusion for
// those people who wish to have a smaller library without this function.
//
// ------------------------------------------------------------------
partial class ZipFile
{
    public void AddSelectedFiles(String selectionCriteria) => this.AddSelectedFiles(selectionCriteria, ".", null, false);
    public void AddSelectedFiles(String selectionCriteria, bool recurseDirectories) => this.AddSelectedFiles(selectionCriteria, ".", null, recurseDirectories);
    public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk) => this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, false);
    public void AddSelectedFiles(String selectionCriteria, String directoryOnDisk, bool recurseDirectories) => this.AddSelectedFiles(selectionCriteria, directoryOnDisk, null, recurseDirectories);
    public void AddSelectedFiles(String selectionCriteria,
                                 String directoryOnDisk,
                                 String directoryPathInArchive) => this.AddSelectedFiles(selectionCriteria, directoryOnDisk, directoryPathInArchive, false);
    public void AddSelectedFiles(String selectionCriteria,
                                 String directoryOnDisk,
                                 String directoryPathInArchive,
                                 bool recurseDirectories) => _AddOrUpdateSelectedFiles(selectionCriteria,
                                  directoryOnDisk,
                                  directoryPathInArchive,
                                  recurseDirectories,
                                  false);
    public void UpdateSelectedFiles(String selectionCriteria,
                                 String directoryOnDisk,
                                 String directoryPathInArchive,
                                 bool recurseDirectories) => _AddOrUpdateSelectedFiles(selectionCriteria,
                                  directoryOnDisk,
                                  directoryPathInArchive,
                                  recurseDirectories,
                                  true);
    private string EnsureendInSlash(string text) => text.EndsWith("\\") ? text : text + "\\";
    private void _AddOrUpdateSelectedFiles(String selectionCriteria,
                                           String directoryOnDisk,
                                           String directoryPathInArchive,
                                           bool recurseDirectories,
                                           bool wantUpdate)
    {
        if (directoryOnDisk == null && (Directory.Exists(selectionCriteria)))
        {
            directoryOnDisk = selectionCriteria;
            selectionCriteria = "*.*";
        }
        else if (String.IsNullOrEmpty(directoryOnDisk))
        {
            directoryOnDisk = ".";
        }
        // workitem 9176
        while (directoryOnDisk.EndsWith("\\")) directoryOnDisk = directoryOnDisk[..^1];
        if (Verbose) StatusMessageTextWriter.WriteLine("adding selection '{0}' from dir '{1}'...",
                                                           selectionCriteria, directoryOnDisk);
        FileSelector ff = new(selectionCriteria,
                                                       AddDirectoryWillTraverseReparsePoints);
        var itemsToAdd = ff.SelectFiles(directoryOnDisk, recurseDirectories);
        if (Verbose) StatusMessageTextWriter.WriteLine("found {0} files...", itemsToAdd.Count);
        OnAddStarted();
        AddOrUpdateAction action = (wantUpdate) ? AddOrUpdateAction.AddOrUpdate : AddOrUpdateAction.AddOnly;
        foreach (var item in itemsToAdd)
        {
            // workitem 10153
            string dirInArchive = (directoryPathInArchive == null)
                ? null
                // workitem 12260
                : ReplaceLeadingDirectory(Path.GetDirectoryName(item),
                                          directoryOnDisk,
                                          directoryPathInArchive);
            if (File.Exists(item))
            {
                if (wantUpdate)
                    this.UpdateFile(item, dirInArchive);
                else
                    this.AddFile(item, dirInArchive);
            }
            else
            {
                // this adds "just" the directory, without recursing to the contained files
                AddOrUpdateDirectoryImpl(item, dirInArchive, action, false, 0);
            }
        }
        OnAddCompleted();
    }
    // workitem 12260
    private static string ReplaceLeadingDirectory(string original,
                                                  string pattern,
                                                  string replacement)
    {
        string upperString = original.ToUpper();
        string upperPattern = pattern.ToUpper();
        int p1 = upperString.IndexOf(upperPattern);
        return p1 != 0 ? original : replacement + original[upperPattern.Length..];
    }
#if NOT
        private static string ReplaceEx(string original,
                                                      string pattern,
                                                      string replacement)
        {
            int count, position0, position1;
            count = position0 = position1 = 0;
            string upperString = original.ToUpper();
            string upperPattern = pattern.ToUpper();
            int inc = (original.Length/pattern.Length) *
                (replacement.Length-pattern.Length);
            char [] chars = new char[original.Length + Math.Max(0, inc)];
            while( (position1 = upperString.IndexOf(upperPattern,
                                                    position0)) != -1 )
            {
                for ( int i=position0 ; i < position1 ; ++i )
                    chars[count++] = original[i];
                for ( int i=0 ; i < replacement.Length ; ++i )
                    chars[count++] = replacement[i];
                position0 = position1+pattern.Length;
            }
            if ( position0 == 0 ) return original;
            for ( int i=position0 ; i < original.Length ; ++i )
                chars[count++] = original[i];
            return new string(chars, 0, count);
        }
#endif
    public ICollection<ZipEntry> SelectEntries(String selectionCriteria)
    {
        FileSelector ff = new(selectionCriteria,
                                                       AddDirectoryWillTraverseReparsePoints);
        return ff.SelectEntries(this);
    }
    public ICollection<ZipEntry> SelectEntries(String selectionCriteria, string directoryPathInArchive)
    {
        FileSelector ff = new(selectionCriteria,
                                                       AddDirectoryWillTraverseReparsePoints);
        return ff.SelectEntries(this, directoryPathInArchive);
    }
    public int RemoveSelectedEntries(String selectionCriteria)
    {
        var selection = this.SelectEntries(selectionCriteria);
        this.RemoveEntries(selection);
        return selection.Count;
    }
    public int RemoveSelectedEntries(String selectionCriteria, string directoryPathInArchive)
    {
        var selection = this.SelectEntries(selectionCriteria, directoryPathInArchive);
        this.RemoveEntries(selection);
        return selection.Count;
    }
    public void ExtractSelectedEntries(String selectionCriteria)
    {
        foreach (ZipEntry e in SelectEntries(selectionCriteria))
        {
            e.Password = _Password; // possibly null
            e.Extract();
        }
    }
    public void ExtractSelectedEntries(String selectionCriteria, ExtractExistingFileAction extractExistingFile)
    {
        foreach (ZipEntry e in SelectEntries(selectionCriteria))
        {
            e.Password = _Password; // possibly null
            e.Extract(extractExistingFile);
        }
    }
    public void ExtractSelectedEntries(String selectionCriteria, String directoryPathInArchive)
    {
        foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryPathInArchive))
        {
            e.Password = _Password; // possibly null
            e.Extract();
        }
    }
    public void ExtractSelectedEntries(String selectionCriteria, string directoryInArchive, string extractDirectory)
    {
        foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryInArchive))
        {
            e.Password = _Password; // possibly null
            e.Extract(extractDirectory);
        }
    }
    public void ExtractSelectedEntries(String selectionCriteria, string directoryPathInArchive, string extractDirectory, ExtractExistingFileAction extractExistingFile)
    {
        foreach (ZipEntry e in SelectEntries(selectionCriteria, directoryPathInArchive))
        {
            e.Password = _Password; // possibly null
            e.Extract(extractDirectory, extractExistingFile);
        }
    }
}
internal abstract partial class SelectionCriterion
{
    internal abstract bool Evaluate(Ionic.Zip.ZipEntry entry);
}
internal partial class NameCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        // swap slashes in reference to local configuration
        string transformedFileName = entry.FileName.Replace(Path.DirectorySeparatorChar == '/' ? '\\' : '/', Path.DirectorySeparatorChar);
        return _Evaluate(transformedFileName);
    }
}
internal partial class SizeCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry) => _Evaluate(entry.UncompressedSize);
}
internal partial class TimeCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        var xValue = Which switch
        {
            WhichTime.atime => entry.AccessedTime,
            WhichTime.mtime => entry.ModifiedTime,
            WhichTime.ctime => entry.CreationTime,
            _ => throw new ArgumentException("??time"),
        };
        return _Evaluate(xValue);
    }
}
internal partial class TypeCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        bool result = (ObjectType == 'D')
            ? entry.IsDirectory
            : !entry.IsDirectory;
        if (Operator != ComparisonOperator.EqualTo)
            result = !result;
        return result;
    }
}
internal partial class AttributesCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        FileAttributes fileAttrs = entry.Attributes;
        return _Evaluate(fileAttrs);
    }
}
internal partial class CompoundCriterion : SelectionCriterion
{
    internal override bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        bool result = Left.Evaluate(entry);
        switch (Conjunction)
        {
            case LogicalConjunction.AND:
                if (result)
                    result = Right.Evaluate(entry);
                break;
            case LogicalConjunction.OR:
                if (!result)
                    result = Right.Evaluate(entry);
                break;
            case LogicalConjunction.XOR:
                result ^= Right.Evaluate(entry);
                break;
        }
        return result;
    }
}
public partial class FileSelector
{
    private bool Evaluate(Ionic.Zip.ZipEntry entry)
    {
        bool result = _Criterion.Evaluate(entry);
        return result;
    }
    public ICollection<Ionic.Zip.ZipEntry> SelectEntries(Ionic.Zip.ZipFile zip)
    {
        if (zip == null) throw new ArgumentNullException(nameof(zip));
        var list = new List<Ionic.Zip.ZipEntry>();
        foreach (Ionic.Zip.ZipEntry e in zip)
        {
            if (this.Evaluate(e))
                list.Add(e);
        }
        return list;
    }
    public ICollection<Ionic.Zip.ZipEntry> SelectEntries(Ionic.Zip.ZipFile zip, string directoryPathInArchive)
    {
        if (zip == null) throw new ArgumentNullException(nameof(zip));
        var list = new List<Ionic.Zip.ZipEntry>();
        // workitem 8559
        string slashSwapped = directoryPathInArchive?.Replace("/", "\\");
        // workitem 9174
        if (slashSwapped != null)
        {
            while (slashSwapped.EndsWith("\\"))
                slashSwapped = slashSwapped[..^1];
        }
        foreach (Ionic.Zip.ZipEntry e in zip)
        {
            if (directoryPathInArchive == null || (Path.GetDirectoryName(e.FileName) == directoryPathInArchive)
                || (Path.GetDirectoryName(e.FileName) == slashSwapped)) // workitem 8559
                if (this.Evaluate(e))
                    list.Add(e);
        }
        return list;
    }
}
