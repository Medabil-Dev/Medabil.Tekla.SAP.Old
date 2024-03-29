﻿using Conexoes;
using DLM.cam;
using DLM.encoder;
using DLM.vars;
using FirstFloor.ModernUI.Windows.Controls;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Text;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MedabilTekla
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private bool Verifica_Aba_0()
        {
            var aba = this.abas.SelectedItem as TabItem;

            if (aba.Name == "origens")
            {
                if (!Directory.Exists(pasta_input.Text))
                {
                    MessageBox.Show("Selecione um input");
                    return false;
                }
                if (!File.Exists(arquivo_excel.Text))
                {
                    MessageBox.Show("Selecione um arquivo de report");
                    return false;
                }
                if (!File.Exists(arquivo_dbase.Text))
                {
                    MessageBox.Show("Selecione uma dbase válida");
                    return false;
                }
                if (!Directory.Exists(pasta_nc1_chapas.Text))
                {
                    MessageBox.Show("Selecione a pasta dos NC1 de Chapas");
                    return false;
                }
                if (!Directory.Exists(pasta_nc1_perfis.Text))
                {
                    MessageBox.Show("Selecione a pasta dos NC1 de Chapas");
                    return false;
                }
                if (!Directory.Exists(pasta_destino.Text))
                {
                    MessageBox.Show("Selecione a pasta de destino.");
                    return false;
                }


                this.Excel = new DLM.encoder.EXCEL(this.arquivo_excel.Text);

                this.lista_etapas.ItemsSource = null;
                this.lista_etapas.ItemsSource = this.Etapas();

                if(this.Excel.Marcas.Count==0)
                {
                    MessageBox.Show("Nenhuma marca encontrada no arquivo\n " + this.arquivo_excel.Text);
                }



                if(Conexoes.DBases.GetdbPerfil().GetPerfisTekla().Count==0)
                {
                    MessageBox.Show("Nenhum perfil encontrado no arquivo\n " + this.arquivo_dbase.Text);
                }

            }
            return true;
        }
        private bool Verifica_Aba_1()
        {
            var aba = this.abas.SelectedItem as TabItem;

            if (selecao_pecas.Name == "selecao_pecas")
            {
                //selecao de marcas
                if (this.lista_marcas.Items.Count == 0)
                {
                    MessageBox.Show("Selecione pelo menos 1 Etapa");
                    return false;
                }
            }
            return true;
        }
        private bool Verifica_Aba_2()
        {
            var aba = this.abas.SelectedItem as TabItem;

            if (aba.Name == "selecao_pecas")
            {
                var sem_cam = this.PosicoesSemCAM();
                if (sem_cam.Count > 0)
                {
                    MessageBox.Show($"Há {sem_cam.Count} peças sem CAM: {string.Join(", ", sem_cam.Select(x=> Utilz.getNome(x)))}");
                    return false;
                }
                else
                {
                    this.Pacote = new DLM.ep.EP_Pacote(PacoteEPTipo.Tekla, this.arquivo_excel.Text, (bool)marcas_simples.IsChecked,false,this.PastaCAMs());
                    this.Pacote.Destino = this.pasta_destino.Text;
                    this.Pacote.GetMarcasTekla(this.GetMarcas());
                    var ver = this.Pacote.Verificar();
                    this.lista_verificacao_report.ItemsSource = null;
                    this.lista_verificacao_report.ItemsSource = ver;
                }


            }
            return true;
        }
        public MainWindow()
        {
            InitializeComponent();
            UpdateAbas();
            Cfg.Init.JanelaWaitMultiThread = false;
            this.Title = System.Windows.Forms.Application.ProductName + " v." + System.Windows.Forms.Application.ProductVersion;
        }

 

        public List<string> GetArqSubCAMS()
        {
            var cams = this.GetCAMS();

            var subcams = cams.SelectMany(x => x.GetSubCams()).Select(x => this.PastaCAMs() + x + ".CAM").ToList();
            return subcams;
        }
        public List<Marca> GetMarcas()
        {
            return this.lista_marcas.Items.Cast<Marca>().ToList().FindAll(x => x.Descr.ToUpper() != "RM");
        }
        public List<Marca> GetMarcasRM()
        {
            return this.lista_marcas.Items.Cast<Marca>().ToList().FindAll(x => x.Descr.ToUpper() == "RM");
        }
        public List<Posicao> GetPosicoes()
        {
            return this.GetMarcas().SelectMany(x => x.Posicoes).GroupBy(x => x.Nome).Select(x => x.First()).ToList();
        }
        public List<string> Etapas()
        {
            return this.Excel.Marcas.Select(x => x.GetEtapa()).Distinct().ToList();
        }
        public List<string> ArqsNC1s()
        {
            List<string> retorno = new List<string>();

            retorno.AddRange(Conexoes.Utilz.GetArquivos(this.PastaNC1(), "*.NC1"));
            retorno.AddRange(Conexoes.Utilz.GetArquivos(this.PastaNC1_2(), "*.NC1"));
            retorno = retorno.GroupBy(x => Utilz.getNome(x).ToUpper()).Select(x => x.First()).ToList();
            return retorno.OrderBy(x=> x).ToList();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!Verifica_Aba_0()) { return; };
            var sel = abas.SelectedIndex;
            if(sel>0)
            {
                abas.SelectedIndex = abas.SelectedIndex - 1;
                UpdateAbas();

            }
        }

        public DLM.encoder.EXCEL Excel { get; set; }
        public DLM.ep.EP_Pacote Pacote { get; set; } = new DLM.ep.EP_Pacote();
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            var sel = abas.SelectedIndex;
            if(sel==0)
            {
                //selecao de diretorios
            if (!Verifica_Aba_0()) { return; } ;
            }
            if(sel==1)
            {
                if (!Verifica_Aba_1()) { return; };
                if (!Verifica_Aba_2()) { return; }
            }
            if (sel < abas.Items.Count - 1)
            {
                abas.SelectedIndex = abas.SelectedIndex + 1;
                UpdateAbas();
            }


        }
        public string Arquivo_RME
        {
            get
            {
               if(Directory.Exists(pasta_destino.Text))
                {
                    return this.pasta_destino.Text + @"\SAP - RME.XLSX";
                }
                return "";
            }
        }


        public List<string> PosicoesSemCAM()
        {
            var cams = GetArqCams();
            cams.AddRange(GetArqSubCAMS());
            return cams.FindAll(x=>!File.Exists(x)).ToList();
        }
        public List<string> GetArqCams()
        {
            var pos = this.GetPosicoes().Select(x => x.Nome);
            var cams = pos.Select(x => this.PastaCAMs() + x + ".CAM").ToList();
            return cams;
        }
        public List<ReadCAM> GetCAMS()
        {
            return this.GetArqCams().FindAll(x => File.Exists(x)).Select(x => new ReadCAM(x)).ToList();
        }
        private void UpdateAbas()
        {
            this.Visibility = Visibility.Hidden;
            for (int i = 0; i < abas.Items.Count; i++)
            {
                if(abas.SelectedIndex<0 && i ==0)
                {

                }
                else if (i != abas.SelectedIndex)
                {
                    ((TabItem)abas.Items[i]).Visibility = Visibility.Collapsed;
                }
                else
                {
                    ((TabItem)abas.Items[i]).Visibility = Visibility.Visible;
                }
            }
            if(abas.SelectedIndex<=0)
            {
                bt_anterior.Visibility = Visibility.Hidden;
            }
            else
            {
                bt_anterior.Visibility = Visibility.Visible;

            }

            if (abas.SelectedIndex == abas.Items.Count-1)
            {
                bt_proximo.Visibility = Visibility.Hidden;
            }
            else
            {
                bt_proximo.Visibility = Visibility.Visible;

            }
            this.Visibility = Visibility.Visible;
            ValidarPastas();
        }


        private void selecionar_destino(object sender, RoutedEventArgs e)
        {
            //get_pasta_destino();
        }

        private void get_pasta_destino()
        {

            if(input_existe())
            {
                this.pasta_destino.Text = Utilz.CriarPasta(this.pasta_input.Text, "Liberação");
                this.pasta_cams.Text = PastaCAMs();
            }
          
        }

        public List<DLM.cam.NC1> GetNC1s()
        {
            List<DLM.cam.NC1> retorno = new List<DLM.cam.NC1>();
            foreach(var s in this.ArqsNC1s())
            {
                DLM.cam.NC1 pr = new DLM.cam.NC1(s);
                retorno.Add(pr);
            }
            return retorno;
        }
        
        private void lista_etapas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var etapas = this.lista_etapas.SelectedItems.Cast<string>().ToList();
            this.lista_marcas.ItemsSource = null;
            this.lista_marcas.ItemsSource = this.Excel.Marcas.FindAll(x => etapas.Find(y => y == x.GetEtapa()) != null).ToList();

            
            
        }
        public List<string> errosNC1 { get; set; } = new List<string>();
        public List<DLM.cam.ReadCAM> Cams { get; set; } = new List<DLM.cam.ReadCAM>();
        private void gerar_cams(object sender, RoutedEventArgs e)
        {
            //this.Cams.Clear();
            //var arquivos_existentes = Utilz.GetArquivos(this.PastaCAM());
            //errosNC1.Clear();
            //    var NC1s = this.NC1s();
            //this.status_nc1.Items.Clear();
            //foreach(var s in this.GetPosicoes())
            //{
            //    var igual = NC1s.Find(x => Utilz.getNome(x).ToUpper() == s.Nome.ToUpper());
            //    if(igual == null)
            //    {
            //        this.status_nc1.Items.Add("Falta o arquivo NC1 para a posição " + igual);
            //    }
            //    else
            //    {
            //        DLM.cam.NC1 pr = new DLM.cam.NC1(igual, this.PastaCAM(),this.dbase);

                   

            //        if(Conexoes.Utilz.Apagar(this.PastaCAM() + s + ".CAM"))
            //        {
            //         var scam =   pr.Gerar();
            //            if(scam!=null)
            //            {
            //            this.Cams.Add(scam.GetReadCam());
            //            }
            //            if (pr.Status != "")
            //            {
            //                this.status_nc1.Items.Add(pr.Status);
            //                this.errosNC1.Add(pr.Status);
            //            }
            //            else
            //            {
            //                this.status_nc1.Items.Add("CAM Gerado => " + s);
            //            }
            //        }
            //        else
            //        {
            //            string ms = "Não foi possível substituir o CAM existente na pasta pelo novo da peça " + s;
            //            this.status_nc1.Items.Add(ms);
            //            this.errosNC1.Add(ms);
            //        }
            //    }

            //}
            
        }
        public List<Report> getErros()
        {
            return this.lista_verificacao_report.Items.Cast<Report>().ToList();
        }
        private void gerar_excel(object sender, RoutedEventArgs e)
        {
            var erros_criticos = getErros().FindAll(x => x.Tipo == TipoReport.Crítico);
            if(erros_criticos.Count>0)
            {
                MessageBox.Show($"Há {erros_criticos.Count} erros críticos que necessitam ser corrigidos para poder gerar.");
                return;
            }
            this.Pacote = new DLM.ep.EP_Pacote( PacoteEPTipo.Tekla,this.arquivo_excel.Text, (bool)marcas_simples.IsChecked,false,this.PastaCAMs());
            this.Pacote.GetMarcasTekla(this.GetMarcas());
            if (this.Pacote.GerarPacote(this.pasta_destino.Text, true,true))
            {
                MessageBox.Show("Pacote gerado!");
            }
            else
            {
                Utilz.ShowReports(this.Pacote.Reports);
            }
        }

        private void set_pasta_input(object sender, RoutedEventArgs e)
        {
            var pasta  = Utilz.Selecao.SelecionarPasta("Selecione", this.pasta_input.Text);
            if(Directory.Exists(pasta))
            {
                this.pasta_input.Text = pasta;
            }
            //this.bt_proximo.IsEnabled = false;
            UpdateDirs();
        }
        public bool input_existe()
        {
            return Directory.Exists(this.pasta_input.Text);
        }
        public string PastaInput()
        {
            if(!input_existe())
            {
                return "";
            }
            var pasta = Utilz.CriarPasta(this.pasta_input.Text, "Reports");
            return pasta;
        }
        public string PastaNC1_2()
        {
            if (!input_existe())
            {
                return "";
            }
            var pasta = Utilz.CriarPasta(this.pasta_input.Text, "DSTV_Plates");
            return pasta;
        }

        public string PastaCAMs()
        {
            if (!input_existe())
            {
                return "";
            }
            var pasta = Utilz.CriarPasta(this.pasta_destino.Text, "CAM");
            return pasta;
        }
        public string PastaNC1()
        {
            if (!input_existe())
            {
                return "";
            }
            var pasta = Utilz.CriarPasta(this.pasta_input.Text, "DSTV_Profiles");
            return pasta;
        }
        private void UpdateDirs()
        {
            if (input_existe())
            {
                this.arquivo_dbase.ItemsSource = null;
                this.get_pasta_destino();
                this.arquivo_dbase.ItemsSource = Utilz.GetArquivos(this.pasta_input.Text, "profiles.lis", SearchOption.AllDirectories);
                if (this.arquivo_dbase.Items.Count > 0)
                {
                    this.arquivo_dbase.SelectedItem = this.arquivo_dbase.Items[0];
                }
                this.arquivo_excel.ItemsSource = null;
                List<string> arqs = new List<string>();
                arqs.AddRange(Conexoes.Utilz.GetArquivos(PastaInput(), "*Medabil*.csv", SearchOption.TopDirectoryOnly));
                arqs.AddRange(Conexoes.Utilz.GetArquivos(PastaInput(), "*Medabil*.xlsx", SearchOption.TopDirectoryOnly));
                arqs.AddRange(Conexoes.Utilz.GetArquivos(PastaInput(), "*Medabil*.xls", SearchOption.TopDirectoryOnly));
                this.arquivo_excel.ItemsSource = arqs.Distinct().ToList().OrderBy(x => x).ToList();

                if (this.arquivo_excel.Items.Count > 0)
                {
                    this.arquivo_excel.SelectedItem = this.arquivo_excel.Items[0];
                }

                this.pasta_nc1_chapas.Text = PastaNC1_2();
                this.pasta_nc1_perfis.Text = PastaNC1();

                this.pasta_nc1_to_cams.Text = this.PastaConvertidos();

            }

            ValidarPastas();
        }

        private void ValidarPastas()
        {
            if (this.arquivo_excel.Items.Count > 0 &&
                               this.arquivo_dbase.Items.Count > 0 &&
                               (Directory.Exists(this.pasta_nc1_perfis.Text)) &&
                               (Directory.Exists(this.pasta_nc1_perfis.Text)) &&
                               (Directory.Exists(this.pasta_destino.Text))
                               )
            {
                this.bt_proximo.Visibility = Visibility.Visible;
            }
            else
            {
                this.bt_proximo.Visibility = Visibility.Collapsed;
            }
        }



        private void ModernWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.UpdateDirs();
        }

        public string PastaConvertidos()
        {
            if(!input_existe())
            {
                return "";
            }
            return Utilz.CriarPasta(this.pasta_input.Text, "NC1_Convertido_Para_CAM");
        }
        private void exporta_tipos(object sender, RoutedEventArgs e)
        {

        }

        private void abre_destino(object sender, RoutedEventArgs e)
        {
            Utilz.Abrir(pasta_destino.Text);
        }

        private void ver_desenhos(object sender, RoutedEventArgs e)
        {
            if (input_existe())
            {
                VisualizadorCAM.Funcoes.AbrirPasta(this.PastaConvertidos(), SearchOption.AllDirectories);
            }
        }

        private void abre_nc1_teste(object sender, RoutedEventArgs e)
        {
            var sel = Utilz.Abrir_String("NC1","");
            if(sel.Existe())
            {
                DLM.cam.NC1 nc1 = new DLM.cam.NC1(sel);
            }

        }

        private void converte_externo(object sender, RoutedEventArgs e)
        {
            Utilz.NC1.Converter(Conexoes.Utilz.Selecao.SelecionarPasta());
        }

        private void desmembrar_cam(object sender, RoutedEventArgs e)
        {
            Utilz.CAM.Desmembrar(Conexoes.Utilz.Selecao.SelecionarPasta());

        }

        private void abre_dbase(object sender, RoutedEventArgs e)
        {
            var sel = Utilz.Abrir_String("*", "");
            if(sel.Existe())
            {
                Conexoes.DBases.GetdbPerfil().GetPerfisSDS(true,sel);
            }
        }

        private void abre_dbase_sds(object sender, RoutedEventArgs e)
        {
            var sel = Utilz.Abrir_String("*", "");
            if (sel.Existe())
            {
                Conexoes.DBases.GetdbPerfil().GetPerfisSDS(true,sel);
            }
        }

        private void abre_dbase_tecnometal(object sender, RoutedEventArgs e)
        {
            var sel = Utilz.Abrir_String("DBF", "");
            if (sel.Existe())
            {
                Conexoes.DBases.GetdbPerfil().GetPerfisTecnoMetal(true, sel);
               var S = Conexoes.DBases.GetdbPerfil().GetPerfisTecnoMetal()[0].Tipo;


            }
        }

        private void consultar_materiais_epi(object sender, RoutedEventArgs e)
        {
            var t = DLM.sap.RfcsSAP.ConsultarMateriais("ZHIB");
        }

        private void exporta_dbprof(object sender, RoutedEventArgs e)
        {
            Conexoes.DBases.GetdbPerfil().ExportarDBTecnoMetal();
        }
    }
}
