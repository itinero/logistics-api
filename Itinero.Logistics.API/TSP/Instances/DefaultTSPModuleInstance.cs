// Itinero - OpenStreetMap (OSM) SDK
// Copyright (C) 2016 Abelshausen Ben
// 
// This file is part of Itinero.
// 
// Itinero is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// Itinero is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Itinero. If not, see <http://www.gnu.org/licenses/>.

using Itinero.Logistics.Routing.TSP;
using Itinero.Logistics.Solutions.TSP.GA.EAX;
using Itinero.Algorithms;
using Itinero.Profiles;
using System;
using System.Collections.Generic;
using Itinero.LocalGeo;
using Itinero.Attributes;

namespace Itinero.Logistics.API.TSP.Instances
{
    /// <summary>
    /// A default routing module instance implemenation.
    /// </summary>
    public class DefaultTSPModuleInstance : ITSPModuleInstance
    {
        private readonly Router _router;
        private readonly Func<Profile, Coordinate[], IWeightMatrixAlgorithm> _createMatrixCalculator;

        /// <summary>
        /// Creates a new default routing instance.
        /// </summary>
        public DefaultTSPModuleInstance(Router router, Func<Profile, Coordinate[], IWeightMatrixAlgorithm> createMatrixCalculator)
        {
            _router = router;
            _createMatrixCalculator = createMatrixCalculator;
        }

        /// <summary>
        /// Creates a new default routing instance.
        /// </summary>
        public DefaultTSPModuleInstance(Router router)
            : this(router, (vehicle, locations) =>
            {
                return new Itinero.Algorithms.WeightMatrixAlgorithm(
                    router, vehicle, locations);
            })
        {

        }

        /// <summary>
        /// Returns true if the given profile is supported.
        /// </summary>
        public bool Supports(Profile profile)
        {
            return _router.SupportsAll(profile);
        }

        /// <summary>
        /// Calculates a route along the given locations.
        /// </summary>
        public Result<Route> Calculate(Profile profile, Coordinate[] locations, AttributeCollection[] attributes, bool? closed,
            Dictionary<string, object> parameters)
        {
            // create matrix calculator.
            var matrixCalculator = new WeightMatrixAlgorithm(
                _router, profile, locations, (edge, i) =>
                {
                    if (attributes.Length > 0)
                    {
                        var edgeTags = _router.Db.GetProfileAndMeta(
                            edge.Data.Profile, edge.Data.MetaId);
                        var locationTags = attributes[i];
                        string locationName;
                        string edgeName;
                        if (edgeTags.TryGetValue("name", out edgeName) &&
                                locationTags.TryGetValue("name", out locationName))
                        {
                            if (!string.IsNullOrWhiteSpace(edgeName) &&
                                   !string.IsNullOrWhiteSpace(locationName))
                            {
                                if (edgeName.LevenshteinMatch(locationName, 90))
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                    return true;
                });
            matrixCalculator.SearchDistanceInMeter = 200;
            matrixCalculator.Run();

            // check success.
            if (!matrixCalculator.HasSucceeded)
            { // weight matrix calculation failed.
                return new Result<Route>("Calculating weight matrix failed.");
            }

            // verify minimum results.
            if (matrixCalculator.RouterPoints.Count < 1 ||
                matrixCalculator.Errors.ContainsKey(0))
            {
                return new Result<Route>("Calculating weight matrix failed.");
            }

            // create TSP router.
            int? last = locations.Length - 1;
            if (closed.HasValue && closed.Value)
            { // problem is closed.
                last = 0;
            }
            else if (closed.HasValue && !closed.Value)
            { // problem is open.
                last = null;
            }
            var tspRouter = new TSPRouter(_router, profile, locations, 0, last, 
                new EAXSolver(new Itinero.Logistics.Solvers.GA.GASettings()
                {
                    CrossOverPercentage = 10,
                    ElitismPercentage = 2,
                    PopulationSize = 300,
                    MaxGenerations = 100000,
                    MutationPercentage = 0,
                    StagnationCount = 100
                }), matrixCalculator);
            tspRouter.Run();
            
            // check success.
            if (!tspRouter.HasSucceeded)
            { // weight matrix calculation failed.
                return new Result<Route>("Calculating final route failed.");
            }

            return new Result<Route>(tspRouter.BuildRoute());
        }
    }
}