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

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Optano.Algorithm.Tuner.Tests")]

namespace Optano.Algorithm.Tuner.Tuning
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;

    using Akka.Actor;

    using Optano.Algorithm.Tuner.AkkaConfiguration;
    using Optano.Algorithm.Tuner.Configuration;
    using Optano.Algorithm.Tuner.GenomeEvaluation;
    using Optano.Algorithm.Tuner.GenomeEvaluation.InstanceSelection;
    using Optano.Algorithm.Tuner.GenomeEvaluation.MiniTournaments.Actors;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage;
    using Optano.Algorithm.Tuner.GenomeEvaluation.ResultStorage.Messages;
    using Optano.Algorithm.Tuner.GenomeEvaluation.Sorting;
    using Optano.Algorithm.Tuner.Genomes;
    using Optano.Algorithm.Tuner.Genomes.Values;
    using Optano.Algorithm.Tuner.Logging;
    using Optano.Algorithm.Tuner.MachineLearning;
    using Optano.Algorithm.Tuner.MachineLearning.Prediction;
    using Optano.Algorithm.Tuner.MachineLearning.RandomForest;
    using Optano.Algorithm.Tuner.MachineLearning.TrainingData;
    using Optano.Algorithm.Tuner.Parameters;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.CovarianceMatrixAdaptation.InformationFlow;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.DifferentialEvolution;
    using Optano.Algorithm.Tuner.PopulationUpdateStrategies.GeneticAlgorithm;
    using Optano.Algorithm.Tuner.Serialization;
    using Optano.Algorithm.Tuner.TargetAlgorithm;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Instances;
    using Optano.Algorithm.Tuner.TargetAlgorithm.Results;
    using Optano.Algorithm.Tuner.TargetAlgorithm.RunEvaluators;
    using Optano.Algorithm.Tuner.Tracking;

    /// <summary>
    /// The algorithm tuner with generic options to define the model-based crossover operator.
    /// </summary>
    /// <typeparam name="TTargetAlgorithm">
    /// The algorithm that should be tuned.
    /// </typeparam>
    /// <typeparam name="TInstance">
    /// The instance type to use.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The result for an individual evaluation.
    /// </typeparam>
    /// <typeparam name="TModelLearner">
    /// The machine learning model that trains the specified <typeparamref name="TPredictorModel"/>.
    /// </typeparam>
    /// <typeparam name="TPredictorModel">
    /// The ML model that predicts the performance for a given potential offspring.
    /// </typeparam>
    /// <typeparam name="TSamplingStrategy">
    /// The strategy that is used for aggregating the observed training data before training the <typeparamref name="TPredictorModel"/>.
    /// </typeparam>
    public class AlgorithmTuner<TTargetAlgorithm, TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy> : IDisposable
        where TTargetAlgorithm : ITargetAlgorithm<TInstance, TResult>
        where TInstance : InstanceBase
        where TResult : ResultBase<TResult>, new()
        where TModelLearner : IGenomeLearner<TPredictorModel, TSamplingStrategy>
        where TPredictorModel : IEnsemblePredictor<GenomePredictionTree>
        where TSamplingStrategy : IEnsembleSamplingStrategy<IGenomePredictor>, new()
    {
        #region Fields

        /// <summary>
        /// The <see cref="GenomeBuilder" /> used in tuning.
        /// </summary>
        private readonly GenomeBuilder _genomeBuilder;

        /// <summary>
        /// The <see cref="InstanceSelector{TInstance}" /> used for tuning.
        /// </summary>
        private readonly InstanceSelector<TInstance> _instanceSelector;

        /// <summary>
        /// All <typeparamref name="TInstance"/>s potentially used for tuning.
        /// </summary>
        private readonly List<TInstance> _trainingInstances;

        /// <summary>
        /// Structure representing the tuneable parameters.
        /// </summary>
        private readonly ParameterTree _parameterTree;

        /// <summary>
        /// Object for evaluating target algorithm runs.
        /// </summary>
        private readonly IRunEvaluator<TResult> _runEvaluator;

        /// <summary>
        /// Actor system where all actors live.
        /// </summary>
        private readonly ActorSystem _targetAlgorithmRunActors;

        /// <summary>
        /// An <see cref="IActorRef" /> to a <see cref="ResultStorageActor{TInstance,TResult}" />
        /// which knows about all executed target algorithm runs and their results.
        /// </summary>
        private readonly IActorRef _targetRunResultStorage;

        /// <summary>
        /// A <see cref="IActorRef"/> to a <see cref="GenomeSorter{TInstance,TResult}"/>.
        /// </summary>
        private readonly IActorRef _genomeSorter;

        /// <summary>
        /// A number of options used for this instance.
        /// </summary>
        private readonly AlgorithmTunerConfiguration _configuration;

        /// <summary>
        /// A <see cref="LogWriter{TInstance,TResult}"/> writing interesting information to file after each generation.
        /// </summary>
        private readonly LogWriter<TInstance, TResult> _logWriter;

        /// <summary>
        /// Manages the different <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/> objects.
        /// </summary>
        private readonly PopulationUpdateStrategyManager<TInstance, TResult> _populationUpdateStrategyManager;

        /// <summary>
        /// The current generation index.
        /// </summary>
        private int _currGeneration = 0;

        /// <summary>
        /// The incumbent genome wrapper.
        /// </summary>
        private IncumbentGenomeWrapper<TResult> _incumbentGenomeWrapper;

        /// <summary>
        /// Contains <see cref="GenerationInformation"/> for all previous generations.
        /// </summary>
        private List<GenerationInformation> _informationHistory = new List<GenerationInformation>();

        /// <summary>
        /// <typeparamref name="TInstance"/>s which might be used for testing.
        /// </summary>
        private List<TInstance> _testInstances = new List<TInstance>();

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/> class.
        /// Initializes a new instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// Produces configured instances of the algorithm to tune.
        /// </param>
        /// <param name="runEvaluator">
        /// Object for evaluating target algorithm runs.
        /// </param>
        /// <param name="trainingInstances">
        /// The set of instances used for tuning.
        /// </param>
        /// <param name="parameterTree">
        /// Provides the tunable parameters.
        /// </param>
        /// <param name="configuration">
        /// Algorithm tuner configuration parameters.
        /// </param>
        public AlgorithmTuner(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            IEnumerable<TInstance> trainingInstances,
            ParameterTree parameterTree,
            AlgorithmTunerConfiguration configuration)
            : this(
                targetAlgorithmFactory,
                runEvaluator,
                trainingInstances,
                parameterTree,
                configuration,
                new GenomeBuilder(parameterTree, configuration))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/> class.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// Produces configured instances of the algorithm to tune.
        /// </param>
        /// <param name="runEvaluator">
        /// Object for evaluating target algorithm runs.
        /// </param>
        /// <param name="trainingInstances">
        /// The set of instances used for tuning.
        /// </param>
        /// <param name="parameterTree">
        /// Provides the tunable parameters.
        /// </param>
        /// <param name="configuration">
        /// Algorithm tuner configuration parameters.
        /// </param>
        /// <param name="genomeBuilder">
        /// Responsible for creation, modification and crossover of genomes.
        /// Needs to be compatible with the given parameter tree and configuration.
        /// This parameter can be left out if an ordinary <see cref="GenomeBuilder"/> should be used.
        /// </param>
        public AlgorithmTuner(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            IEnumerable<TInstance> trainingInstances,
            ParameterTree parameterTree,
            AlgorithmTunerConfiguration configuration,
            GenomeBuilder genomeBuilder)
        {
            ValidateParameters(targetAlgorithmFactory, runEvaluator, trainingInstances, parameterTree, configuration, genomeBuilder);
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Tuning target algorithm with {parameterTree.GetParameters().Count()} parameters.");

            this._configuration = configuration;
            this._runEvaluator = runEvaluator;

            this.GeneticEngineering = new GeneticEngineering<TModelLearner, TPredictorModel, TSamplingStrategy>(parameterTree, this._configuration);
            this.IncumbentQuality = new List<double>();

            this._trainingInstances = trainingInstances.ToList();
            this._instanceSelector = new InstanceSelector<TInstance>(this._trainingInstances, configuration);
            this._parameterTree = parameterTree;
            this._genomeBuilder = genomeBuilder;

            this._targetAlgorithmRunActors = ActorSystem.Create(AkkaNames.ActorSystemName, configuration.AkkaConfiguration);
            this._targetRunResultStorage = this._targetAlgorithmRunActors.ActorOf(
                Props.Create(() => new ResultStorageActor<TInstance, TResult>()),
                AkkaNames.ResultStorageActor);
            this._genomeSorter = this._targetAlgorithmRunActors.ActorOf(
                Props.Create(() => new GenomeSorter<TInstance, TResult>(runEvaluator)),
                AkkaNames.GenomeSorter);

            this._logWriter = new LogWriter<TInstance, TResult>(parameterTree, configuration);

            this._populationUpdateStrategyManager = new PopulationUpdateStrategyManager<TInstance, TResult>(
                this.CreatePopulationUpdateStrategies(targetAlgorithmFactory, runEvaluator).ToList(),
                configuration);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the incumbent quality.
        /// </summary>
        private List<double> IncumbentQuality { get; set; }

        /// <summary>
        /// Gets or sets the genetic engineering.
        /// </summary>
        private GeneticEngineering<TModelLearner, TPredictorModel, TSamplingStrategy> GeneticEngineering { get; set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Sets the instances to use for potential testing.
        /// </summary>
        /// <param name="instances">
        /// <typeparamref name="TInstance"/>s which should be used for testing.
        /// </param>
        public void SetTestInstances(IEnumerable<TInstance> instances)
        {
            if (instances == null)
            {
                this.Dispose();
                throw new ArgumentNullException(nameof(instances));
            }

            this._testInstances = new List<TInstance>(instances);
        }

        /// <summary>
        /// Reads in a status dump and sets the algorithm tuner to continue at that point.
        /// Only call this method if <see cref="Run"/> has not been called yet.
        /// </summary>
        /// <param name="pathToStatusFile">
        /// Path to status file.
        /// </param>
        [SuppressMessage(
            "NDepend",
            "ND3101:DontUseSystemRandomForSecurityPurposes",
            Justification = "User has no control over RNG seed. We want to continue with a new random value. No security related purpose.")]
        public void UseStatusDump(string pathToStatusFile)
        {
            if (this._currGeneration > 0)
            {
                throw new InvalidOperationException("Status dump should only be read in at the very beginning of tuning.");
            }

            // Read status from file.
            var status = StatusBase.ReadFromFile<Status<TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy>>(pathToStatusFile);

            // Check if old tuner parameters match the new ones.
            if (this._configuration.StrictCompatibilityCheck && !this._configuration.IsCompatible(status.Configuration))
            {
                throw new InvalidOperationException(
                    $"Current configuration is too different from the one used to create status file {pathToStatusFile}.");
            }

            if (!this._configuration.IsTechnicallyCompatible(status.Configuration))
            {
                throw new InvalidOperationException(
                    $"It is technically not possible to continue the tuning of {pathToStatusFile} with the current configuration.");
            }

            if (status.ElapsedTime > TimeSpan.Zero)
            {
                this._logWriter.SetElapsedTimeOffset(status.ElapsedTime);
            }

            // Update fields.
            this._currGeneration = status.Generation;
            this._incumbentGenomeWrapper = status.IncumbentGenomeWrapper;
            this.IncumbentQuality = status.IncumbentQuality;
            this.GeneticEngineering = status.GeneticEngineering;
            this.GeneticEngineering?.RestoreInternalDictionariesWithCorrectComparers();
            this._informationHistory = status.InformationHistory;

            // Send all run results to storage.
            foreach (var genomeResults in status.RunResults)
            {
                foreach (var result in genomeResults.Value)
                {
                    this._targetRunResultStorage.Tell(new ResultMessage<TInstance, TResult>(genomeResults.Key, result.Key, result.Value));
                }
            }

            // Restore status of all population update strategies.
            this._populationUpdateStrategyManager.UseStatusDump(status.CurrentUpdateStrategyIndex, status.Population, this.GeneticEngineering);

            Randomizer.Reset();
            Randomizer.Configure(new Random().Next());
        }

        /// <summary>
        /// Starts a new OPTANO Algorithm Tuner run.
        /// </summary>
        /// <returns>Best parameters found for the target algorithm.</returns>
        public Dictionary<string, IAllele> Run()
        {
            if (!this._populationUpdateStrategyManager.HasPopulation)
            {
                var initialPopulation = this.InitializePopulation();
                this._populationUpdateStrategyManager.Initialize(initialPopulation);
            }

            for (; this._currGeneration < this._configuration.Generations; this._currGeneration++)
            {
                // Check at the start of the generation to catch the limit also for continued runs.
                if (this.IsEvaluationLimitMet())
                {
                    break;
                }

                LoggingHelper.WriteLine(VerbosityLevel.Info, $"Generation {this._currGeneration}/{this._configuration.Generations}.");
                this._populationUpdateStrategyManager.CurrentStrategy.LogPopulationToConsole();

                var instancesForEvaluation = this._instanceSelector.Select(this._currGeneration).ToList();

                var currentStrategy = this._populationUpdateStrategyManager.ChangePopulationUpdateStrategy(
                    instancesForEvaluation,
                    this._incumbentGenomeWrapper);
                currentStrategy.PerformIteration(this._currGeneration, instancesForEvaluation);
                this.UpdateIncumbentGenomeWrapper(currentStrategy.FindIncumbentGenome());

                this.UpdateGenerationHistory(currentStrategy);
                this.TrackConvergenceBehavior();

                if (this._currGeneration != this._configuration.Generations - 1)
                {
                    // Functions depending on the complete population may behave unexpectedly in the final generation
                    // if strategies ignore certain steps for that generation to speed up the tuning.
                    currentStrategy.ExportFeatureStandardDeviations();
                    this.DumpStatus();
                }

                this.LogFinishedGeneration();
            }

            this._populationUpdateStrategyManager.FinishPhase();

            this.LogStatistics();
            RunStatisticTracker.ExportConvergenceBehavior(this.IncumbentQuality);

            // Return best parameters.
            return this._incumbentGenomeWrapper.IncumbentGenome.GetFilteredGenes(this._parameterTree);
        }

        /// <summary>
        /// Completes the generation information history and exports it to file.
        /// </summary>
        public void CompleteAndExportGenerationHistory()
        {
            if (this._configuration.ScoreGenerationHistory &&
                this._runEvaluator is IMetricRunEvaluator<TResult> metricRunEvaluator)
            {
                var scorer = new GenerationInformationScorer<TInstance, TResult>(
                    this._genomeSorter,
                    this._targetRunResultStorage,
                    metricRunEvaluator);

                scorer.ScoreInformationHistory(this._informationHistory, this._trainingInstances, this._testInstances);
                RunStatisticTracker.ExportAverageIncumbentScores(this._informationHistory, this._configuration.EvaluationLimit);
            }

            RunStatisticTracker.ExportGenerationHistory(this._informationHistory);
        }

        /// <summary>
        /// Disposes of the underlying <see cref="ActorSystem"/>.
        /// </summary>
        public void Dispose()
        {
            System.Threading.Tasks.Task.Run(async () => await this._targetAlgorithmRunActors?.Terminate()).Wait();
            this._targetAlgorithmRunActors?.Dispose();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the parameters:
        /// * Null checks for every one of them.
        /// * Checks that the number of training instances provided is sufficient considering the given configuration.
        /// * Checks that configuration about whether to use run time tuning
        /// and the usage of the run time tuning result interface fit together
        /// * Checks that the parameter tree contains parameters.
        /// * Checks that those parameters' identifiers are unique.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// Produces configured instances of the target algorithm to tune.
        /// </param>
        /// <param name="runEvaluator">
        /// Object for evaluating target algorithm runs.
        /// </param>
        /// <param name="trainingInstances">
        /// The set of instances used for tuning.
        /// </param>
        /// <param name="parameterTree">
        /// The parameter tree.
        /// </param>
        /// <param name="configuration">
        /// Algorithm tuner configuration parameters.
        /// </param>
        /// <param name="genomeBuilder">
        /// The genome builder.
        /// </param>
        private static void ValidateParameters(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator,
            IEnumerable<TInstance> trainingInstances,
            ParameterTree parameterTree,
            AlgorithmTunerConfiguration configuration,
            GenomeBuilder genomeBuilder)
        {
            // Check argument for nulls.
            if (targetAlgorithmFactory == null)
            {
                throw new ArgumentNullException(nameof(targetAlgorithmFactory), "You must specify a target algorithm factory.");
            }

            if (runEvaluator == null)
            {
                throw new ArgumentNullException(nameof(runEvaluator), "You must specify a run evaluator.");
            }

            if (trainingInstances == null)
            {
                throw new ArgumentNullException(nameof(trainingInstances), "You must specify a list of training instances.");
            }

            if (parameterTree == null)
            {
                throw new ArgumentNullException(nameof(parameterTree), "You must specify a parameter tree.");
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration), "You must specify an algorithm tuner configuration.");
            }

            if (genomeBuilder == null)
            {
                throw new ArgumentNullException(nameof(genomeBuilder), "You must specify a genome builder.");
            }

            // Check enough training instances have been provided.
            var numInstances = trainingInstances.Count();
            if (numInstances < configuration.EndNumInstances)
            {
                throw new ArgumentException(
                    $"In the end, {configuration.EndNumInstances} training instances should be used, but only {numInstances} have been provided.",
                    nameof(trainingInstances));
            }

            // Check that the parameter tree is valid.
            if (!parameterTree.ContainsParameters())
            {
                throw new ArgumentException("Specified parameter tree without parameters.", nameof(parameterTree));
            }

            if (!parameterTree.IdentifiersAreUnique())
            {
                throw new ArgumentException("Specified parameter tree contained duplicate identifiers.", nameof(parameterTree));
            }
        }

        /// <summary>
        /// Creates the different <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>s that may be used to
        /// update the population.
        /// </summary>
        /// <param name="targetAlgorithmFactory">
        /// Produces configured instances of the target algorithm to tune.
        /// </param>
        /// <param name="runEvaluator">
        /// Object for evaluating target algorithm runs.
        /// </param>
        /// <returns>The created <see cref="IPopulationUpdateStrategy{TInstance,TResult}"/>s.</returns>
        private IEnumerable<IPopulationUpdateStrategy<TInstance, TResult>> CreatePopulationUpdateStrategies(
            ITargetAlgorithmFactory<TTargetAlgorithm, TInstance, TResult> targetAlgorithmFactory,
            IRunEvaluator<TResult> runEvaluator)
        {
            var tournamentSelector = this._targetAlgorithmRunActors.ActorOf(
                Props.Create(
                    () => new TournamentSelector<TTargetAlgorithm, TInstance, TResult>(
                        targetAlgorithmFactory,
                        runEvaluator,
                        this._configuration,
                        this._targetRunResultStorage,
                        this._parameterTree)),
                AkkaNames.TournamentSelector);
            yield return new GgaStrategy<TInstance, TResult>(
                this._configuration,
                this._parameterTree,
                this._genomeBuilder,
                tournamentSelector,
                this.GeneticEngineering);

            if (this._configuration.ContinuousOptimizationMethod == ContinuousOptimizationMethod.Jade)
            {
                yield return new DifferentialEvolutionStrategy<TInstance, TResult>(
                    this._configuration,
                    this._parameterTree,
                    this._genomeBuilder,
                    this._genomeSorter,
                    this._targetRunResultStorage);
            }

            if (this._configuration.ContinuousOptimizationMethod == ContinuousOptimizationMethod.CmaEs)
            {
                yield return CovarianceMatrixAdaptationInformationFlowSwitch.CreateCovarianceMatrixAdaptationStrategy<TInstance, TResult>(
                    this._configuration,
                    this._parameterTree,
                    this._genomeBuilder,
                    this._genomeSorter,
                    this._targetRunResultStorage);
            }
        }

        /// <summary>
        /// Updates <see cref="_informationHistory"/> with the current (finished) generation.
        /// </summary>
        /// <param name="currentStrategy">The current strategy.</param>
        private void UpdateGenerationHistory(IPopulationUpdateStrategy<TInstance, TResult> currentStrategy)
        {
            var evaluationCountRequest =
                this._targetRunResultStorage.Ask<EvaluationStatistic>(new EvaluationStatisticRequest());
            evaluationCountRequest.Wait();

            var generationInformation = new GenerationInformation(
                generation: this._currGeneration,
                totalNumberOfEvaluations: evaluationCountRequest.Result.TotalEvaluationCount,
                strategy: currentStrategy.GetType(),
                incumbent: new ImmutableGenome(this._incumbentGenomeWrapper.IncumbentGenome));
            this._informationHistory.Add(generationInformation);
        }

        /// <summary>
        /// Determines whether the maximum number of evaluations is met.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if evaluation limit is met; otherwise, <c>false</c>.
        /// </returns>
        private bool IsEvaluationLimitMet()
        {
            var evaluationCountRequest = this._targetRunResultStorage.Ask<EvaluationStatistic>(new EvaluationStatisticRequest());
            evaluationCountRequest.Wait();

            if (evaluationCountRequest.Result.TotalEvaluationCount >= this._configuration.EvaluationLimit)
            {
                LoggingHelper.WriteLine(
                    VerbosityLevel.Info,
                    $"Terminating OPTANO Algorithm Tuner after {evaluationCountRequest.Result.TotalEvaluationCount} evaluations.");
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Logs information about the finished generation to a log file.
        /// </summary>
        private void LogFinishedGeneration()
        {
            // Ask for all run results of best genome.
            var resultRequest = this._targetRunResultStorage.Ask<GenomeResults<TInstance, TResult>>(
                new GenomeResultsRequest(new ImmutableGenome(this._incumbentGenomeWrapper.IncumbentGenome)));
            resultRequest.Wait();

            // Ask for total number evaluations.
            var evaluationCountRequest = this._targetRunResultStorage.Ask<EvaluationStatistic>(new EvaluationStatisticRequest());
            evaluationCountRequest.Wait();

            this._logWriter.LogFinishedGeneration(
                this._currGeneration + 1,
                evaluationCountRequest.Result.TotalEvaluationCount,
                this._incumbentGenomeWrapper.IncumbentGenome,
                resultRequest.Result);
        }

        /// <summary>
        /// Writes the current status (generation, population, run results) to file.
        /// </summary>
        private void DumpStatus()
        {
            // Keep old status files in zipped form if desired.
            // At generation 0, no old files exist, so nothing has to be zipped.
            if (this._configuration.ZipOldStatusFiles && this._currGeneration > 0)
            {
                var zipFileName = Path.Combine(this._configuration.ZippedStatusFileDirectory, $"status_{this._currGeneration}.zip");
                ZipFile.CreateFromDirectory(this._configuration.StatusFileDirectory, zipFileName);
            }

            // Add current generation, population and the configuration to status.
            // Use +1 because we log AFTER going to the next generation, but BEFORE increasing the field.
            var status = new Status<TInstance, TResult, TModelLearner, TPredictorModel, TSamplingStrategy>(
                this._currGeneration + 1,
                this._populationUpdateStrategyManager.BasePopulation,
                this._configuration,
                this.GeneticEngineering,
                this._populationUpdateStrategyManager.CurrentUpdateStrategyIndex,
                this.IncumbentQuality,
                this._incumbentGenomeWrapper,
                this._informationHistory,
                this._logWriter.TotalElapsedTime);

            // Ask for run results and add those.
            var resultRequest = this._targetRunResultStorage.Ask<AllResults<TInstance, TResult>>(new AllResultsRequest());
            resultRequest.Wait();
            status.SetRunResults(resultRequest.Result.RunResults);

            // Write the whole status to file.
            status.WriteToFile(Path.Combine(this._configuration.StatusFileDirectory, AlgorithmTunerConfiguration.FileName));

            // Finally dump all strategies.
            this._populationUpdateStrategyManager.DumpStatus();
        }

        /// <summary>
        /// Logs statistics about a finished run of this
        /// <see cref="AlgorithmTuner{TTargetAlgorithm,TInstance,TResult,TModelLearner,TPredictorModel,TSamplingStrategy}"/>
        /// instance.
        /// </summary>
        private void LogStatistics()
        {
            var evaluationStatisticRequest = this._targetRunResultStorage.Ask<EvaluationStatistic>(new EvaluationStatisticRequest());
            evaluationStatisticRequest.Wait();
            var statistic = evaluationStatisticRequest.Result;
            LoggingHelper.WriteLine(
                VerbosityLevel.Info,
                $"Explored {statistic.ConfigurationCount} configurations, using a total number of {statistic.TotalEvaluationCount} evaluations.");
        }

        /// <summary>
        /// Creates a population consisting of random genomes having an age and gender such that:
        /// * The size of the non-competitive and competitive parts of the population differs at most by 1.
        /// * The total size of the population matches the one specified by the <see cref="_configuration" />.
        /// * Age is distributed evenly in both population parts.
        /// </summary>
        /// <returns>The created <see cref="Population"/>.</returns>
        private Population InitializePopulation()
        {
            var population = new Population(this._configuration);

            // Decide on the number of genomes each population should get.
            var populationSizeIsOdd = this._configuration.PopulationSize % 2 == 1;
            var sizeOfNonCompetitivePopulation = populationSizeIsOdd && Randomizer.Instance.Decide()
                                                     ? (this._configuration.PopulationSize / 2) + 1
                                                     : this._configuration.PopulationSize / 2;

            // Then add random genomes.
            foreach (var nonCompetitive in this.CreateRandomGenomes(sizeOfNonCompetitivePopulation))
            {
                population.AddGenome(nonCompetitive, false);
            }

            foreach (var competitive in this.CreateRandomGenomes(this._configuration.PopulationSize - sizeOfNonCompetitivePopulation))
            {
                population.AddGenome(competitive, true);
            }

            return population;
        }

        /// <summary>
        /// Creates random genomes with a random age such that age is distributed evenly.
        /// </summary>
        /// <param name="numberIndividuals">
        /// The number of genomes to add.
        /// </param>
        /// <returns>The created <see cref="Genome"/> objects.</returns>
        private IEnumerable<Genome> CreateRandomGenomes(int numberIndividuals)
        {
            // Begin with a random, legal age at least 1.
            var nextAge = 1 + Randomizer.Instance.Next(this._configuration.MaxGenomeAge);

            // Then add the specified number of random individuals.
            for (var i = 0; i < numberIndividuals; i++)
            {
                var genome = this._genomeBuilder.CreateRandomGenome(nextAge);
                yield return genome;

                // Age is incremented by 1 and legalized after every add operation.
                nextAge = 1 + (nextAge % this._configuration.MaxGenomeAge);
            }
        }

        /// <summary>
        /// Updates the incumbent genome wrapper.
        /// </summary>
        /// <param name="generationBest">
        /// Best genome of most recent generation.
        /// </param>
        private void UpdateIncumbentGenomeWrapper(IncumbentGenomeWrapper<TResult> generationBest)
        {
            if (this._incumbentGenomeWrapper == null || !new Genome.GeneValueComparator().Equals(
                    this._incumbentGenomeWrapper.IncumbentGenome,
                    generationBest.IncumbentGenome))
            {
                this._incumbentGenomeWrapper = generationBest;
                LoggingHelper.WriteLine(VerbosityLevel.Info, "Found new incumbent.");
                LoggingHelper.WriteLine(
                    VerbosityLevel.Info,
                    $"Incumbent Genome:\r\n{this._incumbentGenomeWrapper.IncumbentGenome.ToFilteredGeneString(this._parameterTree)}");
            }
            else
            {
                // we want to log the most recent results
                this._incumbentGenomeWrapper.IncumbentInstanceResults = generationBest.IncumbentInstanceResults;
                LoggingHelper.WriteLine(
                    VerbosityLevel.Debug,
                    $"Incumbent Genome:\r\n{this._incumbentGenomeWrapper.IncumbentGenome.ToFilteredGeneString(this._parameterTree)}");
            }
        }

        /// <summary>
        /// Tracks the convergence behavior of the algorithm and logs it to csv.
        /// </summary>
        private void TrackConvergenceBehavior()
        {
            var metricRunEvaluator = this._runEvaluator as IMetricRunEvaluator<TResult>;
            if (metricRunEvaluator == null || !this._configuration.TrackConvergenceBehavior)
            {
                return;
            }

            var currentAverage =
                RunStatisticTracker.TrackConvergenceBehavior(this._incumbentGenomeWrapper, metricRunEvaluator);
            this.IncumbentQuality.Add(currentAverage);
        }

        #endregion
    }
}