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
using System.Collections.Generic;
using Microsoft.Win32;

namespace SystemInfoApp
{
    public partial class Form1 : Form
    {
        private SplitContainer conteinerDivisor;
        private TreeView menuLateral;
        private ListView listaDetalhes;
        private Button botaoSalvar;
        private System.Windows.Forms.Timer temporizadorMonitoramento;
        private ProgressBar barraProgresso;

        public Form1()
        {
            this.Text = "Painel de Informações do Sistema";
            this.MinimumSize = new Size(850, 550);
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
            noSensores.Nodes.Add("Sensores Térmicos (Temperaturas)");

            TreeNode noHardware = menuLateral.Nodes.Add("Hardware");
            noHardware.Nodes.Add("Processador (CPU)");
            noHardware.Nodes.Add("Placa-Mãe e BIOS");
            noHardware.Nodes.Add("Placa de Vídeo (GPU)");
            noHardware.Nodes.Add("Telas e Monitores");
            noHardware.Nodes.Add("Memória RAM");
            noHardware.Nodes.Add("Armazenamento");
            noHardware.Nodes.Add("Dispositivos USB");
            noHardware.Nodes.Add("Dispositivos de Áudio");
            noHardware.Nodes.Add("Impressoras e Fax");
            noHardware.Nodes.Add("Bateria e Energia");

            TreeNode noSoftware = menuLateral.Nodes.Add("Software");
            noSoftware.Nodes.Add("Sistema Operacional");
            noSoftware.Nodes.Add("Programas Instalados");
            noSoftware.Nodes.Add("Programas de Inicialização"); // Nova funcionalidade adicionada
            noSoftware.Nodes.Add("Processos em Execução");
            noSoftware.Nodes.Add("Contas de Usuário");
            noSoftware.Nodes.Add("Serviços do Sistema");

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

            barraProgresso = new ProgressBar();
            barraProgresso.Dock = DockStyle.Bottom;
            barraProgresso.Height = 15;
            barraProgresso.Visible = false;

            conteinerDivisor.Panel1.Controls.Add(botaoSalvar);
            conteinerDivisor.Panel1.Controls.Add(menuLateral);

            conteinerDivisor.Panel2.Controls.Add(barraProgresso);
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
                catch (Exception) { }
            }
        }

        private async Task MenuLateral_AfterSelectAsync(TreeViewEventArgs e)
        {
            temporizadorMonitoramento.Stop();

            barraProgresso.Style = ProgressBarStyle.Marquee;
            barraProgresso.Value = 0;
            barraProgresso.Visible = true;
            Application.DoEvents();

            listaDetalhes.BeginUpdate();

            if (e.Node.Text != "Monitoramento de CPU e RAM")
            {
                listaDetalhes.Items.Clear();
            }

            if (e.Node.Text == "Monitoramento de CPU e RAM")
            {
                listaDetalhes.Items.Clear();
                listaDetalhes.EndUpdate();
                await CarregarDadosTempoRealAsync();
                temporizadorMonitoramento.Start();
            }
            else
            {
                if (e.Node.Text == "Sensores Térmicos (Temperaturas)") CarregarDadosTemperaturas();
                else if (e.Node.Text == "Processador (CPU)") CarregarDadosProcessador();
                else if (e.Node.Text == "Placa-Mãe e BIOS") CarregarDadosPlacaMae();
                else if (e.Node.Text == "Placa de Vídeo (GPU)") CarregarDadosPlacaDeVideo();
                else if (e.Node.Text == "Telas e Monitores") CarregarDadosMonitores();
                else if (e.Node.Text == "Memória RAM") CarregarDadosMemoriaRAM();
                else if (e.Node.Text == "Armazenamento") CarregarDadosArmazenamento();
                else if (e.Node.Text == "Dispositivos USB") CarregarDadosUSB();
                else if (e.Node.Text == "Dispositivos de Áudio") CarregarDadosAudio();
                else if (e.Node.Text == "Impressoras e Fax") CarregarDadosImpressoras();
                else if (e.Node.Text == "Bateria e Energia") CarregarDadosBateria();
                else if (e.Node.Text == "Sistema Operacional") CarregarDadosSistemaOperacional();
                else if (e.Node.Text == "Programas Instalados") CarregarDadosProgramas();
                else if (e.Node.Text == "Programas de Inicialização") CarregarDadosInicializacao(); // Mapeamento ativado
                else if (e.Node.Text == "Contas de Usuário") CarregarDadosUsuarios();
                else if (e.Node.Text == "Processos em Execução") CarregarDadosProcessos();
                else if (e.Node.Text == "Serviços do Sistema") CarregarDadosServicos();
                else if (e.Node.Text == "Adaptadores de Conexão") CarregarDadosRede();

                listaDetalhes.EndUpdate();
            }

            barraProgresso.Visible = false;
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
                            porcentagemCpu = item["LoadPercentage"]?.ToString() + "%";
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

                listaDetalhes.BeginUpdate();

                void AtualizarOuInserirLinha(string chaveBusca, string textoPropriedade, string valorPropriedade)
                {
                    if (listaDetalhes.Items.ContainsKey(chaveBusca))
                    {
                        listaDetalhes.Items[chaveBusca].SubItems[1].Text = valorPropriedade;
                    }
                    else
                    {
                        ListViewItem linha = new ListViewItem(new[] { textoPropriedade, valorPropriedade });
                        linha.Name = chaveBusca;
                        listaDetalhes.Items.Add(linha);
                    }
                }

                AtualizarOuInserirLinha("USO_CPU", "Uso Atual do Processador (CPU)", porcentagemCpu);
                AtualizarOuInserirLinha("USO_RAM", "Uso Atual da Memória RAM", statusRam);

                listaDetalhes.EndUpdate();
            }
            catch (Exception) { }
        }

        private void CarregarDadosTemperaturas()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- SENSORES TÉRMICOS NATIVOS (WMI) ---"));
                int contadorZonas = 0;

                try
                {
                    ObjectQuery consultaTermica = new ObjectQuery("SELECT CurrentTemperature, InstanceName FROM MSAcpi_ThermalZoneTemperature");
                    using (ManagementObjectSearcher buscadorTermico = new ManagementObjectSearcher(@"root\WMI", consultaTermica.QueryString))
                    {
                        foreach (ManagementObject zona in buscadorTermico.Get())
                        {
                            string nomeZona = zona["InstanceName"]?.ToString();
                            if (zona["CurrentTemperature"] != null)
                            {
                                double temperaturaKelvinDecimos = Convert.ToDouble(zona["CurrentTemperature"]);
                                double temperaturaCelsius = (temperaturaKelvinDecimos / 10.0) - 273.15;

                                if (temperaturaCelsius > 0 && temperaturaCelsius < 150)
                                {
                                    listaDetalhes.Items.Add(new ListViewItem(new[] { $"Placa-Mãe: ACPI Zone ({nomeZona})", $"{Math.Round(temperaturaCelsius, 1)} °C" }));
                                    contadorZonas++;
                                }
                            }
                        }
                    }
                }
                catch (ManagementException) { }

                if (contadorZonas == 0)
                {
                    listaDetalhes.Items.Add(new ListViewItem(""));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Status de Leitura Térmica", "Não Suportado pelo WMI Padrão" }));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Restrição Técnica (GPU / CPU / Discos)", "As temperaturas físicas diretas do silício são bloqueadas pelo kernel do Windows." }));
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Sensores)", erro.Message })); }
        }

        private void CarregarDadosMonitores()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- MONITORES CONECTADOS E RECONHECIDOS ---"));
                Screen[] telas = Screen.AllScreens;
                int contador = 1;

                foreach (Screen tela in telas)
                {
                    string statusPrincipal = tela.Primary ? " (Tela Principal)" : "";
                    listaDetalhes.Items.Add(new ListViewItem(new[] { $"Monitor {contador}{statusPrincipal}", tela.DeviceName }));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "  Resolução Física Atual", $"{tela.Bounds.Width} x {tela.Bounds.Height} Pixels" }));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "  Área de Trabalho Útil", $"{tela.WorkingArea.Width} x {tela.WorkingArea.Height} Pixels" }));
                    listaDetalhes.Items.Add(new ListViewItem(""));
                    contador++;
                }

                listaDetalhes.Items.Add(new ListViewItem("--- ESPECIFICAÇÕES DO FABRICANTE (WMI) ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT Caption, MonitorManufacturer FROM Win32_DesktopMonitor");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nomeMonitor = item["Caption"]?.ToString();
                        if (!string.IsNullOrEmpty(nomeMonitor) && nomeMonitor != "Monitor Genérico PnP")
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo do Painel", nomeMonitor }));
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Fabricante Registrado", item["MonitorManufacturer"]?.ToString() }));
                            listaDetalhes.Items.Add(new ListViewItem(""));
                        }
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Monitores)", erro.Message })); }
        }

        private void CarregarDadosBateria()
        {
            try
            {
                PowerStatus energia = SystemInformation.PowerStatus;
                string statusTomada = energia.PowerLineStatus == PowerLineStatus.Online ? "Conectado (Na Tomada)" : "Desconectado (Na Bateria)";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Status da Fonte de Energia", statusTomada }));

                int porcentagemCarga = (int)(energia.BatteryLifePercent * 100);
                string textoPorcentagem = porcentagemCarga > 100 ? "Bateria não detectada (Desktop)" : porcentagemCarga + "%";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Nível de Carga Atual", textoPorcentagem }));

                string statusCarga = energia.BatteryChargeStatus.ToString();
                if (statusCarga == "NoSystemBattery") statusCarga = "Sem bateria conectada";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Estado de Carregamento", statusCarga }));

                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem("--- ANÁLISE DE SAÚDE DA BATERIA ---"));

                ObjectQuery consultaBateria = new ObjectQuery("SELECT DesignCapacity, FullChargeCapacity FROM Win32_Battery");
                using (ManagementObjectSearcher buscadorBateria = new ManagementObjectSearcher(consultaBateria))
                {
                    bool bateriaDetectadaWMI = false;
                    foreach (ManagementObject bateria in buscadorBateria.Get())
                    {
                        bateriaDetectadaWMI = true;
                        if (bateria["DesignCapacity"] != null && bateria["FullChargeCapacity"] != null)
                        {
                            uint capacidadeFabrica = Convert.ToUInt32(bateria["DesignCapacity"]);
                            uint capacidadeMaximaAtual = Convert.ToUInt32(bateria["FullChargeCapacity"]);

                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Capacidade Original de Fábrica", $"{capacidadeFabrica} mWh" }));
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Capacidade Máxima Atual", $"{capacidadeMaximaAtual} mWh" }));

                            if (capacidadeFabrica > 0)
                            {
                                double saudePorcentagem = Math.Round(((double)capacidadeMaximaAtual / capacidadeFabrica) * 100, 2);
                                string diagnosticoSaude = saudePorcentagem >= 80 ? "Boa (Saudável)" :
                                                          saudePorcentagem >= 50 ? "Atenção (Desgastada)" : "Crítica (Substituição Recomendada)";

                                listaDetalhes.Items.Add(new ListViewItem(new[] { "Saúde Estimada da Célula", $"{saudePorcentagem}% - Status: {diagnosticoSaude}" }));
                            }
                        }
                    }
                    if (!bateriaDetectadaWMI)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Diagnóstico de Saúde", "Não disponível para este equipamento." }));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Bateria)", erro.Message })); }
        }

        private void CarregarDadosSistemaOperacional()
        {
            try
            {
                ObjectQuery consultaOS = new ObjectQuery("SELECT Caption, CSDVersion, BuildNumber, InstallDate, SerialNumber, OSArchitecture, SystemDirectory FROM Win32_OperatingSystem");
                using (ManagementObjectSearcher buscadorOS = new ManagementObjectSearcher(consultaOS))
                {
                    foreach (ManagementObject os in buscadorOS.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Edição do Windows", os["Caption"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Arquitetura do Sistema", os["OSArchitecture"]?.ToString() }));

                        string servicePack = os["CSDVersion"]?.ToString();
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Service Pack", string.IsNullOrEmpty(servicePack) ? "Nenhum" : servicePack }));

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Número da Compilação (Build)", os["BuildNumber"]?.ToString() }));

                        if (os["InstallDate"] != null)
                        {
                            DateTime dataInstalacao = ManagementDateTimeConverter.ToDateTime(os["InstallDate"].ToString());
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Data de Instalação", dataInstalacao.ToString("dd/MM/yyyy HH:mm:ss") }));
                        }

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Número de Série do SO", os["SerialNumber"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Diretório do Sistema", os["SystemDirectory"]?.ToString() }));
                    }
                }

                try
                {
                    using (RegistryKey chave = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (chave != null)
                        {
                            string versaoComercial = chave.GetValue("DisplayVersion")?.ToString();
                            if (!string.IsNullOrEmpty(versaoComercial))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "Versão de Lançamento", versaoComercial }));
                            }
                        }
                    }
                }
                catch { }

                try
                {
                    using (RegistryKey chave = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SoftwareProtectionPlatform"))
                    {
                        if (chave != null)
                        {
                            string chaveProduto = chave.GetValue("BackupProductKeyDefault")?.ToString();
                            if (!string.IsNullOrEmpty(chaveProduto))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "Chave de Ativação (Product Key)", chaveProduto }));
                            }
                        }
                    }
                }
                catch { }

                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Nome do Computador", Environment.MachineName }));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Usuário Atual", Environment.UserName }));

                TimeSpan tempoLigado = TimeSpan.FromMilliseconds(Environment.TickCount64);
                string textoUptime = $"{tempoLigado.Days} dias, {tempoLigado.Hours} horas, {tempoLigado.Minutes} minutos";
                listaDetalhes.Items.Add(new ListViewItem(new[] { "Tempo Ligado (Uptime)", textoUptime }));
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (OS)", erro.Message })); }
        }

        private void CarregarDadosPlacaMae()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- ESPECIFICAÇÕES DA PLACA-MÃE ---"));
                ObjectQuery consultaPlaca = new ObjectQuery("SELECT Manufacturer, Product, SerialNumber, Version FROM Win32_BaseBoard");
                using (ManagementObjectSearcher buscadorPlaca = new ManagementObjectSearcher(consultaPlaca))
                {
                    foreach (ManagementObject mb in buscadorPlaca.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Fabricante da Placa", mb["Manufacturer"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo (Produto)", mb["Product"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Revisão / Versão", mb["Version"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Número de Série Físico", mb["SerialNumber"]?.ToString() }));
                    }
                }

                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem("--- ESPECIFICAÇÕES DO BIOS ---"));
                ObjectQuery consultaBios = new ObjectQuery("SELECT Manufacturer, Name, Version, ReleaseDate FROM Win32_BIOS");
                using (ManagementObjectSearcher buscadorBios = new ManagementObjectSearcher(consultaBios))
                {
                    foreach (ManagementObject bios in buscadorBios.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Desenvolvedor do BIOS", bios["Manufacturer"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Versão Instalada", bios["Name"]?.ToString() }));

                        if (bios["ReleaseDate"] != null)
                        {
                            string dataBruta = bios["ReleaseDate"].ToString();
                            if (dataBruta.Length >= 8)
                            {
                                string ano = dataBruta.Substring(0, 4);
                                string mes = dataBruta.Substring(4, 2);
                                string dia = dataBruta.Substring(6, 2);
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "Data de Compilação", $"{dia}/{mes}/{ano}" }));
                            }
                        }
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Placa-Mãe)", erro.Message })); }
        }

        private void CarregarDadosProcessador()
        {
            try
            {
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed, L2CacheSize, L3CacheSize, SocketDesignation FROM Win32_Processor");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contadorSocket = 1;

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
                ObjectQuery consulta = new ObjectQuery("SELECT Name, AdapterRAM, DriverVersion, VideoProcessor, CurrentBitsPerPixel, CurrentHorizontalResolution, CurrentVerticalResolution, CurrentRefreshRate FROM Win32_VideoController");
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

                        if (item["CurrentBitsPerPixel"] != null)
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Profundidade de Cores", item["CurrentBitsPerPixel"]?.ToString() + " Bits" }));
                        }

                        if (item["CurrentHorizontalResolution"] != null)
                        {
                            string resolucao = $"{item["CurrentHorizontalResolution"]} x {item["CurrentVerticalResolution"]}";
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Resolução Atual", resolucao }));
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Taxa de Atualização Máxima", item["CurrentRefreshRate"]?.ToString() + " Hz" }));
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
                ObjectQuery consulta = new ObjectQuery("SELECT Capacity, Speed, DataWidth, Manufacturer, PartNumber, SMBIOSMemoryType FROM Win32_PhysicalMemory");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    long memoriaTotalBytes = 0;
                    int contadorModulo = 1;

                    foreach (ManagementObject item in buscador.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem($"--- Módulo {contadorModulo} ---"));
                        long capacidadeBytes = Convert.ToInt64(item["Capacity"]);
                        memoriaTotalBytes += capacidadeBytes;

                        string tipoMemoria = "Desconhecido";
                        if (item["SMBIOSMemoryType"] != null)
                        {
                            int codigoTipo = Convert.ToInt32(item["SMBIOSMemoryType"]);
                            if (codigoTipo == 20) tipoMemoria = "DDR";
                            else if (codigoTipo == 21) tipoMemoria = "DDR2";
                            else if (codigoTipo == 24) tipoMemoria = "DDR3";
                            else if (codigoTipo == 26) tipoMemoria = "DDR4";
                            else if (codigoTipo == 34) tipoMemoria = "DDR5";
                        }

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Capacidade", (capacidadeBytes / (1024 * 1024 * 1024)) + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Tecnologia", tipoMemoria }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Frequência (Velocidade)", item["Speed"]?.ToString() + " MHz" }));

                        string velocidadeSTR = item["Speed"]?.ToString();
                        string larguraDadosSTR = item["DataWidth"]?.ToString();
                        if (int.TryParse(velocidadeSTR, out int velocidade) && int.TryParse(larguraDadosSTR, out int larguraDados))
                        {
                            int bandaMBs = (velocidade * larguraDados) / 8;
                            double bandaGBs = Math.Round(bandaMBs / 1024.0, 2);
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "Largura de Banda Máxima", bandaGBs.ToString("0.00") + " GB/s" }));
                        }

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
                ObjectQuery consultaDiscos = new ObjectQuery("SELECT Model, InterfaceType, Size, SerialNumber, Status FROM Win32_DiskDrive");
                using (ManagementObjectSearcher buscadorDiscos = new ManagementObjectSearcher(consultaDiscos))
                {
                    listaDetalhes.Items.Add(new ListViewItem("--- DISCOS FÍSICOS (HARDWARE) ---"));
                    foreach (ManagementObject discoFisico in buscadorDiscos.Get())
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Modelo do Disco", discoFisico["Model"]?.ToString() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Número de Série", discoFisico["SerialNumber"]?.ToString().Trim() }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Tipo de Interface", discoFisico["InterfaceType"]?.ToString() }));

                        string statusSaude = discoFisico["Status"]?.ToString();
                        string diagnostico = statusSaude == "OK" ? "Boa (Saudável)" :
                                             statusSaude == "Pred Fail" ? "Alerta (Falha Iminente SMART)" :
                                             statusSaude == "Error" ? "Crítico (Erros de Leitura)" : statusSaude;

                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Status de Saúde (SMART)", diagnostico }));

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
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Unidade " + disco.Name, "Sistema de Arquivos: " + disco.DriveFormat }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Tamanho Total", espacoTotalGB + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Espaço Livre", espacoLivreGB + " GB" }));
                        listaDetalhes.Items.Add(new ListViewItem(""));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Armazenamento)", erro.Message })); }
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

        private void CarregarDadosUSB()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- DISPOSITIVOS USB CONECTADOS ---"));

                ObjectQuery consulta = new ObjectQuery("SELECT Caption, Manufacturer FROM Win32_PnPEntity WHERE DeviceID LIKE '%USB%'");
                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contador = 1;
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nome = item["Caption"]?.ToString();

                        if (!string.IsNullOrEmpty(nome) && !nome.Contains("Hub") && !nome.Contains("Root"))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { $"Dispositivo {contador}", nome }));

                            string fabricante = item["Manufacturer"]?.ToString();
                            if (!string.IsNullOrEmpty(fabricante) && fabricante != "(Bateria Padrão do Sistema)" && fabricante != "(Standard USB Host Controller)")
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Fabricante", fabricante }));
                            }

                            listaDetalhes.Items.Add(new ListViewItem(""));
                            contador++;
                        }
                    }

                    if (contador == 1)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Status", "Nenhum periférico USB detectado no momento." }));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (USB)", erro.Message })); }
        }

        private void CarregarDadosAudio()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- DISPOSITIVOS DE ÁUDIO ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Manufacturer, Status FROM Win32_SoundDevice");

                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contador = 1;
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nome = item["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(nome))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { $"Dispositivo {contador}", nome }));

                            string fabricante = item["Manufacturer"]?.ToString();
                            if (!string.IsNullOrEmpty(fabricante))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Fabricante", fabricante }));
                            }

                            string status = item["Status"]?.ToString();
                            if (!string.IsNullOrEmpty(status))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Status de Operação", status }));
                            }

                            listaDetalhes.Items.Add(new ListViewItem(""));
                            contador++;
                        }
                    }

                    if (contador == 1)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Status", "Nenhuma controladora de áudio instalada encontrada." }));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Áudio)", erro.Message })); }
        }

        private void CarregarDadosImpressoras()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- IMPRESSORAS E DISPOSITIVOS DE FAX ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Default, PortName, Shared FROM Win32_Printer");

                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contador = 1;
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nome = item["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(nome))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { $"Dispositivo {contador}", nome }));

                            bool padrao = item["Default"] != null && Convert.ToBoolean(item["Default"]);
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "  Impressora Padrão", padrao ? "Sim (Principal)" : "Não" }));

                            string porta = item["PortName"]?.ToString();
                            if (!string.IsNullOrEmpty(porta))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Porta de Comunicação", porta }));
                            }

                            bool compartilhada = item["Shared"] != null && Convert.ToBoolean(item["Shared"]);
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "  Compartilhada na Rede", compartilhada ? "Sim" : "Não" }));

                            listaDetalhes.Items.Add(new ListViewItem(""));
                            contador++;
                        }
                    }

                    if (contador == 1)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Status", "Nenhuma impressora instalada no sistema." }));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Impressoras)", erro.Message })); }
        }

        private void CarregarDadosProcessos()
        {
            try
            {
                Process[] todosProcessos = Process.GetProcesses();

                barraProgresso.Style = ProgressBarStyle.Continuous;
                barraProgresso.Maximum = todosProcessos.Length;
                barraProgresso.Value = 0;
                barraProgresso.Visible = true;

                List<ListViewItem> cacheLinhas = new List<ListViewItem>();
                var processosOrdenados = todosProcessos.OrderByDescending(p => p.WorkingSet64).Take(50);

                foreach (Process processo in processosOrdenados)
                {
                    long usoRamMB = processo.WorkingSet64 / (1024 * 1024);
                    int threadsCpu = processo.Threads.Count;
                    cacheLinhas.Add(new ListViewItem(new[] { processo.ProcessName, $"{usoRamMB} MB RAM | {threadsCpu} Threads no Processador" }));

                    barraProgresso.Value++;
                    Application.DoEvents();
                }

                listaDetalhes.Items.Add(new ListViewItem(new[] { "Total de Processos Ativos", todosProcessos.Length.ToString() }));
                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "NOME DO PROCESSO", "USO DE RECURSOS (RAM E CPU)" }));
                listaDetalhes.Items.AddRange(cacheLinhas.ToArray());
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Processos)", erro.Message })); }
            finally { barraProgresso.Visible = false; }
        }

        private void CarregarDadosServicos()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- ESTATÍSTICAS DOS SERVIÇOS DO SISTEMA ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT DisplayName, State, StartMode FROM Win32_Service");

                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    ManagementObjectCollection colecao = buscador.Get();

                    barraProgresso.Style = ProgressBarStyle.Continuous;
                    barraProgresso.Maximum = colecao.Count;
                    barraProgresso.Value = 0;
                    barraProgresso.Visible = true;

                    int contadorAtivos = 0;
                    int contadorInativos = 0;
                    List<ListViewItem> cacheLinhas = new List<ListViewItem>();

                    foreach (ManagementObject item in colecao)
                    {
                        string nome = item["DisplayName"]?.ToString();
                        string estado = item["State"]?.ToString();
                        string modo = item["StartMode"]?.ToString();

                        if (estado == "Running") contadorAtivos++;
                        else contadorInativos++;

                        if (!string.IsNullOrEmpty(nome))
                        {
                            string statusFormatado = $"{estado} (Modo: {modo})";
                            cacheLinhas.Add(new ListViewItem(new[] { nome, statusFormatado }));
                        }

                        barraProgresso.Value++;
                        Application.DoEvents();
                    }

                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Serviços em Execução (Ativos)", contadorAtivos.ToString() }));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "Serviços Parados (Inativos)", contadorInativos.ToString() }));
                    listaDetalhes.Items.Add(new ListViewItem(""));
                    listaDetalhes.Items.Add(new ListViewItem(new[] { "NOME DO SERVIÇO", "STATUS E MODO DE INICIALIZAÇÃO" }));
                    listaDetalhes.Items.AddRange(cacheLinhas.ToArray());
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Serviços)", erro.Message })); }
            finally { barraProgresso.Visible = false; }
        }

        private void CarregarDadosUsuarios()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- CONTAS DE USUÁRIO LOCAIS ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT Name, FullName, Disabled, PasswordRequired, Lockout, Status FROM Win32_UserAccount WHERE LocalAccount=True");

                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contador = 1;
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nome = item["Name"]?.ToString();
                        listaDetalhes.Items.Add(new ListViewItem(new[] { $"Conta Local {contador}", nome }));

                        string nomeCompleto = item["FullName"]?.ToString();
                        if (!string.IsNullOrEmpty(nomeCompleto))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "  Nome Completo de Registro", nomeCompleto }));
                        }

                        bool inativo = item["Disabled"] != null && Convert.ToBoolean(item["Disabled"]);
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Status da Conta de Usuário", inativo ? "Inativa (Desabilitada)" : "Ativa" }));

                        bool bloqueada = item["Lockout"] != null && Convert.ToBoolean(item["Lockout"]);
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Bloqueio por Erro de Senha", bloqueada ? "Bloqueada" : "Livre" }));

                        bool exigeSenha = item["PasswordRequired"] != null && Convert.ToBoolean(item["PasswordRequired"]);
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "  Exigência de Senha no Login", exigeSenha ? "Sim" : "Não" }));

                        string status = item["Status"]?.ToString();
                        if (!string.IsNullOrEmpty(status))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { "  Condição Geral", status }));
                        }

                        listaDetalhes.Items.Add(new ListViewItem(""));
                        contador++;
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Usuários)", erro.Message })); }
        }

        private void CarregarDadosProgramas()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- SOFTWARES INSTALADOS NO SISTEMA ---"));

                List<ListViewItem> cacheLinhas = new List<ListViewItem>();
                List<string> caminhosRegistro = new List<string>
                {
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
                };

                foreach (string caminho in caminhosRegistro)
                {
                    using (RegistryKey chaveBase = Registry.LocalMachine.OpenSubKey(caminho))
                    {
                        if (chaveBase != null)
                        {
                            string[] subChaves = chaveBase.GetSubKeyNames();

                            barraProgresso.Style = ProgressBarStyle.Continuous;
                            barraProgresso.Maximum = subChaves.Length > 0 ? subChaves.Length : 100;
                            barraProgresso.Value = 0;
                            barraProgresso.Visible = true;

                            foreach (string subChave in subChaves)
                            {
                                using (RegistryKey programa = chaveBase.OpenSubKey(subChave))
                                {
                                    if (programa != null)
                                    {
                                        string nome = programa.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrEmpty(nome))
                                        {
                                            string versao = programa.GetValue("DisplayVersion")?.ToString() ?? "Desconhecida";
                                            string desenvolvedor = programa.GetValue("Publisher")?.ToString() ?? "Não informado";

                                            cacheLinhas.Add(new ListViewItem(new[] { nome, $"Versão: {versao} | Desenvolvedor: {desenvolvedor}" }));
                                        }
                                    }
                                }

                                if (barraProgresso.Value < barraProgresso.Maximum) barraProgresso.Value++;
                                if (barraProgresso.Value % 10 == 0) Application.DoEvents();
                            }
                        }
                    }
                }

                var linhasOrdenadas = cacheLinhas.OrderBy(l => l.Text).ToArray();

                listaDetalhes.Items.Add(new ListViewItem(new[] { "Total de Programas Encontrados", linhasOrdenadas.Length.ToString() }));
                listaDetalhes.Items.Add(new ListViewItem(""));
                listaDetalhes.Items.Add(new ListViewItem(new[] { "NOME DO SOFTWARE", "DETALHES (VERSÃO E FABRICANTE)" }));
                listaDetalhes.Items.AddRange(linhasOrdenadas);
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Programas)", erro.Message })); }
            finally { barraProgresso.Visible = false; }
        }

        // --- NOVA FUNÇÃO: PROGRAMAS DE INICIALIZAÇÃO ---
        private void CarregarDadosInicializacao()
        {
            try
            {
                listaDetalhes.Items.Add(new ListViewItem("--- PROGRAMAS QUE INICIAM COM O WINDOWS ---"));
                ObjectQuery consulta = new ObjectQuery("SELECT Name, Command, Location, User FROM Win32_StartupCommand");

                using (ManagementObjectSearcher buscador = new ManagementObjectSearcher(consulta))
                {
                    int contador = 1;
                    foreach (ManagementObject item in buscador.Get())
                    {
                        string nome = item["Name"]?.ToString();
                        if (!string.IsNullOrEmpty(nome))
                        {
                            listaDetalhes.Items.Add(new ListViewItem(new[] { $"Entrada {contador}: {nome}", item["Command"]?.ToString() }));

                            string local = item["Location"]?.ToString();
                            if (!string.IsNullOrEmpty(local))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Origem do Gatilho", local }));
                            }

                            string usuario = item["User"]?.ToString();
                            if (!string.IsNullOrEmpty(usuario))
                            {
                                listaDetalhes.Items.Add(new ListViewItem(new[] { "  Escopo de Usuário", usuario }));
                            }

                            listaDetalhes.Items.Add(new ListViewItem(""));
                            contador++;
                        }
                    }

                    if (contador == 1)
                    {
                        listaDetalhes.Items.Add(new ListViewItem(new[] { "Status", "Nenhum programa de inicialização detectado." }));
                    }
                }
            }
            catch (Exception erro) { listaDetalhes.Items.Add(new ListViewItem(new[] { "Erro (Inicialização)", erro.Message })); }
        }
    }
}