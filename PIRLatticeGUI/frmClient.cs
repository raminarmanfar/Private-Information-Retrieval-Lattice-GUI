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

namespace PIRLatticeGUI
{
    public partial class frmClient : Form
    {
        long Protocol1Time, Protocol3Time,MatrixProcessTime;

        BigIntMatrix A, B, M, Delta, D, P;
        //static Matrix<double>[] MJegon, MPrime;
        BigIntMatrix[] MPrime, Ms;

        int i0, N, l0;
        long n = 0;
        BigInteger p, q;
        List<int> dePermutList = new List<int>();

        frmServer frmParent;
        public frmClient(frmServer frmParent)
        {
            InitializeComponent();
            this.frmParent = frmParent;
        }

        private static BigIntMatrix[] permute(BigIntMatrix[] GivenMatrix, long n, List<int> pList, int N)
        {
            BigIntMatrix[] result = new BigIntMatrix[n];//, N, 2 * N];
            for (long i = 0; i < n; i++)
            {
                result[i] = new BigIntMatrix(N, 2 * N);
                result[i] = GivenMatrix[i].SwapCol(pList);
            }
            return result;
        }

        private static BigIntMatrix Depermute(BigIntMatrix vect, List<int> pList, int N)
        {
            BigIntMatrix result = new BigIntMatrix(1, 2 * N, 0);
            result = vect.SwapCol(pList);
            return result;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        BigIntMatrix invA, invDelta;
        private void btnSetupRequest_Click(object sender, EventArgs e)
        {
            txtReturnTime.Text = "0";
            txtResult.Text = "-";
            btnSendRequest.Enabled = false;

            n = frmParent.n;
            this.i0 = (int)nud_i0.Value;
            int NI = 1;
            while ((Math.Ceiling(Math.Log(n * NI)) + 1) * NI < frmParent.ll) NI++;
            N = NI;

            l0 = (int)Math.Ceiling(Math.Log(n * N)) + 1;
            q = BigInteger.Pow(2, 2 * l0);
            p = MathClass.GenerateLargePrime(l0);
            nud_i0.Maximum = n;

            txtN.Text = N.ToString();
            txt_n.Text = n.ToString();
            txt_p.Text = p.ToString();
            txt_q.Text = q.ToString();
            txt_l0.Text = l0.ToString();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            MPrime = new BigIntMatrix[n];
            A = new BigIntMatrix(N, N);
            B = new BigIntMatrix(N, N);
            Delta = new BigIntMatrix(N, N);
            invA = A.GenerateRandomInvertibleMatrix(p, l0, false);
            B.GenerateRandomMatrix(p, l0);

            M = A.Append(B, BigIntMatrix.AppendMode.Beside);
            invDelta = Delta.GenerateRandomInvertibleMatrix(p, l0, true);

            txtLog.AppendText(A.Print("A"));
            txtLog.AppendText(B.Print("B"));
            txtLog.AppendText(M.Print("M"));
            txtLog.AppendText(Delta.Print("Delta"));

            BigIntMatrix P, Ai, Bi, D;
            P = new BigIntMatrix(N, N);
            D = new BigIntMatrix(N, N);
            Ai = new BigIntMatrix(N, N);
            Bi = new BigIntMatrix(N, N);
            txtLog.AppendText("\r\n>>> N: " + N.ToString());
            txtLog.AppendText("\r\n---------------------------- Matrix Operations Started --------------------------");
            Stopwatch swMat = new Stopwatch();
            swMat.Start();
            for (long i = 0; i < n; i++)
            {
                // --- M Jegon ---
                //---P.GenerateRandomInvertibleMatrix(p, l0, false);
                P.GenerateRandomMatrix(p, l0);
                // --- Soft Noise Matrix D ---
                D.GenerateSoftNoiseMatrix();
                // --- hard noise ---
                if (i == i0) D.GenerateHardNoiseMatrix(q);
                else D.GenerateSoftNoiseMatrix();

                Ai = P * A;
                Bi = (P * B) + (D * Delta);
                MPrime[i] = Ai.Append(Bi, BigIntMatrix.AppendMode.Beside) % p;
            }
            swMat.Stop();
            MatrixProcessTime = swMat.ElapsedMilliseconds;
            txtLog.AppendText("\r\n>>> Matrix Process Duration: " + MatrixProcessTime.ToString());
            txtLog.AppendText("\r\n---------------------------- Matrix Operations Finished --------------------------");

            // --- make permutation & depermiutation vector ---
            Random rnd = new Random(DateTime.Now.Millisecond);
            List<int> tmpList = new List<int>();
            List<int> permutList = new List<int>();
            dePermutList.Clear();
            for (int i = 0; i < 2 * N; i++)
            {
                tmpList.Add(i);
                dePermutList.Add(0);
            }

            int j = 0;
            while (tmpList.Count > 0)
            {
                int i = rnd.Next(tmpList.Count);
                permutList.Add(tmpList[i]);
                dePermutList[tmpList[i]] = j++;
                tmpList.RemoveAt(i);
            }

            Ms = permute(MPrime, n, permutList, N);

            sw.Stop();
            Protocol1Time = sw.ElapsedMilliseconds;
            btnSendRequest.Enabled = true;
            txtLog.AppendText("\r\n---------------------------- Request has been sent to the server --------------------------");
        }

        private void btnSendRequest_Click(object sender, EventArgs e)
        {
            BigIntMatrix V = frmParent.getAndProcessRequest(Ms, l0, N, p);

            //MessageBox.Show("Your request has been sent to the server...", "request is on going", MessageBoxButtons.OK, MessageBoxIcon.Information);
            txtLog.AppendText("\r\n---------------------------- Server Respond to the query ------------------------------------");
            txtLog.AppendText(V.Print("Result Vector from server"));

            //--- DePermute V ---
            Stopwatch sw = new Stopwatch();
            sw.Start();

            BigIntMatrix VPrime = Depermute(V, dePermutList, N);

            BigIntMatrix undisturbedHalf = VPrime.GetSubMatrix(0, 1, 0, N);
            //BigIntMatrix invA = A.ModInverse(p);
            //***---- BigIntMatrix invA = A.GaussJordanModInverse(p);

            BigIntMatrix resultHalf = undisturbedHalf * invA * B % p;
            BigIntMatrix undisturbedVector = undisturbedHalf.Append(resultHalf, BigIntMatrix.AppendMode.Beside);
            BigIntMatrix ScrambledNoise = (VPrime - undisturbedVector) % p;

            //BigIntMatrix invDelta = Delta.ModInverse(p);
            //***---- invDelta = Delta.GaussJordanModInverse(p);

            BigIntMatrix halfScrambledNoise = ScrambledNoise.GetSubMatrix(0, 1, N, N);
            BigIntMatrix unScrambledNoise = halfScrambledNoise * invDelta % p;

            //BigInteger[] eea = MyClass.Extended_GCD(q, p);
            //---BigInteger qInv = MathClass.NumberModInverse(q, p);
            BigInteger qInv = MathClass.EEA(q, p);
            //long qInv = (long)eea[1];

            BigInteger[,] eJegon = new BigInteger[1, N];
            for (int i = 0; i < N; i++)
            {
                BigInteger epsilon = MathClass.mod(unScrambledNoise.GetElement(0, i), q);
                if (epsilon >= q / 2) epsilon -= q;
                eJegon[0, i] = MathClass.mod(unScrambledNoise.GetElement(0, i) - epsilon, p);
                eJegon[0, i] *= qInv;// % p;//MyClass.mod((long)eJegon[0, i] * qInv, p);
            }

            //Matrix<double> ResultVector = Matrix<double>.Build.DenseOfArray(eJegon).Multiply(qInv).Modulus(p);
            BigIntMatrix ResultVector = new BigIntMatrix(eJegon).Modulus(p);

            BigInteger[] resVect = new BigInteger[N];
            for (int i = 0; i < N; i++)
            {
                resVect[i] = ResultVector.GetElement(0, i);
            }

            BigInteger result = 0;
            for (int i = 0; i < N; i++)
            {
                result <<= l0;
                result += resVect[i];
            }

            sw.Stop();
            Protocol3Time = sw.ElapsedMilliseconds;
            txtLog.AppendText(ResultVector.Print("Obtained Result Vector"));
            txtLog.AppendText(invA.Print("Inv A"));
            txtLog.AppendText(invDelta.Print("Inv Delta"));
            txtLog.AppendText("\r\n>>>>>>>>>> Inv q = " + qInv.ToString());
            txtLog.AppendText("\r\n>>>>>>>>>> Obtained Result for index(" + i0.ToString() + "): " + result.ToString());
            txtLog.AppendText("\r\n--------------------------------------------- Timing ----------------------------------------------------");
            long totalDuration = Protocol1Time + Protocol3Time + frmParent.Protocol2Time;
            txtLog.AppendText("\r\n>>> Protocol 1 Duration: "+Protocol1Time.ToString());
            txtLog.AppendText("\r\n>>> Protocol 2 Duration: " + frmParent.Protocol2Time.ToString());
            txtLog.AppendText("\r\n>>> Protocol 3 Duration: " + Protocol3Time.ToString());
            txtLog.AppendText("\r\n>>> Total Duration: " + totalDuration.ToString());
            txtLog.AppendText("\r\n>>> Matrix Process Duration: " + MatrixProcessTime.ToString());
            txtLog.AppendText("\r\n>>> Deference check: " + (frmParent.getValueByIndex(i0) - result).ToString());
            txtLog.AppendText("\r\n--------------------------------------------- Timing ----------------------------------------------------");

            txtResult.Text = result.ToString();
            txtReturnTime.Text = totalDuration.ToString();
            MessageBox.Show("Query process has been finished successfully...", "Query Operation", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClearLogs_Click(object sender, EventArgs e)
        {
            txtLog.Text = string.Empty;
        }
    }
}
