using System;
using System.Drawing;
using System.Management; // É obrigatório ter instalado este pacote via NuGet
using System.Windows.Forms;

namespace SystemInfoApp
{
    public partial class Form1 : Form
    {
        // Declaração dos componentes da interface na memória
        private SplitContainer conteinerDivisor;
        private TreeView menuLateral;
        private ListView listaDetalhes;

        public Form1()
        {
            // Configurações básicas da janela principal
            this.Text = "Painel de Informações do Sistema";
            this.Size = new Size(800, 450);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Chama a função que desenhará toda a tela
            MontarInterfaceViaCodigo();
        }

        private void MontarInterfaceViaCodigo()
        {
            // 1. Criação do Divisor da Tela (Painel esquerdo e direito)
            conteinerDivisor = new SplitContainer();
            conteinerDivisor.Dock = DockStyle.Fill;
            conteinerDivisor.SplitterDistance = 250;

            // 2. Criação do Menu Lateral (TreeView)
            menuLateral = new TreeView();
            menuLateral.Dock = DockStyle.Fill;

            // Adição estruturada dos itens no menu
            TreeNode noHardware = menuLateral.Nodes.Add("Hardware");
            noHardware.Nodes.Add("Memória RAM");
            menuLateral.ExpandAll(); // Mantém o menu aberto por padrão

            // Definição do que acontece quando o menu é clicado
            menuLateral.AfterSelect += MenuLateral_AfterSelect;

            // 3. Criação da Tabela de Informações (ListView)
            listaDetalhes = new ListView();
            listaDetalhes.Dock = DockStyle.Fill;
            listaDetalhes.View = View.Details; // Ativa o modo de linhas e colunas
            listaDetalhes.FullRowSelect = true;

            // Adição das colunas na tabela
            listaDetalhes.Columns.Add("Propriedade", 200);
            listaDetalhes.Columns.Add("Valor", 400);

            // 4. Montagem final: Inserção dos componentes dentro da janela
            conteinerDivisor.Panel1.Controls.Add(menuLateral);
            conteinerDivisor.Panel2.Controls.Add(listaDetalhes);
            this.Controls.Add(conteinerDivisor);
        }

        // Evento disparado pelo clique no menu lateral
        private void MenuLateral_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // A tabela é limpa antes de carregar novas informações
            listaDetalhes.Items.Clear();

            // Uma verificação lógica identifica qual item foi selecionado
            if (e.Node.Text == "Memória RAM")
            {
                CarregarDadosMemoriaRAM();
            }
        }

        // Função isolada que contém o código WMI para leitura da RAM
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

                // A informação é formatada e enviada como uma nova linha para a tabela visual
                ListViewItem linha = new ListViewItem("Capacidade Total");
                linha.SubItems.Add(memoriaEmGB + " GB");
                listaDetalhes.Items.Add(linha);
            }
            catch (Exception erro)
            {
                // Tratamento de falhas: impede o fechamento do sistema caso haja erro na leitura
                ListViewItem linhaErro = new ListViewItem("Erro de leitura");
                linhaErro.SubItems.Add(erro.Message);
                listaDetalhes.Items.Add(linhaErro);
            }
        }
    }
}