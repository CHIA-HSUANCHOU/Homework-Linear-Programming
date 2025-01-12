// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        // 給定的約束條件 S: 79x1 + 53x2 + 53x3 + 45x4 + 45x5 ≤ 178 且 x1, x2, x3, x4, x5 是二進制變數
        List<int[]> feasibleSolutions = GenerateFeasibleSolutions();

        // 生成所有滿足條件的整數解 (x1, x2, x3, x4, x5)
        static List<int[]> GenerateFeasibleSolutions()
        {
            List<int[]> feasibleSolutions = new List<int[]>();

            // 使用位元枚舉生成所有二進制解
            for (int i = 0; i < (1 << 5); i++) // 2^5 = 32
            {
                int x1 = (i >> 4) & 1;
                int x2 = (i >> 3) & 1;
                int x3 = (i >> 2) & 1;
                int x4 = (i >> 1) & 1;
                int x5 = i & 1;

                // 檢查是否滿足約束條件
                int lhs = 79 * x1 + 53 * x2 + 53 * x3 + 45 * x4 + 45 * x5;
                if (lhs <= 178)
                {
                    feasibleSolutions.Add(new int[] { x1, x2, x3, x4, x5 });
                }
            }

            return feasibleSolutions;
        }
        
        // 印出所有可行解
        Console.WriteLine("Feasible Solutions(S):");
        foreach (var solution in feasibleSolutions)
        {
            Console.WriteLine(string.Join(", ", solution));
        }

        // 初始化頂點集合 v
        List<int[]> vertices = new List<int[]>();

        // 尋找頂點
        for (int i = 0; i < feasibleSolutions.Count; i++)
        {
            int[] xk = feasibleSolutions[i];
            bool isVertex = true;

            // 檢查是否 xk 是 xi 和 xj 的凸組合
            for (int j = 0; j < feasibleSolutions.Count; j++)
            {
                if (i == j) continue;
                int[] xi = feasibleSolutions[j];

                for (int k = 0; k < feasibleSolutions.Count; k++)
                {
                    if (k == i || k == j) continue;
                    int[] xj = feasibleSolutions[k];

                    // 檢查 xk 是否可以表示為 xi 和 xj 的凸組合
                    for (double lambda = 0.01; lambda < 1; lambda += 0.01)
                    {
                        double[] combination = new double[xk.Length];
                        for (int m = 0; m < combination.Length; m++)
                        {
                            combination[m] = lambda * xi[m] + (1 - lambda) * xj[m];
                        }

                        if (AreEqual(xk, combination))
                        {
                            isVertex = false;
                            break;
                        }
                    }

                    if (!isVertex)
                        break;
                }

                if (!isVertex)
                    break;
            }

            if (isVertex)
            {
                vertices.Add(xk);
            }
        }

        // 輸出頂點
        Console.WriteLine("\nVertices of the convex hull:");
        foreach (var vertex in vertices)
        {
            Console.WriteLine(string.Join(", ", vertex));
        }
    }

    // 判斷現在的點和\lambda線性組合的點是否相同
    static bool AreEqual(int[] xk, double[] combination)
    {
        for (int i = 0; i < xk.Length; i++)
        {
            if (xk[i] != (int)Math.Round(combination[i]))
            {
                return false;
            }
        }
        return true;
    }

}


