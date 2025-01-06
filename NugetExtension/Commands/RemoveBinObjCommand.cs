using System;
using System.IO;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;

namespace NugetExtension.Commands
{
    internal sealed class RemoveBinObjCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("bea4976d-811a-4dd0-9745-0bed7a658b5d");

        private readonly Package package;

        private RemoveBinObjCommand(Package package)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            OleMenuCommandService commandService =
                ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.MenuItemCallback, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static RemoveBinObjCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        /// <summary>
        /// Inicializa a instância do comando.
        /// </summary>
        public static void Initialize(Package package)
        {
            Instance = new RemoveBinObjCommand(package);
        }

        /// <summary>
        /// Método chamado quando o usuário seleciona a opção do menu.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Podemos exibir um MessageBox perguntando se o usuário deseja realmente fazer a limpeza.
            // Vou omitir esse passo, mas recomendo fortemente que haja uma confirmação prévia.

            // Obtemos a instância do DTE (para acessar informações da solution).
            if (!(ServiceProvider.GetService(typeof(DTE)) is DTE dte) || string.IsNullOrEmpty(dte.Solution.FullName))
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    "Não foi possível obter a instância do DTE ou não há uma solução aberta.",
                    "Erro",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }

            // Caminho raiz da solução
            string solutionDir = Path.GetDirectoryName(dte.Solution.FullName);

            // Remove as pastas bin/obj
            try
            {
                RemoveBinObjDirectories(solutionDir);

                // Mensagem de sucesso ao término do processo
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    "Limpeza de pastas bin e obj concluída com sucesso!",
                    "Limpeza Concluída",
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                VsShellUtilities.ShowMessageBox(
                    this.ServiceProvider,
                    $"Ocorreu um erro durante a remoção das pastas: {ex.Message}",
                    "Erro ao limpar pastas",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        /// <summary>
        /// Método para remover recursivamente todas as pastas bin e obj.
        /// </summary>
        /// <param name="rootPath">Caminho raiz (pasta da solution)</param>
        private void RemoveBinObjDirectories(string rootPath)
        {
            // Busca recursiva por diretórios "bin" e "obj"
            foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
            {
                // Verifica se o nome da pasta é bin ou obj (sem case sensitivity).
                var dirName = Path.GetFileName(directory);
                if (dirName.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    dirName.Equals("obj", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Directory.Delete(directory, true); // true = recursivo
                    }
                    catch (IOException ioEx)
                    {
                        // Lidar com exceções de IO (ex.: arquivo em uso)
                        // Dependendo do caso, podemos tentar forçar o unlock, ou apenas ignorar.
                        System.Diagnostics.Debug.WriteLine(
                            $"Não foi possível excluir o diretório {directory}: {ioEx.Message}");
                    }
                    catch (UnauthorizedAccessException uaEx)
                    {
                        // Lidar com exceções de permissão
                        System.Diagnostics.Debug.WriteLine(
                            $"Acesso não autorizado ao excluir o diretório {directory}: {uaEx.Message}");
                    }
                }
            }
        }
    }
}
