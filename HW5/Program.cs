// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using ILOG.Concert;
using ILOG.CPLEX;
using System.ComponentModel;
using System.Numerics;
using System.Diagnostics;  // 引入 Stopwatch


public class HW5
{
    static void Main(string[] args)
    {
 
        // 讀取 CSV 檔
        string filePath = "assignment02_coef_100X100.csv";
        // 預先不知道矩陣大小，先用List儲存資料
        List<string[]> data = new List<string[]>();
        using (StreamReader sr = new StreamReader(filePath))
        {
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                // 將每行資料以逗號分割，並存入List中
                string[] row = line.Split(',');
                data.Add(row);
            }
        }
        // 解析 CSV 中的數據
        int nrow = data.Count-1;      //100
        int ncol = data[0].Length-2;  //101

        double[,] Coeffs = new double[nrow, ncol];

        // 限制係數矩陣：應該取從 data 的第二行到最後一行，第二列到倒數第二列
        for (int i = 2; i < nrow+1; i++)  // 限制條件是總行數減一
        {
            for (int j = 2; j < ncol+1; j++)  // 不包括 RHS 列
            {
                if (double.TryParse(data[i][j], out double parsedValue))
                {
                    Coeffs[i - 2, j - 2] = parsedValue;  // 正確填充係數矩陣
                }
            }
        }

        // RHS 值：每一行的最後一列
        for (int i = 2; i < nrow+1; i++)
        {
            if (double.TryParse(data[i][ncol+1], out double parsedRHS))
            {
                Coeffs[i - 2, ncol-1] = parsedRHS;  // 正確填充 RHS 列
            }
        }

        // 目標函數：應該是第一行
        for (int i = 2; i < ncol+1; i++)
        {
            if (double.TryParse(data[1][i], out double parsedObj))
            {
                Coeffs[nrow-1, i - 2] = parsedObj;  // 正確填充目標函數的係數
            }

        }

        // 使用 Stopwatch 來測量運行時間
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();  // 開始計時

        // 讀取 LPMethod 和 pgradient 參數
        int lpMethod = int.Parse(args[0]);  // 1 for Primal, 2 for Dual
        int pgradient = int.Parse(args[1]); // -1, 0, 1, 2, 3, 4 for different pricing strategies

        // 建立 CPLEX 模型
        // 設定 LPMethod 和 pgradient
        Cplex model = new Cplex();
        model.SetParam(Cplex.Param.RootAlgorithm, lpMethod);
        model.SetParam(Cplex.Param.Simplex.PGradient, pgradient); 

        // 決策變數
        INumVar[] vars = model.NumVarArray(ncol - 1, 0, double.MaxValue, NumVarType.Float);

        // 目標函數: maximize c1*x1 + c2*x2 + ... + cn*xn
        double[] objectiveCoeffs = new double[ncol - 1];
        for (int i = 0; i < ncol - 1; i++)
        {
            objectiveCoeffs[i] = Coeffs[nrow - 1, i];  // 目標函數的係數在最後一行
        }
        model.AddMaximize(model.ScalProd(vars, objectiveCoeffs));
        
        // 添加限制式: A * x <= RHS
        for (int i = 0; i < nrow - 1; i++)
        {
            ILinearNumExpr expr = model.LinearNumExpr();
            for (int j = 0; j < ncol - 1; j++)
            {
                expr.AddTerm(Coeffs[i, j], vars[j]);
            }
            model.AddLe(expr, Coeffs[i, ncol - 1]);  // 使用 RHS 列
        }


      
        //求解數學模型
        model.Solve();


        stopwatch.Stop();  // 停止計時
        double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;  // 獲取經過的秒數

        // 獲取求解狀態和目標值
        string result = $"lpMethod: {lpMethod}, pgradient: {pgradient}\n";
        Console.WriteLine($"Running with lpMethod: {lpMethod}, pgradient: {pgradient}");
        //result += $"Solution status = {model.GetStatus()}\n";
        result += $"Objective value = {model.ObjValue}\n";

        Console.WriteLine("Basic Feasible Solution (BFS):");

        // 獲取並輸出原始變數解
        double[] primalSolution = model.GetValues(vars);
        result += "Basic solution:\n";
        for (int i = 0; i < primalSolution.Length; i++)
        {
            // 只輸出值大於 0 的變數解
            if (primalSolution[i] != 0)
            {
                result += $"x{i + 1} = {primalSolution[i]}\n";
            }
        }

        double[] SlackSolution = model.GetValues(vars);
        result += "Slack solution:\n";
        for (int i = 0; i < primalSolution.Length; i++)
        {
            // 只輸出值大於 0 的變數解
            if (primalSolution[i] == 0)
            {
                result += $"x{i + 1} = {primalSolution[i]}\n";
            }
        }

        // 獲取運行時間
        result += $"Interationtime: {model.Niterations}\n"; ;
        result += $"Runtime: {elapsedSeconds} seconds\n";  // 用 Stopwatch 獲取的時間

        // 將結果寫入文件
        File.AppendAllText("result.txt", result + "\n");

        model.End();

    }
}