// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILOG.CPLEX;
using ILOG.Concert;
using System.Numerics;

namespace FarmerProblem
{
    class Program
    {
        static void Main(string[] args)
        {
            double[] PossibleCount = [100, 200, 500, 1000, 5000, 10000];
       
            // 每英畝的產量 (噸/英畝)
            var yields = new Dictionary<string, double>
        {
            { "wheat", 2.5 },
            { "corn", 3.0 },
            { "sugar_beets", 20.0 }
        };

            // 每英畝的種植成本 (美元/英畝)
            var plantingCosts = new Dictionary<string, double>
        {
            { "wheat", 150.0 },
            { "corn", 230.0 },
            { "sugar_beets", 260.0 }
        };

            // 每噸的銷售價格 (美元/噸)
            var sellingPrices = new Dictionary<string, double>
        {
            { "wheat", 170.0 },
            { "corn", 150.0 },
            { "sugar_beets_below_6000", 36.0 },
            { "sugar_beets_above_6000", 10.0 }
        };

            // 每噸的購買價格 (美元/噸)
            var purchasePrices = new Dictionary<string, double>
        {
            { "wheat", 238.0 },
            { "corn", 210.0 }
        };

            // 每種作物的最低需求 (噸)
            var minRequirements = new Dictionary<string, double>
        {
            { "wheat", 200.0 },
            { "corn", 240.0 }
        };

            // 可用土地總面積
            double totalLand = 500.0;
            Random random = new Random();
            foreach (double count in PossibleCount)
            {
                Cplex model = new Cplex();

                // First-stage:x1, x2, x3
                INumVar x_wheat = model.NumVar(0, totalLand, "x_wheat");
                INumVar x_corn = model.NumVar(0, totalLand, "x_corn");
                INumVar x_sugar = model.NumVar(0, totalLand, "x_sugar");

                // Land  constraint:x1+x2+x3<=500
                model.AddLe(model.Sum(x_wheat, x_corn, x_sugar), totalLand, "Land");

                //First:150*x1+230x2+260x3
                ILinearNumExpr firstStageCosts = model.LinearNumExpr();
                firstStageCosts.AddTerm(plantingCosts["wheat"], x_wheat);
                firstStageCosts.AddTerm(plantingCosts["corn"], x_corn);
                firstStageCosts.AddTerm(plantingCosts["sugar_beets"], x_sugar);

                int intCount = (int)count;  
                INumVar[] y_wheat_purchased = new INumVar[intCount];
                INumVar[] y_corn_purchased = new INumVar[intCount];
                INumVar[] w_wheat_sold = new INumVar[intCount];
                INumVar[] w_corn_sold = new INumVar[intCount];
                INumVar[] w_sugar_sold_at_36 = new INumVar[intCount];
                INumVar[] w_sugar_sold_at_10 = new INumVar[intCount];
                INumVar[] profits = new INumVar[intCount];

                for (int i = 0; i < count; i++)
                {
                    // 設定產量變動正負 20% 的範圍 (0.8 ~ 1.2)
                    double yieldWheat_mul = 0.8 + 0.4 * random.NextDouble();
                    double yieldCorn_mul = 0.8 + 0.4 * random.NextDouble();
                    double yieldSugarBeets_mul = 0.8 + 0.4 * random.NextDouble();

                    //y1, y2:tons of wheat and corn buy
                    y_wheat_purchased[i] = model.NumVar(0, double.MaxValue, $"y_wheat_purchased_{i}");
                    y_corn_purchased[i] = model.NumVar(0, double.MaxValue, $"y_corn_purchased_{i}");

                    //w1,w2,w3,w4:tons of wheat, corn, sugar sold
                    w_wheat_sold[i] = model.NumVar(0, double.MaxValue, $"w_wheat_sold{i}");
                    w_corn_sold[i] = model.NumVar(0, double.MaxValue, $"w_corn_sold_{i}");
                    w_sugar_sold_at_36[i] = model.NumVar(0, 6000, $"w_sugar_sold_at_36_{i}");
                    w_sugar_sold_at_10[i] = model.NumVar(0, double.MaxValue, $"w_sugar_sold_at_10_{i}");
                    profits[i] = model.NumVar(double.MinValue, double.MaxValue, $"profit_{i}");

                    // Production expressions(ex:2.5x1,3x2, 1x3)
                    INumExpr wheat_temp = model.Prod(x_wheat, yields["wheat"] * yieldWheat_mul);
                    INumExpr corn_temp = model.Prod(x_corn, yields["corn"] * yieldCorn_mul);
                    INumExpr sugar_temp = model.Prod(x_sugar, yields["sugar_beets"] * yieldSugarBeets_mul);
                   
                    // Constraints for minimum requirements
                    //2.5x1+y1-w1>=200, 3x2+y2-w2>=240
                    model.AddGe(model.Sum(wheat_temp, y_wheat_purchased[i], model.Diff(0, w_wheat_sold[i])), minRequirements["wheat"], $"WheatRequirement_{i}");
                    model.AddGe(model.Sum(corn_temp, y_corn_purchased[i], model.Diff(0, w_corn_sold[i])), minRequirements["corn"], $"CornRequirement_{i}");

                    // Constraints for sugar beet sales
                    //w3+w4<=20x3
                    model.AddLe(model.Sum(w_sugar_sold_at_36[i], w_sugar_sold_at_10[i]), sugar_temp, $"SugarSales{i}");

                    // Revenue
                    INumExpr wheat_revenue = model.Prod(sellingPrices["wheat"]* yieldWheat_mul, w_wheat_sold[i]);
                    INumExpr corn_revenue = model.Prod(sellingPrices["corn"]* yieldCorn_mul, w_corn_sold[i]);
                    INumExpr sugar_revenue = model.Sum(
                        model.Prod(36, w_sugar_sold_at_36[i]),
                        model.Prod(10, w_sugar_sold_at_10[i])
                    );

                    //Cost
                    INumExpr wheat_purchase_cost = model.Prod(purchasePrices["wheat"], y_wheat_purchased[i]);
                    INumExpr corn_purchase_cost = model.Prod(purchasePrices["corn"], y_corn_purchased[i]);

                    // Profit
                    INumExpr profit_per = model.Diff(
                        model.Sum(wheat_revenue, corn_revenue, sugar_revenue),
                        model.Sum(wheat_purchase_cost, corn_purchase_cost)
                    );
                    model.AddEq(profits[i], profit_per, $"Profit_{i}");
                }

                
                ILinearNumExpr expectedProfit = model.LinearNumExpr();
                for (int j = 0; j < count; j++)
                {
                    expectedProfit.AddTerm(1 / count, profits[j]);
                }

                // min:first stage cost-profit =>max:profit - first stage cost
                IObjective objective = model.AddMaximize(model.Diff(expectedProfit, firstStageCosts));

                // Start timer and solve the model
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                if (model.Solve())
                {
                    stopwatch.Stop();

                    // Obj
                    double FirstStageCostValue = model.GetValue(firstStageCosts);
                    double TotalProfit = model.ObjValue;
                    double SecondStageProfit = TotalProfit + FirstStageCostValue;

                    // First stage x
                    double Wheat_x = model.GetValue(x_wheat);
                    double Corn_x = model.GetValue(x_corn);
                    double Sugar_x = model.GetValue(x_sugar);

                    // Output the results
                    Console.WriteLine($"==================================================Result for {count} possible outcomes====================================");
                    Console.WriteLine($"First-Stage Objective Value: {Math.Round(-FirstStageCostValue, 4)}");
                    Console.WriteLine($"Second-Stage Objective Value: {Math.Round(SecondStageProfit, 4)}");
                    Console.WriteLine($"Total Objective Value: {Math.Round(TotalProfit, 4)}");
                    Console.WriteLine($"Wheat Lands(x_1): {Math.Round(Wheat_x, 4)} acres");
                    Console.WriteLine($"Corn Lands(x_2): {Math.Round(Corn_x, 4)} acres");
                    Console.WriteLine($"Sugar Beet Lands(x_3): {Math.Round(Sugar_x, 4)} acres");
                    Console.WriteLine($"Run-Time: {Math.Round(stopwatch.Elapsed.TotalSeconds, 4)} seconds\n");
                }

                model.End();
            }
        }
    }
 }


