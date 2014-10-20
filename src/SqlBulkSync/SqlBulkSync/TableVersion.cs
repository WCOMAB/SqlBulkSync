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
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
namespace WCOM.SqlBulkSync
{
    using System;

    public class TableVersion
    {
        public string TableName { get; set; }
        public long CurrentVersion { get; set; }
        public long MinValidVersion { get; set; }
        public DateTime QueriedDateTime { get; set; }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TableVersion) obj);
        }

        
        protected bool Equals(TableVersion other)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(TableName, other.TableName) &&
                   CurrentVersion == other.CurrentVersion &&
                   MinValidVersion == other.MinValidVersion;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                
                var hashCode = StringComparer.OrdinalIgnoreCase.GetHashCode(TableName);
                hashCode = (hashCode*397) ^ CurrentVersion.GetHashCode();
                hashCode = (hashCode*397) ^ MinValidVersion.GetHashCode();
                return hashCode;
            }
        }
    }
}