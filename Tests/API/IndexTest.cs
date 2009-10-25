﻿/*
 * Copyright (C) 2009, nulltoken <emeric.fermas@gmail.com>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using GitSharp.Tests;
using System.IO;

namespace Git.Tests
{
    [TestFixture]
    public class IndexTest : ApiTestCase
    {
        [Test]
        public void IndexAdd()
        {
            var workingDirectory = Path.Combine(trash.FullName, "test");
            var repo = Repository.Init(workingDirectory);
            var index_path = Path.Combine(repo.Directory, "index");
            var old_index = Path.Combine(repo.Directory, "old_index");
            var index = repo.Index;
            index.Write(); // write empty index
            new FileInfo(index_path).CopyTo(old_index);
            string filepath = Path.Combine(workingDirectory, "for henon.txt");
            File.WriteAllText(filepath, "Weißbier");
            repo.Index.Add(filepath);
            // now verify
            Assert.IsTrue(new FileInfo(index_path).Exists);
            var new_index = new Repository(repo.Directory).Index;
            Assert.AreNotEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));

            // make another addition
            var index_1 = Path.Combine(repo.Directory, "index_1");
            new FileInfo(index_path).CopyTo(index_1);
            string filepath1 = Path.Combine(workingDirectory, "for nulltoken.txt");
            File.WriteAllText(filepath1, "Rotwein");
            index = new Index(repo);
            index.Add(filepath1);
            Assert.AreNotEqual(File.ReadAllBytes(index_1), File.ReadAllBytes(index_path));
            Assert.DoesNotThrow(() => repo.Index.Read());

            var status = repo.Status;
            Assert.IsTrue(status.Added.Contains("for henon.txt"));
            Assert.IsTrue(status.Added.Contains("for nulltoken.txt"));
            Assert.AreEqual(2, status.Added.Count);
            Assert.AreEqual(0, status.Changed.Count);
            Assert.AreEqual(0, status.Missing.Count);
            Assert.AreEqual(0, status.Modified.Count);
            Assert.AreEqual(0, status.Removed.Count);
        }

        [Test]
        public void Read_write_empty_index()
        {
            var repo = GetTrashRepository();
            var index_path = Path.Combine(repo.Directory, "index");
            var old_index = Path.Combine(repo.Directory, "old_index");
            var index = repo.Index;
            index.Write(); // write empty index
            Assert.IsTrue(new FileInfo(index_path).Exists);
            new FileInfo(index_path).MoveTo(old_index);
            Assert.IsFalse(new FileInfo(index_path).Exists);
            var new_index = new Repository(repo.Directory).Index;
            new_index.Write(); // see if the read index is rewritten identitcally
            Assert.IsTrue(new FileInfo(index_path).Exists);
            Assert.AreEqual(File.ReadAllBytes(old_index), File.ReadAllBytes(index_path));
        }

        [Test]
        public void Change_special_msysgit_index()
        {
            var repo = GetTrashRepository();
            var index_path = Path.Combine(repo.Directory, "index");
            new FileInfo("Resources/index_originating_from_msysgit").CopyTo(index_path);
            var a = writeTrashFile("a.txt", "Data:a");
            // msysgit status should work here
            repo.Index.Add(a.FullName);
            // msysgit status doesn't work any more
        }

        [Test]
        public void Diff_special_msysgit_index()
        {
            var repo = GetTrashRepository();
            var index_path = Path.Combine(repo.Directory, "index");
            new FileInfo("Resources/index_originating_from_msysgit").CopyTo(index_path);

            // [henon] this is output from msysgit for "git status" with that index in an empty repository
            //# Changes to be committed:
            //#   (use "git reset HEAD <file>..." to unstage)
            //#
            //#       new file:   New Folder/New Ruby Program.rb
            //#       deleted:    a/a1
            //#       deleted:    a/a1.txt
            //#       deleted:    a/a2.txt
            //#       deleted:    b/b1.txt
            //#       deleted:    b/b2.txt
            //#       deleted:    c/c1.txt
            //#       deleted:    c/c2.txt
            //#       new file:   for henon.txt
            //#       deleted:    master.txt
            //#       new file:   test.cmd
            //#
            //# Changed but not updated:
            //#   (use "git add/rm <file>..." to update what will be committed)
            //#   (use "git checkout -- <file>..." to discard changes in working directory)
            //#
            //#       deleted:    New Folder/New Ruby Program.rb
            //#       deleted:    for henon.txt
            //#       deleted:    test.cmd
            //#

            var status = repo.Status;
            var added = new[] {            
                "New Folder/New Ruby Program.rb",
                "for henon.txt",
                "test.cmd", 
            }.Select(s => Path.Combine(repo.WorkingDirectory, s)).ToArray();
            var deleted = new[] {
                  "a/a1", "a/a1.txt", "a/a2.txt", "b/b1.txt", "b/b2.txt", "c/c1.txt", "c/c2.txt", "master.txt" 
            }.Select(s => Path.Combine(repo.WorkingDirectory, s)).ToArray();


            AssertContains(status.Added, added);
            AssertContains(status.Removed, deleted);
            Assert.AreEqual(0, status.Changed.Count);
            AssertContains(status.Missing, added);
            Assert.AreEqual(0, status.Modified.Count);

            //var a = writeTrashFile("a.txt", "Data:a");
            // msysgit status should work here
            //repo.Index.Add(a.FullName);
            // msysgit status doesn't work any more
        }

        private void AssertContains<T>(HashSet<T> set, params T[] items)
        {
            foreach (var item in items)
                Assert.IsTrue(set.Contains(item), "set does not contain item:" + item.ToString());
        }
    }
}
