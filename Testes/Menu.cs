using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Biblioteca_Daniel;
using System.IO;
using System.Diagnostics;
using DLM.vars;
using Conexoes;

namespace DLM.cam
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Cam S = new Cam(Application.StartupPath + @"\teste.cam");
            S.Perfil.Comprimento = 250;
            S.Perfil.Largura = 150;
            S.Cabecalho.Marca = "marca";
            S.Perfil.Espessura = 7.90;
            S.Furacoes.Liv1.Add(new Estrutura.Furo(25, -25, 14));
            S.Gerar();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string arquivo = @"C:\MBS-TESTE\ObraApresentacaoAlpha\OF1.dxf";
            //Funcoes.CriarCamMBS(@"C:\MBS-TESTE\ObraApresentacaoAlpha\OF1.dxf");
            //Funcoes.CriarCamMBS(@"C:\MBS-TESTE\ObraApresentacaoAlpha\WB2.dxf");

            //DxfDocument dxf = netDxf.DxfDocument.Load(arquivo);

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            PastaMBS.Text = Utilz.Selecao.SelecionarPasta("Selecione a Origem dos inputs");
            if (Conexoes.Utilz.Pergunta("Definir a mesma pasta como destino?"))
            {
                Destino.Text = PastaMBS.Text;
            }
            else
            {
                SetDest.PerformClick();
            }
            Atualizar();

        }

        private void Atualizar()
        {
            if (Directory.Exists(PastaMBS.Text))
            {
                List<string> Ships = Utilz.GetArquivos(PastaMBS.Text, "Ship**.txt");
                Ship.Items.Clear();
                Ships.Sort();
                foreach (string Shipa in Ships)
                {
                    this.Ship.Items.Add(Conexoes.Utilz.getNome(Shipa));
                }
                if (Ship.Items.Count > 0)
                {
                    Ship.SelectedIndex = 0;
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button4_Click(object sender, EventArgs e)
        {
            Atualizar();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Funcoes_ini.gravarcfg(DadosdaObra, PastaMBS.Text + CAMMBS.Config.CFGMBS);
            SetDadosObra();

            if (Directory.Exists(PastaMBS.Text) == false)
            {
                MessageBox.Show("Pasta de origem inválida - Não existe.");
                return;
            }
            if (Directory.Exists(Destino.Text) == false)
            {
                MessageBox.Show("Pasta de destino inválida - Não existe.");
                return;
            }



            CriaCAMSMBS(PastaMBS.Text, PastaMBS.Text + @"\" + Ship.Text + ".txt", PastaMBS.Text + @"\PLATE4.OUT", Destino.Text);

        }
        /// <summary>
        /// Lembrar de prencher as Vars da obra
        /// </summary>
        /// <param name="PastaDXFs">Pasta de origem dos DXFs</param>
        /// <param name="ShipArq">Endereço completo do arquivo Ship</param>
        /// <param name="Plate4Arq">Endereço completo do arquivo Plate4.out</param>
        /// <param name="Destino">Destino dos CAMs</param>
        private void CriaCAMSMBS(string PastaDXFs, string ShipArq, string Plate4Arq, string Destino, bool AdicionarMarca = true)
        {

            List<MBS_Marca> Marcas = FuncoesDLMCam.ListarMarcas(PastaDXFs, ShipArq, Plate4Arq);
            List<MBS_Pos> Croquis = new List<MBS_Pos>();
            List<MBS_Pos> CroquisMarcas = Marcas.SelectMany(x => x.Croquis).ToList();
            if (AdicionarMarca)
            {
                Croquis.AddRange(CroquisMarcas);
            }
            else
            {
                foreach (MBS_Pos Croq in CroquisMarcas)
                {
                    if (Croquis.Find(x => x.Nome == Croq.Nome) == null)
                    {
                        Croq.Marcas = CroquisMarcas.FindAll(x => x.Nome == Croq.Nome).Select(x => x.Marca).ToList();
                        Croquis.Add(Croq);
                    }
                }
            }
            foreach (MBS_Pos Croq in CroquisMarcas)
            {
                if (Croquis.Find(x => x.Nome == Croq.Nome) == null)
                {
                    Croq.Marcas = CroquisMarcas.FindAll(x => x.Nome == Croq.Nome).Select(x => x.Marca).ToList();
                    Croquis.Add(Croq);

                }
            }


            MBS_XML s = new MBS_XML(PastaDXFs, ShipArq);

            if (Croquis.Count + s.Tercas.Count > 0)
            {
                if (Conexoes.Utilz.Pergunta("Foram encontrados " + (Croquis.Count + s.Tercas.Count).ToString() + " deseja iniciar o processo?"))
                {
                    progressBar1.Maximum = Croquis.Count;
                    progressBar1.Value = 0;
                    List<Task> Tarefas = new List<Task>();

                    foreach (MBS_Pos P in Croquis)
                    {
                        progressBar1.Value = progressBar1.Value + 1;
                        if (File.Exists(P.CroquiDXF))
                        {
                            Tarefas.Add(Task.Factory.StartNew(() => FuncoesDLMCam.NovoCAMMBS(P, Destino, AdicionarMarca)));
                            //Funcoes.NovoCAMMBS(P, Destino, AdicionarMarca);
                        }
                    }
                    Task.WaitAll(Tarefas.ToArray());

                    List<Cam> Cams = new List<Cam>();

                    progressBar1.Value = 0;
                    progressBar1.Maximum = s.Tercas.Count();
                    foreach (MBS_XML.MBS_Purlin tc in s.Tercas)
                    {
                        progressBar1.Value = progressBar1.Value + 1;

                        if (tc.Descricao.ToLower().StartsWith("z") | tc.Descricao.ToLower().StartsWith("c"))
                        {
                            Z_Purlin Z = new Z_Purlin(tc.Descricao, tc.Altura, tc.Largura, tc.Espessura, tc.Aba, tc.Comprimento);
                            Cam cm = new Cam(Destino + @"\" + tc.Nome + ".CAM", Z);
                            cm.Furacoes.Liv1.AddRange(tc.Furos);
                            cm.Cabecalho.Material = tc.Material;
                            Cams.Add(cm);
                            cm.Gerar();
                        }

                    }
                    //TecnoUtilz.Funcoes.GerarSAP(Cams, Destino + @"\SAP - RME.XLSX");


                    MessageBox.Show("Cams gerados!");

                }
            }
            else
            {
                MessageBox.Show("Não há nenhum croqui referente ao " + ShipArq + " na pasta " + PastaDXFs);
            }
        }

        private void SetDadosObra()
        {
            DLM.vars.CAMVars.NomeObra = ObraMBS.Text;
            DLM.vars.CAMVars.Cliente = ClienteMBS.Text;
            DLM.vars.CAMVars.Lugar = LocalMBS.Text;
            DLM.vars.CAMVars.Pedido = PedidoMBS.Text;
            DLM.vars.CAMVars.Etapa = PedidoMBS.Text;
            DLM.vars.CAMVars.Equipe = EquipeMBS.Text;
            DLM.vars.CAMVars.Rev = RevisaolMBS.Text;
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dxfs = new PacoteDXF(GridDXFCAM, propertyGrid1);
            Funcoes_ini.lercfg(NC1Dirs, CAMMBS.Config.CFG);
            //tabControl1.TabPages.Remove(tabPage1);
            //tabControl1.TabPages.Remove(tabPage3);
            this.Text = Application.ProductName;
            Funcoes_ini.lercfg(Pastas, CAMMBS.Config.CFG);

            Atualizar();
            SetDadosObra();

            if (Directory.Exists(NC1Origem.Text))
            {
                ArquivosNC1 = Arquivo_Pasta.lista(NC1Origem.Text, "*.nc1");
                StatusNC1.Text = ArquivosNC1.Count.ToString() + " arquivos NC1 na pasta selecionada.";
                this.Update();
            }

            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            pacoteCams = new PacoteCAM(PropCams, GridCamCTF);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Funcoes_ini.gravarcfg(Pastas, CAMMBS.Config.CFG);

            Funcoes_ini.gravarcfg(NC1Dirs, CAMMBS.Config.CFG);

            Funcoes_ini.gravarcfg(Destino, CAMMBS.Config.CFG);

            Funcoes_ini.gravarcfg(DadosdaObra, PastaMBS.Text + CAMMBS.Config.CFGMBS);
        }

        private void textBox7_KeyPress(object sender, KeyPressEventArgs e)
        {
            Texto.verifica(e, this, "ABCDEFGHIJKLMNOPQRSTUVXYZ _ÃÕÁÉÍÓÚÇ0123456789-");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Arquivo_Pasta.pasta("Selecione o destino dos CAMS", Destino);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE.CAM";
            Cam S = new Cam(Arq, 50, 100, 6.35);
            S.Dobras.Liv1.Add(new Estrutura.Dobra((double)numericUpDown1.Value, (double)numericUpDown2.Value, S, checkBox1.Checked));
            S.Gerar();

            Process.Start(Arq);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";

            Cam s = new Cam(Arq, new I_Soldado(5500, 550, 175, 12.7, 6.35));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(150, 50, 21));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(150, -20, 21));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(350, -150, 21, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(150, -38, 21));

            s.Furacoes.Liv3.Add(new Estrutura.Furo(150, 38, 21));
            s.Furacoes.Liv3.Add(new Estrutura.Furo(150, -38, 21));

            //s.Recorte.REC_DIAG_ESQ(100, 0);

            //s.Recorte.SUPDIR_RETANGULAR_MESAINCLINADA(200, 200, 100,100);
            //s.Recorte.INFDIR_RETANGULAR_MESAINCLINADA(200, 200, 100,100);
            //s.Recorte.SUPESQ_RETANGULAR_MESAINCLINADA(200, 200, 100,100);
            //s.Recorte.INFESQ_RETANGULAR_MESAINCLINADA(200, 200, 100,100);

            //s.Recorte.REC_DIAG_DIR(100, 0);

            //s.Recorte.SUPDIR_RETANGULAR_INCLINADO(250, 150, 50);
            //s.Recorte.INFDIR_RETANGULAR_MESAINCLINADA(250, 0, 50);
            //s.Recorte.REC_DIAG_DIR(0, 200);


            //s.Recorte.SUPESQ_RETANGULAR_INCLINADO(250, 150, 50);
            //s.Recorte.INFESQ_RETANGULAR_MESAINCLINADA(250, 0, 50);
            //s.Recorte.REC_DIAG_ESQ(0, 200);


            //s.ContraFlecha = 40;
            s.GerareVisualizar();

        }

        private void button7_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            Cam s = new Cam(Arq, new Wlam(2550, 101, 350, 5.1, 5.7, 9.8, "W310X21 A572"));
            s.Trabalhos.Recortes.Add(new Recorte(100, Origem.InfEsq, TipoRecorte.DiagonalMesaTotal));
            //s.Recortes.Add(new Recorte(100, Origem.InfDir, TipoRecorte.DiagonalVertical));
            //s.Recortes.Add(new Recorte(100, 35, Origem.InfEsq, TipoRecorte.Retangular));
            s.Trabalhos.Recortes.Add(new Recorte(120, 35, Origem.SupDir, TipoRecorte.Retangular));
            //s.Recortes.Add(new Recorte(95, 15, Origem.InfDir, TipoRecorte.Retangular));
            s.Furacoes.Add(150, 21, 50, Mesa_Lado.Ambas);
            s.Furacoes.Add(250, 21, 21, 50, Mesa_Lado.Ambas);

            s.GerareVisualizar();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            Cam s = new Cam(Arq, 5000, 150, 6.35);
            s.Furacoes.Add(50, -50, 21, 21, 90);
            s.ChapaGirar();
            s.GerareVisualizar();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            Cam s = new Cam(Arq, new L_Laminado(1500, 50, 6.35, 2.25, "L 50X6.35"));
            s.Trabalhos.Recortes.Add(new Recorte(70, 35, Origem.Liv1Esq));
            s.Trabalhos.Recortes.Add(new Recorte(65, 30, Origem.Liv1Dir));

            s.Trabalhos.Recortes.Add(new Recorte(55, 20, Origem.Liv2Esq));
            s.Trabalhos.Recortes.Add(new Recorte(60, 25, Origem.Liv2Dir));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(100, -25, 21, 21, 90));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(100, -25, 21, 21, 90));

            //s.Recortes.Add(new Recorte(100, 110, 20, 10, Origem.Liv1Esq));
            //s.Recortes.Add(new Recorte(100, 110, 20, 10, Origem.Liv1Dir));

            //s.Recortes.Add(new Recorte(100, 110, 20, 10, Origem.Liv2Esq));
            //s.Recortes.Add(new Recorte(100, 110, 20, 10, Origem.Liv2Dir));

            //s.Recortes.Add(new Recorte(100,120,20,10,Origem.Liv2Esq));
            //s.Recortes.Add(new Recorte(100,120,20,10,Origem.Liv2Dir));

            s.GerareVisualizar();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            Estrutura.Propriedades x = new Estrutura.Propriedades();
            x.Volume = 100 * 100 * 6.35;
            MessageBox.Show(x.Peso.ToString());
            //MessageBox.Show("Raio:" + Funcoes.Arco.Raio(200, 2500).ToString());
            //MessageBox.Show("Comprimento:" + Funcoes.Arco.Comprimento(200, 2500).ToString());
            //MessageBox.Show(Funcoes.Arco.DifY(2500, 200, 1250).ToString());
        }

        private void button11_Click(object sender, EventArgs e)
        {
            Process.Start(Application.StartupPath);
        }

        private void button12_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            Cam s = new Cam(Arq, new I_Soldado(4500, 450, 175, 9.5, 6.35));

            s.Trabalhos.SUPDIR_RETANGULAR(150, 25);
            s.Trabalhos.SUPESQ_RETANGULAR(150, 25);

            s.Trabalhos.INFDIR_RETANGULAR(150, 25);
            s.Trabalhos.INFESQ_RETANGULAR(150, 25);



            s.Furacoes.Liv2.Add(new Estrutura.Furo(150, 38, 21));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(150, -38, 21));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(350, -150, 21, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(150, -38, 21));

            s.Furacoes.Liv3.Add(new Estrutura.Furo(150, 38, 21));
            s.Furacoes.Liv3.Add(new Estrutura.Furo(150, -38, 21));
            s.ContraFlecha = 55;
            s.Gerar();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TESTE2.CAM";
            Cam s = new Cam(Arq, new I_Soldado((double)Comp.Value, 450, 175, 9.5, 6.35));
            s.Furacoes.Add(250, 21, 0, 76, Mesa_Lado.Ambas);
            s.Furacoes.Add(300, 21, 0, 76, Mesa_Lado.Ambas);
            s.Furacoes.Add(350, 21, 0, 76, Mesa_Lado.Ambas);
            s.Furacoes.Add(400, 21, 0, 76, Mesa_Lado.Ambas);
            s.Furacoes.Add(450, 21, 0, 76, Mesa_Lado.Ambas);

            s.Furacoes.Liv1.Add(new Estrutura.Furo(550, -150, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(550, -200, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(550, -250, 21));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(600, -150, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(600, -200, 21));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(600, -250, 21));



            s.Furacoes.Liv1.Add(new Estrutura.Furo(1550, -150, 14));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(1550, -200, 14));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(1550, -250, 14));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(1700, -150, 14));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(1700, -200, 14));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(1700, -250, 14));


            //retangular
            //s.Recortes.Add(new Recorte(150, 200, 100, 100, Origem.InfEsq));
            //s.Recortes.Add(new Recorte(150, 200, 100, 100, Origem.InfDir));
            //s.Recortes.Add(new Recorte(150, 200, 100, 100, Origem.SupEsq));
            //s.Recortes.Add(new Recorte(150, 200, 100, 100, Origem.SupDir));


            //chanfrado
            //s.Recortes.Add(new Recorte(150, 150, 50, 0, Origem.SupEsq));




            //s.Recortes.Add(new Recorte(150,Origem.InfDir));

            //s.Recortes.Add(new Recorte(150, 50, Origem.InfEsq));
            //s.Recortes.Add(new Recorte(150, 50, Origem.InfDir));
            //s.Recortes.Add(new Recorte(150, 50, Origem.SupEsq));
            //s.Recortes.Add(new Recorte(150, 50, Origem.SupDir));

            s.ContraFlecha = (double)ValorCTF.Value;

            s.Gerar();
            //MessageBox.Show(s.Info.Area.ToString());
            //foreach (Cam sc in s.Filhos.Buffer)
            //{
            //    MessageBox.Show(sc.Info.Peso.ToString());
            //}

        }

        private void button14_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "CAIXAO1.CAM";
            //Perfil.VigaCaixao Cx = new Perfil.VigaCaixao(5000, 400, new Chapa(175, 12.70), new Chapa(150, 25.4), 6.35, 135);
            //Perfil.VigaCaixao Cx = new Perfil.VigaCaixao(5000, 250, 250, 7.90, 4.75, 150);
            Caixao Cx = new Caixao(5000, 450, new Chapa(200, 6.35), new Chapa(250, 7.9), 12.7, 150);
            Cam t = new Cam(Arq, Cx);
            //t.Furacoes.Add(1500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(2500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(3500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(4500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Liv1.Add(new Estrutura.Furo(2500, -100, 21));
            t.GerareVisualizar();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "CAIXAO1.CAM";
            //Perfil.VigaCaixao Cx = new Perfil.VigaCaixao(5000, 400, new Chapa(175, 12.70), new Chapa(150, 25.4), 6.35, 135);
            Caixao Cx = new Caixao(5000, 400, 175, 7.90, 4.75, 135);
            Cam t = new Cam(Arq, Cx);
            //t.Furacoes.Add(1500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(2500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(3500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Add(4500, 21, 76, Face.AmbasMesas);
            //t.Furacoes.Liv1.Add(new Estrutura.Furo(2500, -100, 21));
            t.Formato.LIV1.AddRecorteInterno(100, 100, new Estrutura.Liv(500, -50));
            t.Formato.LIV1.AddRecorteInterno(100, 125, new Estrutura.Liv(1500, -60));
            t.Formato.LIV1.AddRecorteInterno(100, 150, new Estrutura.Liv(2000, -70));
            t.Formato.LIV1.AddRecorteInterno(100, 175, new Estrutura.Liv(2500, -80));
            t.Formato.LIV1.AddRecorteInterno(100, 200, new Estrutura.Liv(3000, -90));
            t.GerareVisualizar();

            if (t.Formato.LIV1.FaceSemBordas.Nao_Retangular)
            {

            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            //NC1 x = new NC1(@"D:\Desktop\Nova pasta\DSTV\DSTV\CHAPAS\374.nc1", @"D:\Desktop\Nova pasta\DSTV\DSTV\CHAPAS\");
        }
        private static List<string> ArquivosNC1 = new List<string>();
        private void button15_Click(object sender, EventArgs e)
        {
        retentar:
            ArquivosNC1.Clear();
            NC1Origem.Text = Utilz.Selecao.SelecionarPasta("Selecione a Origem dos arquivos NC1");

            ArquivosNC1 = Utilz.GetArquivos(NC1Origem.Text, "*.nc1");

            if (ArquivosNC1.Count > 0)
            {
                StatusNC1.Text = ArquivosNC1.Count.ToString() + " arquivos NC1 na pasta selecionada.";
                this.Update();
                if (Conexoes.Utilz.Pergunta("Definir a mesma pasta como destino?"))
                {

                    NC1Dest.Text = NC1Origem.Text;
                }
                else
                {
                    btdest.PerformClick();
                }
            }
            else
            {
                if (Conexoes.Utilz.Pergunta("Não há arquivos NC1 na pasta selecionada. Deseja escolher outra pasta?"))
                {
                    goto retentar;
                }
            }

        }

        private void btdest_Click(object sender, EventArgs e)
        {
            NC1Dest.Text = Utilz.Selecao.SelecionarPasta("Selecione o destino dos arquivos CAM");

        }

        private void button17_Click(object sender, EventArgs e)
        {


            if (ArquivosNC1.Count > 0 && Directory.Exists(NC1Dest.Text))
            {
                double contra_flecha = Utilz.Double(Conexoes.Utilz.Prompt("Digite o contra-flecha", "", "30"));
                List<NC1> NC1s = new List<NC1>();
                foreach (string ArqNC1 in ArquivosNC1)
                {

                    var s = new NC1(ArqNC1);
                    s.ContraFlecha = contra_flecha;
                    NC1s.Add(s);

                }
                if (Conexoes.Utilz.Pergunta("Tem certeza que deseja continuar?"))
                {
                    foreach (DLM.cam.NC1 nc in NC1s)
                    {
                        nc.GetCAM(true, NC1Dest.Text + @"\");
                        if (nc.Status == "")
                        {
                           richTextBox1.AppendText(Environment.NewLine + nc.Posicao + " gerado");
                        }
                        else
                        {
                           richTextBox1.AppendText(Environment.NewLine + nc.Posicao + "==>" + nc.Status);

                        }
                    }


                    MessageBox.Show("Finalizado!");
                    Utilz.Abrir(NC1Dest.Text);
                }
            }
                //richTextBox1.Clear();
                //if (ArquivosNC1.Count > 0 && Directory.Exists(NC1Dest.Text))
                //{
                //    ProgNC1.Maximum = ArquivosNC1.Count;
                //    ProgNC1.Value = 0;
                //    List<NC1> NC1s = new List<NC1>();
                //    foreach (string ArqNC1 in ArquivosNC1)
                //    {

                //        NC1s.Add(new NC1(ArqNC1, NC1Dest.Text + @"\"));
                //    }
                //    foreach (NC1 n in NC1s)
                //    {
                //        //n.ContraFlecha = 25;
                //        ProgNC1.Value = ProgNC1.Value + 1;
                //        StatusNC1.Text = "Gerando NC1 do arquivo " + n.Posicao;
                //        this.Update();
                //        if (n.Status != "")
                //        {
                //            richTextBox1.AppendText(Environment.NewLine + n.Status);
                //        }
                //        else
                //        {
                //            n.Gerar();
                //            if (n.Status != "")
                //            {
                //                richTextBox1.AppendText(Environment.NewLine + n.Status);

                //            }
                //            else
                //            {
                //                if (File.Exists(n.Destino))
                //                {
                //                    richTextBox1.AppendText(Environment.NewLine + "Cam " + n.Posicao + " gerado.");
                //                }
                //            }


                //        }
                //        richTextBox1.ScrollToCaret();

                //    }
                //    StatusNC1.Text = "Finalizado!";
                //    this.Update();
                //}
            }

        private void sobreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Application.ProductName + "\n" +
                "Powered By Daniel Lins Maciel" + "\ndan.8p@hotmail.com");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "TRAPEZIO.CAM";
            Geometrias.Trapezio TR = new Geometrias.Trapezio(200, 50, 50, 50);

            Cam S = new Cam(Arq, TR.Formato, 6.35);
            S.GerareVisualizar();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "CIRCULO.CAM";
            Geometrias.Circulo TR = new Geometrias.Circulo(150);

            Cam S = new Cam(Arq, TR, 6.35);
            S.GerareVisualizar();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            ReadCam x = new ReadCam(Arquivo_Pasta.abrir_string("cam", "Selecione um arquivo", ""));

            var s = x.GetSoldaLIV1().Count + x.GetSoldaLIV2().Count + x.GetSoldaLIV3().Count + x.GetSoldaLIV4().Count;
        }

        private void button20_Click(object sender, EventArgs e)
        {

            string Arq = CAMMBS.Config.Start + "TERCAZ.CAM";
            Z_Purlin z = new Z_Purlin("PERFIL PADRAO Z 216 - 1.55", 216, 64, 1.55, 15.6, 10674);
            //z.Corte = 360;
            Cam ter = new Cam(Arq, z);
            ter.Cabecalho.Material = "ZAR";



            ter.Furacoes.AddLiv1("H     14.00     32.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00     32.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    337.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    337.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    642.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    642.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   2775.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   2775.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   7899.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   7899.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10032.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10032.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10337.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10337.00    -89.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10642.00   -165.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10642.00    -89.00      0.00      8.00     90.00      0.00");

            ter.GerareVisualizar();

            //Funcoes.GerarSAP(new List<Cam> { ter }, Application.StartupPath + @"\SAP-RME.XLSX");
        }

        private void button21_Click(object sender, EventArgs e)
        {
            string Arq = CAMMBS.Config.Start + "tercac.CAM";
            C_Enrigecido z = new C_Enrigecido("PERFIL PADRAO C 165 - 1.55", 165, 64, 1.55, 24, 10674);

            Cam ter = new Cam(Arq, z);
            ter.Cabecalho.Material = "ZAR";
            ter.Furacoes.AddLiv1("H     14.00     32.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00     32.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    337.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    337.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    642.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00    642.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   2775.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   2775.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   7899.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00   7899.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10032.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10032.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10337.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10337.00    -38.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10642.00   -114.00      0.00      8.00     90.00      0.00");
            ter.Furacoes.AddLiv1("H     14.00  10642.00    -38.00      0.00      8.00     90.00      0.00");

            ter.GerareVisualizar();
        }

        private void button22_Click(object sender, EventArgs e)
        {

        }

        private void button22_Click_1(object sender, EventArgs e)
        {
            DLM.vars.CAM_Quebra.Compmax = 2500;
            DLM.vars.CAM_Quebra.TamSegmento = 100;


            Quebra xt = new Quebra(Arquivo_Pasta.abrir_string("cam", "Selecione um arquivo", ""));
            xt.Quebrar();

        }

        private void button23_Click(object sender, EventArgs e)
        {

        }
        private static PacoteDXF dxfs;
        private void button23_Click_1(object sender, EventArgs e)
        {
            string DXF = Arquivo_Pasta.abrir_string("dxf", "", "");
            if (DXF != null)
            {
                DXFtoCAM t = new DXFtoCAM(DXF, (string)Arquivo_Pasta.Nome_Pasta(DXF));
                t.Escala = 20;
                //t.Cam.Deformar(20);
                t.Gerar();
            }
        }

        private void pastaToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void LerPasta()
        {
            dxfs = new PacoteDXF(PastaDXF.Text, DXFDestino.Text, GridDXFCAM, propertyGrid1);
            CurrencyManager myCurrencyManager;
            myCurrencyManager = (CurrencyManager)this.BindingContext[GridDXFCAM.DataSource];
            myCurrencyManager.Refresh();

        }

        private void DXFDestino_ClientSizeChanged(object sender, EventArgs e)
        {

        }

        private void DXFDestino_Click(object sender, EventArgs e)
        {

        }

        private void DXFDestino_TextChanged(object sender, EventArgs e)
        {

        }

        private void atualizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LerPasta();
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Arquivo_Pasta.pasta("Selecione a Origem dos DXFs", PastaDXF);
            if (Conexoes.Utilz.Pergunta("Definir a mesma pasta de origem para destino?"))
            {

                DXFDestino.Text = PastaDXF.Text;
            }
            else
            {
                Arquivo_Pasta.pasta("Selecione o destino dos DXFs", DXFDestino);
                dxfs.Destino = DXFDestino.Text;
            }
            LerPasta();
        }

        private void definirDestinoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Arquivo_Pasta.pasta("Selecione o destino dos DXFs", DXFDestino);
            dxfs.Destino = DXFDestino.Text;
        }

        private void gerarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Conexoes.Utilz.Pergunta("Tem Certeza?"))
            {
                foreach (DXFtoCAM c in dxfs.Cams)
                {
                    c.Gerar();
                }
            }

        }
        private PacoteCAM pacoteCams = new PacoteCAM();

        private void pastaToolStripMenuItem1_Click(object sender, EventArgs e)
        {


        }

        private void adicionarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Filter = "Almas CAM|*.cam";
            d.Multiselect = true;
            DialogResult dr = d.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {

                pacoteCams.Add(d.FileNames.ToList());
                // Read the files
                //foreach (String file in d.FileNames)
                //{
                //    // Create a PictureBox.
                //    try
                //    {


                //    }
                //    catch (Exception ex)
                //    {

                //    }

                //}


            }

            CurrencyManager myCurrencyManager;
            myCurrencyManager = (CurrencyManager)this.BindingContext[GridCamCTF.DataSource];
            myCurrencyManager.Refresh();
        }

        private void gerarToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            progcam.Value = 0;
            progcam.Maximum = pacoteCams.Cams.Count();
            if (Conexoes.Utilz.Pergunta("Tem certeza?"))
            {
                foreach (CamCTF c in pacoteCams.Cams)
                {
                    c.Gerar();
                    progcam.Value = progcam.Value + 1;
                }
            }
        }

        private void removerToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void limparListaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pacoteCams.Cams.Clear();
            pacoteCams.Update();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            Cam s = new Cam(CAMMBS.Config.Start + "BARRA.CAM", new Barra_Redonda("FERRO 1 A36", 25.4, 600, 100, 50));
            s.GerareVisualizar();
        }

        private void button25_Click(object sender, EventArgs e)
        {
            Cam s = new Cam(CAMMBS.Config.Start + "BARRA.CAM", new Barra_Chata("Barra Chata 2", 4800, 50.8, 4.76));
            s.GerareVisualizar();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            Cam s = new Cam(CAMMBS.Config.Start + "PERFILU.CAM", new U_Dobrado("U100X46X3.00", 100, 46, 3, 5364));
            s.Furacoes.Liv1.Add(new Estrutura.Furo(250, -50, 21));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(250, -23, 21));
            s.Furacoes.Liv3.Add(new Estrutura.Furo(250, -23, 21));

            s.Furacoes.Liv1.Add(new Estrutura.Furo(1250, -50, 21));
            s.Furacoes.Liv2.Add(new Estrutura.Furo(1250, -23, 21));
            s.Furacoes.Liv3.Add(new Estrutura.Furo(1250, -23, 21));
            s.GerareVisualizar();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            Cam s = new Cam(CAMMBS.Config.Start + "PERFILU.CAM", new Diagonal_Medajoist(new U_Dobrado(50, 32, 1.95, 500), new Recorte_Diagonal(62, 18, 15)));
            s.Furacoes.Add(23, 14, true);
            //s.Furacoes.Add(25, -25, 14);
            //s.Furacoes.Add(475, -25, 14);
            s.GerareVisualizar();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            L_Dobrado pp = new L_Dobrado("L76X76X6.35", 76, 76, 6.35, 1500, 76, 0);
            Cam PS = new Cam(CAMMBS.Config.Start + "PERFIL_L.CAM", pp);
            PS.GerareVisualizar();
        }

        private void button29_Click(object sender, EventArgs e)
        {
            var perfil = Utilz.Abrir_String("profiles.lis", "Selecione a dbase", "Selecione a dbase");
            if (File.Exists(perfil))
            {
                dbTEKLA tk = new dbTEKLA(perfil);

                var pasta = Utilz.getPasta(perfil);

                var ss = pasta + "perfis.csv";

                var ls = tk.Perfis.Select(x => x.Type + ";" + x.SubType + ";" + x.Nome).ToList();
                Utilz.Arquivo.Gravar(ss, ls);

            }
        }

        private void button30_Click(object sender, EventArgs e)
        {

        }

        private void button30_Click_1(object sender, EventArgs e)
        {
            Cam p = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA.CAM", new L_Dobrado("L 200x100x6.35", 200, 100, 6.35, 500, 50, 50));

            p.Furacoes.AddLiv2(50, -75, 14, 8);
            p.Furacoes.AddLiv2(p.Perfil.Comprimento - 50, -75, 14, 8);
            //p.GerareVisualizar();
            List<Face> f1;
            List<Face> f2;

            var ss = p.Formato.GetPlanificada(out f1, out f2);
            if (ss.Liv.Count > 0)
            {
                foreach (var s in f1)
                {
                    s.Cor = System.Windows.Media.Brushes.Red;
                }
                foreach (var s in f2)
                {
                    s.Cor = System.Windows.Media.Brushes.Blue;
                }

                var lista = new List<Face>();
                //lista.Add(ss);
                lista.AddRange(f1);

                lista.AddRange(f2);
                DLM.helix.Renders.View.Faces(lista);
                //Cam p2 = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA_U.CAM", ss);
                //p2.GerareVisualizar();
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            Cam p = new Cam(CAMMBS.Config.Start + "U_PLANIFICADA.CAM", new U_Dobrado("U 200x100x6.35", 200, 100, 6.35, 500));

            p.Furacoes.AddLiv1(50, -75, 14, 8);
            p.Furacoes.AddLiv1(p.Perfil.Comprimento - 50, -75, 14, 8);
            //p.GerareVisualizar();
            List<Face> originais;
            List<Face> gerado;

            var ss = p.Formato.GetPlanificada(out originais, out gerado);
            if (ss.Liv.Count > 0)
            {
                foreach (var s in originais)
                {
                    s.Cor = System.Windows.Media.Brushes.Red;
                }
                foreach (var s in gerado)
                {
                    s.Cor = System.Windows.Media.Brushes.Blue;
                }

                var lista = new List<Face>();
                //lista.Add(ss);
                lista.AddRange(gerado);
                lista.AddRange(originais);

                DLM.helix.Renders.View.Faces(lista);
                //Cam p2 = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA_U.CAM", ss);
                //p2.GerareVisualizar();
            }
        }

        private void button32_Click(object sender, EventArgs e)
        {
            Cam p = new Cam(CAMMBS.Config.Start + "Z_PLANIFICADA.CAM", new Z_Dobrado("Z 200x100x6.35", 200, 100, 6.35, 500));

            p.Furacoes.AddLiv1(50, -75, 14, 8);
            p.Furacoes.AddLiv1(p.Perfil.Comprimento - 50, -75, 14, 8);
            //p.GerareVisualizar();
            List<Face> originais;
            List<Face> gerado;

            var ss = p.Formato.GetPlanificada(out originais, out gerado);
            if (ss.Liv.Count > 0)
            {
                foreach (var s in originais)
                {
                    s.Cor = System.Windows.Media.Brushes.Red;
                }
                foreach (var s in gerado)
                {
                    s.Cor = System.Windows.Media.Brushes.Blue;
                }

                var lista = new List<Face>();
                //lista.Add(ss);
                lista.AddRange(gerado);
                lista.AddRange(originais);

                DLM.helix.Renders.View.Faces(lista);
                //Cam p2 = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA_U.CAM", ss);
                //p2.GerareVisualizar();
            }

        }

        private void button33_Click(object sender, EventArgs e)
        {
            Cam p = new Cam(CAMMBS.Config.Start + "ZENR_PLANIFICADA.CAM", new Z_Purlin("Z 200x100x6.35", 200, 100, 6.35,25, 500));

            p.Furacoes.AddLiv1(50, -75, 14, 8);
            p.Furacoes.AddLiv1(p.Perfil.Comprimento - 50, -75, 14, 8);
            //p.GerareVisualizar();
            List<Face> originais;
            List<Face> gerado;

            var ss = p.Formato.GetPlanificada(out originais, out gerado);
            if (ss.Liv.Count > 0)
            {
                foreach (var s in originais)
                {
                    s.Cor = System.Windows.Media.Brushes.Red;
                }
                foreach (var s in gerado)
                {
                    s.Cor = System.Windows.Media.Brushes.Blue;
                }

                var lista = new List<Face>();
                //lista.Add(ss);
                lista.AddRange(gerado);
                lista.AddRange(originais);

                DLM.helix.Renders.View.Faces(lista);
                //Cam p2 = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA_U.CAM", ss);
                //p2.GerareVisualizar();
            }
        }

        private void button34_Click(object sender, EventArgs e)
        {
            Cam p = new Cam(CAMMBS.Config.Start + "CENR_PLANIFICADA.CAM", new C_Enrigecido("C 200x100x6.35", 200, 100, 6.35, 25, 500));

            p.Furacoes.AddLiv1(50, -75, 14, 8);
            p.Furacoes.AddLiv1(p.Perfil.Comprimento - 50, -75, 14, 8);
            //p.GerareVisualizar();
            List<Face> originais;
            List<Face> gerado;

            var ss = p.Formato.GetPlanificada(out originais, out gerado);
            if (ss.Liv.Count > 0)
            {
                foreach (var s in originais)
                {
                    s.Cor = System.Windows.Media.Brushes.Red;
                }
                foreach (var s in gerado)
                {
                    s.Cor = System.Windows.Media.Brushes.Blue;
                }

                var lista = new List<Face>();
                //lista.Add(ss);
                lista.AddRange(gerado);
                lista.AddRange(originais);

                DLM.helix.Renders.View.Faces(lista);
                //Cam p2 = new Cam(CAMMBS.Config.Start + "CANTONEIRA_PLANIFICADA_U.CAM", ss);
                //p2.GerareVisualizar();
            }
        }

        private void button35_Click(object sender, EventArgs e)
        {
            var arq = Utilz.Abrir_String("CAM","");
            if(arq!=null)
            {
                var read = new ReadCam(arq);

                var cam = new Cam(read, read.Pasta);

                List<Face> originais;
                List<Face> gerado;

                var ss = cam.Formato.GetPlanificada(out originais, out gerado);
                if (ss.Liv.Count > 0)
                {
                    foreach (var s in originais)
                    {
                        s.Cor = System.Windows.Media.Brushes.Red;
                    }
                    foreach (var s in gerado)
                    {
                        s.Cor = System.Windows.Media.Brushes.Blue;
                    }

                    var lista = new List<Face>();
                    //lista.Add(ss);
                    lista.AddRange(originais);
                    lista.AddRange(gerado);

                    DLM.helix.Renders.View.Faces(lista);
                    if(gerado.Count==1)
                    {
                        var pasta = Utilz.CriarPasta(cam.Pasta, "PLANIFICADO");
                        Cam p2 = new Cam(pasta + cam.Nome+ "_U.CAM", ss);
                        p2.GerareVisualizar();
                    }

                }
            }
        }

        private void button36_Click(object sender, EventArgs e)
        {
            var sels = Utilz.Abrir_Strings("Selecione", new List<string> { "CAM" });

            if(sels.Count>0)
            {
                var reads = sels.Select(x => new ReadCam(x));

                foreach (var s in reads)
                {
                    var subs = s.GetCam().Desmembrar();
                    foreach(var p in subs)
                    {
                        p.Gerar();
                    }
                }
            }
        }

        private void ver_desenho(object sender, EventArgs e)
        {
            ReadCam x = new ReadCam(Arquivo_Pasta.abrir_string("cam", "Selecione um arquivo", ""));

            var s = x.GetSoldaLIV1().Count + x.GetSoldaLIV2().Count + x.GetSoldaLIV3().Count + x.GetSoldaLIV4().Count;



            DLM.helix.Renders.View.Cam(x);
        }
    }
}
