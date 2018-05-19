using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace SecureChat
{
    public partial class Form1 : Form
    {
        string key;
        int s = 1;
        int m = 0;
        IPEndPoint ipep;
        Socket newsock;
        byte[] data;
        Socket client;
        Thread thr;
        bool check;
        string checkstr;
        public Form1()
        {
            InitializeComponent();
            ipep = new IPEndPoint(IPAddress.Any, 10000);
            newsock = new Socket(AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp);
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string tamp1 = "";
            string iv = RandomIV();
            int n = 0;
            if (textBox1.Text.Length == 0)
                MessageBox.Show("Ko dc de trong");
            else
            {
                if (textBox1.Text.Length < 16)
                {
                    DateTime dt = DateTime.Now;
                    string padding = dt.ToString("ddMMyyyyhhmmss");
                    n = 16 - textBox1.Text.Length;
                    string tamp = textBox1.Text + padding.Substring(0, n);
                    tamp1 = EncryptStringToBytes_Aes(tamp, key, iv);
                    
                }
                else
                {
                    tamp1 = EncryptStringToBytes_Aes(textBox1.Text, key, iv);
                }
            }
            listBox1.Items.Add("<Me> : " + textBox1.Text);
            string check = textBox1.Text + key;
            string hash = MD5(check);
            data = new byte[1024];
            data = Encoding.ASCII.GetBytes(hash);
            client.Send(data, data.Length, SocketFlags.None);

            Thread.Sleep(500);
            tamp1 = iv + ";" + tamp1 + ";" + n.ToString();
            data = Encoding.ASCII.GetBytes(tamp1);
            client.Send(data,data.Length,SocketFlags.None);
        }
        static string EncryptStringToBytes_Aes(string plainText, string Key, string IV)
        {
            byte[] encrypted;
            byte[] data = Encoding.ASCII.GetBytes(plainText);
            AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider();
            aesAlg.KeySize = 256;
            aesAlg.BlockSize = 128;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.None;
            aesAlg.Key = Encoding.ASCII.GetBytes(Key);
            aesAlg.IV = Encoding.ASCII.GetBytes(IV);


            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);
            encrypted = encryptor.TransformFinalBlock(data, 0, data.Length);
            return Convert.ToBase64String(encrypted); ;

        }

        static string DecryptStringFromBytes_Aes(string Text, string Key, string IV)
        {
            byte[] data= Convert.FromBase64String(Text);
            byte[] decrypted;
            AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider();
            aesAlg.KeySize = 256;
            aesAlg.BlockSize = 128;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.Padding = PaddingMode.None;
            aesAlg.Key = Encoding.ASCII.GetBytes(Key);
            aesAlg.IV = Encoding.ASCII.GetBytes(IV);



            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
            decrypted = decryptor.TransformFinalBlock(data, 0, data.Length);
            return Encoding.ASCII.GetString(decrypted);

        }
        public void exchangepublickey()
        {
            data = new byte[1024];
            Random rd = new Random();
            this.Invoke(new MethodInvoker(delegate ()
                {   listBox1.Items.Add("Thỏa Thuận : p = 23 , g = 5");     }));
            string str = "23,5";
            data = Encoding.ASCII.GetBytes(str);
            client.Send(data, data.Length, SocketFlags.None);

            int a = rd.Next(1, 10);
            int A = (int)Math.Pow(5, a) % 23;

            Thread.Sleep(500);
            data = Encoding.ASCII.GetBytes(A.ToString());
            client.Send(data, data.Length, SocketFlags.None);
            Thread.Sleep(500);
            
            data = new byte[1024];
            int recv = client.Receive(data, data.Length, SocketFlags.None);
            string k = Encoding.ASCII.GetString(data, 0, recv);

            int B = int.Parse(k);
            int s = (int)Math.Pow(B, a) % 23;
            key = MD5(s.ToString());
            this.Invoke(new MethodInvoker(delegate ()
            { listBox1.Items.Add("*************************"); }));
            this.Invoke(new MethodInvoker(delegate ()
            { listBox1.Items.Add("My private key : " + s.ToString()); }));
            this.Invoke(new MethodInvoker(delegate ()
            { listBox1.Items.Add("*************************"); }));
            this.Invoke(new MethodInvoker(delegate ()
                 {   textBox4.Text = key;    }));
            
        }
        public string RandomIV()
        {
            Random rd = new Random();
            string str = "";
            for (int i = 0; i < 16; i++)
            {
                int n = rd.Next(0, 10);
                str = str + n.ToString();
            }
            return str;
        }
        public string MD5(string input)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            s++;
            if(s < 10) label1.Text = "0" + m +":0" +s.ToString();
            else label1.Text = "0"+ m +":"+ s.ToString();
            if (s % 59 == 0 && s != 0)
            {
                m++;
                if (m == 1)
                {
                    data = new byte[1024];
                    data = Encoding.ASCII.GetBytes("endsession123456");
                    client.Send(data, data.Length, SocketFlags.None);
                    m = 0;
                }
                s = 0;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            newsock.Bind(ipep);
            newsock.Listen(10);
            listBox1.Items.Add("Waiting for a client...");
            client = newsock.Accept();

            IPEndPoint clientep = (IPEndPoint)client.RemoteEndPoint;
            listBox1.Items.Add("Connected with " + clientep.Address + " at port" +
                clientep.Port);
            exchangepublickey();
            thr = new Thread(new ThreadStart(HandleConnection));
            thr.Start();
            
        }
        public void HandleConnection()
        {
            while (true)
            {
                byte[] data = new byte[1024];
                int recv = client.Receive(data, SocketFlags.None);
                string k = Encoding.ASCII.GetString(data, 0, recv);
                if (k.Length == 32)
                {
                    checkstr = k;
                }
                else
                {
                    if (k == "endsession123456")  exchangepublickey();
                    else
                    {
                        string[] tamp = k.Split(new char[] { ';' });
                        string decrypt = DecryptStringFromBytes_Aes(tamp[1], textBox4.Text, tamp[0]);
                        if (MD5(decrypt.Substring(0, 16 - int.Parse(tamp[2])) + key) == checkstr) check = true;
                        else check = false;
                        if (check)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox7.Text = k; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox4.Text = key; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox2.Text = tamp[0]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox3.Text = tamp[1]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox5.Text = tamp[2]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox6.Text = decrypt.Substring(0, 16 - int.Parse(tamp[2])); }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { listBox1.Items.Add("<Client 2> : " + textBox6.Text); }));
                        }
                    }
                }
            }
        }


    }
}
