/* Copyright 2012 Remigius stalder, Descom Consulting Ltd.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace Hpdi.Vss2Git
{
    /// <summary>
    /// Wraps execution of Hg and implements the common Hg commands.
    /// </summary>
    /// <author>Remigius Stalder</author>
    class HgWrapper : AbstractVcsWrapper
    {
        public static readonly string hgMetaDir = ".hg";
        public static readonly string hgTagsFile = ".hgtags";
        public static readonly string hgExecutable = "hg";

        private Encoding commitEncoding = Encoding.UTF8;

        public Encoding CommitEncoding
        {
            get { return commitEncoding; }
            set { commitEncoding = value; }
        }

        public HgWrapper(string outputDirectory, Logger logger, Encoding commitEncoding)
            : base(outputDirectory, logger)
        {
            this.commitEncoding = commitEncoding;
        }

        public override string GetVcs()
        {
            return hgExecutable;
        }

        public override string GetMetaDir()
        {
            return hgMetaDir;
        }

        public override string[] GetCompareExcludes()
        {
            return new string[] { hgMetaDir, hgTagsFile };
        }

        public override void Init(bool resetRepo)
        {
            if (resetRepo)
            {
                DeleteDirectory(GetOutputDirectory(), true);
                Directory.CreateDirectory(GetOutputDirectory());
            }
            VcsExec("init");
        }

        public override void Configure()
        {
            CheckOutputDirectory();
        }

        public override bool Add(string path)
        {
            VcsExec("add " + QuoteRelativePath(path));
            SetNeedsCommit();
            return true;
        }

        public override bool AddDir(string path)
        {
            // do nothing - hg does not care about directories
            return true;
        }

        public override bool AddAll()
        {
            /*
            var startInfo = GetStartInfo("add -A");

            // add fails if there are no files (directories don't count)
            bool result = ExecuteUnless(startInfo, "did not match any files");
            if (result) SetNeedsCommit();
            return result;
             * */
            // do nothing - if not needed
            return true;
        }

        public override void RemoveFile(string path)
        {
            VcsExec("remove " + QuoteRelativePath(path));
            SetNeedsCommit();
        }

        public override void RemoveDir(string path, bool recursive)
        {
            VcsExec("remove -f " + QuoteRelativePath(path));
            SetNeedsCommit();
        }

        public override void RemoveEmptyDir(string path)
        {
            // do nothing - remove only on file system - hg doesn't care about directories with no files
        }

        public override void Move(string sourcePath, string destPath)
        {
            VcsExec("mv " + QuoteRelativePath(sourcePath) + " " + QuoteRelativePath(destPath));
            SetNeedsCommit();
        }

        public override void MoveEmptyDir(string sourcePath, string destPath)
        {
            // move only on file system - hg doesn't care about directories with no files
            Directory.Move(sourcePath, destPath);
        }

        public override bool DoCommit(string authorName, string authorEmail, string comment, DateTime localTime)
        {
            TempFile commentFile;

            var args = "commit -u " + Quote(authorName + " <" + authorEmail + ">")
                + " -d " + Quote(GetUtcTimeString(localTime));
            AddComment(comment, ref args, out commentFile);

            using (commentFile)
            {
                var startInfo = GetStartInfo(args);
                // ignore empty commits, since they are non-trivial to detect
                // (e.g. when renaming a directory)
                return ExecuteUnless(startInfo, "nothing changed");
            }
        }

        public override void Tag(string name, string taggerName, string taggerEmail, string comment, DateTime localTime)
        {
            // tag names are not quoted because they cannot contain whitespace or quotes
            // hg cannot read tag comments from temp files
            var args = "tag -u " + Quote(taggerName + " <" + taggerEmail + ">")
                + " -d " + Quote(GetUtcTimeString(localTime)) + " -m " + Quote(comment)
                + " " + name;

            VcsExec(args);
        }

        private void AddComment(string comment, ref string args, out TempFile tempFile)
        {
            tempFile = null;
            if (!string.IsNullOrEmpty(comment))
            {
                // need to use a temporary file to specify the comment when not
                // using the system default code page or it contains newlines
                if (commitEncoding.CodePage != Encoding.Default.CodePage || comment.IndexOf('\n') >= 0)
                {
                    Logger.WriteLine("Generating temp file for comment: {0}", comment);
                    tempFile = new TempFile();
                    tempFile.Write(comment, commitEncoding);

                    // temporary path might contain spaces (e.g. "Documents and Settings")
                    args += " -l " + Quote(tempFile.Name);
                }
                else
                {
                    args += " -m " + Quote(comment);
                }
            }
        }

        private static string GetUtcTimeString(DateTime localTime)
        {
            // convert local time to UTC based on whether DST was in effect at the time
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime);

            // format time according to ISO 8601 (avoiding locale-dependent month/day names)
            return utcTime.ToString("yyyy'-'MM'-'dd HH':'mm':'ss +0000");
        }
    }
}
