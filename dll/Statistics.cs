using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    class Statistics
    {

        public class FleissTable
        {
            public FleissTable() { }
            public int C1; // category #1
            public int C2; // category #2
            public int C3; // category #3
        }

        public double FliessKappa(List<FleissTable> table)
        {
            //---------------------------------------------------------------------------------------------------
            // Fleiss_Kappa = (p_a - p_e) / (1 - p_e)
            // p_a = (1 / ( m * n * (m - 1))) * (Zigma[i=1,...,n]Zigma[j=1,...,k](x_ij^2) - m * n)
            // p_e = Zigma[j=1,...,k](q_j^2)
            // q_j = (1 / (n * m)) * Zigma[i=1,...,n](x_ij)
            // n = the number of subjects 
            // k = the number of evaluation categories
            // m = the number of judges for each subject
            //---------------------------------------------------------------------------------------------------
            double fleiss_kappa = 0.0;
            double x_ij_2 = 0.0;
            double p_a = 0.0, p_e = 0.0;
            double n, m;
            n = table.Count;
            m = 2.0; // in here, only as an example we set the number of judges equal to '2'
                     // ************************************************************
                     // Step 1: calculating Zigma[i=1,...,n]Zigma[j=1,...,k](x_ij^2)
            for (int i = 0; i < table.Count; i++)
            {
                x_ij_2 = x_ij_2 + Math.Pow(table[i].C1, 2.0) + Math.Pow(table[i].C2, 2.0) + Math.Pow(table[i].C3, 2.0);
            }
            // ************************************************************
            // Step 2: calculating p_a
            p_a = (double)(x_ij_2 - (m * n)) / (double)(m * n * (m - 1));
            // ************************************************************
            // Step 3: calculating q_j
            double q_j_1 = 0, q_j_2 = 0, q_j_3 = 0;
            for (int j = 0; j < table.Count; j++)
            {
                q_j_1 = q_j_1 + table[j].C1;
                q_j_2 = q_j_2 + table[j].C2;
                q_j_3 = q_j_3 + table[j].C3;
            }
            q_j_1 = q_j_1 / (m * n);
            q_j_2 = q_j_2 / (m * n);
            q_j_3 = q_j_3 / (m * n);
            // ************************************************************
            // Step 4: calculating p_e
            p_e = Math.Pow(q_j_1, 2.0) + Math.Pow(q_j_2, 2.0) + Math.Pow(q_j_3, 2.0);
            // ************************************************************
            // Step 5: calculating fleiss`s kappa 
            fleiss_kappa = (p_a - p_e) / (1 - p_e);
            return fleiss_kappa;
        }

    }
}
