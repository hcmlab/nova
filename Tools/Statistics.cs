using DirectShowLib;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace ssi
{
    public class Statistics
    {
        public Statistics()
        {
           
        }


        public static double MSE(List<AnnoList> al, bool normalize)
        {
            double mse = 0;
            double nsm = 0;
            if (al.Count == 2)
            {
                double sum_sq = 0;
                double minerr = double.MaxValue;
                double maxerr = 0.0;

                double[] array = new double[al[0].Count];
                double length = GetAnnoListMinLength(al);
                {
                    for (int i = 0; i < length; i++)
                    {
                        double err = al[0][i].Score - al[1][i].Score;
                        if (err > maxerr) maxerr = err;
                        if (err < minerr) minerr = err;
                        sum_sq += err * err;
                    }
                    mse = (double)sum_sq / (al[0].Count);
                    nsm = mse / (maxerr - minerr);
                }
            }

            if (normalize) return nsm;
            else return mse;
        }

        public static double Cronbachsalpha(List<AnnoList> annolists, int decimals = 3)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            int N = int.MaxValue;

            foreach (AnnoList a in annolists)
            {
                if (a.Count < N) N = a.Count;
            }

            double[] varj = new double[n];
            double[] vari = new double[N];

            double[][] data = new double[n][];

            for (int i = 0; i < n; i++)
            {
                double[] row = new double[N];
                for (int j = 0; j < N; j++)
                {
                    double inputValue = annolists[i][j].Score;
                    row[j] = Math.Round(inputValue, decimals);
                }

                data[i] = row;
            }

            Matrix<double> CorrelationMatrix = SpearmanCorrelationMatrix(data);

            Matrix<double> uppertriangle = CorrelationMatrix.UpperTriangle();

            double rvec = 0.0;

            for (int i = 0; i < CorrelationMatrix.ColumnCount; i++)
            {
                for (int j = 0; j < CorrelationMatrix.RowCount; j++)
                {
                    rvec += uppertriangle[i, j];
                }
            }

            double factor = (n * (n - 1)) / 2.0;

            rvec = (rvec - (double)n) / factor;

            double alpha = (n * rvec) / (1 + (n - 1) * rvec);

            return alpha;
        }
        public static double SpearmanCorrelationMathNet(AnnoList xs, AnnoList ys)
        {
            int N = ys.Count;
            if (xs.Count < ys.Count) N = xs.Count;

            double[] list1 = new double[N];
            double[] list2 = new double[N];

            for (int i = 0; i < N; i++)
            {
                list1[i] = xs[i].Score;
                list2[i] = ys[i].Score;
            }

            double r = Correlation.Spearman(list1, list2);
            return r;
        }

        public static double ConcordanceCorrelationCoefficient(AnnoList xs, AnnoList ys)
        {
            double meanxs, meanys;
            double variancexs, varianceys;
            double covariance = 0;
            double p;

            int n = xs.Count() > ys.Count() ? ys.Count() : xs.Count();

            double[] listx = new double[n];
            double[] listy = new double[n];

            for (int i = 0; i < n; i++)
            {
                if (!Double.IsNaN(xs[i].Score) && !Double.IsNaN(ys[i].Score))
                {
                    listx[i] = xs[i].Score;
                    listy[i] = ys[i].Score;
                }
            }

            meanxs = listx.Mean();
            meanys = listy.Mean();

            variancexs = listx.Variance();
            varianceys = listy.Variance();

            for (int i = 0; i < n; i++)
            {
                covariance = covariance + ((listx[i] - meanxs) * (listy[i] - meanys));
            }
            covariance = covariance / (n - 1);

            p = (2 * covariance) / (variancexs + varianceys + Math.Pow((meanxs - meanys), 2));

            return p;
        }
        public static double PearsonCorrelationMathNet(AnnoList xs, AnnoList ys)
        {
            int N = ys.Count;
            if (xs.Count < ys.Count) N = xs.Count;

            double[] list1 = new double[N];
            double[] list2 = new double[N];


            for (int i = 0; i < N; i++)
            {
                if (double.IsNaN(xs[i].Score))
                {
                    if (i > 0) xs[i].Score = xs[i - 1].Score;
                    else xs[i].Score = ((xs.Scheme.MaxScore - xs.Scheme.MinScore) / 2.0);
                }
                if (double.IsNaN(ys[i].Score))
                {
                    if (i > 0) ys[i].Score = ys[i - 1].Score;
                    else ys[i].Score = ((ys.Scheme.MaxScore - ys.Scheme.MinScore) / 2.0);
                }

                list1[i] = xs[i].Score;
                list2[i] = ys[i].Score;
            }

            double mean = Math.Abs(list1.Mean() - list2.Mean());

            double p = MathNet.Numerics.ExcelFunctions.TDist(mean, list1.Count(), 2);

            double r = Correlation.Pearson(list1, list2);
            return r;
        }

        public static double FleissKappa(List<AnnoList> annolists)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            List<AnnoScheme.Label> classes = annolists[0].Scheme.Labels;
            //AnnoScheme.Label rest = new AnnoScheme.Label(restclass, System.Windows.Media.Colors.Black);
            //classes.Add(rest);

            int k = 0;  //k = number of classes

            //For Discrete Annotations find number of classes, todo, find number of classes on free annotations.
            if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                k = classes.Count;
            }

            int N = annolists[0].Count;

            double[] pj = new double[k];
            double[] Pi = new double[N];

            int dim = n * N;

            //add  and initalize matrix
            int[,] matrix = new int[N, k];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    matrix[i, j] = 0;
                }
            }

            //fill the matrix

            foreach (AnnoList al in annolists)
            {
                int count = 0;
                foreach (AnnoListItem ali in al)
                {
                    for (int i = 0; i < classes.Count; i++)
                    {
                        if (ali.Label == classes[i].Name)
                        {
                            matrix[count, i] = matrix[count, i] + 1;
                        }
                    }

                    count++;
                }
            }

            //calculate pj
            for (int j = 0; j < k; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    pj[j] = pj[j] + matrix[i, j];
                }

                pj[j] = pj[j] / dim;
            }

            //Calculate Pi

            for (int i = 0; i < N; i++)
            {
                double sum = 0;
                for (int j = 0; j < k; j++)
                {
                    sum = sum + (Math.Pow(matrix[i, j], 2.0) - matrix[i, j]);
                }

                Pi[i] = (1.0 / (n * (n - 1.0))) * (sum);
            }

            //calculate Pd
            double Pd = 0;

            for (int i = 0; i < N; i++)
            {
                Pd = Pd + Pi[i];
            }

            Pd = (1.0 / (((double)N) * (n * n - 1.0))) * (Pd * (n * n - 1.0));

            double Pe = 0;

            for (int i = 0; i < k; i++)
            {
                Pe = Pe + Math.Pow(pj[i], 2.0);
            }

            double fleiss_kappa = 0.0;

            fleiss_kappa = (Pd - Pe) / (1.0 - Pe);

            return fleiss_kappa;

            //todo recheck the formula.
        }

        public static double CohensKappa(List<AnnoList> annolists)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            List<AnnoScheme.Label> classes = annolists[0].Scheme.Labels;
            //add the restclass we introduced in last step.
            //AnnoScheme.Label rest = new AnnoScheme.Label(restclass, System.Windows.Media.Colors.Black);
            //classes.Add(rest);

            int k = 0;  //k = number of classes
            //For Discrete Annotations find number of classes, todo, find number of classes on free annotations.
            if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                k = classes.Count;
            }

            int N = int.MaxValue;

            foreach (AnnoList a in annolists)
            {
                if (a.Count < N) N = a.Count;
            }

            double[] pj = new double[k];
            double[] Pi = new double[N];

            int dim = n * N;

            //add  and initalize matrix
            int[,] matrix = new int[N, k];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    matrix[i, j] = 0;
                }
            }

            //fill the matrix

            foreach (AnnoList al in annolists)
            {
                int count = 0;
                foreach (AnnoListItem ali in al)
                {
                    for (int i = 0; i < classes.Count; i++)
                    {
                        if (ali.Label == classes[i].Name)
                        {
                            matrix[count, i] = matrix[count, i] + 1;
                        }
                    }

                    count++;
                }
            }

            //calculate pj
            for (int j = 0; j < k; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    pj[j] = pj[j] + matrix[i, j];
                }

                pj[j] = pj[j] / dim;
            }

            //here it differs from fleiss' kappa

            //Calculate Pi

            for (int i = 0; i < N; i++)
            {
                double sum = 0;
                for (int j = 0; j < k; j++)
                {
                    sum = sum + (Math.Pow(matrix[i, j], 2.0) - matrix[i, j]);
                }

                Pi[i] = (sum / (n * (n - 1.0)));
            }

            //calculate Pd
            double Pd = 0;

            for (int i = 0; i < N; i++)
            {
                Pd = Pd + Pi[i];
            }

            Pd = (1.0 / (N * n * (n - 1.0))) * Pd * n * (n - 1);

            double Pc = 0;

            for (int i = 0; i < k; i++)
            {
                Pc = Pc + Math.Pow(pj[i], 2.0);
            }

            double cohens_kappa = 0.0;

            cohens_kappa = (Pd - Pc) / (1.0 - Pc);

            return cohens_kappa;
        }




        //calcs

        public static double Variance(double[] nums)
        {
            if (nums.Length > 1)
            {
                double avg = Average(nums);

                double sumofSquares = 0.0;

                foreach (double num in nums)
                {
                    sumofSquares += Math.Pow(num - avg, 2.0);
                }

                return sumofSquares / (double)(nums.Length - 1);
            }
            else return 0.0;
        }

        public static double Sum(double[] nums)
        {
            double sum = 0;
            foreach (double num in nums)
            {
                sum += num;
            }
            return sum;
        }

        public static double Average(double[] nums)
        {
            double sum = 0;

            if (nums.Length > 1)
            {
                foreach (double num in nums)
                {
                    sum += num;
                }
                return sum / (double)nums.Length;
            }
            else return (double)nums[0];
        }

        public static double StandardDeviation(double variance)
        {
            return Math.Sqrt(variance);
        }
        private static MathNet.Numerics.LinearAlgebra.Matrix<double> SpearmanCorrelationMatrix(double[][] data)
        {
            var m = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseIdentity(data.Length);
            for (int i = 0; i < data.Length; i++)
                for (int j = i + 1; j < data.Length; j++)
                {
                    var c = Correlation.Spearman(data[i], data[j]);
                    m.At(i, j, c);
                    m.At(j, i, c);
                }
            return m;
        }

        public static double transform_r_to_z(double r, int N)
        {

            double z = 0.5 * (Math.Log(1 + r) - Math.Log(1 - r));
            return z;
        }

        public static double transform_2r_to_z(double r1, double N1, double r2, double N2)
        {

            double leftlog = (1.0 + r1) / (1.0 - r1);
            double rightlog = (1.0 + r2) / (1.0 - r2);
            double upperleftpart = 0.5 * Math.Log(leftlog);
            double upperrightpart = 0.5 * Math.Log(rightlog);
            double upperpart = upperleftpart - upperrightpart;

            double lowerinnerleftpart = 1.0 / ((double)N1 - 3.0);
            double lowerinnerrightpart = 1.0 / ((double)N2 - 3.0);

            double lowerinnerpart = lowerinnerleftpart + lowerinnerrightpart;
            double lowerpart = Math.Sqrt(lowerinnerpart);
            return upperpart / lowerpart;
        }

        public static double transform_z_to_p(double z)
        {
            double Z_MAX = 6;
            double y, x, w;
            if (z == 0.0)
            {
                x = 0.0;
            }
            else
            {
                y = 0.5 * Math.Abs(z);
                if (y > (Z_MAX * 0.5))
                {
                    x = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    x = ((((((((0.000124818987 * w
                        - 0.001075204047) * w + 0.005198775019) * w
                        - 0.019198292004) * w + 0.059054035642) * w
                        - 0.151968751364) * w + 0.319152932694) * w
                        - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y -= 2.0;
                    x = (((((((((((((-0.000045255659 * y
                        + 0.000152529290) * y - 0.000019538132) * y
                        - 0.000676904986) * y + 0.001390604284) * y
                        - 0.000794620820) * y - 0.002034254874) * y
                        + 0.006549791214) * y - 0.010557625006) * y
                        + 0.011630447319) * y - 0.009279453341) * y
                        + 0.005353579108) * y - 0.002141268741) * y
                        + 0.000535310849) * y + 0.999936657524;
                }
            }
            return z > 0.0 ? ((x + 1.0) * 0.5) : ((1.0 - x) * 0.5);
        }


        //helper

        public static List<AnnoList> convertAnnoListsToMatrix(List<AnnoList> annolists, string restclass)
        {
            List<AnnoList> convertedlists = new List<AnnoList>();

            double maxlength = GetAnnoListMinLength(annolists);
            double chunksize = Properties.Settings.Default.DefaultMinSegmentSize; //Todo make option

            foreach (AnnoList al in annolists)
            {
                AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxlength, restclass);
                convertedlists.Add(list);
            }

            return convertedlists;
        }

   
        private static AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "REST")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
            result.Source.StoreToDatabase = true;
            result.Source.Database.Session = annolist.Source.Database.Session;
            double currentpos = 0;

            while (currentpos < end)
            {
                bool foundlabel = false;
                foreach (AnnoListItem orgitem in annolist)
                {
                    if (orgitem.Start < currentpos && orgitem.Stop > currentpos)
                    {
                        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                        result.Add(ali);
                        foundlabel = true;
                        break;
                    }

                    //else if (orgitem.Start < currentpos && orgitem.Stop < currentpos)
                    //{
                    //    if (orgitem.Stop - currentpos > chunksize / 2)
                    //    {
                    //        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                    //        result.Add(ali);
                    //        foundlabel = true;
                    //    }
                        
                    //    break;
                    //}
                    //else if (orgitem.Start > currentpos && orgitem.Stop > currentpos)
                    //{
                    //    if (currentpos - orgitem.Start > chunksize / 2)
                    //    {
                    //        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                    //        result.Add(ali);
                    //        foundlabel = true;
                    //    }
                       
                    //    break;
                    //}
                    //else if (orgitem.Start > currentpos && orgitem.Stop < currentpos)
                    //{
                    //    if (orgitem.Stop - currentpos + currentpos - orgitem.Start > chunksize / 2)
                    //    {
                    //        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                    //        result.Add(ali);
                    //        foundlabel = true;
                    //    }
                     
                    //    break;

                    //}
                }

                if (foundlabel == false)
                {
                    AnnoListItem ali = new AnnoListItem(currentpos, chunksize, restclass);
                    result.Add(ali);
                }

                currentpos = currentpos + chunksize;
            }

            return result;
        }

        private static double GetAnnoListMinLength(List<AnnoList> annolists)
        {
            double length = 0;
            foreach (AnnoList al in annolists)
            {
                if (al.Count > 0 && al.ElementAt(al.Count - 1).Stop > length) length = al.ElementAt(al.Count - 1).Stop;
            }

            return length;
        }

        private static double normalizermvalue(double value, AnnoScheme scheme)
        {
            if (scheme.MinScore >= 0)
            {
                double norm = (value - scheme.MinScore) / (scheme.MaxScore - scheme.MinScore);
                value = norm * 2 - 1;
            }

            return value;
        }

        private static double denormalize(double value, AnnoScheme scheme)
        {
            double norm = value / 2 + 1;

            double result = norm * (scheme.MaxScore - scheme.MinScore) + scheme.MinScore;

            return value;
        }


        //interpretations
        public static string Pearsoninterpretation(double r, int N)
        {
            string interpretation = "";
            if (r <= -1) interpretation = "perfect downhill (negative) linear relationship";
            else if (r > -1 && r <= -0.7) interpretation = "strong downhill (negative) linear relationship";
            else if (r > -0.7 && r <= -0.5) interpretation = "moderate downhill (negative) relationship";
            else if (r > -0.5 && r <= -0.3) interpretation = "weak downhill (negative) linear relationship";
            else if (r > -0.3 && r <= 0.3) interpretation = "no linear relationship";
            else if (r > 0.3 && r <= 0.5) interpretation = "weak uphill (positive) linear relationship";
            else if (r > 0.5 && r <= 0.7) interpretation = "moderate uphill (positive) relationship";
            else if (r > 0.7 && r < 1) interpretation = "strong uphill (positive) linear relationship";
            else if (r >= 1.0) interpretation = "perfect uphill (positive) linear relationship";

            //double t = r / (Math.Sqrt((1 - (r * r)) / (N - 2)));





            //double t = r * Math.Sqrt(N-2) / (Math.Sqrt((1 - (r * r))));

            //Chart Chart1 = new Chart();
            //double p = Chart1.DataManipulator.Statistics.TDistribution(t, N - 2, false);
            //string significance = "";


            //if (p < 0.05) significance = p.ToString("F6") + "< 0.05" ;
            //else  significance = p.ToString("F6") + ">= 0.05";

            return interpretation; // + " " + significance;
        }

        public static string CCCinterpretation(double ccc)
        {
            string interpretation = "";

            if (ccc < 0.9)
            {
                interpretation = "Poor";
            }
            else if (ccc >= 0.9 && ccc < 0.95)
            {
                interpretation = "Moderate";
            }
            else if (ccc >= 0.95 && ccc <= 0.99)
            {
                interpretation = "Substantial";
            }
            else
            {
                interpretation = "Almost perfext";
            }

            return interpretation;
        }

        public static string Spearmaninterpretation(double spearmancorrelation)
        {
            string interpretation = "";
            if (spearmancorrelation <= 0.19) interpretation = "Very week";
            else if (spearmancorrelation >= 0.20 && spearmancorrelation < 0.39) interpretation = "Weak";
            else if (spearmancorrelation >= 0.40 && spearmancorrelation < 0.59) interpretation = "Moderate";
            else if (spearmancorrelation >= 0.60 && spearmancorrelation < 0.79) interpretation = "Strong";
            else if (spearmancorrelation >= 0.8) interpretation = "Very strong";

            return interpretation;
        }

        public static string Cronbachinterpretation(double cronbachalpha)
        {
            string interpretation = "";
            if (cronbachalpha <= 0.5) interpretation = "Unacceptable agreement";
            else if (cronbachalpha >= 0.51 && cronbachalpha < 0.61) interpretation = "Poor agreement";
            else if (cronbachalpha >= 0.61 && cronbachalpha < 0.71) interpretation = "Questionable agreement";
            else if (cronbachalpha >= 0.71 && cronbachalpha < 0.81) interpretation = "Acceptable agreement";
            else if (cronbachalpha >= 0.81 && cronbachalpha < 0.90) interpretation = "Good agreement";
            else if (cronbachalpha >= 0.9) interpretation = "Excellent agreement";

            return interpretation;
        }
    }




}