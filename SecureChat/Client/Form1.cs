using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Client
{
    public partial class Form1 : Form
    {
        string key;
        Socket newsock;
        IPEndPoint ipep;
        int recv ;
        string k;
        byte[] data;
        Thread thr;
        string checkstr;
        bool check;
        public Form1()
        {
            InitializeComponent();
            ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 10000);
            newsock = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            newsock.Connect(ipep);
            EndPoint ep = (EndPoint)ipep;
            listBox1.Items.Add("connect thanh cong.");
            exchangepublickey();
            
            thr = new Thread( new ThreadStart(HandleConnection));
            thr.Start();
        }
        public void exchangepublickey()
        {
            data = new byte[1024];
            Random rd = new Random();
            recv = newsock.Receive(data, data.Length, SocketFlags.None);
            k = Encoding.ASCII.GetString(data, 0, recv);
            string[] tamp = k.Split(new char[] { ',' });
            this.Invoke(new MethodInvoker(delegate ()
            {   listBox1.Items.Add("Thỏa Thuận : " + "p = " + tamp[0] + ", g = " + tamp[1]);    }));
            int p = 23, g = 5;
            int b = rd.Next(1, 10);

            data = new byte[1024];
            recv = newsock.Receive(data, data.Length, SocketFlags.None);
            k = Encoding.ASCII.GetString(data, 0, recv);
            int A = int.Parse(k);


            int B = (int)Math.Pow(g, b) % p;
            data = new byte[1024];
            data = Encoding.ASCII.GetBytes(B.ToString());
            newsock.Send(data, data.Length, SocketFlags.None);
            
            
            int s = (int)Math.Pow(A, b) % 23;

            key = MD5(s.ToString());
            this.Invoke(new MethodInvoker(delegate ()
            { listBox1.Items.Add("*************************");   }));
            this.Invoke(new MethodInvoker(delegate ()
                {   listBox1.Items.Add("My private key : " + s.ToString()); }));
            this.Invoke(new MethodInvoker(delegate ()
            { listBox1.Items.Add("*************************");   }));
            this.Invoke(new MethodInvoker(delegate ()
                {   textBox4.Text = key;    }));
            
        }
        static string DecryptStringFromBytes_Aes(string Text, string Key, string IV)
        {
            byte[] data = Convert.FromBase64String(Text);
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
        public void HandleConnection()
        {
            while (true)
            {
                byte[] data = new byte[1024];
                int recv = newsock.Receive(data, SocketFlags.None);
            
                k = Encoding.ASCII.GetString(data, 0, recv);
                if (k.Length == 32) checkstr = k;
                else
                {
                    
                    if (k == "endsession123456")
                    {
                        data = Encoding.ASCII.GetBytes("endsession123456");
                        newsock.Send(data, data.Length, SocketFlags.None);
                        exchangepublickey();

                    }
                    else
                    {
                        string[] tamp = k.Split(new char[] { ';' });
                        string decrypt = DecryptStringFromBytes_Aes(tamp[1], textBox4.Text, tamp[0]);
                        if (MD5(decrypt.Substring(0, 16 - int.Parse(tamp[2])) + key) == checkstr) check = true;
                        else check = false;
                        if (check)
                        {
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox1.Text = k; }));
                            textBox4.Text = key;
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox2.Text = tamp[0]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox3.Text = tamp[1]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                                { textBox5.Text = tamp[2]; }));
                            this.Invoke(new MethodInvoker(delegate ()
                            { textBox6.Text = decrypt.Substring(0, 16 - int.Parse(tamp[2])); }));

                            this.Invoke(new MethodInvoker(delegate ()
                            { listBox1.Items.Add("<Client 1> : " + textBox6.Text); }));
                        }
                    }
                }
            }
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
        private void button1_Click(object sender, EventArgs e)
        {
            string tamp1 = "";
            string iv = RandomIV();
            int n = 0;
            if (textBox7.Text.Length == 0)
                MessageBox.Show("Ko dc de trong");
            else
            {
                if (textBox7.Text.Length < 16)
                {
                    DateTime dt = DateTime.Now;
                    string padding = dt.ToString("ddMMyyyyhhmmss");
                    n = 16 - textBox7.Text.Length;
                    string tamp = textBox7.Text + padding.Substring(0, n);
                    tamp1 = EncryptStringToBytes_Aes(tamp, key, iv);

                }
                else
                {
                    tamp1 = EncryptStringToBytes_Aes(textBox7.Text, key, iv);
                }
            }
            this.Invoke(new MethodInvoker(delegate ()
                {   listBox1.Items.Add("<Me> : " + textBox7.Text);  }));

            string check = textBox7.Text + key;
            string hash = MD5(check);
            data = new byte[1024];
            data = Encoding.ASCII.GetBytes(hash);
            newsock.Send(data, data.Length, SocketFlags.None);

            Thread.Sleep(500);
            tamp1 = iv + ";" + tamp1 + ";" + n.ToString();
            data = new byte[1024];
            data = Encoding.ASCII.GetBytes(tamp1);
            newsock.Send(data, data.Length, SocketFlags.None);
            
        }
    }
}
