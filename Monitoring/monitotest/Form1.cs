﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using monitotest.Services;

namespace monitotest
{
    public partial class Form1 : Form
    {
        delegate void StringArgReturningVoidDelegate(string text, TextBox textbox); // Delegate enable asynchronous call for setting txt property on the textBox9
        public TcpClient client;
        private TcpListener tcpListener;
        private Thread listenThread;
        List<string> address = new List<string>();
        List<string> ticketNumber = new List<string>();
        List<string> ticketVoted = new List<string>();
        string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\EVA";
        int actualyear = DateTime.Now.Year;
        Dictionary<string, List<string>> votes;

        public Form1()
        {
            InitializeComponent();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            this.textBox1.AutoSize = true;
            this.textBox2.AutoSize = true;
            Task.Factory.StartNew(() => this.LoadUsers());
        }

        private void LoadUsers()
        {
            StreamReader sr = new StreamReader(this.path + "\\" + actualyear + "ticket.txt");
            string line;
            while((line = sr.ReadLine()) != null)
            {
                ticketNumber.Add(line);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = true;
            this.button2.Enabled = false;

            try
            {
                foreach (string element in this.address)
                {
                    Network.SendPacket("VOTEOFF", element);
                }
                if (!File.Exists(path + "\\" + actualyear + "log.txt"))
                {
                    FileStream fs = File.Create(path + "\\" + actualyear + "log.txt");
                    fs.Close();
                    fs.Dispose();
                }

                StreamWriter sw = new StreamWriter(path + "\\" + actualyear + "log.txt"); // Get the file where the log wi
                sw.WriteLine(textBox9.Text); // Copy the text in the log file when "Stop" button is pressed
                sw.Close(); // Close the file
                sw.Dispose(); // Release the memory used by the StreamWriter
                client.Close(); // Stop the local server
                this.CollectVote();
                //this.Close();
            }

            catch (NullReferenceException)
            {

            }


        }

        private void CollectVote()
        {
            foreach (var element in this.votes)
            {
                if (!File.Exists(path + "\\" + actualyear + element.Key + ".txt"))

                {
                    FileStream fs = File.Create(path + "\\" + actualyear + element.Key + ".txt");
                    fs.Close();
                    fs.Dispose();
                }
                StreamWriter sw = new StreamWriter(path + "\\" + actualyear + element.Key + ".txt");
                Dictionary<string, int> votes = new Dictionary<string, int>();
                foreach (string item in element.Value)
                {
                    if (!votes.ContainsKey(item))
                    {
                        votes.Add(item, 1);
                    }
                    else
                    {
                        int count = 0;
                        votes.TryGetValue(item, out count);
                        votes.Remove(item);
                        votes.Add(item, count + 1);
                    }
                }
                foreach (KeyValuePair<string, int> entry in votes)
                {
                    sw.WriteLine(entry.Key + ": " + entry.Value);
                }
                #region test
                //switch (element.Value.ToString())
                //{
                //    case "Meteor":
                //        increment = element.Value.Exists(x => x == "Meteor") ? increment++ : increment += 0;
                //        sw.Write(String.Join(Environment.NewLine, "Meteor : " + increment));
                //        break;
                //    case "BloodMoon":
                //        increment = 0;
                //        increment = element.Value.Exists(x => x == "BloodMoon") ? increment++ : increment += 0;
                //        sw.Write(String.Join(Environment.NewLine, "BloodMoon : " + increment));
                //        break;
                //}
                #endregion
                //sw.Write(string.Join(Environment.NewLine, element.Value));
                sw.Close();
                sw.Dispose();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.button1.Enabled = false;
            this.button2.Enabled = true;
            this.votes = new Dictionary<string, List<string>>()
            {
                    {"Prix1", new List<string>()},
                    {"Prix2", new List<string>()},
                    {"Prix3", new List<string>()},
                    {"Prix4", new List<string>()},
                    {"Prix5", new List<string>()}
            };
            MessageBox.Show("Monitoring started successfully");
            this.SetText("Starting...", this.textBox9); // method "SetText" is executed on the worker thread => thread-safe call on the textBox9
            tcpListener = new TcpListener(IPAddress.Any, 3000);
            listenThread = new Thread(new ThreadStart(ListenForClients));
            listenThread.Start();

            void ListenForClients()
            {
                try
                {
                    this.tcpListener.Start();
                    this.SetText("Sucessfully started", this.textBox9);
                    while (true)
                    {
                        //blocks until a client has connected to the server
                        TcpClient client = this.tcpListener.AcceptTcpClient();

                        //create a thread to handle communication 
                        //with connected client
                        Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                        clientThread.Start(client);

                    }

                }
                catch (SocketException) { }

            }
            void HandleClientComm(object client)
            {
                TcpClient tcpClient = (TcpClient)client;
                this.client = tcpClient;
                NetworkStream clientStream = tcpClient.GetStream();

                ASCIIEncoding encoder = new ASCIIEncoding();
                //byte[] buffer = encoder.GetBytes("Hello Client!");
                //clientStream.Write(buffer, 0, buffer.Length);
                //clientStream.Flush();

                byte[] message = new byte[4096];
                int bytesRead;

                while (true)
                {
                    bytesRead = 0;

                    try
                    {
                        //blocks until a client sends a message
                        bytesRead = clientStream.Read(message, 0, 4096);
                    }
                    catch
                    {
                        //a socket error has occured
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        //the client has disconnected from the server
                        break;
                    }
                    //if (encoder.GetString(message, 0, bytesRead) == "CHECKVOTE")
                    //{
                    //    clientStream.Write(encoder.GetBytes("VOTEOK"), 0, encoder.GetBytes("VOTEOK").Length);
                    //    clientStream.Flush();
                    //}
                    else
                    {
                        this.TreatText(encoder.GetString(message, 0, bytesRead));
                        clientStream.Flush();
                    }
                    //File.WriteAllText("Log.txt", textBox9.Text);

                }

            }


        }
        private void TreatText(string message)
        {
            var split = message.Split(':');
            if (!this.address.Contains(split[1])) this.address.Add(split[1]);
            //var increment = this.votes.Exists(x => x == split[3]) ? increment++ : increment += 0;
            switch (split[0])
            {
                case "CHECKVOTE":
                    //string incID = split[2];
                    //if (!ticketNumber.Contains(incID))
                    //{
                    Network.SendPacket("VOTEOK", split[1]);
                    //    ticketNumber.Add(split[2]);
                    //}
                    //else
                    //{
                    //    Network.SendPacket("REGISTERED", split[1]);
                    //}
                    break;
                case "CHECKUSER":
                    if(split[2] == "66568539816433") //INVITE
                    {
                        Network.SendPacket("USEROK", split[1]);
                    }
                    if (ticketNumber.Contains(split[2]))
                    {
                        if (!ticketVoted.Contains(split[2]))
                        {
                            Network.SendPacket("USEROK", split[1]);
                            ticketVoted.Add(split[2]);
                        }
                    }
                    break;
                case "PAGE":
                    switch (split[1])
                    {
                        case "1":
                            this.SetText(split[3], this.textBox2);
                            break;
                        case "2":
                            this.SetText(split[3], this.textBox4);
                            break;
                        case "3":
                            this.SetText(split[3], this.textBox6);
                            break;
                        case "4":
                            this.SetText(split[3], this.textBox8);
                        break;
                    }
                    break;
                case "CONNEXION":
                    switch (split[1])
                    {
                        case "1":
                            this.SetText(split[3], this.textBox1);
                            break;
                        case "2":
                            this.SetText(split[3], this.textBox3);
                            break;
                        case "3":
                            this.SetText(split[3], this.textBox5);
                            break;
                        case "4":
                            this.SetText(split[3], this.textBox7);
                            break;
                    }
                    this.SetText(split[3] + " s'est connecté", this.textBox9);
                    break;
                case "VOTE1":
                    this.SetText(split[2] + " a voté pour le prix 1", textBox9);
                    this.votes["Prix1"].Add(split[3]);
                    break;
                case "VOTE2":
                    this.SetText(split[2] + " a voté pour le prix 2", textBox9);
                    this.votes["Prix2"].Add(split[3]);
                    break;
                case "VOTE3":
                    this.SetText(split[2] + " a voté pour le prix 3", textBox9);
                    this.votes["Prix3"].Add(split[3]);
                    break;
                case "VOTE4":
                    this.SetText(split[2] + " a voté pour le prix 4", textBox9);
                    this.votes["Prix4"].Add(split[3]);
                    break;
                case "VOTE5":
                    this.SetText(split[2] + " a voté pour le prix 5", textBox9);
                    this.votes["Prix5"].Add(split[3]);
                    break;
            }
        }

        private void SetText(string text, TextBox textbox)
        {
            // if the calling thread is different from the thread that created the textBox control, it will create a delegate 
            // and will call itself asynchronously using the Invoke method

            //if the calling thread is the same from the thread that created the textBox control, the text property is set directly
            string oldText = textbox.Text;
            if (textbox.InvokeRequired) // InvokeRequired compares the calling thread ID to the creating thread ID
            {   // if these threads are different, it will returns true
                StringArgReturningVoidDelegate d = new StringArgReturningVoidDelegate(SetText);
                this.Invoke(d, new object[] { text, textbox });
            }
            else
            {
                // ternary operators : var = [condition] ? [if condition is true] : [if condition is false] ; 
                var actualText = $"[{DateTime.Now}] : {text}";
                textbox.Text = textbox == this.textBox9 ? oldText.TrimStart() + Environment.NewLine + actualText : actualText;
            }

        }

        #region Useless atm
        private void Form1_Load(object sender, EventArgs e)
        {
        }
        private void label1_Click(object sender, EventArgs e)
        {

        }
        private void label3_Click(object sender, EventArgs e)
        {

        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
        private void textBox9_TextChanged_1(object sender, EventArgs e)
        {
            textBox9.SelectionStart = textBox9.Text.Length;
            textBox9.ScrollToCaret();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
        #endregion
        
    }
}
