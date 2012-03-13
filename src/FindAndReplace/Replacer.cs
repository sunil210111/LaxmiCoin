﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FindAndReplace
{
	public class ReplacerEventArgs : EventArgs
	{
		public Replacer.ReplaceResultItem ResultItem { get; set; }
		public Stats Stats { get; set; }
		public Status Status { get; set; }

		public ReplacerEventArgs(Replacer.ReplaceResultItem resultItem, Stats stats, Status status)
		{
			ResultItem = resultItem;
			Stats = stats;
			Status = status;
		}
	}

	public delegate void ReplaceFileProcessedEventHandler(object sender, ReplacerEventArgs e);
	
	public class Replacer
	{
		public string Dir { get; set; }
		public string FileMask { get; set; }
		public bool IncludeSubDirectories { get; set; }
		public string FindText { get; set; }
		public string ReplaceText { get; set; }
		public bool IsCaseSensitive { get; set; }
		public bool FindTextHasRegEx { get; set; }
		public string ExcludeFileMask { get; set; }
		public bool IsCancelRequested { get; set; }
		
		public class ReplaceResultItem 
		{
			public string FileName { get; set; }
			public string FilePath { get; set; }
			public string FileRelativePath { get; set; }
			public int NumMatches { get; set; }
			public MatchCollection Matches { get; set; }
			public bool IsSuccess { get; set; }
			public bool IsBinaryFile { get; set; }
			public bool FailedToOpen { get; set; }
			public bool FailedToWrite { get; set; }
			public string ErrorMessage { get; set; }
			public List<MatchPreviewLineNumber> LineNumbers { get; set; }

			public bool IncludeInResultsList
			{
				get
				{
					if (IsSuccess && NumMatches > 0)
						return true;

					if (!IsSuccess && !String.IsNullOrEmpty(ErrorMessage))
						return true;

					return false;
				}
			}
		}

		public class ReplaceResult
		{
			public List<ReplaceResultItem> ResultItems { get; set; }

			public Stats Stats { get; set; }
		}

		public ReplaceResult Replace()
		{
			Verify.Argument.IsNotEmpty(Dir, "Dir");
			Verify.Argument.IsNotEmpty(FileMask, "FileMask");
			Verify.Argument.IsNotEmpty(FindText, "FindText");
			Verify.Argument.IsNotNull(ReplaceText, "ReplaceText");

			Status replacerStatus = Status.Processing;

			var startTime = DateTime.Now;
			string[] filesInDirectory = Utils.GetFilesInDirectory(Dir, FileMask, IncludeSubDirectories, ExcludeFileMask);

			var resultItems = new List<ReplaceResultItem>();
			var stats = new Stats();
			stats.Files.Total = filesInDirectory.Length;

			var startTimeProcessingFiles = DateTime.Now;
			
			foreach (string filePath in filesInDirectory)
			{
				var resultItem = ReplaceTextInFile(filePath);
				stats.Files.Processed++;
				stats.Matches.Found += resultItem.NumMatches;

				if (resultItem.IsSuccess)
				{
					if (resultItem.NumMatches > 0)
					{
						stats.Files.WithMatches++;
						stats.Matches.Replaced += resultItem.NumMatches;
					}
					else
					{
						stats.Files.WithoutMatches++;
					}
				}
				else
				{
					if (resultItem.FailedToOpen)
						stats.Files.FailedToRead++;
		
					if (resultItem.IsBinaryFile)
						stats.Files.Binary++;

					if (resultItem.FailedToWrite)
						stats.Files.FailedToWrite++;
				}
				
				if (resultItem.IncludeInResultsList)
					resultItems.Add(resultItem);

				stats.UpdateTime(startTime, startTimeProcessingFiles);

				if (IsCancelRequested) replacerStatus = Status.Cancelled;
				
				OnFileProcessed(new ReplacerEventArgs(resultItem, stats, replacerStatus));

				if (IsCancelRequested) break;
			}

			replacerStatus = Status.Completed;
			
			if (filesInDirectory.Length == 0) 
				OnFileProcessed(new ReplacerEventArgs(new ReplaceResultItem(), stats, replacerStatus));

			return new ReplaceResult() {ResultItems = resultItems, Stats = stats};
		}

		void CheckIfBinary(string filePath, ref ReplaceResultItem resultItem)
		{
			string shortContent = string.Empty;

			//Check if can read first
			try
			{
				var buffer = new char[1024];

				using (var sr = new StreamReader(filePath))
				{
					int k = sr.Read(buffer, 0, 1024);

					shortContent = new string(buffer, 0, k);
				}
			}
			catch (Exception exception)
			{
				resultItem.IsSuccess = false;
				resultItem.FailedToOpen = true;
				resultItem.ErrorMessage = exception.Message;
			}

			//Load 1KB or 10KB of data and check for /0/0/0/0
			if (Utils.IsBinaryFile(shortContent))
			{
				resultItem.IsSuccess = false;
				resultItem.IsBinaryFile = true;
			}
		}

		public void CancelReplace()
		{
			IsCancelRequested = true;
		}
		
		private ReplaceResultItem ReplaceTextInFile(string filePath)
		{
			string fileContent = string.Empty;

			var resultItem = new ReplaceResultItem();
			resultItem.IsSuccess = true;
			resultItem.FileName = Path.GetFileName(filePath);
			resultItem.FilePath = filePath;
			resultItem.FileRelativePath = "." + filePath.Substring(Dir.Length);

			CheckIfBinary(filePath, ref resultItem);
			
			if (!resultItem.IsSuccess) return resultItem;
			
			using (StreamReader sr = new StreamReader(filePath))
			{
				fileContent = sr.ReadToEnd();
			}

			RegexOptions regexOptions = Utils.GetRegExOptions(IsCaseSensitive);

			var finderText = FindTextHasRegEx ? FindText : Regex.Escape(FindText);
			MatchCollection matches;

			if (!FindTextHasRegEx)
			{
				matches= Regex.Matches(fileContent, Regex.Escape(FindText), Utils.GetRegExOptions(IsCaseSensitive));
			}
			else
			{
				matches = Regex.Matches(fileContent, finderText, regexOptions);
			}

			
			resultItem.NumMatches = matches.Count;
			resultItem.Matches = matches;
		
			if (matches.Count > 0)
			{
				try
				{
					string newContent = Regex.Replace(fileContent, finderText, ReplaceText, regexOptions);

					using (var sw = new StreamWriter(filePath))
					{
						sw.Write(newContent);
					}

					resultItem.LineNumbers = Utils.GetLineNumbersForMatchesPreview(filePath, matches);
				}
				catch (Exception ex)
				{
					resultItem.IsSuccess = false;
					resultItem.FailedToWrite = true;
					resultItem.ErrorMessage = ex.Message;
				}
			}

			return resultItem;
		}

		public event ReplaceFileProcessedEventHandler FileProcessed;

		protected virtual void OnFileProcessed(ReplacerEventArgs e)
		{
			if (FileProcessed != null)
				FileProcessed(this, e);
		}
	}
}
