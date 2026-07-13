using System;
using System.Drawing;
using System.Management;
using System.IO;
using System.Windows.Forms;

namespace SystemInfoApp
{
    public partial class Form1 : Form
    {
        private SplitContainer conteinerDivisor;
        private TreeView menuLateral;
        private ListView listaDetalhes;

        public Form1()
        {
            this.Text = "Painel de Informações do Sistema";
            this.MinimumSize = new Size(700, 400);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            MontarInterfaceViaCodigo();
        }

        private void MontarInterfaceViaCodigo()
        {
            conteinerDivisor = new SplitContainer();
            conteinerDivisor.Dock = DockStyle.Fill;
            conteinerDivisor.SplitterDistance = 220;

            menuLateral = new TreeView();
            menuLateral.Dock = DockStyle.Fill;

            TreeNode noHardware = menuLateral.Nodes.Add("Hardware");
            // 1. Novo nó inserido na interface
            noHardware.Nodes.Add("Processador (CPU)");
            noHardware.Nodes.Add("Memória RAM");
            noHardware.Nodes.Add("Armazenamento");

            TreeNode noSoftware = menuLateral.Nodes.Add("Software");
            noSoftware.Nodes.Add("Sistema Operacional");

            menuLateral.ExpandAll();
            menuLateral.AfterSelect += MenuLateral_AfterSelect;

            listaDetalhes = new ListView();
            listaDetalhes.Dock = DockStyle.Fill;
            listaDetalhes.View = View.Details;
            listaDetalhes.FullRowSelect = true;
            listaDetalhes.GridLines = true;

            listaDetalhes.Columns.Clear();
            listaDetalhes.Columns.Add("Propriedade", 160);
            listaDetalhes.Columns.Add("Valor", 300);

            listaDetalhes.Resize += (sender, evento) => AjustarColunas();

            conteinerDivisor.Panel1.Controls.Add(menuLateral);
            conteinerDivisor.Panel2.Controls.Add(listaDetalhes);
            this.Controls.Add(conteinerDivisor);
        }

        private void MenuLateral_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listaDetalhes.Items.Clear();

            // 2. Novo mapeamento de clique adicionado
            if (e.Node.Text == "Processador (CPU)")
            {
                CarregarDadosProcessador();
            }
            else if (e.Node.Text == "Memória RAM")
            {
                CarregarDadosMemoriaRAM();
            }
            else if (e.Node.Text == "Armazenamento")
            {
                CarregarDadosArmazenamento();
            }
            else if (e.Node.Text == "Sistema Operacional")
            {
                CarregarDadosSistemaOperacional();
            }

            AjustarColunas();
        }

        private void AjustarColunas()
        {
            if (listaDetalhes.Columns.Count >= 2)
            {
                int larguraPainel = listaDetalhes.ClientSize.Width;
                int larguraPropriedade = 160;

                listaDetalhes.Columns[0].Width = larguraPropriedade;

                if (larguraPainel > larguraPropriedade)
                {
                    listaDetalhes.Columns[1].Width = larguraPainel - larguraPropriedade;
                }
            }
        }

        // 3. Nova função estruturada para leitura do Processador
        private void CarregarDadosProcessador()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
                ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta);

                foreach (ManagementObject item in buscador.Get())
                {
                    ListViewItem linhaNome = new ListViewItem("Modelo");
                    linhaNome.SubItems.Add(item["Name"]?.ToString());
                    listaDetalhes.Items.Add(linhaNome);

                    ListViewItem linhaFab = new ListViewItem("Fabricante");
                    linhaFab.SubItems.Add(item["Manufacturer"]?.ToString());
                    listaDetalhes.Items.Add(linhaFab);

                    ListViewItem linhaCores = new ListViewItem("Núcleos Físicos");
                    linhaCores.SubItems.Add(item["NumberOfCores"]?.ToString());
                    listaDetalhes.Items.Add(linhaCores);

                    ListViewItem linhaThreads = new ListViewItem("Processadores Lógicos");
                    linhaThreads.SubItems.Add(item["NumberOfLogicalProcessors"]?.ToString());
                    listaDetalhes.Items.Add(linhaThreads);

                    ListViewItem linhaClock = new ListViewItem("Frequência Máxima");
                    linhaClock.SubItems.Add(item["MaxClockSpeed"]?.ToString() + " MHz");
                    listaDetalhes.Items.Add(linhaClock);
                }
            }
            catch (Exception erro)
            {
                ListViewItem linhaErro = new ListViewItem("Erro de leitura do processador");
                linhaErro.SubItems.Add(erro.Message);
                listaDetalhes.Items.Add(linhaErro);
            }
        }

        private void CarregarDadosMemoriaRAM()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Capacity FROM Win32_PhysicalMemory");
                ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta);
                long memoriaTotalBytes = 0;

                foreach (ManagementObject item in buscador.Get())
                {
                    memoriaTotalBytes += Convert.ToInt64(item["Capacity"]);
                }

                long memoriaEmGB = memoriaTotalBytes / (1024 * 1024 * 1024);

                ListViewItem linha = new ListViewItem("Capacidade Total");
                linha.SubItems.Add(memoriaEmGB + " GB");
                listaDetalhes.Items.Add(linha);
            }
            catch (Exception erro)
            {
                ListViewItem linhaErro = new ListViewItem("Erro de leitura");
                linhaErro.SubItems.Add(erro.Message);
                listaDetalhes.Items.Add(linhaErro);
            }
        }

        private void CarregarDadosArmazenamento()
        {
            try
            {
                DriveInfo[] discos = DriveInfo.GetDrives();

                foreach (DriveInfo disco in discos)
                {
                    if (disco.IsReady)
                    {
                        long espacoTotalGB = disco.TotalSize / (1024 * 1024 * 1024);
                        long espacoLivreGB = disco.AvailableFreeSpace / (1024 * 1024 * 1024);

                        ListViewItem linhaNome = new ListViewItem("Unidade " + disco.Name);
                        linhaNome.SubItems.Add("Formato: " + disco.DriveFormat);
                        listaDetalhes.Items.Add(linhaNome);

                        ListViewItem linhaTotal = new ListViewItem("  Tamanho Total");
                        linhaTotal.SubItems.Add(espacoTotalGB + " GB");
                        listaDetalhes.Items.Add(linhaTotal);

                        ListViewItem linhaLivre = new ListViewItem("  Espaço Livre");
                        linhaLivre.SubItems.Add(espacoLivreGB + " GB");
                        listaDetalhes.Items.Add(linhaLivre);

                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }
            }
            catch (Exception erro)
            {
                ListViewItem linhaErro = new ListViewItem("Erro de leitura do disco");
                linhaErro.SubItems.Add(erro.Message);
                listaDetalhes.Items.Add(linhaErro);
            }
        }

        private void CarregarDadosSistemaOperacional()
        {
            try
            {
                ListViewItem linhaMaquina = new ListViewItem("Nome do Computador");
                linhaMaquina.SubItems.Add(Environment.MachineName);
                listaDetalhes.Items.Add(linhaMaquina);

                ListViewItem linhaUsuario = new ListViewItem("Usuário Atual");
                linhaUsuario.SubItems.Add(Environment.UserName);
                listaDetalhes.Items.Add(linhaUsuario);

                ListViewItem linhaVersao = new ListViewItem("Versão do Núcleo");
                linhaVersao.SubItems.Add(Environment.OSVersion.ToString());
                listaDetalhes.Items.Add(linhaVersao);

                ListViewItem linhaArquitetura = new ListViewItem("Arquitetura do Sistema");
                string arquitetura = Environment.Is64BitOperatingSystem ? "64 Bits" : "32 Bits";
                linhaArquitetura.SubItems.Add(arquitetura);
                listaDetalhes.Items.Add(linhaArquitetura);

                ListViewItem linhaDiretorio = new ListViewItem("Diretório do Sistema");
                linhaDiretorio.SubItems.Add(Environment.SystemDirectory);
                listaDetalhes.Items.Add(linhaDiretorio);
            }
            catch (Exception erro)
            {
                ListViewItem linhaErro = new ListViewItem("Erro de leitura do sistema");
                linhaErro.SubItems.Add(erro.Message);
                listaDetalhes.Items.Add(linhaErro);
            }
        }
    }
}