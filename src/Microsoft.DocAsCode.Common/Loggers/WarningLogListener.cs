// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DocAsCode.Common
{
    using System;
    using System.IO;

    public sealed class WarningLogListener : ILoggerListener
    {
        private readonly StreamWriter _writer;

        private const LogLevel LogLevelThreshold = LogLevel.Warning;

#if !NetCore
        public WarningLogListener(string reportPath)
        {
            var dir = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            _writer = new StreamWriter(reportPath, true);
        }
#endif

        public WarningLogListener(StreamWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }
            _writer = writer;
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public void WriteLine(ILogItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            if (item.LogLevel != LogLevelThreshold) return;
            _writer.WriteLine(JsonUtility.Serialize(item));
        }
    }
}
