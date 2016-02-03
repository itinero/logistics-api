// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2016 Abelshausen Ben
// 
// This file is part of OsmSharp.
// 
// OsmSharp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 2 of the License, or
// (at your option) any later version.
// 
// OsmSharp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with OsmSharp. If not, see <http://www.gnu.org/licenses/>.

using OsmSharp.Collections.Tags;
using OsmSharp.Geo;
using OsmSharp.Geo.Features;
using OsmSharp.Logistics.Routing.TSP;
using OsmSharp.Math.Geo;
using OsmSharp.Routing;
using OsmSharp.Routing.Algorithms;
using OsmSharp.Routing.Profiles;
using System;
using System.Collections.Generic;

namespace OsmSharp.Logistics.API.TSP.Instances
{
    /// <summary>
    /// A default routing module instance implemenation.
    /// </summary>
    public class DefaultTSPModuleInstance : ITSPModuleInstance
    {
        private readonly IRouter _router;
        private readonly Func<Profile, GeoCoordinate[], IWeightMatrixAlgorithm> _createMatrixCalculator;

        /// <summary>
        /// Creates a new default routing instance.
        /// </summary>
        public DefaultTSPModuleInstance(IRouter router, Func<Profile, GeoCoordinate[], IWeightMatrixAlgorithm> createMatrixCalculator)
        {
            _router = router;
            _createMatrixCalculator = createMatrixCalculator;
        }

        /// <summary>
        /// Creates a new default routing instance.
        /// </summary>
        public DefaultTSPModuleInstance(IRouter router)
            : this(router, (vehicle, locations) =>
            { 
                return new OsmSharp.Routing.Algorithms.WeightMatrixAlgorithm(
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
        public Result<Route> Calculate(Profile profile, GeoCoordinate[] locations, bool? closed,
            Dictionary<string, object> parameters)
        {
            // create matrix calculator.
            var matrixCalculator = _createMatrixCalculator(profile, locations);
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
            if(closed.HasValue && closed.Value)
            { // problem is closed.
                last = 0;
            }
            else
            { // problem is open.
                last = null;
            }
            var tspRouter = new TSPRouter(_router, profile, locations, 0, last, matrixCalculator);
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
