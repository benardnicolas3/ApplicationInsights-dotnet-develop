﻿namespace Microsoft.ApplicationInsights.Metrics
{
    using System;
    using System.Collections.Generic;
    using static System.FormattableString;

    /// <summary>Encapsulates the configuration for a metric and its respective data time series.</summary>
    public class MetricConfiguration : IEquatable<MetricConfiguration>
    {
        private readonly int hashCode;

        private readonly int[] valuesPerDimensionLimits = new int[MetricIdentifier.MaxDimensionsCount];

        /// <summary>Creates a new instance of <c>MetricConfiguration</c>.</summary>
        /// <param name="seriesCountLimit">How many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="valuesPerDimensionLimit">How many different values each of the dimensions of a metric can
        /// have as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="seriesConfig">The configuration for how each series of this metric should be aggregated.</param>
        public MetricConfiguration(
                                int seriesCountLimit,
                                int valuesPerDimensionLimit,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    Invariant($"Metrics must allow at least one data series (but {seriesCountLimit} was specified)."));
            }

            this.SeriesCountLimit = seriesCountLimit;

            if (valuesPerDimensionLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimit));
            }

            for (int d = 0; d < this.valuesPerDimensionLimits.Length; d++)
            {
                this.valuesPerDimensionLimits[d] = valuesPerDimensionLimit;
            }

            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));
            this.SeriesConfig = seriesConfig;

            this.hashCode = this.ComputeHashCode();
        }

        /// <summary>Creates a new instance of <c>MetricConfiguration</c>.</summary>
        /// <param name="seriesCountLimit">How many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="valuesPerDimensionLimits">How many different values each of the dimensions of a metric can
        /// have as a maximum. If this enumeration contains less elements than the number of supported dimensions,
        /// then the last specified element is replicated for subsequent dimensions. If this enumeration contains
        /// too many elements, superfluous elements are ignored.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</param>
        /// <param name="seriesConfig">The configuration for how each series of this metric should be aggregated.</param>
        public MetricConfiguration(
                                int seriesCountLimit,
                                IEnumerable<int> valuesPerDimensionLimits,
                                IMetricSeriesConfiguration seriesConfig)
        {
            if (seriesCountLimit < 1)
            {
                throw new ArgumentOutOfRangeException(
                                                    nameof(seriesCountLimit),
                                                    Invariant($"Metrics must allow at least one data series (but {seriesCountLimit} was specified)."));
            }

            this.SeriesCountLimit = seriesCountLimit;

            if (valuesPerDimensionLimits == null)
            {
                throw new ArgumentNullException(nameof(valuesPerDimensionLimits));
            }

            int lastLim = 0, d = 0;
            foreach (int lim in valuesPerDimensionLimits)
            {
                lastLim = lim;

                if (lastLim < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(valuesPerDimensionLimits) + "[" + d + "]");
                }

                this.valuesPerDimensionLimits[d] = lastLim;

                d++;
                if (d >= MetricIdentifier.MaxDimensionsCount)
                {
                    break;
                }
            }

            for (; d < this.valuesPerDimensionLimits.Length; d++)
            {
                this.valuesPerDimensionLimits[d] = lastLim;
            }

            Util.ValidateNotNull(seriesConfig, nameof(seriesConfig));
            this.SeriesConfig = seriesConfig;

            this.hashCode = this.ComputeHashCode();
        }

        /// <summary>Gets how many data time series a metric can contain as a maximum.
        /// Once this limit is reached, calls to <c>TrackValue(..)</c>, <c>TryGetDataSeries(..)</c> and similar
        /// that would normally result in new series will return <c>false</c>.</summary>
        public int SeriesCountLimit { get; }

        /// <summary>Gets the configuration for how each series of this metric should be aggregated.</summary>
        public IMetricSeriesConfiguration SeriesConfig { get; }

        /// <summary>
        /// Gets the maximum number of distinct values for a dimension identified by the specified 1-based dimension index.
        /// </summary>
        /// <param name="dimensionNumber">1-based dimension number. Currently it can be <c>1</c>...<c>10</c>.</param>
        /// <returns>The maximum number of distinct values for the specified dimension.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233: Operations should not overflow", Justification = "No overflow")]
        public int GetValuesPerDimensionLimit(int dimensionNumber)
        {
            MetricIdentifier.ValidateDimensionNumberForGetter(dimensionNumber, MetricIdentifier.MaxDimensionsCount);

            int dimensionIndex = dimensionNumber - 1;
            return this.valuesPerDimensionLimits[dimensionIndex];
        }

        /// <summary>Gets whether tho objects describe identical configuration.</summary>
        /// <param name="obj">A configuration object.</param>
        /// <returns>Whether tho objects describe identical configuration.</returns>
        public override bool Equals(object obj)
        {
            if (obj != null)
            {
                var otherConfig = obj as MetricConfiguration;
                if (otherConfig != null)
                {
                    return this.Equals(otherConfig);
                }
            }

            return false;
        }

        /// <summary>Gets whether tho objects describe identical configuration.</summary>
        /// <param name="other">A configuration object.</param>
        /// <returns>Whether tho objects describe identical configuration.</returns>
        public virtual bool Equals(MetricConfiguration other)
        {
            if (other == null)
            {
                return false;
            }

            if (Object.ReferenceEquals(this, other))
            {
                return true;
            }

            if ((this.SeriesCountLimit != other.SeriesCountLimit)
                    || (this.valuesPerDimensionLimits?.Length != other.valuesPerDimensionLimits?.Length)
                    || (false == this.GetType().Equals(other.GetType()))
                    || (false == this.SeriesConfig.Equals(other.SeriesConfig)))
            {
                return false;
            }

            if (this.valuesPerDimensionLimits == other.valuesPerDimensionLimits)
            {
                return true;
            }

            if (this.valuesPerDimensionLimits == null || other.valuesPerDimensionLimits == null)
            {
                return false;
            }

            for (int d = 0; d < this.valuesPerDimensionLimits.Length; d++)
            {
                if (this.valuesPerDimensionLimits[d] != other.valuesPerDimensionLimits[d])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Gets the Hash Code for this object.</summary>
        /// <returns>The Hash Code for this object.</returns>
        public override int GetHashCode()
        {
            return this.hashCode;
        }

        private int ComputeHashCode()
        {
            return Util.CombineHashCodes(
                                        this.SeriesCountLimit.GetHashCode(),
                                        Util.CombineHashCodes(this.valuesPerDimensionLimits),
                                        this.SeriesConfig.GetType().FullName.GetHashCode(),
                                        this.SeriesConfig.GetHashCode());
        }
    }
}
