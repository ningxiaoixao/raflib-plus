/*
 * RAF Library - Plus (RAFlib-Plus)
 * Copyright 2012 Adrian Astley
 *
 *This file is part of RAFlib-Plus.
 *
 *RAFlib-Plus is free software: you can redistribute it and/or modify
 *it under the terms of the GNU General Public License as published by
 *the Free Software Foundation, either version 3 of the License, or
 *(at your option) any later version.

 *RAFlib-Plus is distributed in the hope that it will be useful,
 *but WITHOUT ANY WARRANTY; without even the implied warranty of
 *MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *GNU General Public License for more details.

 *You should have received a copy of the GNU General Public License
 *along with RAFlib-Plus.  If not, see <http://www.gnu.org/licenses/>
*/

/*
 * This class is designed to help extract and 
 * inject files from the League of Legends game files.
 * http://www.leagueoflegends.com 
 * 
 * This class is a modification of the orignal 
 * RAFlib generously created and provided by ItzWarty
 * and found here http://code.google.com/p/raf-manager/source/browse/#svn%2FProjects%2FRAFLib
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ItzWarty;
using zlib = ComponentAce.Compression.Libs.zlib;
using System.Threading;
using System.Globalization;

namespace RAFlibPlus
{
    public class RAFArchive
    {
        private string rafPath = "";
        /// <summary>
        /// Magic value used to identify the file type, must be 0x18BE0EF0
        /// </summary>
        private UInt32 magic = 0;
        /// <summary>
        /// // Version of the archive format, must be 1
        /// </summary>
        private UInt32 version = 0;
        /// <summary>
        /// An index that is used by the runtime, do not modify
        /// Have no idea what this really does, at the moment...
        /// </summary>
        private UInt32 mgrIndex = 0;

        // Byte array to hold the contents of the .raf file
        private byte[] content = null;

        /// <summary>
        /// Dictionary with the full path of the RAF entry as the key
        /// </summary>
        private Dictionary<String, RAFFileListEntry> fileDictFull = null;
        /// <summary>
        /// Dictionary with just the file name as the key
        /// </summary>
        private Dictionary<String, List<RAFFileListEntry>> fileDictShort = null;

        // Constructor
        public RAFArchive(string rafPath)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            this.rafPath = rafPath;

            this.content = System.IO.File.ReadAllBytes(rafPath);
            this.magic = BitConverter.ToUInt32(content.SubArray(0, 4), 0);
            this.version = BitConverter.ToUInt32(content.SubArray(4, 4), 0);
            this.mgrIndex = BitConverter.ToUInt32(content.SubArray(8, 4), 0);

            // Offset to the table of contents from the start of the file
            UInt32 offsetFileList = BitConverter.ToUInt32(content.SubArray(12, 4), 0);
            // Offset to the string table from the start of the file
            UInt32 offsetStringTable = BitConverter.ToUInt32(content.SubArray(16, 4), 0);

            //UINT32 is casted to INT32.  This should be fine, since i doubt that the RAF will become
            //a size of 2^31-1 in bytes.

            this.fileDictFull = new Dictionary<String, RAFFileListEntry>();
            this.fileDictShort = new Dictionary<String, List<RAFFileListEntry>>();
            createFileDicts(this, offsetFileList, offsetStringTable);
        }

        /// <summary>
        /// Returns what i'm calling the ID of an archive, though it's probably related to LoL versioning.
        /// IE: 0.0.0.25, 0.0.0.26
        /// </summary>
        /// <returns></returns>
        public string GetID()
        {
            return new FileInfo(this.rafPath).Directory.Name;
        }

        public string RAFFilePath
        {
            get
            {
                return rafPath;
            }
        }

        #region FileDict functions

        private void createFileDicts(RAFArchive raf, UInt32 offsetFileList, UInt32 offsetStringTable)
        {
            //The file list starts with a uint stating how many files we have
            UInt32 fileListCount = BitConverter.ToUInt32(content.SubArray((Int32)offsetFileList, 4), 0);

            //After the file list count, we have the actual data.
            offsetFileList += 4;

            for (UInt32 currentOffset = offsetFileList; currentOffset < offsetFileList + 16 * fileListCount; currentOffset += 16)
            {
                RAFFileListEntry entry = new RAFFileListEntry(raf, ref raf.content, currentOffset, offsetStringTable);
                raf.fileDictFull.Add(entry.FileName.ToLower(), entry);

                FileInfo fi = new FileInfo(entry.FileName);
                if (!raf.fileDictShort.ContainsKey(fi.Name.ToLower()))
                    raf.fileDictShort.Add(fi.Name.ToLower(), new List<RAFFileListEntry> { entry });
                else
                    raf.fileDictShort[fi.Name.ToLower()].Add(entry);
            }
        }

        public RAFFileListEntry GetFileEntry(string fullPath)
        {
            string lowerPath = fullPath.ToLower();
            if (this.fileDictFull.ContainsKey(fullPath))
                return fileDictFull[fullPath];
            else
                return null;
        }

        public enum RAFSearchType
        {
            All,
            End
        }

        public Dictionary<String, RAFFileListEntry> FileDictFull
        {
            get
            {
                return this.fileDictFull;
            }
        }

        public Dictionary<String, List<RAFFileListEntry>> FileDictShort
        {
            get
            {
                return this.fileDictShort;
            }
        }

        /// <summary>
        /// Finds file entries.
        /// 
        /// Returns any entries whose filepath contains the search string.
        /// Ie: ahri would return /DATA/Characters/Ahri/Ahri.skn .
        /// </summary>
        /// <param name="path">Path to </param>
        /// <returns></returns>
        public List<RAFFileListEntry> SearchFileEntries(string searchPhrase)
        {
            RAFSearchType searchType = RAFSearchType.All;

            string lowerPhrase = searchPhrase.ToLower();
            List<RAFFileListEntry> result = new List<RAFFileListEntry>();

            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in this.fileDictFull)
            {
                string lowerFilename = entryKVP.Value.FileName.ToLower();
                if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                {
                    result.Add(entryKVP.Value);
                }
                else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                {
                    result.Add(entryKVP.Value);
                }
            }
            return result;
        }

        public struct RAFSearchResult
        {
            public int searchPhraseIndex;
            public RAFFileListEntry value;
        }

        /// <summary>
        /// Finds file entries.
        /// 
        /// SearchType.All returns any entries whose filepath contains the search string.
        /// Ie: /ahri/ would return /DATA/Characters/Ahri/Ahri.skn .
        /// SearchType.End returns any entries whose filepath ends with the search string.
        /// Ie: /ezreal_tx_cm.dds would return /DATA/Characters/Ezreal/Ezreal_TX_CM.dds
        /// </summary>
        public List<RAFFileListEntry> SearchFileEntries(String searchPhrase, RAFSearchType searchType)
        {
            string lowerPhrase = searchPhrase.ToLower();
            List<RAFFileListEntry> results = new List<RAFFileListEntry>();

            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in this.fileDictFull)
            {
                String lowerFilename = entryKVP.Value.FileName.ToLower();
                if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                {
                    results.Add(entryKVP.Value);
                }
                else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                {
                    results.Add(entryKVP.Value);
                }
            }
            return results;
        }

        /// <summary>
        /// Overloaded for simultaneous multiple searches.
        /// Returns a struct with the found RAFFileListEntry and the index of the search phrase that triggered it.
        /// </summary>
        public List<RAFSearchResult> SearchFileEntries(String[] searchPhrases, RAFSearchType searchType)
        {
            List<RAFSearchResult> results = new List<RAFSearchResult>();

            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in this.fileDictFull)
            {
                string lowerFilename = entryKVP.Value.FileName.ToLower();
                for (int i = 0; i < searchPhrases.Length; i++)
                {
                    String lowerPhrase = searchPhrases[i].ToLower();
                    if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                    {
                        RAFSearchResult result;
                        result.searchPhraseIndex = i;
                        result.value = entryKVP.Value;
                        results.Add(result);
                        break;
                    }
                    else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                    {
                        RAFSearchResult result;
                        result.searchPhraseIndex = i;
                        result.value = entryKVP.Value;
                        results.Add(result);
                        break;
                    }
                }
            }
            return results;
        }

        #endregion // FileDict functions

        #region RAF Editing

        /// <summary>
        /// Insert a file into the .dat and updates memory of the new location. 
        /// You HAVE to rebuild the .raf file after you finish all the inserts. 
        /// If you are inserting lots of files, supply a FileStream to the .dat file to increase performance. 
        /// Just remember to close the stream after all the inserts.
        /// </summary>
        public bool InsertFile(string fileName, byte[] content, bool createNewIfNoExist = false)
        {
            // Open the .dat file
            FileStream datFileStream = new FileStream(this.rafPath + ".dat", FileMode.Open);

            bool returnVal = insertFileHelperFunc(fileName, content, datFileStream, createNewIfNoExist);

            // Close the steam since we're done with it
            datFileStream.Close();

            return returnVal;
        }

        /// <summary>
        /// Insert a file into the .dat and updates memory of the new location. 
        /// You HAVE to rebuild the .raf file after you finish all the inserts. 
        /// If you are inserting many files, supply a FileStream to the .dat file to increase performance. 
        /// Just remember to close the stream after all the inserts.
        /// </summary>
        public bool InsertFile(string fileName, byte[] content, FileStream datFileStream, bool createNewIfNoExist = false)
        {
            return insertFileHelperFunc(fileName, content, datFileStream, createNewIfNoExist);
        }

        private bool insertFileHelperFunc(string fileName, byte[] content, FileStream datFileStream, bool createNewIfNoExist = false)
        {
            RAFFileListEntry fileEntry = this.GetFileEntry(fileName);
            // File exists in archive
            if (fileEntry != null)
            {
                return fileEntry.ReplaceContent(content, datFileStream);
            }
            // Create a new file if specified
            else if (createNewIfNoExist)
            {
                // Create a virtual RAFFileEntry using dummy offsets and filesize
                fileEntry = CreateFileEntry(fileName, 0, 0);
                return fileEntry.ReplaceContent(content, datFileStream);
            }

            return false;
            
        }

        private RAFFileListEntry CreateFileEntry(string rafPath, UInt32 offset, UInt32 fileSize)
        {
            RAFFileListEntry result = new RAFFileListEntry(this, rafPath, offset, fileSize);
            this.fileDictFull.Add(result.FileName, result);
            FileInfo fi = new FileInfo(result.FileName);
            if (!this.fileDictShort.ContainsKey(fi.Name))
                this.fileDictShort.Add(fi.Name, new List<RAFFileListEntry> { result });
            else
                this.fileDictShort[fi.Name].Add(result);
            return result;
        }

        public void SaveRAFFile()
        {
            //Calls to bitconverter were avoided until the end... just to make code prettier
            int dictLength = this.fileDictFull.Count;

            List<UInt32> result = new List<UInt32>();
            //Header
            result.Add(magic);
            result.Add(version);

            //Table of Contents
            result.Add(mgrIndex);
            result.Add(5 * 4);  //Offset of file list
            result.Add(
                (UInt32)(
                       5 * 4 + 4 + /*file list offset and entry itself*/
                       4 * 4 * dictLength /* Size of all entries total */
                )//Offset to string table
            );

            //File List Header
            result.Add((UInt32)dictLength); //F

            {   //File List Entries
                UInt32 i = 0;
                foreach (KeyValuePair<String, RAFFileListEntry> entry in this.fileDictFull)
                {
                    result.Add(entry.Value.StringNameHash);
                    result.Add(entry.Value.FileOffset);
                    result.Add(entry.Value.FileSize);
                    result.Add(i++);
                }
            }


            //String table Header.
            int stringTableHeader_SizeOffset = result.Count; //We will store this value later...
            result.Add(1337); //This value will be changed later to reflect the size of the string table
            result.Add((UInt32)dictLength);  //# strings in table

            //UInt32[] offsets = new UInt32[fileListEntries.Count]; //Stores offsets for entries

            //Set currentOffset to point to where our strings will be stored
            UInt32 currentOffset = 4 * 2 /*StringTableHeader Size*/ + (UInt32)(4 * 2 * dictLength);

            List<byte> stringTableContent = new List<byte>();
            //Insert entry, add filename to our string name bytes
            foreach (KeyValuePair<String, RAFFileListEntry> entry in this.fileDictFull)
            {
                result.Add(currentOffset); //offset to this string
                result.Add((UInt32)entry.Value.FileName.Length + 1);
                currentOffset += (UInt32)entry.Value.FileName.Length + 1;
                stringTableContent.AddRange(Encoding.ASCII.GetBytes(entry.Value.FileName));
                stringTableContent.Add(0);
            }

            //Update string table header with size of all data
            result[stringTableHeader_SizeOffset] = currentOffset;

            byte[] resultOutput = new byte[result.Count * 4 + stringTableContent.Count];
            for (int i = 0; i < result.Count; i++)
            {
                Array.Copy(
                    BitConverter.GetBytes(result[i]), 0, resultOutput, i * 4, 4
                );
            }
            Array.Copy(stringTableContent.ToArray(), 0, resultOutput, result.Count * 4, stringTableContent.Count);
            File.WriteAllBytes(this.rafPath, resultOutput);
        }

        #endregion // RAF Editing


    }
}
