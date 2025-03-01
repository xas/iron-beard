using McMaster.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace IronBeard.Cli.Features.Commands
{
    [Command(Description = "Watch a directory for changes and rebuild automatically")]
    public class WatchCommand : GenerateCommand
    {
        private CommandLineApplication _app;
        private FileSystemWatcher _watcher;

        /// <summary>
        /// Main execution method for the Watch command. It starts a FileWatcher
        /// on the provided input directory. If anything changes, it fires off the
        /// `generate` command to rebuild
        /// </summary>
        /// <param name="app">App context</param>
        /// <returns>Status code</returns>
        public new async Task<int> OnExecuteAsync(CommandLineApplication app)
        {
            _app = app;
            
            // normalize input path
            var inputPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, InputDirectory));

            // build up FileWatcher and bind events
            _watcher = new FileSystemWatcher(inputPath);
            _watcher.Renamed += async (s, e) => await Renamed(s, e);
            _watcher.Deleted += async (s, e) => await Changed(s, e);
            _watcher.Changed += async (s, e) => await Changed(s, e);
            _watcher.Created += async (s, e) => await Changed(s, e);
            _watcher.IncludeSubdirectories = true;
            _watcher.Filter = "";

            // run the initial generate command
            await RunGenerate();
            
            // keep running always until user closes
            while (true) {
                Thread.Sleep(10);
            };
        }

        /// <summary>
        /// If any file or directory is renamed, this will handle that event
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        /// <returns>Task</returns>
        private async Task Renamed(object sender, RenamedEventArgs e) {
            Console.WriteLine(DateTime.Now + ": " + e.ChangeType + " " + e.FullPath);
            await RunGenerate();
        }

        /// <summary>
        /// If the file or directory is changed in any way (deleted, contents changed, created, etc.) 
        /// this will handle that event.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event args</param>
        /// <returns>Task</returns>
        private async Task Changed(object sender, FileSystemEventArgs e) {
            Console.WriteLine(DateTime.Now + ": " + e.ChangeType + " " + e.FullPath);
            await RunGenerate();
        }

        /// <summary>
        /// Responsible for running the `generate` command and passing in the inital arguments
        /// It disables the FileWatcher events during the command execution to avoid triggering the 
        /// change events as the output and temp folders are created.
        /// </summary>
        /// <returns>Task</returns>
        private async Task RunGenerate(){
            _watcher.EnableRaisingEvents = false;
            await base.OnExecuteAsync(_app);
            _watcher.EnableRaisingEvents = true;
            Console.WriteLine("Watching...");
        }
    }
}