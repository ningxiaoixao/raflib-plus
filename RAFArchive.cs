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
 * This class is a modification of the original 
 * RAFlib generously created and provided by ItzWarty
 * and found here http://code.google.com/p/raf-manager/source/browse/#svn%2FProjects%2FRAFLib
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using ItzWarty;

namespace RAFlibPlus
{
    /// <summary>
    /// A class that allows the easy manipulation of RAF archives
    /// </summary>
    public class RAFArchive
    {
        // Path to the archive
        private string m_rafPath;

        private RAFArchiveID m_id;

        // Magic value used to identify the file type, must be 0x18BE0EF0
        private UInt32 m_magic;

        // Version of the archive format, must be 1
        private UInt32 m_version;

        // An index of the filetype. Don't modify this
        private UInt32 m_mgrIndex;

        // Byte array to hold the contents of the .raf file
        private byte[] m_content;

        // Dictionary with the full path of the RAF entry as the key
        private Dictionary<String, RAFFileListEntry> m_fileDictFull;

        // Dictionary with just the file name as the key
        private Dictionary<String, List<RAFFileListEntry>> m_fileDictShort;

        /// <summary>
        /// A class that allows the easy manipulation of RAF archives
        /// </summary>
        /// <param name="rafPath">Path to the .raf file</param>
        public RAFArchive(string rafPath)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            m_rafPath = rafPath;
            FileInfo fi = new FileInfo(rafPath);
            m_id = RAFArchiveID.CreateID(fi.Directory.Name);

            m_content = File.ReadAllBytes(rafPath);
            m_magic = BitConverter.ToUInt32(m_content.SubArray(0, 4), 0);
            m_version = BitConverter.ToUInt32(m_content.SubArray(4, 4), 0);
            m_mgrIndex = BitConverter.ToUInt32(m_content.SubArray(8, 4), 0);

            // Offset to the table of contents from the start of the file
            UInt32 offsetFileList = BitConverter.ToUInt32(m_content.SubArray(12, 4), 0);
            // Offset to the string table from the start of the file
            UInt32 offsetStringTable = BitConverter.ToUInt32(m_content.SubArray(16, 4), 0);

            //UINT32 is casted to INT32.  This should be fine, since i doubt that the RAF will become
            //a size of 2^31-1 in bytes.

            m_fileDictFull = new Dictionary<String, RAFFileListEntry>();
            m_fileDictShort = new Dictionary<String, List<RAFFileListEntry>>();
            CreateFileDicts(this, offsetFileList, offsetStringTable);
        }

        /// <summary>
        /// Returns the ID of an archive. It is the taken from name of the folder that holds the .raf and .dat files, ie. 0.0.0.25
        /// </summary>
        /// <returns>ID of the archive</returns>
        public RAFArchiveID ID
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// Returns the local path to the .raf file, ie. C:\Archive_114252416.raf
        /// </summary>
        public string RAFFilePath
        {
            get
            {
                return m_rafPath;
            }
        }

        #region FileDict functions

        private void CreateFileDicts(RAFArchive raf, UInt32 offsetFileList, UInt32 offsetStringTable)
        {
            //The file list starts with a uint stating how many files we have
            UInt32 fileListCount = BitConverter.ToUInt32(m_content.SubArray((Int32)offsetFileList, 4), 0);

            //After the file list count, we have the actual data.
            offsetFileList += 4;

            for (UInt32 currentOffset = offsetFileList; currentOffset < offsetFileList + 16 * fileListCount; currentOffset += 16)
            {
                RAFFileListEntry entry = new RAFFileListEntry(raf, ref raf.m_content, currentOffset, offsetStringTable);
                raf.m_fileDictFull.Add(entry.FileName.ToLower(), entry);

                FileInfo fi = new FileInfo(entry.FileName);
                if (!raf.m_fileDictShort.ContainsKey(fi.Name.ToLower()))
                    raf.m_fileDictShort.Add(fi.Name.ToLower(), new List<RAFFileListEntry> { entry });
                else
                    raf.m_fileDictShort[fi.Name.ToLower()].Add(entry);
            }
        }

        /// <summary>
        /// Looks up the path in the RAFFileListEntry dictionary. The path must be exact. Use SearchFileEntries for partial paths
        /// </summary>
        /// <param name="fullPath">Full RAFFileListEntry path, ie, DATA/Characters/Ahri/Ahri.skn (case insensitive)</param>
        /// <returns></returns>
        public RAFFileListEntry GetFileEntry(string fullPath)
        {
            string lowerPath = fullPath.ToLower();
            if (m_fileDictFull.ContainsKey(lowerPath))
                return m_fileDictFull[lowerPath];

            return null;
        }

        /// <summary>
        /// Returns the file dictionary which uses the full-path, (lower-cased) file names as keys, ie. "data/characters/ahri/ahri.skn"
        /// </summary>
        public Dictionary<String, RAFFileListEntry> FileDictFull
        {
            get
            {
                return m_fileDictFull;
            }
        }

        /// <summary>
        /// Returns the file dictionary which uses the (lower-cased) file names as keys, ie. "ahri.skn". The values are List&lt;RAFFileListEntry&gt; to accomidate collisions
        /// </summary>
        public Dictionary<String, List<RAFFileListEntry>> FileDictShort
        {
            get
            {
                return m_fileDictShort;
            }
        }

        /// <summary>
        /// Returns any entries whose filepath contains the search string. Use the RAFSearchType to specify how to search
        /// </summary>
        /// <param name="searchPhrase">The phrase to look for</param>
        /// <param name="searchType">SearchType.All returns any entries whose filepath contains the search string. SearchType.End returns any entries whose filepath ends with the search string.</param>
        /// <returns></returns>
        public List<RAFFileListEntry> SearchFileEntries(String searchPhrase, RAFSearchType searchType = RAFSearchType.All)
        {
            string lowerPhrase = searchPhrase.ToLower();
            List<RAFFileListEntry> results = new List<RAFFileListEntry>();

            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in m_fileDictFull)
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
        /// Simultaneously search for entries whose filepath contain a search phrase. Use the RAFSearchType to specify how to search
        /// </summary>
        /// <param name="searchPhrases">Array of phrases to look for</param>
        /// <param name="searchType">SearchType.All returns any entries whose filepath contains the search string. SearchType.End returns any entries whose filepath ends with the search string.</param>
        /// <returns>A struct with the found RAFFileListEntry and the search phrase that triggered it</returns>
        public List<RAFSearchResult> SearchFileEntries(String[] searchPhrases, RAFSearchType searchType = RAFSearchType.All)
        {
            List<RAFSearchResult> results = new List<RAFSearchResult>();

            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in m_fileDictFull)
            {
                string lowerFilename = entryKVP.Value.FileName.ToLower();
                foreach(String phrase in searchPhrases)
                {
                    String lowerPhrase = phrase.ToLower();
                    if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                    {
                        RAFSearchResult result;
                        result.SearchPhrase = phrase;
                        result.Value = entryKVP.Value;
                        results.Add(result);
                        break;
                    }

                    if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                    {
                        RAFSearchResult result;
                        result.SearchPhrase = phrase;
                        result.Value = entryKVP.Value;
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
        /// Replace the content of the RAFFileListEntry and update memory of this new data.
        /// You HAVE to call &lt;RAFArchive&gt;.SaveRAFFile() after you finish all the inserts.
        /// </summary>
        /// <param name="fileName">Full path of the RAFFileListEntry, ie. DATA/Characters/Ahri/Ahri.skn</param>
        /// <param name="content">Content to overwrite the previous file data</param>
        /// <param name="createNewIfNoExist">Should a new RAFFileListEntry be created if a RAFFileListEntry can't be found with the given fileName</param>
        /// <returns></returns>
        public bool InsertFile(string fileName, byte[] content, bool createNewIfNoExist = false)
        {
            // Open the .dat file
            FileStream datFileStream = new FileStream(m_rafPath + ".dat", FileMode.Open);

            bool returnVal = InsertFileHelperFunc(fileName, content, datFileStream, createNewIfNoExist);

            // Close the steam since we're done with it
            datFileStream.Close();

            return returnVal;
        }

        /// <summary>
        /// Replace the content of the RAFFileListEntry and update memory of this new data.
        /// You HAVE to call &lt;RAFArchive&gt;.SaveRAFFile() after you finish all the inserts.
        /// </summary>
        /// <param name="fileName">Full path of the RAFFileListEntry, ie. DATA/Characters/Ahri/Ahri.skn</param>
        /// <param name="content">Content to overwrite the previous file data</param>
        /// <param name="datFileStream">FileStream to the RAF .dat file</param>
        /// <param name="createNewIfNoExist">Should a new RAFFileListEntry be created if a RAFFileListEntry can't be found with the given fileName</param>
        /// <returns></returns>
        public bool InsertFile(string fileName, byte[] content, FileStream datFileStream, bool createNewIfNoExist = false)
        {
            return InsertFileHelperFunc(fileName, content, datFileStream, createNewIfNoExist);
        }

        private bool InsertFileHelperFunc(string fileName, byte[] content, FileStream datFileStream, bool createNewIfNoExist = false)
        {
            RAFFileListEntry fileEntry = GetFileEntry(fileName);
            // File exists in archive
            if (fileEntry != null)
            {
                return fileEntry.ReplaceContent(content, datFileStream);
            }
            
			// Create a new file if specified
	        if (createNewIfNoExist)
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
            m_fileDictFull.Add(result.FileName, result);
            FileInfo fi = new FileInfo(result.FileName);
            if (!m_fileDictShort.ContainsKey(fi.Name))
                m_fileDictShort.Add(fi.Name, new List<RAFFileListEntry> { result });
            else
                m_fileDictShort[fi.Name].Add(result);
            return result;
        }

        /// <summary>
        /// Rebuilds the .dat file. This is neccessary after any file inserting. 
        /// </summary>
        public void SaveRAFFile()
        {
            //Calls to bitconverter were avoided until the end... just to make code prettier
            int dictLength = m_fileDictFull.Count;

            List<UInt32> result = new List<UInt32>();
            //Header
            result.Add(m_magic);
            result.Add(m_version);

            //Table of Contents
            result.Add(m_mgrIndex);
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
                foreach (KeyValuePair<String, RAFFileListEntry> entry in m_fileDictFull)
                {
                    result.Add(entry.Value.StringNameHash);
                    result.Add(entry.Value.FileOffset);
                    result.Add(entry.Value.FileSize);
                    result.Add(i++);
                }
            }


            //String table Header.
            int stringTableHeaderSizeOffset = result.Count; //We will store this value later...
            result.Add(1337); //This value will be changed later to reflect the size of the string table
            result.Add((UInt32)dictLength);  //# strings in table

            //UInt32[] offsets = new UInt32[fileListEntries.Count]; //Stores offsets for entries

            //Set currentOffset to point to where our strings will be stored
            UInt32 currentOffset = 4 * 2 /*StringTableHeader Size*/ + (UInt32)(4 * 2 * dictLength);

            List<byte> stringTableContent = new List<byte>();
            //Insert entry, add filename to our string name bytes
            foreach (KeyValuePair<String, RAFFileListEntry> entry in m_fileDictFull)
            {
                result.Add(currentOffset); //offset to this string
                result.Add((UInt32)entry.Value.FileName.Length + 1);
                currentOffset += (UInt32)entry.Value.FileName.Length + 1;
                stringTableContent.AddRange(Encoding.ASCII.GetBytes(entry.Value.FileName));
                stringTableContent.Add(0);
            }

            //Update string table header with size of all data
            result[stringTableHeaderSizeOffset] = currentOffset;

            byte[] resultOutput = new byte[result.Count * 4 + stringTableContent.Count];
            for (int i = 0; i < result.Count; i++)
            {
                Array.Copy(
                    BitConverter.GetBytes(result[i]), 0, resultOutput, i * 4, 4
                );
            }
            Array.Copy(stringTableContent.ToArray(), 0, resultOutput, result.Count * 4, stringTableContent.Count);
            File.WriteAllBytes(m_rafPath, resultOutput);
        }

        #endregion // RAF Editing


    }

    
}
