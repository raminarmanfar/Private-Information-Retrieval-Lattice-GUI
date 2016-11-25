using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Numerics;
using System.IO;

namespace PIRLatticeGUI
{
    public partial class frmServer : Form
    {
        public long n { get; set; }
        public int ll { get; set; }

        static BigInteger[] db;

        frmClient frm;
        bool dbGenerated;

        public frmServer()
        {
            InitializeComponent();
            dbGenerated = false;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnGenerateDB_Click(object sender, EventArgs e)
        {
            n = (long)nudDBSize.Value;
            ll = (int)nud_l.Value;

            BigInteger tmp;
            db = new BigInteger[n];
            Random rnd = new Random(DateTime.Now.Millisecond);
            long len = (long)Math.Ceiling((double)ll / 8.0);
            if (ll % 8 == 0) len++;
            byte mask = (byte)(Math.Pow(2, ll % 8) - 1);
            BigInteger smallNum = new BigInteger();
            smallNum = BigInteger.Pow(2, ll - 1);
            BigInteger bigNum = new BigInteger();
            bigNum = BigInteger.Pow(2, ll);

            byte[] rndBytes = new byte[len];
            bool isGrader, isLess;
            long l = 0;
            dgvDB.Rows.Clear();
            string line = string.Empty;
            for (uint i = 0; i < n; i++)
            {
                do
                {
                    rnd.NextBytes(rndBytes);
                    rndBytes[len - 1] &= mask;
                    tmp = new BigInteger(rndBytes);
                    isGrader = isLess = false;
                    if (BigInteger.Compare(tmp, bigNum) == 1) isGrader = true;
                    if (BigInteger.Compare(tmp, smallNum) == -1) isLess = true;
                } while (/*db.Contains(tmp) ||*/ isGrader || isLess);
                db[l++] = tmp;
                line += tmp.ToString() + "\r\n";
                dgvDB.Rows.Add(i, tmp);
                //---if (l % 5000 == 0) Console.Write("{0}, ", l);
            }

            string FileName = "D:\\PIR\\DBs_" + n.ToString() + "_" + ll.ToString() + "Bit.txt";
            StreamWriter sw = new StreamWriter(FileName);
            sw.WriteLine(line);
            sw.Close();

            txtLog.AppendText(">>> New database has been generated with " + n.ToString() + " Records each " + l.ToString() + " bit length...\r\n");
            txtLog.AppendText(">>> The new generated database has been written in the file (" + FileName + ")...\r\n");
            MessageBox.Show("Database has been generated successfully...", "Database generation", MessageBoxButtons.OK, MessageBoxIcon.Information);
            dbGenerated = true;
        }

        private void btnLoadFromFile_Click(object sender, EventArgs e)
        {
            n = (long)nudDBSize.Value;
            ll = (int)nud_l.Value;

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "D:\\PIR\\DBs";
            openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Insert code to read the stream here.
                    BigInteger tmp;
                    db = new BigInteger[n];

                    string line;
                    StreamReader file = new StreamReader(openFileDialog1.FileName);
                    long counter = 0;

                    //---txtLog.AppendText("Database Content...");
                    dgvDB.Rows.Clear();
                    while ((line = file.ReadLine()) != null && counter < n)
                    {
                        //---txtLog.AppendText(counter.ToString() + ") " + line);
                        db[counter] = BigInteger.Parse(line);
                        dgvDB.Rows.Add(counter, db[counter]);
                        counter++;
                    }
                    file.Close();
                    dbGenerated = true;
                    MessageBox.Show("Database has been loaded from file successfully...", "Database load", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        public BigInteger getValueByIndex(long index)
        {
            return db[index];
        }


        public long Protocol2Time;
        public BigIntMatrix getAndProcessRequest(BigIntMatrix[] Ms, int l0, int N, BigInteger p)
        {
            txtLog.AppendText("---------------------------- Server Started query process --------------------------");
            Stopwatch sw = new Stopwatch();
            sw.Start();
            BigInteger[,] splittedList = spliteDB(l0, ll, N);

            /******* show splited list ******
            for (long i = 0; i < splittedList.GetLength(0); i++)
            {
                txtLog.AppendText(">>>>>>>>>> Splited Item({0})...", i);
                for (long j = 0; j < splittedList.GetLength(1); j++)
                {
                    txtLog.AppendText("> Splited({0}): {1}", j, splittedList[i,j]);
                }
            }
            /*****************************/

            BigIntMatrix Mat = CreateUserQuery(Ms, splittedList, N, n, p);
            BigIntMatrix V = new BigIntMatrix(1, 2 * N, 0);
            long len = splittedList.GetLength(0) * splittedList.GetLength(1);
            for (int i = 0; i < len; i++)
            {
                for (int j = 0; j < 2 * N; j++)
                {
                    V.SumElement(0, j, Mat.GetElement(i, j));
                }
            }

            sw.Stop();
            Protocol2Time = sw.ElapsedMilliseconds;
            //txtLog.AppendText(">>>>>>>>>> Matrix Mat: {0}", Mat.ToString());
            txtLog.AppendText(V.Modulus(p).Print("Matrix V"));
            txtLog.AppendText("---------------------------- Server Finished query process --------------------------");
            return V.Modulus(p);
        }

        private BigInteger[,] spliteDB(int l0, int l, int N)
        {
            BigInteger mask = (BigInteger)Math.Pow(2, l0) - 1;
            //long len = (long)Math.Ceiling((double)(l / l0));
            BigInteger[,] splittedList = new BigInteger[db.Length, N];
            for (long i = 0; i < db.Length; i++)
            {
                BigInteger num = db[i];
                //---splittedList.Add(new List<BigInteger>());
                for (long j = N - 1; j >= 0; j--)
                {
                    splittedList[i, j] = num & mask;
                    num >>= l0;
                }
                //splittedList[splittedList.GetLength(0) - 1].Reverse();
            }
            return splittedList;
        }

        private BigIntMatrix CreateUserQuery(BigIntMatrix[] Ms, BigInteger[,] splitted, int N, long n, BigInteger p)
        {
            int len = splitted.GetLength(0) * splitted.GetLength(1);
            BigIntMatrix Mat = new BigIntMatrix(len, 2 * N, 0);
            int l = 0;
            for (long i = 0; i < n; i++)
            {
                for (long j = 0; j < N; j++)
                {
                    for (int k = 0; k < 2 * N; k++)
                    {
                        //BigInteger num = BigInteger.Remainder(BigInteger.Multiply(splitted[i][j], Ms[i, j, k]), p);
                        BigInteger tmp = BigInteger.Multiply(splitted[i, j], Ms[i].GetElement(j, k));
                        Mat.SumElement(l, k, tmp);
                        //V[0, k] = MyClass.mod((long)V[0, k], p);
                    }
                    l++;
                }
            }
            return Mat.Modulus(p);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (dbGenerated)
            {
                frm = new frmClient(this);
                frm.Show();
            }
            else
            {
                MessageBox.Show("Database has not been generated yet...\r\nPlease generate database and try again", "Cloud server starting error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
