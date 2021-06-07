/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Interfaces;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// This is a regression algorithm to ensure coarse data does not enable potential look-ahead bias.
    /// </summary>
    public class CoarseNoLookAheadBiasAlgorithm : QCAlgorithm, IRegressionAlgorithmDefinition
    {
        private const int NumberOfSymbols = 1;
        private static Dictionary<Symbol, decimal> _coarsePrices = new Dictionary<Symbol, decimal>();

        public override void Initialize()
        {
            UniverseSettings.Resolution = Resolution.Daily;

            SetStartDate(2014, 03, 24);
            SetEndDate(2014, 04, 06);
            SetCash(50000);

            AddUniverse(CoarseSelectionFunction);

            // schedule an event at 10 AM every day
            Schedule.On(
                DateRules.EveryDay(),
                TimeRules.At(10, 0),
                () =>
                {
                    foreach (var symbol in _coarsePrices.Keys)
                    {
                        if (Securities.ContainsKey(symbol))
                        {
                            // If the coarse price is emitted at midnight for the same date, we would have look-ahead bias
                            // i.e. _coarsePrices[symbol] would be the closing price of the current day,
                            // which we obviously cannot know at 10 AM :)
                            // As the coarse data is now emitted for the previous day, there is no look-ahead bias:
                            // _coarsePrices[symbol] and Securities[symbol].Price will have the same value (equal to the previous closing price)
                            // for the backtesting period, so we expect this algorithm to make zero trades.
                            if (_coarsePrices[symbol] > Securities[symbol].Price)
                            {
                                SetHoldings(symbol, 1m / NumberOfSymbols);
                            }
                            else
                            {
                                Liquidate(symbol);
                            }
                        }
                    }
                }
            );
        }

        private static IEnumerable<Symbol> CoarseSelectionFunction(IEnumerable<CoarseFundamental> coarse)
        {
            var sortedByDollarVolume = coarse.OrderByDescending(x => x.DollarVolume);
            var top = sortedByDollarVolume.Take(NumberOfSymbols).ToList();

            // save the coarse adjusted prices in a dictionary, so we can access them in the scheduled event handler
            _coarsePrices = top.ToDictionary(c => c.Symbol, c => c.AdjustedPrice);

            return top.Select(x => x.Symbol);
        }

        /// <summary>
        /// This is used by the regression test system to indicate if the open source Lean repository has the required data to run this algorithm.
        /// </summary>
        public bool CanRunLocally { get; } = true;

        /// <summary>
        /// This is used by the regression test system to indicate which languages this algorithm is written in.
        /// </summary>
        public Language[] Languages { get; } = { Language.CSharp };

        /// <summary>
        /// This is used by the regression test system to indicate what the expected statistics are from running the algorithm
        /// </summary>
        public Dictionary<string, string> ExpectedStatistics => new Dictionary<string, string>
        {
            {"Total Trades", "0"},
            {"Average Win", "0%"},
            {"Average Loss", "0%"},
            {"Compounding Annual Return", "0%"},
            {"Drawdown", "0%"},
            {"Expectancy", "0"},
            {"Net Profit", "0%"},
            {"Sharpe Ratio", "0"},
            {"Probabilistic Sharpe Ratio", "0%"},
            {"Loss Rate", "0%"},
            {"Win Rate", "0%"},
            {"Profit-Loss Ratio", "0"},
            {"Alpha", "0"},
            {"Beta", "0"},
            {"Annual Standard Deviation", "0"},
            {"Annual Variance", "0"},
            {"Information Ratio", "-1.388"},
            {"Tracking Error", "0.096"},
            {"Treynor Ratio", "0"},
            {"Total Fees", "$0.00"},
            {"Estimated Strategy Capacity", "$0"},
            {"Lowest Capacity Asset", ""},
            {"Fitness Score", "0"},
            {"Kelly Criterion Estimate", "0"},
            {"Kelly Criterion Probability Value", "0"},
            {"Sortino Ratio", "79228162514264337593543950335"},
            {"Return Over Maximum Drawdown", "79228162514264337593543950335"},
            {"Portfolio Turnover", "0"},
            {"Total Insights Generated", "0"},
            {"Total Insights Closed", "0"},
            {"Total Insights Analysis Completed", "0"},
            {"Long Insight Count", "0"},
            {"Short Insight Count", "0"},
            {"Long/Short Ratio", "100%"},
            {"Estimated Monthly Alpha Value", "$0"},
            {"Total Accumulated Estimated Alpha Value", "$0"},
            {"Mean Population Estimated Insight Value", "$0"},
            {"Mean Population Direction", "0%"},
            {"Mean Population Magnitude", "0%"},
            {"Rolling Averaged Population Direction", "0%"},
            {"Rolling Averaged Population Magnitude", "0%"},
            {"OrderListHash", "d41d8cd98f00b204e9800998ecf8427e"}
        };
    }
}