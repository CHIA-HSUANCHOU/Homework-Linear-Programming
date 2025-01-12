// See https://aka.ms/new-console-template for more information
// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;
using ILOG.Concert;
using ILOG.CPLEX;


public class HW2
{
    static void Main(string[] args)
    {
        // 設定 Excel 檔案路徑
        string excelFilePath = "C:\\Users\\chouchiahsuan\\source\\repos\\HW2\\assignment02_coef_900X900.xlsx";
        List<List<double>> data = new List<List<double>>();

        // EPPlus 需要這一行來授權使用非商業用途
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        // 打開並讀取 Excel 檔案
        using (var package = new ExcelPackage(new FileInfo(excelFilePath)))
        {
            var workbook = package.Workbook;
            var worksheetCount = workbook.Worksheets.Count;


            // 選取第一個工作表
            ExcelWorksheet worksheet = package.Workbook.Worksheets[0];

            // 假設資料從第三行開始（跳過前兩行的標題和係數狀態）
            int startRow = 2;
            int startColumn = 3; // 跳過第一列

            // 取得總行數和列數
            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            // 讀取每一行數據
            for (int row = startRow; row <= rowCount; row++)
            {
                var rowData = new List<double>();

                for (int col = startColumn; col <= colCount; col++)
                {
                    // 讀取每一個單元格的數據，轉換成 double
                    if (double.TryParse(worksheet.Cells[row, col].Text, out double value))
                    {
                        rowData.Add(value);
                    }
                }

                data.Add(rowData); // 將行數據加入 data 列表
            }
        }

        // 解析 CSV 中的數據
        int ncol = data[0].Count;
        int nrow = data.Count;

        // 印出行數和列數
        // Console.WriteLine($"Number of cols: {ncol}"); //901
        //Console.WriteLine($"Number of row: {nrow}");//900

        // 設定矩陣大小 (m+1) x (n+1)，其中 m 是限制條件的數量，n 是變數的數量
        double[,] Coeffs = new double[nrow, ncol];


        // 限制係數矩陣：應該取從 data 的第二行到最後一行，第二列到倒數第二列
        for (int i = 0; i < nrow - 1; i++)  // 限制條件是總行數減一
        {
            for (int j = 0; j < ncol - 1; j++)  // 不包括 RHS 列
            {
                Coeffs[i, j] = data[i + 1][j];  // 正確填充係數矩陣
            }
        }

        // RHS 值：每一行的最後一列
        for (int i = 0; i < nrow - 1; i++)
        {
            Coeffs[i, ncol - 1] = data[i + 1][ncol - 1];  // 正確填充 RHS 列
        }

        // 目標函數：應該是第一行
        for (int i = 0; i < ncol - 1; i++)
        {
            Coeffs[nrow - 1, i] = data[0][i];  // 正確填充目標函數的係數
        }

        // 建立 CPLEX 模型
        Cplex model = new Cplex();

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

        //將求解過程呈現在視窗中
        System.Console.WriteLine("First solution status = " + model.GetStatus());
        System.Console.WriteLine("First objective value = " + model.ObjValue);

        //將求解結果轉存成文字檔
        model.WriteSolution("solution.txt");

        model.End();
        Console.ReadLine();
    }
}


