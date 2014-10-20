// ----------------------------------------------------------------------------------------------
// Copyright (c) WCOM AB.
// ----------------------------------------------------------------------------------------------
// This source code is subject to terms and conditions of the Microsoft Public License. A 
// copy of the license can be found in the License.html file at the root of this distribution. 
// If you cannot locate the  Microsoft Public License, please send an email to 
// dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
//  by the terms of the Microsoft Public License.
// ----------------------------------------------------------------------------------------------
// You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------------------------
// ReSharper disable UnusedMember.Global
// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global
namespace WCOM.SqlBulkSync
{
    public class Column
    {
        public string Name { get; set; }
        public string QuoteName { get; set; }
        public string Type { get; set; }
        public bool IsIdentity { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsNullable { get; set; }
    }
}