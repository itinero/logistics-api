﻿// OsmSharp - OpenStreetMap (OSM) SDK
// Copyright (C) 2015 Abelshausen Ben
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

using System.Configuration;

namespace OsmSharp.Routing.API.Configurations
{
    /// <summary>
    /// Represents a configuration for one routing instance inside an existing API.
    /// </summary>
    public class InstanceConfiguration : ConfigurationElement
    {
        /// <summary>
        /// Returns the file to load the routing data from.
        /// </summary>
        [ConfigurationProperty("routerdb", IsRequired = false)]
        public string RouterDb
        {
            get { return this["routerdb"] as string; }
        }

        /// <summary>
        /// Returns the name of this instance.
        /// </summary>
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return this["name"] as string; }
        }
    }
}