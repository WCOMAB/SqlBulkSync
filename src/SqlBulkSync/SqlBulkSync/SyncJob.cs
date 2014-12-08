// ----------------------------------------------------------------------------------------------
// Copyright (c) WCOM AB.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A 
// copy of the license can be found in the LICENSE.md file at the root of this distribution. 
// If you cannot locate the  Microsoft Public License, please send an email to 
// dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
//  by the terms of the Microsoft Public License.
// ----------------------------------------------------------------------------------------------
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------------------------
namespace WCOM.SqlBulkSync
{
    using Source.Common;
    using Source.HRON;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class SyncJob
    {
        public string SourceDbConnection { get; set; }
        public string TargetDbConnection { get; set; }
        public List<string> Tables { get; set; }
        public int? BatchSize { get; set; }
        public static bool TryLoad(string jobPath, out SyncJob syncJob)
        {
            syncJob = null;
            HRONObjectParseError[] errors;
            return (
                File.Exists(jobPath) &&
                HRONSerializer.TryParseObject(
                    0,
                    File.ReadAllText(jobPath, Encoding.UTF8).ReadLines(),
                    out syncJob,
                    out errors
                    )
                );
        }

        public void Save(string jobPath)
        {
            File.WriteAllText(
                jobPath,
                HRONSerializer.ObjectAsString(
                    this
                    ),
                Encoding.UTF8
                );
        }
    }
}