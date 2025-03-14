﻿//---------------------------------------------------------------------
// <copyright file="VsFunctionAllocationsComparer.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using ResultsComparer.VsProfiler.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace ResultsComparer.VsProfiler
{
    /// <summary>
    /// An <see cref="Core.IResultsComparer"/> that can compare
    /// Function Allocations reports generated by the VS .NET Object Allocation Tracker.
    /// These reports are generated by selecting the desired rows in the VS UI, right-clicking
    /// then copy-pasting the data into a text file.
    /// </summary>
    public class VsFunctionAllocationsComparer : VsProfilerComparer<VsProfilerFunctionAllocations>
    {
        /// <inheritdoc/>
        public override string Name => "VS .NET Object Allocations - Functions";

        /// <inheritdoc/>
        protected override IDictionary<string, string> MetricNameMap => new Dictionary<string, string>()
        {
            { "Total", "Total Allocations" },
            { "Self", "Self Allocations" },
            { "Size", "Self Size (bytes)" },
            { "Bytes", "Self Size (bytes)" }
        };

        /// <inheritdoc/>
        protected override string DefaultMetric => "Self";

        /// <inheritdoc/>
        public override bool CanReadFile(string filePath)
        {
            using var reader = new StreamReader(filePath);
            string firstLine = reader.ReadLine();
            return firstLine != null
                && firstLine.Contains("Name")
                && (firstLine.Contains("Allocations") || firstLine.Contains("Bytes"));
        }

        /// <inheritdoc/>
        protected override string GetItemId(VsProfilerFunctionAllocations item)
        {
            return item.Name;
        }

        /// <inheritdoc/>
        protected override long GetMetricValue(VsProfilerFunctionAllocations item, string metric)
        {
            if (metric.Equals("Total", StringComparison.OrdinalIgnoreCase))
            {
                return item.TotalAllocations;
            }
            else if (metric.Equals("Self", StringComparison.OrdinalIgnoreCase))
            {
                return item.SelfAllocations;
            }
            else if (metric.Equals("Size", StringComparison.OrdinalIgnoreCase) || metric.Equals("Bytes", StringComparison.OrdinalIgnoreCase))
            {
                return item.Size;
            }

            throw new Exception($"Unsupported metric {metric} for VS Profiler Allocations Comparer");
        }
    }
}
