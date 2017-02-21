// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Build.Engine.Incrementals
{
    using System;

    using Newtonsoft.Json;

    public abstract class DependencyItemSourceInfo : IDependencyItemSourceInfo, IEquatable<DependencyItemSourceInfo>
    {
        [JsonIgnore]
        public abstract StringComparer ValueComparer { get; }

        [JsonProperty("sourceType")]
        public abstract DependencyItemSourceType SourceType { get; }

        [JsonProperty("value")]
        public string Value { get; set; }

        public DependencyItemSourceInfo ChangeSourceType(DependencyItemSourceType type)
        {
            return new DependencyItemSourceInfo { SourceType = type, Value = this.Value };
        }

        public DependencyItemSourceInfo ChangeValue(string value)
        {
            return new DependencyItemSourceInfo { SourceType = this.SourceType, Value = value };
        }

        public bool Equals(DependencyItemSourceInfo other)
        {
            if (other == null)
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return ValueComparer.Equals(Value, other.Value) &&
                SourceType == other.SourceType;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DependencyItemSourceInfo);
        }

        public override string ToString()
        {
            return $"SourceType: {SourceType}, Value: {Value}.";
        }

        public override int GetHashCode()
        {
            return ValueComparer.GetHashCode(Value) ^ (SourceType.GetHashCode() >> 1);
        }
    }

    public enum DependencyItemSourceType
    {
        File,
        Reference,
    }
}
