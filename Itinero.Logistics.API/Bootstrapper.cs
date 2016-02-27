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

using Nancy;
using Itinero.Logistics.API.Configurations;
using Itinero;
using Itinero.Osm.Vehicles;
using System;
using System.Configuration;
using System.IO;

namespace Itinero.Logistics.API
{
    /// <summary>
    /// A bootstrapper.
    /// </summary>
    public static class Bootstrapper
    {
        /// <summary>
        /// Initializes all routing instance from the configuration in the configuration file.
        /// </summary>
        public static void BootFromConfiguration()
        {
            // register vehicle profiles.
            Vehicle.RegisterVehicles();

            // enable logging and use the console as output.
            Itinero.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", origin, level, message));
            };
            Itinero.Logistics.Logging.Logger.LogAction = (origin, level, message, parameters) =>
            {
                Console.WriteLine(string.Format("[{0}] {1} - {2}", origin, level, message));
            };

            // get the api configuration.
            var apiConfiguration = (ApiConfiguration)ConfigurationManager.GetSection("ApiConfiguration");

            // load all relevant routers.
            foreach (InstanceConfiguration instanceConfiguration in apiConfiguration.Instances)
            {
                var thread = new System.Threading.Thread(new System.Threading.ThreadStart(() =>
                {                   
                    // create routing instance.
                    Itinero.Logistics.Logging.Logger.Log("Bootstrapper", Itinero.Logistics.Logging.TraceEventType.Information,
                        string.Format("Creating {0} instance...", instanceConfiguration.Name));

                    // load data.
                    RouterDb routerDb = null;
                    using(var stream = new FileInfo(instanceConfiguration.RouterDb).OpenRead())
                    {
                        routerDb = RouterDb.Deserialize(stream);
                    }

                    // register instance.
                    var router = new Router(routerDb);
                    router.CustomRouteBuilder = (db, profile, getFactor, 
                        source, target, path) =>
                    {
                        var builder = new Itinero.Algorithms.Routes.FastRouteBuilder(
                            db, profile, getFactor, source, target, path);
                        builder.Run();
                        if(builder.HasSucceeded)
                        {
                            return new Result<Route>(builder.Route);
                        }
                        return new Result<Route>("Building route failed.");
                    };
                    
                    router.ProfileFactorCache = new Itinero.Profiles.ProfileFactorCache(router.Db);
                    router.ProfileFactorCache.CalculateFor(Vehicle.Car.Fastest());

                    TSP.TSPBootstrapper.Register(instanceConfiguration.Name,
                        new TSP.Instances.DefaultTSPModuleInstance(router));

                    Itinero.Logistics.Logging.Logger.Log("Bootstrapper", Itinero.Logistics.Logging.TraceEventType.Information,
                        string.Format("Instance {0} created successfully!", instanceConfiguration.Name));
                }));
                thread.Start();
            }
        }

        /// <summary>
        /// A function to validate requests.
        /// </summary>
        public static Func<NancyModule, dynamic, bool> ValidateRequest = (m, _) =>
        {
            return true;
        };
    }
}