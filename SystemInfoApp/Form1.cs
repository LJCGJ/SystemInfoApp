using System;
using System.Drawing;
using System.Management;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Linq;

namespace SystemInfoApp
{
    public partial class Form1 : Form
    {
        private SplitContainer conteinerDivisor;
        private TreeView menuLateral;
        private ListView listaDetalhes;
        private Button botaoSalvar;
        private System.Windows.Forms.Timer temporizadorMonitoramento;

        public Form1()
        {
            this.Text = "Painel de Informações do Sistema";
            this.MinimumSize = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.WindowState = FormWindowState.Maximized;
            MontarInterfaceViaCodigo();
        }

        private void MontarInterfaceViaCodigo()
        {
            conteinerDivisor = new SplitContainer();
            conteinerDivisor.Dock = DockStyle.Fill;

            botaoSalvar = new Button();
            botaoSalvar.Text = "Exportar Relatório (.txt)";
            botaoSalvar.Height = 40;
            botaoSalvar.Dock = DockStyle.Bottom;
            botaoSalvar.Click += BotaoSalvar_Click;

            menuLateral = new TreeView();
            menuLateral.Dock = DockStyle.Fill;

            TreeNode noSensores = menuLateral.Nodes.Add("Sensores em Tempo Real");
            noSensores.Nodes.Add("Monitoramento de CPU e RAM");

            TreeNode noHardware = menuLateral.Nodes.Add("Hardware");
            noHardware.Nodes.Add("Processador (CPU)");
            noHardware.Nodes.Add("Placa de Vídeo (GPU)");
            noHardware.Nodes.Add("Memória RAM");
            noHardware.Nodes.Add("Armazenamento");
            noHardware.Nodes.Add("Bateria e Energia");

            TreeNode noSoftware = menuLateral.Nodes.Add("Software");
            noSoftware.Nodes.Add("Sistema Operacional");
            noSoftware.Nodes.Add("Processos em Execução");

            TreeNode noRede = menuLateral.Nodes.Add("Rede");
            noRede.Nodes.Add("Adaptadores de Conexão");

            menuLateral.ExpandAll();
            menuLateral.AfterSelect += async (sender, e) => await MenuLateral_AfterSelectAsync(e);

            listaDetalhes = new ListView();
            listaDetalhes.Dock = DockStyle.Fill;
            listaDetalhes.View = View.Details;
            listaDetalhes.FullRowSelect = true;
            listaDetalhes.GridLines = true;

            listaDetalhes.Columns.Clear();
            listaDetalhes.Columns.Add("Propriedade", 350);
            listaDetalhes.Columns.Add("Valor", 450);

            listaDetalhes.Resize += (sender, evento) => AjustarColunas();

            conteinerDivisor.Panel1.Controls.Add(botaoSalvar);
            conteinerDivisor.Panel1.Controls.Add(menuLateral);

            conteinerDivisor.Panel2.Controls.Add(listaDetalhes);
            this.Controls.Add(conteinerDivisor);

            temporizadorMonitoramento = new System.Windows.Forms.Timer();
            temporizadorMonitoramento.Interval = 1000;
            temporizadorMonitoramento.Tick += async (sender, e) => await CarregarDadosTempoRealAsync();

            this.Load += (sender, evento) =>
            {
                conteinerDivisor.SplitterDistance = this.ClientSize.Width / 3;
            };
        }

        private void BotaoSalvar_Click(object sender, EventArgs e)
        {
            if (listaDetalhes.Items.Count == 0) return;

            SaveFileDialog janelaSalvar = new SaveFileDialog();
            janelaSalvar.Filter = "Arquivo de Texto (*.txt)|*.txt";
            janelaSalvar.Title = "Salvar Relatório do Sistema";
            janelaSalvar.FileName = "Relatorio_Sistema.txt";

            if (janelaSalvar.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    using (StreamWriter escritor = new StreamWriter(janelaSalvar.FileName))
                    {
                        escritor.WriteLine("--- RELATÓRIO DO SISTEMA ---");
                        escritor.WriteLine("Data: " + DateTime.Now.ToString());
                        escritor.WriteLine("----------------------------\n");

                        foreach (ListViewItem item in listaDetalhes.Items)
                        {
                            string propriedade = item.Text;
                            string valor = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";
                            escritor.WriteLine(propriedade.PadRight(45) + ": " + valor);
                        }
                    }
                    MessageBox.Show("Relatório salvo com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception erro) { }
            }
        }

        private async Task MenuLateral_AfterSelectAsync(TreeViewEventArgs e)
        {
            temporizadorMonitoramento.Stop();

            if (e.Node.Text != "Monitoramento de CPU e RAM")
            {
                listaDetalhes.Items.Clear();
            }

            if (e.Node.Text == "Monitoramento de CPU e RAM")
            {
                listaDetalhes.Items.Clear();
                await CarregarDadosTempoRealAsync();
                temporizadorMonitoramento.Start();
            }
            else if (e.Node.Text == "Processador (CPU)") CarregarDadosProcessador();
            else if (e.Node.Text == "Placa de Vídeo (GPU)") CarregarDadosPlacaDeVideo();
            else if (e.Node.Text == "Memória RAM") CarregarDadosMemoriaRAM();
            else if (e.Node.Text == "Armazenamento") CarregarDadosArmazenamento();
            else if (e.Node.Text == "Bateria e Energia") CarregarDadosBateria();
            else if (e.Node.Text == "Sistema Operacional") CarregarDadosSistemaOperacional();
            else if (e.Node.Text == "Processos em Execução") CarregarDadosProcessos();
            else if (e.Node.Text == "Adaptadores de Conexão") CarregarDadosRede();

            AjustarColunas();
        }

        private void AjustarColunas()
        {
            if (listaDetalhes.Columns.Count >= 2)
            {
                int larguraPainel = listaDetalhes.ClientSize.Width;
                int larguraPropriedade = 350;
                listaDetalhes.Columns[0].Width = larguraPropriedade;
                if (larguraPainel > larguraPropriedade)
                {
                    listaDetalhes.Columns[1].Width = larguraPainel - larguraPropriedade;
                }
            }
        }

        private async Task CarregarDadosTempoRealAsync()
        {
            try
            {
                string porcentagemCpu = "0%";
                string statusRam = "0 GB / 0 GB (0%)";

                await Task.Run(() =>
                {
                    ObjectQuery consultaCpu = new ObjectQuery("SELECT LoadPercentage FROM Win32_Processor");
                    using (ManagementObjectSearcher buscadorCpu = new ManagementObjectSearcher(consultaCpu))
                    {
                        foreach (ManagementObject item in buscadorCpu.Get())
                        {
                            porcentagemCpu = item["LoadPercentage"]?.ToString() + "%";
                        }
                    }

                    ObjectQuery consultaRam = new ObjectQuery("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
                    using (ManagementObjectSearcher buscadorRam = new ManagementObjectSearcher(consultaRam))
                    {
                        foreach (ManagementObject item in buscadorRam.Get())
                        {
                            long totalKB = Convert.ToInt64(item["TotalVisibleMemorySize"]);
                            long livreKB = Convert.ToInt64(item["FreePhysicalMemory"]);
                            long usadaKB = totalKB - livreKB;

                            double totalGB = Math.Round(totalKB / (1024.0 * 1024.0), 2);
                            double usadaGB = Math.Round(usadaKB / (1024.0 * 1024.0), 2);
                            long porcentagemRam = (usadaKB * 100) / totalKB;

                            statusRam = $"{usadaGB} GB em uso de {totalGB} GB ({porcentagemRam}%)";
                        }
                    }
                });

                if (listaDetalhes.Items.Count >= 2 && listaDetalhes.Items[0].Text == "Uso Atual do Processador (CPU)")
                {
                    listaDetalhes.Items[0].SubItems[1].Text = porcentagemCpu;
                    listaDetalhes.Items[1].SubItems[1].Text = statusRam;
                }
                else
                {
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Uso Atual do Processador (CPU)", porcentagemCpu }));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Uso Atual da Memória RAM", statusRam }));
                }
            }
            catch (Exception) { }
        }

        // LEITURA DE PROCESSOS COM DADOS DE CPU
        private void CarregarDadosProcessos()
        {
            try
            {
                Process[] todosProcessos = Process.GetProcesses();

                listaDetalhes.Items.Add(new ListViewItem(new[] { "Total de Processos Ativos", todosProcessos.Length.ToString() }));
                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "NOME DO PROCESSO", "USO DE RECURSOS (RAM E CPU)" }));

                var processosOrdenados = todosProcessos.OrderByDescending(p => p.WorkingSet64).Take(50);

                foreach (Process processo in processosOrdenados)
                {
                    long usoRamMB = processo.WorkingSet64 / (1024 * 1024);
                    int threadsCpu = processo.Threads.Count; // Leitura estrita de linhas em execução no processador

                    listaDetalhes.Items.Add(new ListViewItem(new[] { processo.ProcessName, $"{usoRamMB} MB RAM | {threadsCpu} Threads no Processador" }));
                }
            }
            catch (Exception erro)
            {
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Processos)", erro.Message }));
            }
        }

        private void CarregarDadosBateria()
        {
            try
            {
                PowerStatus energia = SystemInformation.PowerStatus;
                string statusTomada = energia.PowerLineStatus == PowerLineStatus.Online ? "Conectado (Na Tomada)" : "Desconectado (Na Bateria)";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Status da Fonte de Energia", statusTomada }));
                int porcentagem = (int)(energia.BatteryLifePercent * 100);
                string textoPorcentagem = porcentagem > 100 ? "Bateria não detectada (Desktop)" : porcentagem + "%";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Nível de Carga da Bateria", textoPorcentagem }));
                string statusCarga = energia.BatteryChargeStatus.ToString();
                if (statusCarga == "NoSystemBattery") statusCarga = "Sem bateria conectada";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Estado de Carregamento", statusCarga }));
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Bateria)", erro.Message })); }
        }

        // LEITURA DE PROCESSADOR COM TRATAMENTO DE MULTIPLOS SOKETS
        private void CarregarDadosProcessador()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize, SocketDesignation FROM Win32_Processor");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contadorSocket = 1; // Contador para servidores com mais de um processador

                    foreach (ManagementObject item in buscador.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem($"--- PROCESSADOR FÍSICO {contadorSocket} ---"));

                        string nomeProcessador = item["Name"]?.ToString();
                        string soqueteCorrigido = item["SocketDesignation"]?.ToString();

                        if (!string.IsNullOrEmpty(nomeProcessador))
                        {
                            if (nomeProcessador.Contains("i3-2") || nomeProcessador.Contains("i5-2") || nomeProcessador.Contains("i7-2") ||
                                nomeProcessador.Contains("i3-3") || nomeProcessador.Contains("i5-3") || nomeProcessador.Contains("i7-3"))
                                soqueteCorrigido = "LGA 1155";
                            else if (nomeProcessador.Contains("i3-4") || nomeProcessador.Contains("i5-4") || nomeProcessador.Contains("i7-4"))
                                soqueteCorrigido = "LGA 1150";
                            else if (nomeProcessador.Contains("i3-6") || nomeProcessador.Contains("i5-6") || nomeProcessador.Contains("i7-6") ||
                                     nomeProcessador.Contains("i3-7") || nomeProcessador.Contains("i5-7") || nomeProcessador.Contains("i7-7") ||
                                     nomeProcessador.Contains("i3-8") || nomeProcessador.Contains("i5-8") || nomeProcessador.Contains("i7-8") ||
                                     nomeProcessador.Contains("i3-9") || nomeProcessador.Contains("i5-9") || nomeProcessador.Contains("i7-9"))
                                soqueteCorrigido = "LGA 1151";
                            else if (nomeProcessador.Contains("i3-10") || nomeProcessador.Contains("i5-10") || nomeProcessador.Contains("i7-10") ||
                                     nomeProcessador.Contains("i3-11") || nomeProcessador.Contains("i5-11") || nomeProcessador.Contains("i7-11"))
                                soqueteCorrigido = "LGA 1200";
                            else if (nomeProcessador.Contains("Ryzen"))
                                soqueteCorrigido = (nomeProcessador.Contains(" 7") && nomeProcessador.Length >= 12) ? "AM5" : "AM4";
                        }

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo", nomeProcessador }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Fabricante", item["Manufacturer"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Soquete (Socket)", soqueteCorrigido }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Núcleos Físicos", item["NumberOfCores"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Processadores Lógicos (Threads)", item["NumberOfLogicalProcessors"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Frequência Máxima", item["MaxClockSpeed"]?.ToString() + " MHz" }));

                        if (item["L2CacheSize"] != null) listaDetalhes.Items.Add(new ListViewItem(new[] { "Cache L2", item["L2CacheSize"]?.ToString() + " KB" }));
                        if (item["L3CacheSize"] != null && item["L3CacheSize"].ToString() != "0") listaDetalhes.Items.Add(new ListViewItem(new[] { "Cache L3", (Convert.ToInt32(item["L3CacheSize"]) / 1024) + " MB" }));

                        listaDetalhes.Items.Add(new ListViewItem(""));
                        contadorSocket++;
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (CPU)", erro.Message })); }
        }

        private void CarregarDadosPlacaDeVideo()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Name, AdapterRAM, DriverVersion, VideoProcessor, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    foreach (ManagementObject item in buscador.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo", item["Name"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Processador Gráfico", item["VideoProcessor"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Versão do Driver", item["DriverVersion"]?.ToString() }));

                        if (item["AdapterRAM"] != null)
                        {
                            long vramMB = Convert.ToInt64(item["AdapterRAM"]) / (1024 * 1024);
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Memória de Vídeo (VRAM)", vramMB + " MB" }));
                        }

                        if (item["CurrentHorizontalResolution"] != null)
                        {
                            string resolucao = $"{item["CurrentHorizontalResolution"]} x {item["CurrentVerticalResolution"]}";
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Resolução Atual", resolucao }));
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Taxa de Atualização", item["CurrentRefreshRate"]?.ToString() + " Hz" }));
                        }
                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (GPU)", erro.Message })); }
        }

        private void CarregarDadosMemoriaRAM()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Capacity, Speed, Manufacturer, PartNumber FROM Win32_PhysicalMemory");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    long memoriaTotalBytes = 0;
                    int contadorModulo = 1;

                    foreach (ManagementObject item in buscador.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem($"--- Módulo {contadorModulo} ---"));
                        long capacidadeBytes = Convert.ToInt64(item["Capacity"]);
                        memoriaTotalBytes += capacidadeBytes;

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Capacidade", (capacidadeBytes / (1024 * 1024 * 1024)) + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Frequência (Velocidade)", item["Speed"]?.ToString() + " MHz" }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Fabricante", item["Manufacturer"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Número de Série (Part Number)", item["PartNumber"]?.ToString().Trim() }));
                        listaDetalhes.Items.Add(new ListViewItem(""));
                        contadorModulo++;
                    }
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "MEMÓRIA RAM TOTAL", (memoriaTotalBytes / (1024 * 1024 * 1024)) + " GB" }));
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (RAM)", erro.Message })); }
        }

        private void CarregarDadosArmazenamento()
        {
            try
            {
                ObjectQuery consultaDiscos = new ObjectQuery("SELECT Model, InterfaceType, Size FROM Win32_DiskDrive");
                using (ManagementObjectSearcher buscadorDiscos = new ManagementObjectSearcher(consultaDiscos))
                {
                    listaDetalhes.Items.Add(new ListViewItem("--- DISCOS FÍSICOS (HARDWARE) ---"));
                    foreach (ManagementObject discoFisico in buscadorDiscos.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo do Disco", discoFisico["Model"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Tipo de Interface", discoFisico["InterfaceType"]?.ToString() }));
                        if (discoFisico["Size"] != null)
                        {
                            long tamanhoGB = Convert.ToInt64(discoFisico["Size"]) / (1024 * 1024 * 1024);
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Capacidade Bruta", tamanhoGB + " GB" }));
                        }
                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }

                listaDetalhes.Items.Add(new ListViewItem("--- PARTIÇÕES LÓGICAS (VOLUMES) ---"));
                DriveInfo[] discos = DriveInfo.GetDrives();
                foreach (DriveInfo disco in discos)
                {
                    if (disco.IsReady)
                    {
                        long espacoTotalGB = disco.TotalSize / (1024 * 1024 * 1024);
                        long espacoLivreGB = disco.AvailableFreeSpace / (1024 * 1024 * 1024);
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Unidade " + disco.Name, "Formato: " + disco.DriveFormat }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Tamanho Total", espacoTotalGB + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Espaço Livre", espacoLivreGB + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Armazenamento)", erro.Message })); }
        }

        private void CarregarDadosSistemaOperacional()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Nome do Computador", Environment.MachineName }));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Usuário Atual", Environment.UserName }));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Versão do Núcleo", Environment.OSVersion.ToString() }));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Arquitetura do Sistema", Environment.Is64BitOperatingSystem ? "64 Bits" : "32 Bits" }));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Diretório do Sistema", Environment.SystemDirectory }));

                TimeSpan tempoLigado = TimeSpan.FromMilliseconds(Environment.TickCount64);
                string textoUptime = $"{tempoLigado.Days} dias, {tempoLigado.Hours} horas, {tempoLigado.Minutes} minutos";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Tempo Ligado (Uptime)", textoUptime }));
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (OS)", erro.Message })); }
        }

        private void CarregarDadosRede()
        {
            try
            {
                NetworkInterface[] adaptadores = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adaptador in adaptadores)
                {
                    if (adaptador.OperationalStatus == OperationalStatus.Up)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Nome da Placa", adaptador.Name }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Descrição", adaptador.Description }));
                        long velocidadeMbps = adaptador.Speed / 1000000;
                        if (velocidadeMbps > 0)
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Velocidade do Link", velocidadeMbps + " Mbps" }));

                        string macAdress = BitConverter.ToString(adaptador.GetPhysicalAddress().GetAddressBytes());
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Endereço Físico (MAC)", macAdress }));

                        IPInterfaceProperties propriedades = adaptador.GetIPProperties();
                        foreach (UnicastIPAddressInformation ip in propriedades.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "Endereço IPv4", ip.Address.ToString() }));
                        }
                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Rede)", erro.Message })); }
        }
    }
}