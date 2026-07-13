using System;
using System.Drawing;
using System.Management;
using System.IO; // 1. Nova biblioteca adicionada para leitura de discos
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
            this.Size = new Size(800, 450);
            this.StartPosition = FormStartPosition.CenterScreen;
            MontarInterfaceViaCodigo();
        }

        private void MontarInterfaceViaCodigo()
        {
            conteinerDivisor = new SplitContainer();
            conteinerDivisor.Dock = DockStyle.Fill;
            conteinerDivisor.SplitterDistance = 250;

            menuLateral = new TreeView();
            menuLateral.Dock = DockStyle.Fill;

            TreeNode noHardware = menuLateral.Nodes.Add("Hardware");
            noHardware.Nodes.Add("Memória RAM");
            noHardware.Nodes.Add("Armazenamento"); // 2. Novo item inserido no menu lateral
            menuLateral.ExpandAll();

            menuLateral.AfterSelect += MenuLateral_AfterSelect;

            listaDetalhes = new ListView();
            listaDetalhes.Dock = DockStyle.Fill;
            listaDetalhes.View = View.Details;
            listaDetalhes.FullRowSelect = true;

            listaDetalhes.Columns.Add("Propriedade", 200);
            listaDetalhes.Columns.Add("Valor", 400);

            conteinerDivisor.Panel1.Controls.Add(menuLateral);
            conteinerDivisor.Panel2.Controls.Add(listaDetalhes);
            this.Controls.Add(conteinerDivisor);
        }

        private void MenuLateral_AfterSelect(object sender, TreeViewEventArgs e)
        {
            listaDetalhes.Items.Clear();

            if (e.Node.Text == "Memória RAM")
            {
                CarregarDadosMemoriaRAM();
            }
            else if (e.Node.Text == "Armazenamento") // 3. Identificação do novo clique
            {
                CarregarDadosArmazenamento();
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

        // 4. Nova função estruturada para leitura dos discos
        private void CarregarDadosArmazenamento()
        {
            try
            {
                // A classe DriveInfo mapeia todos os volumes lógicos do sistema operacional
                DriveInfo[] discos = DriveInfo.GetDrives();

                foreach (DriveInfo disco in discos)
                {
                    // A propriedade IsReady previne travamentos ao tentar ler discos removíveis vazios
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

                        // Inserção de uma linha vazia para separar visualmente as unidades de armazenamento
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
    }
}