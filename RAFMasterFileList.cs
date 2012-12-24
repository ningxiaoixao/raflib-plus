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
using System.Linq;
using System.Text;
using System.IO;

namespace RAFlibPlus
{
    /// <summary>
    /// Allows the easy manipulation of RAF archives. With this class the user can pretend there is only one giant RAF archive
    /// </summary>
    public class RAFMasterFileList
    {
        private Dictionary<String, List<RAFFileListEntry>> fileDictFull = new Dictionary<String, List<RAFFileListEntry>>();
        private Dictionary<String, List<RAFFileListEntry>> fileDictShort = new Dictionary<String, List<RAFFileListEntry>>();

        /// <summary>
        /// Allows the easy manipulation of RAF archives. With this class the user can pretend there is only one giant RAF archive
        /// </summary>
        /// <param name="fileArchivePath">The path to RADS\projects\lol_game_client\filearchives</param>
        public RAFMasterFileList(String fileArchivePath)
        {
            List<String> rafFilePaths = getRAFFiles(fileArchivePath);

            foreach (String path in rafFilePaths)
            {
                RAFArchive raf = new RAFArchive(path);

                fileDictFull = CombineFileDicts(fileDictFull, raf.FileDictFull);
                fileDictShort = CombineFileDicts(fileDictShort, raf.FileDictShort);
            }
        }

        /// <summary>
        /// Allows the easy manipulation of RAF archives. With this class the user can pretend there is only one giant RAF archive
        /// </summary>
        /// <param name="rafFilePaths">An array whose values are the paths to each RAF file you want to be combined together</param>
        public RAFMasterFileList(String[] rafFilePaths)
        {
            foreach (String path in rafFilePaths)
            {
                RAFArchive raf = new RAFArchive(path);

                fileDictFull = CombineFileDicts(fileDictFull, raf.FileDictFull);
                fileDictShort = CombineFileDicts(fileDictShort, raf.FileDictShort);
            }
        }

        #region Accessors

        /// <summary>
        /// Looks up the path in the RAFFileListEntry dictionary. The path must be exact. Use SearchFileEntries for partial paths
        /// </summary>
        /// <param name="fullPath">Full RAFFileListEntry path, ie, DATA/Characters/Ahri/Ahri.skn (case insensitive)</param>
        /// <returns></returns>
        public List<RAFFileListEntry> GetFileEntry(string fullPath)
        {
            string lowerPath = fullPath.ToLower();
            List<RAFFileListEntry> returnValue;
            fileDictFull.TryGetValue(lowerPath, out returnValue);
            return returnValue;
        }

        /// <summary>
        /// Returns the file dictionary which uses the full-path (lower-cased) file names as keys, ie. "data/characters/ahri/ahri.skn"
        /// </summary>
        public Dictionary<String, List<RAFFileListEntry>> FileDictFull
        {
            get
            {
                return this.fileDictFull;
            }
        }

        /// <summary>
        /// Returns the file dictionary which uses the (lower-cased) file names as keys, ie. "ahri.skn". The values are List&lt;RAFFileListEntry&gt; to accomidate collisions
        /// </summary>
        public Dictionary<String, List<RAFFileListEntry>> FileDictShort
        {
            get
            {
                return this.fileDictShort;
            }
        }

        #endregion // Accessors

        #region Searching

        /// <summary>
        /// Specifies how to do a phrase search
        /// </summary>
        public enum RAFSearchType
        {
            /// <summary>
            /// Returns any entries whose filepath contains the search string, ie. "/ahri/" would return DATA/Characters/Ahri/Ahri.skn
            /// </summary>
            All,
            /// <summary>
            /// Returns any entries whose filepath ends with the search string, ie. "/ezreal_tx_cm.dds" would return DATA/Characters/Ezreal/Ezreal_TX_CM.dds
            /// </summary>
            End
        }

        /// <summary>
        /// Returns any entries whose filepath contains the search string, ie: ahri would return DATA/Characters/Ahri/Ahri.skn.
        /// </summary>
        /// <param name="searchPhrase">The phrase to look for</param>
        public List<RAFFileListEntry> SearchFileEntries(string searchPhrase)
        {
            RAFSearchType searchType = RAFSearchType.All;

            string lowerPhrase = searchPhrase.ToLower();
            List<RAFFileListEntry> result = new List<RAFFileListEntry>();

            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in this.fileDictFull)
            {
                foreach (RAFFileListEntry entry in entryKVP.Value)
                {
                    string lowerFilename = entry.FileName.ToLower();
                    if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                    {
                        result.Add(entry);
                    }
                    else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                    {
                        result.Add(entry);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns any entries whose filepath contains the search string. Use the RAFSearchType to specify how to search
        /// </summary>
        /// <param name="searchPhrase">The phrase to look for</param>
        /// <param name="searchType">SearchType.All returns any entries whose filepath contains the search string. SearchType.End returns any entries whose filepath ends with the search string.</param>
        /// <returns></returns>
        public List<RAFFileListEntry> SearchFileEntries(String searchPhrase, RAFSearchType searchType)
        {
            string lowerPhrase = searchPhrase.ToLower();
            List<RAFFileListEntry> results = new List<RAFFileListEntry>();

            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in this.fileDictFull)
            {
                foreach (RAFFileListEntry entry in entryKVP.Value)
                {
                    String lowerFilename = entry.FileName.ToLower();
                    if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                    {
                        results.Add(entry);
                    }
                    else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                    {
                        results.Add(entry);
                    }
                }
            }
            return results;
        }

        public struct RAFSearchResult
        {
            public String searchPhrase;
            public RAFFileListEntry value;
        }

        /// <summary>
        /// Simultaneously search for entries whose filepath contain a search phrase. Use the RAFSearchType to specify how to search
        /// </summary>
        /// <param name="searchPhrases">Array of phrases to look for</param>
        /// <param name="searchType">SearchType.All returns any entries whose filepath contains the search string. SearchType.End returns any entries whose filepath ends with the search string.</param>
        /// <returns>A struct with the found RAFFileListEntry and the search phrase that triggered it</returns>
        public List<RAFSearchResult> SearchFileEntries(String[] searchPhrases, RAFSearchType searchType)
        {
            List<RAFSearchResult> results = new List<RAFSearchResult>();

            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in this.fileDictFull)
            {
                foreach (RAFFileListEntry entry in entryKVP.Value)
                {
                    string lowerFilename = entry.FileName.ToLower();
                    foreach (String phrase in searchPhrases)
                    {
                        String lowerPhrase = phrase.ToLower();
                        if (searchType == RAFSearchType.All && lowerFilename.Contains(lowerPhrase))
                        {
                            RAFSearchResult result;
                            result.searchPhrase = phrase;
                            result.value = entry;
                            results.Add(result);
                            break;
                        }
                        else if (searchType == RAFSearchType.End && lowerFilename.EndsWith(lowerPhrase))
                        {
                            RAFSearchResult result;
                            result.searchPhrase = phrase;
                            result.value = entry;
                            results.Add(result);
                            break;
                        }
                    }
                }
            }
            return results;
        }

        #endregion // Searching

        #region Helper functions

        /// <summary>
        /// Searches each folder inside the base directory for .raf files, ignoring any sub-directories
        /// </summary>
        /// <param name="baseDir">The path to RADS\projects\lol_game_client\filearchives</param>
        /// <returns></returns>
        public List<String> getRAFFiles(String baseDir)
        {
            String[] folders = Directory.GetDirectories(baseDir);

            List<String> returnFiles = new List<String>();

            foreach (String folder in folders)
            {
                String[] files = Directory.GetFiles(folder, "*.raf", SearchOption.TopDirectoryOnly);
                if (files.Length > 1)
                    throw new System.InvalidOperationException("Multiple RAF files found within specific archive folder.\nPlease delete your " + baseDir + "folder and repair your client");
                returnFiles.AddRange(files);
            }
            return returnFiles;
        }

        // Add Full dictionary to MasterFileList dict
        private static Dictionary<String, List<RAFFileListEntry>> CombineFileDicts(Dictionary<String, List<RAFFileListEntry>> Dict1, Dictionary<String, RAFFileListEntry> Dict2)
        {
            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, new List<RAFFileListEntry> { entryKVP.Value });
                else
                {
                    Dict1[entryKVP.Key].Add(entryKVP.Value);
                }
            }
            return Dict1;
        }

        // Add Short dictionary to MasterFileList dict
        private static Dictionary<String, List<RAFFileListEntry>> CombineFileDicts(Dictionary<String, List<RAFFileListEntry>> Dict1, Dictionary<String, List<RAFFileListEntry>> Dict2)
        {
            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, new List<RAFFileListEntry>(entryKVP.Value));
                else
                {
                    foreach (RAFFileListEntry entry in Dict2[entryKVP.Key])
                    {
                        Dict1[entryKVP.Key].Add(entry);
                    }
                }
            }
            return Dict1;
        }

        #endregion // Helper functions
    }
}
