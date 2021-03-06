﻿#region Copyright (c) OPTANO GmbH

// ////////////////////////////////////////////////////////////////////////////////
// 
//        OPTANO GmbH Source Code
//        Copyright (c) 2010-2020 OPTANO GmbH
//        ALL RIGHTS RESERVED.
// 
//    The entire contents of this file is protected by German and
//    International Copyright Laws. Unauthorized reproduction,
//    reverse-engineering, and distribution of all or any portion of
//    the code contained in this file is strictly prohibited and may
//    result in severe civil and criminal penalties and will be
//    prosecuted to the maximum extent possible under the law.
// 
//    RESTRICTIONS
// 
//    THIS SOURCE CODE AND ALL RESULTING INTERMEDIATE FILES
//    ARE CONFIDENTIAL AND PROPRIETARY TRADE SECRETS OF
//    OPTANO GMBH.
// 
//    THE SOURCE CODE CONTAINED WITHIN THIS FILE AND ALL RELATED
//    FILES OR ANY PORTION OF ITS CONTENTS SHALL AT NO TIME BE
//    COPIED, TRANSFERRED, SOLD, DISTRIBUTED, OR OTHERWISE MADE
//    AVAILABLE TO OTHER INDIVIDUALS WITHOUT WRITTEN CONSENT
//    AND PERMISSION FROM OPTANO GMBH.
// 
// ////////////////////////////////////////////////////////////////////////////////

#endregion

namespace Optano.Algorithm.Tuner.Tests.PopulationUpdateStrategies.DifferentialEvolution
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using Optano.Algorithm.Tuner;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution.SearchPoint;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.Tests.Serialization;
    using Optano.Algorithm.Tuner.Tests.TargetAlgorithm.InterfaceImplementations;

    using Xunit;

    /// <summary>
    /// Contains tests for the <see cref="DifferentialEvolutionStrategyStatus{TInstance}"/> class.
    /// </summary>
    public class DifferentialEvolutionStrategyStatusTest
        : StatusBaseTest<DifferentialEvolutionStrategyStatus<TestInstance>>
    {
        #region Fields

        /// <summary>
        /// Evaluation instances used in tests.
        /// </summary>
        private readonly List<TestInstance> _currentEvaluationInstances = new List<TestInstance>();

        /// <summary>
        /// Sorting used in tests.
        /// </summary>
        private readonly List<GenomeSearchPoint> _mostRecentSorting = new List<GenomeSearchPoint>();

        /// <summary>
        /// Incumbent genome used in tests.
        /// </summary>
        private Genome _originalIncumbent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets a path to which the status file will get written in tests.
        /// </summary>
        protected override string StatusFilePath => PathUtils.GetAbsolutePathFromExecutableFolderRelative(
            Path.Combine("status", DifferentialEvolutionStrategyStatus<TestInstance>.FileName));

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyStatusBase{TSearchPoint,TInstance}.CurrentEvaluationInstances"/>
        /// returns the instances provided on initialization.
        /// </summary>
        [Fact]
        public void EvaluationInstancesAreSetCorrectly()
        {
            var status = new DifferentialEvolutionStrategyStatus<TestInstance>(
                this._originalIncumbent,
                this._currentEvaluationInstances,
                this._mostRecentSorting);
            Assert.Equal(
                this._currentEvaluationInstances,
                status.CurrentEvaluationInstances);
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyStatusBase{TSearchPoint,TestInstance}.MostRecentSorting"/>
        /// returns the sorting provided on initialization.
        /// </summary>
        [Fact]
        public void SortingIsSetCorrectly()
        {
            var status = new DifferentialEvolutionStrategyStatus<TestInstance>(
                this._originalIncumbent,
                this._currentEvaluationInstances,
                this._mostRecentSorting);
            Assert.Equal(
                this._mostRecentSorting,
                status.MostRecentSorting);
        }

        /// <summary>
        /// Checks that
        /// <see cref="ContinuousOptimizationStrategyStatusBase{TSearchPoint,TestInstance}.OriginalIncumbent"/>
        /// returns the genome provided on initialization.
        /// </summary>
        [Fact]
        public void OriginalIncumbentIsSetCorrectly()
        {
            var status = new DifferentialEvolutionStrategyStatus<TestInstance>(
                this._originalIncumbent,
                this._currentEvaluationInstances,
                this._mostRecentSorting);
            Assert.Equal(
                this._originalIncumbent.ToString(),
                status.OriginalIncumbent.ToString());
        }

        /// <summary>
        /// Checks that <see cref="StatusBase.ReadFromFile{Status}"/> correctly deserializes a
        /// status object written to file by <see cref="StatusBase.WriteToFile"/>.
        /// </summary>
        [Fact]
        public override void ReadFromFileDeserializesCorrectly()
        {
            var status = new DifferentialEvolutionStrategyStatus<TestInstance>(
                this._originalIncumbent,
                this._currentEvaluationInstances,
                this._mostRecentSorting);
            status.WriteToFile(this.StatusFilePath);
            var deserializedStatus =
                StatusBase.ReadFromFile<DifferentialEvolutionStrategyStatus<TestInstance>>(this.StatusFilePath);

            // Use to string to not compare references.
            Assert.Equal(
                status.OriginalIncumbent.ToString(),
                deserializedStatus.OriginalIncumbent.ToString());
            Assert.Equal(
                status.MostRecentSorting.Select(point => point.Genome.ToString()).ToArray(),
                deserializedStatus.MostRecentSorting.Select(point => point.Genome.ToString()).ToArray());
            Assert.Equal(
                status.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray(),
                deserializedStatus.CurrentEvaluationInstances.Select(instance => instance.ToString()).ToArray());
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called before every test case.
        /// </summary>
        protected override void InitializeDefault()
        {
            var parameterTree = this.GetDefaultParameterTree();
            var genomeBuilder = new GenomeBuilder(parameterTree, this.GetDefaultAlgorithmTunerConfiguration());

            this._originalIncumbent = genomeBuilder.CreateRandomGenome(age: 2);

            for (int i = 0; i < 5; i++)
            {
                var searchPoint = GenomeSearchPoint.CreateFromGenome(
                    genomeBuilder.CreateRandomGenome(age: 0),
                    parameterTree,
                    minimumDomainSize: 5,
                    genomeBuilder: genomeBuilder);
                this._mostRecentSorting.Add(searchPoint);
                this._currentEvaluationInstances.Add(new TestInstance(i.ToString()));
            }
        }

        /// <summary>
        /// Creates a status object which can be (de)serialized successfully.
        /// </summary>
        /// <returns>The created object.</returns>
        protected override DifferentialEvolutionStrategyStatus<TestInstance> CreateTestStatus()
        {
            return new DifferentialEvolutionStrategyStatus<TestInstance>(
                this._originalIncumbent,
                this._currentEvaluationInstances,
                this._mostRecentSorting);
        }

        #endregion
    }
}