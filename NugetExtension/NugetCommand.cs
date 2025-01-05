using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NugetExtension
{
    internal sealed class NugetCommand
    {
        // Valores devem bater com o .vsct
        public const int MyCommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("AA17F731-AA76-4D7C-8F73-8C6A4100399E");

        private readonly AsyncPackage _package;

        private NugetCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            var cmdId = new CommandID(CommandSet, MyCommandId);

            // Adiciona o evento de clique 
            var menuItem = new OleMenuCommand(Execute, cmdId);
            commandService?.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            // Precisamos da Main Thread para lidar com menus
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            if (!(await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService))
                return;

            // Cria a instância do comando
            _ = new NugetCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Exemplo de ação: exibir um MessageBox
            VsShellUtilities.ShowMessageBox(
                _package,
                "Comando executado com sucesso!",
                "MyExtension",
                Microsoft.VisualStudio.Shell.Interop.OLEMSGICON.OLEMSGICON_INFO,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGBUTTON.OLEMSGBUTTON_OK,
                Microsoft.VisualStudio.Shell.Interop.OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
