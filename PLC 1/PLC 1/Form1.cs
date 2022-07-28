using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Modbus.Device;
using System.Net.Sockets;
using System.IO.IsolatedStorage;
using System.IO;


namespace PLC_1
{
    public partial class Form1 : Form
    {
        
        ModbusIpMaster maestroPLC;
        bool[] coils = null;
        ushort[] holdings=null;
        bool despachando = false;
        string inicioDeTransaccion;
        string finDeTransaccion=null;
        

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            
            EstadoPLC();
            timer1.Enabled = true;
            inicio();
            

        }

        //void GuardarDatos()
        //{
        //    IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null);

        //    if (isoStore.FileExists("TestStore.txt"))
        //    {
        //        programaCerrado = true;
        //    }
        //    else
        //    {
        //        using (IsolatedStorageFileStream isoStream = new IsolatedStorageFileStream("TestStore.txt", FileMode.CreateNew, isoStore))
        //        {
        //            using (StreamWriter writer = new StreamWriter(isoStream))
        //            {
        //                writer.WriteLine("Hello Isolated Storage");
        //                Console.WriteLine("You have written to the file.");
        //            }
        //            programaCerrado = false;
        //        }
        //        //programaCerrado = false;
        //    }
        //    //if (s)

        //}

        public async void EstadoPLC()
        {
            
                try
                {
                    TcpClient clientePLC = new TcpClient();

                    await clientePLC.ConnectAsync("192.168.1.10", 502);

                    maestroPLC = ModbusIpMaster.CreateIp(clientePLC);

                    label2.Text = "PLC conectado";
                    
            }

                catch (Exception ex)
                {
                    label2.Text = "Error en la conexion";
                }
              
            
            
            
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            try
            {
                var holdings = await maestroPLC.ReadHoldingRegistersAsync(1, 0, 8);
                var tag = holdings[0];
                label7.Text = tag.ToString();
                maestroPLC.WriteMultipleCoils(5, new bool[] { true });
            }
            catch
            {
                EstadoPLC();
            }
        }

       
        private async void timer1_Tick_1(object sender, EventArgs e)
        {
            

            if (maestroPLC== null)
            {
                return;
            }

            try
            {
                var resultadoDeCoils = await maestroPLC.ReadCoilsAsync(1, 0, 6);
                var resultadoDeHoldings = await maestroPLC.ReadHoldingRegistersAsync(1, 0, 8);
                coils = resultadoDeCoils;
                holdings = resultadoDeHoldings;

            }
            catch
            {
                EstadoPLC();

            }
            //try
            //{
            //    maestroPLC.WriteMultipleCoils(4, new bool[] { false });
            //    maestroPLC.WriteMultipleCoils(5, new bool[] { true });
            //    maestroPLC.WriteMultipleCoils(3, new bool[] { true });
            //}
            //catch
            //{
            //    EstadoPLC();
            //}

            //if (coils[4] == true)
            //{
            //    panel4.BackColor = Color.White;
            //}
            //else
            //{
            //    panel4.BackColor = Color.Red;
            //}


            if (coils[0] == true)
            {
                label1.Text = "Vehiculo Presente";
                panel1.BackColor = Color.Green;

                if (coils[4] == true){
                  
                    try
                    {

                        maestroPLC.WriteMultipleCoils(4, new bool[] { false });
                    }
                    catch
                    {
                        EstadoPLC();
                    }

                    using (baseEn db = new baseEn())
                    {
                        inicio otabla_inicio = new inicio();
                        otabla_inicio.inicio1 = Convert.ToString(DateTime.Now);
                        db.inicio.Add(otabla_inicio);
                        db.SaveChanges();
                        var xd = db.inicio.OrderByDescending(x => x.di).FirstOrDefault();
                        var lst = db.inicio;
                        inicioDeTransaccion = xd.inicio1; //.Max(x => x.ID_C); //Convert.ToString((from inicio in db.inicio select inicio).ToList());

                    }


                }


                if (coils[1] == true)
                {
                    if (coils[5] == false)
                    {
                        label4.Text = "Pidiendo autorizacion...";
                        button1.Enabled = true;
                    }
                    else
                    {
                        label4.Text = "Autorizado";
                        panel2.BackColor = Color.Green;
                        button1.Enabled = false;
                    }

                    if (coils[2] == true && coils[3] == true)
                    {


                        label5.Text = "Despachando";
                        panel3.BackColor = Color.Green;
                        despachando = true;

                    }



                }
                else
                {
                    label4.Text = "No se ha pediod autorizacion";
                    panel2.BackColor = Color.Red;
                    maestroPLC.WriteMultipleCoils(5, new bool[] { false });
                    button1.Enabled = false;
                }
            }
            else
            {

                if (despachando != true)
                {
                    inicio();
                    maestroPLC.WriteMultipleCoils(0, new bool[] { true, false, false, false });
                    maestroPLC.WriteMultipleCoils(5, new bool[] { false });
                }
            }

            if (despachando == true && coils[3] == false)
            {

                label5.Text = "Carga terminada";
                panel3.BackColor = Color.Yellow;
                button1.Enabled = false;


                if (coils[0] == false)
                {
                    var litros = Convert.ToString(holdings[1]);
                    var mililitros = Convert.ToString(holdings[2]);
                    var litrosTotales = Convert.ToString(holdings[6]);
                    var mililitrosTotales = Convert.ToString(holdings[7]);
                    finDeTransaccion = Convert.ToString(DateTime.Now);


                    using (be db = new be())
                    {
                        gasolina otabla_gas = new gasolina();
                        otabla_gas.Tag = Convert.ToInt32(holdings[0]);
                        otabla_gas.Litros = litros + "." + mililitros;
                        otabla_gas.Densidad = (double)(int?)Convert.ToSingle(holdings[3]);
                        otabla_gas.Temperatura = (double)(int?)Convert.ToSingle(holdings[4]);
                        otabla_gas.Flujo = (double)(int?)Convert.ToSingle(holdings[5]);
                        otabla_gas.Totalizado = litrosTotales + "." + mililitrosTotales;
                        otabla_gas.Fecha_de__inicio = inicioDeTransaccion;
                        otabla_gas.Fecha_de_terminio = finDeTransaccion;
                        db.gasolina.Add(otabla_gas);
                        db.SaveChanges();
                        var lst = db.gasolina;
                    }
                    maestroPLC.WriteMultipleCoils(2, new bool[] { false });
                    maestroPLC.WriteMultipleCoils(5, new bool[] { false });
                    inicio();
                    despachando = false;
                    inicioDeTransaccion = null;
                    maestroPLC.WriteMultipleCoils(4, new bool[] { true });


                }
            }

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //apagados++;
            
            //using (baseEn db = new baseEn())
            //{
            //    inicio otabla_inicio = new inicio();
            //    otabla_inicio.inicio1 = inicioDeTransaccion;
            //    otabla_inicio.apagado = apagados;
            //    db.inicio.Add(otabla_inicio);
            //    db.SaveChanges();
            //    var lst = db.inicio;


            //}
        }

        private void inicio()
        {
            button1.Enabled = false;
            label1.Text = "En espera de un vehiculo";
            label3.Text = "Estado de la conexion: ";
            label4.Text = "No se ha pedido autorizacion";
            label5.Text = "No despachando";
            label6.Text = "Tag del vehiculo";
            label7.Text = "No leida aun";
            panel1.BackColor = Color.Red;
            panel2.BackColor = Color.Red;
            panel3.BackColor = Color.Red;
            
        }
    }

   

}
