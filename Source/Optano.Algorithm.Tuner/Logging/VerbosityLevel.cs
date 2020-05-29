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

namespace Optano.Algorithm.Tuner.Logging
{
    /// <summary>
    /// Levels of verbosity for console output.
    /// </summary>
    public enum VerbosityLevel
    {
        /// <summary>
        /// No console output apart from warnings, errors and downing of computing nodes.
        /// </summary>
        Warn,

        /// <summary>
        /// Prints everything printed by <see cref="Warn"/>. 
        /// Additionally prints the most relevant information, like generation numbers or the discovery of a new best genome.
        /// </summary>
        Info,

        /// <summary>
        /// Prints everything printed by <see cref="Info"/>.
        /// Additionally prints detailed information about the run, e.g. current population.
        /// </summary>
        Debug,

        /// <summary>
        /// Prints everything printed by <see cref="Debug"/>.
        /// Additionally prints information about internal mechanisms as well as very detailed run information,
        /// e.g. calls to or results from the target algorithm.
        /// </summary>
        Trace,
    }
}