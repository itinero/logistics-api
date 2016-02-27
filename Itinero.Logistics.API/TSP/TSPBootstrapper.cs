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

using System.Collections.Generic;

namespace Itinero.Logistics.API.TSP
{
    /// <summary>
    /// The bootstrapper for the TSP module.
    /// </summary>
    public class TSPBootstrapper
    {
        /// <summary>
        /// Holds the service instances.
        /// </summary>
        private static Dictionary<string, ITSPModuleInstance> _instances =
            new Dictionary<string, ITSPModuleInstance>();

        /// <summary>
        /// Returns true if the given instance is active.
        /// </summary>
        public static bool IsActive(string name)
        {
            return _instances.ContainsKey(name);
        }

        /// <summary>
        /// Returns the routing module instance with the given name.
        /// </summary>
        public static ITSPModuleInstance Get(string name)
        {
            return _instances[name];
        }

        /// <summary>
        /// Registers a new instance.
        /// </summary>
        public static void Register(string name, ITSPModuleInstance instance)
        {
            _instances[name] = instance;
        }
    }
}