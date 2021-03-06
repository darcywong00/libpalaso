﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Palaso.Extensions;
using Palaso.IO;
using Palaso.Linq;
using Palaso.Text;

namespace Palaso.UI.WindowsForms.ImageGallery
{
	public class ArtOfReadingImageCollection :IImageCollection
	{
		private Dictionary<string, List<string>> _wordToPartialPathIndex;
		private Dictionary<string, string> _partialPathToWordsIndex;

		public ArtOfReadingImageCollection()
		{
		   _wordToPartialPathIndex = new Dictionary<string, List<string>>();
		   _partialPathToWordsIndex = new Dictionary<string, string>();
		}

		public string RootImagePath { get; set; }

		public void LoadIndex(string indexFilePath)
		{
			using (var f = File.OpenText(indexFilePath))
			{
				while (!f.EndOfStream)
				{
					var line = f.ReadLine();
					var parts = line.Split(new char[] { '\t' });
					Debug.Assert(parts.Length == 2);
					if (parts.Length != 2)
						continue;
					var partialPath = parts[0];
					var keyString = parts[1].Trim(new char[] { ' ', '"' });//some have quotes, some don't
					_partialPathToWordsIndex.Add(partialPath, keyString);
					var keys = keyString.SplitTrimmed(',');
					foreach (var key in keys)
					{
						_wordToPartialPathIndex.GetOrCreate(key).Add(partialPath);
					}
				}
			}
		}

		public IEnumerable<object> GetMatchingPictures(string keywords, out bool foundExactMatches)
		{
			keywords = GetCleanedUpSearchString(keywords.ToLowerInvariant());
			return GetMatchingPictures(keywords.SplitTrimmed(' '), out foundExactMatches);
		}

		public string GetCleanedUpSearchString(string keywords)
		{
			keywords = keywords.Replace(",", string.Empty);
			keywords = keywords.Replace(";", string.Empty);
			keywords = keywords.Replace("-", string.Empty);
			keywords = keywords.Replace(".", string.Empty);
			keywords = keywords.Replace("(", string.Empty);
			return  keywords.Replace(")", string.Empty);
		}

		public Image GetThumbNail(object imageToken)
		{
			throw new System.NotImplementedException();
		}

		public CaptionMethodDelegate CaptionMethod
		{
			get { return GetCaption; }
		}

		private string GetCaption(string path)
		{
			try
			{
				var partialPath = path.Replace(RootImagePath, "");
				partialPath = partialPath.Replace(Path.DirectorySeparatorChar, ':');
				partialPath = partialPath.Trim(new char[] {':'});
				return _partialPathToWordsIndex[partialPath];
			}
			catch (Exception)
			{
				return "error";
			}
		}


		public IEnumerable<string> GetPathsFromResults(IEnumerable<object> results, bool limitToThoseActuallyAvailable)
		{
			foreach (var macPath in results)
			{
				var path = Path.Combine(RootImagePath, ((string) macPath).Replace(':', Path.DirectorySeparatorChar));
				if (!limitToThoseActuallyAvailable)
				{
					yield return path;
					continue; // don't look further
				}

				if (File.Exists(path))
				{
					yield return path;
					continue; // don't look further
				}

				// These results may be from an older index that has the original file names, and were tifs.
				// The re-republished versions (which have embedd3ed metadata and watermarks) start with AOR_ and end with png
				var updatedPath =
					Path.GetDirectoryName(path).CombineForPath("AOR_" + Path.GetFileName(path).Replace(".tif", ".png"));

				if (File.Exists(updatedPath))
				{
					yield return updatedPath;
					continue; // don't look further
				}

				// In version 3, some country's files (Mexico only?) were split into two subdirectories.
				// Instead of republishing the index and making everybody reinstall AOR,
				// we'll see if we can find the file in a subdirectory.
				var parentDir = Path.GetDirectoryName(path);
				var fileName = Path.GetFileName(path);
				var subDirs = Directory.EnumerateDirectories(parentDir);
				foreach (var subDir in subDirs)
				{
					updatedPath = Path.Combine(parentDir, subDir, fileName);
					if (File.Exists(updatedPath))
					{
						yield return updatedPath;
						continue;
					}
				}
			}
		}

		private IEnumerable<object> GetMatchingPictures(IEnumerable<string> keywords, out bool foundExactMatches)
		{
			var pictures = new List<string>();
			foundExactMatches = false;
			foreach (var term in keywords)
			{
				List<string> picturesForThisKey;

				//first, try for exact matches
				if (_wordToPartialPathIndex.TryGetValue(term, out picturesForThisKey))
				{
					pictures.AddRange(picturesForThisKey);
					foundExactMatches = true;
				}
				//then look  for approximate matches
				else
				{
					foundExactMatches = false;
					var kMaxEditDistance = 1;
					var itemFormExtractor = new ApproximateMatcher.GetStringDelegate<KeyValuePair<string, List<string>>>(pair => pair.Key);
					var matches = ApproximateMatcher.FindClosestForms<KeyValuePair<string, List<string>>>(_wordToPartialPathIndex, itemFormExtractor,
																										  term,
																										  ApproximateMatcherOptions.None,
																										  kMaxEditDistance);

					if (matches != null && matches.Count > 0)
					{
						foreach (var keyValuePair in matches)
						{
							pictures.AddRange(keyValuePair.Value);
						}
					}
				}

			}
			var results = new List<object>();
			pictures.Distinct().ForEach(p => results.Add(p));
			return results;
		}


		public string StripNonMatchingKeywords(string stringWhichMayContainKeywords)
		{
			string result = string.Empty;
			stringWhichMayContainKeywords = GetCleanedUpSearchString(stringWhichMayContainKeywords);
			var words = stringWhichMayContainKeywords.SplitTrimmed(' ');
			foreach (var key in words)
			{
				if (_wordToPartialPathIndex.ContainsKey(key))
					result += " " + key;
			}
			return result.Trim();
		}

		public static bool IsAvailable()
		{
			string imagesPath = TryToGetRootImageCatalogPath();
			return !string.IsNullOrEmpty(imagesPath) && !string.IsNullOrEmpty(TryToGetPathToIndex(imagesPath));
		}

		public static string TryToGetRootImageCatalogPath()
		{
			//look for the cd/dvd
/* retire this            var cdPath = TryToGetPathToCollectionOnCd();
			if (!string.IsNullOrEmpty(cdPath))
				return cdPath;
*/
			var distributedWithApp = FileLocator.GetDirectoryDistributedWithApplication(true,"Art Of Reading", "images");
			if(!string.IsNullOrEmpty(distributedWithApp) && Directory.Exists(distributedWithApp))
				return distributedWithApp;

			//look for it in a hard-coded location
			if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				var unixPaths = new[]
				{
					@"/usr/share/ArtOfReading/images",		// new package location (standard LSB/FHS location)
					@"/usr/share/SIL/ArtOfReading/images",	// old (lost) package location
					@"/var/share/ArtOfReading/images"		// obsolete test location (?)
				};

				foreach (var path in unixPaths)
				{
					if (Directory.Exists(path))
						return path;
				}
			}
			else
			{
				//look for the folder created by the ArtOfReadingFree installer
				var aorInstallerTarget = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).CombineForPath("SIL", "Art Of Reading", "images");

				//the rest of these are for before we had an installer for AOR
				var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).CombineForPath("Art Of Reading", "images");
				var appDataNoSpace = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData).CombineForPath("ArtOfReading", "images");
				var winPaths = new[] { aorInstallerTarget,  @"c:\art of reading\images", @"c:/ArtOfReading/images", appData, appDataNoSpace };

				foreach (var path in winPaths)
				{
					if (Directory.Exists(path))
						return path;
				}
			}

			return null;
		}

		private static string TryToGetPathToCollectionOnCd()
		{
			//look for CD
			foreach (var drive in DriveInfo.GetDrives())
			{
				try
				{
					if(drive.IsReady && drive.VolumeLabel.Contains("Art Of Reading"))
						return Path.Combine(drive.RootDirectory.FullName, "images");
				}
				catch (Exception)
				{
				}
			}
			return null;
		}

		public static bool DoNotFindArtOfReading_Test = false;

		public static IImageCollection FromStandardLocations()
		{
			string path = TryToGetRootImageCatalogPath();
			if (DoNotFindArtOfReading_Test)
				path = null;

			if (path == null)
				return null;

			var c = new ArtOfReadingImageCollection();

			c.RootImagePath = path;

			string pathToIndexFile = TryToGetPathToIndex(path);
			c.LoadIndex(pathToIndexFile);
			return c;
		}

		public static string TryToGetPathToIndex(string imagesPath)
		{
			// ArtOfReading3FreeSetp.exe installs index.txt in the parent directory 
			// of images.  The previous verison of this code looked in images directory
			// as well.
			if (!string.IsNullOrEmpty(imagesPath))
			{
				var path = Directory.GetParent(imagesPath).FullName.CombineForPath("index.txt");
				if (File.Exists(path))
					return path;

				path = imagesPath.CombineForPath("index.txt");
				if (File.Exists(path))
					return path;
			}

			return FileLocator.GetFileDistributedWithApplication(true, "ArtOfReadingIndexV3_en.txt");
		}
	}
}
