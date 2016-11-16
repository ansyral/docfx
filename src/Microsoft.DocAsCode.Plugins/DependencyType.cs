// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Plugins
{
    using System;

    public class DependencyType
    {
        /// <summary>
        /// name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// whether the dependency type is transitive
        /// </summary>
        public bool IsTransitive { get; set; }

        [Obsolete]
        public bool TriggerBuild { get; set; }

        /// <summary>
        /// the build phase that the dependency type could have an effect on
        /// </summary>
        public BuildPhase Phase { get; set; }
    }
}
