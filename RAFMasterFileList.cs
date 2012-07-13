using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RAFlibPlus
{
    public class RAFMasterFileList
    {
        private Dictionary<String, RAFFileListEntry> fileDictFull = new Dictionary<String, RAFFileListEntry>();
        private Dictionary<String, List<RAFFileListEntry>> fileDictShort = new Dictionary<String, List<RAFFileListEntry>>();

        /// <summary>
        /// Supply the path to RADS\projects\lol_game_client\filearchives
        /// </summary>
        public RAFMasterFileList(String fileArchivePath)
        {
            List<String> rafFilePaths = getRAFFiles(fileArchivePath);

            foreach (String path in rafFilePaths)
            {
                RAFArchive raf = new RAFArchive(path);

                fileDictFull = combineFileDicts(fileDictFull, raf.FileDictFull);
                fileDictShort = combineFileDicts(fileDictShort, raf.FileDictShort);
            }
        }

        /// <summary>
        /// Supply an array whose values are the paths to each RAF file you want to be combined together
        /// </summary>
        public RAFMasterFileList(String[] rafFilePaths)
        {
            foreach (String path in rafFilePaths)
            {
                RAFArchive raf = new RAFArchive(path);

                fileDictFull = combineFileDicts(fileDictFull, raf.FileDictFull);
                fileDictShort = combineFileDicts(fileDictShort, raf.FileDictShort);
            }
        }

        #region Accessors

        public RAFFileListEntry GetFileEntry(string fullPath)
        {
            string lowerPath = fullPath.ToLower();
            if (this.fileDictFull.ContainsKey(fullPath))
                return fileDictFull[fullPath];
            else
                return null;
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

        #endregion // Accessors

        #region Searching

        public enum RAFSearchType
        {
            All,
            End
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

        #endregion // Searching

        #region Helper functions

        // Searches each folder inside the base directory for .raf files, ignoring any sub-directories
        private List<String> getRAFFiles(String baseDir)
        {
            String[] folders = Directory.GetDirectories(baseDir);

            List<String> returnFiles = new List<String>();

            foreach (String folder in folders)
            {
                returnFiles.AddRange(Directory.GetFiles(folder, "*.raf", SearchOption.TopDirectoryOnly));
            }
            return returnFiles;
        }

        private Dictionary<String, RAFFileListEntry> combineFileDicts(Dictionary<String, RAFFileListEntry> Dict1, Dictionary<String, RAFFileListEntry> Dict2)
        {
            foreach (KeyValuePair<String, RAFFileListEntry> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, entryKVP.Value);
                else
                {
                    if (Convert.ToInt32(entryKVP.Value.RAFArchive.GetID().Replace(".", "")) > Convert.ToInt32(Dict1[entryKVP.Key].RAFArchive.GetID().Replace(".", "")))
                        Dict1[entryKVP.Key] = entryKVP.Value;
                }
            }
            return Dict1;
        }

        private Dictionary<String, List<RAFFileListEntry>> combineFileDicts(Dictionary<String, List<RAFFileListEntry>> Dict1, Dictionary<String, List<RAFFileListEntry>> Dict2)
        {
            foreach (KeyValuePair<String, List<RAFFileListEntry>> entryKVP in Dict2)
            {
                if (!Dict1.ContainsKey(entryKVP.Key))
                    Dict1.Add(entryKVP.Key, entryKVP.Value);
                else
                {
                    for (int i = 0; i < entryKVP.Value.Count; i++)
                    {
                        Boolean conflict = false;
                        for (int j = 0; j < Dict1[entryKVP.Key].Count; j++)
                        {
                            if (entryKVP.Value[i].FileName == Dict1[entryKVP.Key][j].FileName)
                            {
                                conflict = true;
                                if (Convert.ToInt32(entryKVP.Value[i].RAFArchive.GetID().Replace(".", "")) > Convert.ToInt32(Dict1[entryKVP.Key][j].RAFArchive.GetID().Replace(".", "")))
                                {
                                    Dict1[entryKVP.Key][j] = entryKVP.Value[i];
                                }
                            }
                        }
                        if (!conflict)
                            Dict1[entryKVP.Key].Add(entryKVP.Value[i]);
                    }
                }
            }
            return Dict1;
        }

        #endregion // Helper functions
    }
}
