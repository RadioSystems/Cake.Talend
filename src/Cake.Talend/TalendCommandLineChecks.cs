﻿
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using System.Linq;

namespace Cake.Talend {
    internal class TalendCommandLineChecks : System.IDisposable {
        private readonly string _originalFileText;
        private readonly FilePath _workspaceProperties;
        private readonly FilePath _metadataLogFile;

        public TalendCommandLineChecks(DirectoryPath workspaceDirectory, string projectName, ICakeLog log) {
            _log = log;
            _workspaceProperties = workspaceDirectory.CombineWithFilePath(".metadata/.plugins/org.eclipse.m2e.core/workspacestate.properties");

            if (System.IO.File.Exists(_workspaceProperties.FullPath)) {
                _originalFileText = System.IO.File.ReadAllText(_workspaceProperties.FullPath);
            }

            WriteWorkspaceStateFile(workspaceDirectory, projectName);

            _metadataLogFile = workspaceDirectory.CombineWithFilePath(".metadata/.log");
            if (System.IO.File.Exists(_metadataLogFile.FullPath)) {
                System.IO.File.Delete(_metadataLogFile.FullPath);
            }
        }

        private void WriteWorkspaceStateFile(DirectoryPath workspaceDirectory, string projectName) {
            var projectPOM = workspaceDirectory.CombineWithFilePath($"{projectName}/pom.xml");
            var javaPOM = workspaceDirectory.CombineWithFilePath(".Java/pom.xml");

            var talendWorkspaceFiles = new string[] {
                $"YOUR_GROUP\\:{projectName.Trim().ToUpper()}\\:pom\\:\\:SNAPSHOT=" + projectPOM.ToString().Replace(@"/", @"\\").Replace(":", @"\:"),
                $"org.talend.master.{projectName.Trim().ToUpper()}\\:code.Master\\:pom\\:\\:6.4.1=" + javaPOM.ToString().Replace(@"/", @"\\").Replace(":", @"\:")
            };

            System.IO.File.WriteAllLines(_workspaceProperties.FullPath, talendWorkspaceFiles);
        }

        private void CheckLogFileAndThrowIfError() {
            try {
                var logFileLines = System.IO.File.ReadAllLines(_metadataLogFile.FullPath);

                if (logFileLines.Any(x => x.StartsWith("!MESSAGE FAILED"))) {
                    foreach (var line in logFileLines) {
                        _log.Error(line);
                    }

                    throw new System.Exception("Talend Failure");

                } else {

                    foreach (var line in logFileLines) {
                        _log.Information(line);
                    }
                }
            } catch (System.IO.FileNotFoundException e) {
                throw new System.Exception($"Can't find Talend Log File at path \"{_metadataLogFile.FullPath}\". Did it succeed? {e}", e);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private readonly ICakeLog _log;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                // Restore original text. just in case.
                if (disposing) {
                    try {
                        System.IO.File.WriteAllText(_workspaceProperties.FullPath, _originalFileText);
#pragma warning disable RCS1075 // Avoid empty catch clause that catches System.Exception.
                    } catch (System.Exception) {
#pragma warning restore RCS1075 // Avoid empty catch clause that catches System.Exception.
                    }

                    CheckLogFileAndThrowIfError();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TalendCommandLineChecks() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void System.IDisposable.Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
